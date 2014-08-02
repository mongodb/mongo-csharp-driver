/* Copyright 2013-2014 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a server in a MongoDB cluster.
    /// </summary>
    internal sealed class Server : IClusterableServer
    {
        // fields
        private readonly ExponentiallyWeightedMovingAverage _averageRoundTripTimeCalculator = new ExponentiallyWeightedMovingAverage(0.2);
        private readonly CancellationTokenSource _backgroundTaskCancellationTokenSource = new CancellationTokenSource();
        private readonly ServerDescription _baseDescription;
        private readonly IConnectionPool _connectionPool;
        private ServerDescription _currentDescription;
        private TaskCompletionSource<bool> _descriptionChangedTaskCompletionSource = new TaskCompletionSource<bool>();
        private readonly EndPoint _endPoint;
        private readonly IConnectionFactory _heartbeatConnectionFactory;
        private IConnection _heartbeatConnection;
        private readonly IServerListener _listener;
        private readonly ServerId _serverId;
        private readonly ServerSettings _settings;
        private readonly InterlockedInt32 _state;

        // events
        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        // constructors
        public Server(ServerSettings settings, ClusterId clusterId, EndPoint endPoint, IConnectionPoolFactory connectionPoolFactory, IConnectionFactory hearbeatConnectionFactory, IServerListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings"); ;
            Ensure.IsNotNull(clusterId, "clusterId");
            _endPoint = Ensure.IsNotNull(endPoint, "endPoint");
            Ensure.IsNotNull(connectionPoolFactory, "connectionPoolFactory");
            _heartbeatConnectionFactory = Ensure.IsNotNull(hearbeatConnectionFactory, "hearbeatConnectionFactory");
            _listener = listener;

            _serverId = new ServerId(clusterId, endPoint);
            _baseDescription = _currentDescription = new ServerDescription(_serverId, endPoint);
            _connectionPool = connectionPoolFactory.CreateConnectionPool(_serverId, endPoint);
            _state = new InterlockedInt32(State.Initial);
        }

        // properties
        public ServerDescription Description
        {
            get
            {
                return Interlocked.CompareExchange(ref _currentDescription, null, null);
            }
        }

        public EndPoint EndPoint
        {
            get { return _endPoint; }
        }

        // methods
        public void Initialize()
        {
            if (_state.TryChange(State.Initial, State.Open))
            {
                _connectionPool.Initialize();
                AsyncBackgroundTask.Start(
                    HeartbeatBackgroundTask,
                    _settings.HeartbeatInterval,
                    _backgroundTaskCancellationTokenSource.Token)
                    .LogUnobservedExceptions();
            }
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                _backgroundTaskCancellationTokenSource.Cancel();
                _backgroundTaskCancellationTokenSource.Dispose();
                _connectionPool.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public async Task<IConnectionHandle> GetConnectionAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Ensure.IsInfiniteOrGreaterThanOrEqualToZero(timeout, "timeout");
            ThrowIfNotOpen();

            var slidingTimeout = new SlidingTimeout(timeout);
            var connection = await _connectionPool.AcquireConnectionAsync(slidingTimeout, cancellationToken);
            try
            {
                await connection.OpenAsync(slidingTimeout, cancellationToken);
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private async Task HeartbeatBackgroundTask(CancellationToken cancellationToken)
        {
            HeartbeatInfo heartbeatInfo = null;
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                if (_heartbeatConnection == null)
                {
                    _heartbeatConnection = _heartbeatConnectionFactory.CreateConnection(_serverId, _endPoint);
                    await _heartbeatConnection.OpenAsync(TimeSpan.FromMinutes(1), cancellationToken);
                }

                try
                {
                    heartbeatInfo = await GetHeartbeatInfoAsync(_heartbeatConnection, cancellationToken);
                    break;
                }
                catch
                {
                    if (attempt == 1)
                    {
                        _heartbeatConnection.Dispose();
                        _heartbeatConnection = null;
                    }
                }
            }

            ServerDescription newDescription;
            if (heartbeatInfo != null)
            {
                var averageRoundTripTime = _averageRoundTripTimeCalculator.AddSample(heartbeatInfo.RoundTripTime);
                var averageRoundTripTimeRounded = TimeSpan.FromMilliseconds(Math.Round(averageRoundTripTime.TotalMilliseconds));
                var isMasterResult = heartbeatInfo.IsMasterResult;
                var buildInfoResult = heartbeatInfo.BuildInfoResult;

                newDescription = _baseDescription.WithHeartbeatInfo(
                    averageRoundTripTimeRounded,
                    isMasterResult.GetReplicaSetConfig(_endPoint.AddressFamily),
                    isMasterResult.Tags,
                    isMasterResult.ServerType,
                    buildInfoResult.ServerVersion);
            }
            else
            {
                newDescription = _baseDescription;
            }

            OnDescriptionChanged(newDescription);
        }

        private async Task<HeartbeatInfo> GetHeartbeatInfoAsync(IConnection connection, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_listener != null)
            {
                var args = new SendingHeartbeatEventArgs(_endPoint);
                _listener.SendingHeartbeat(args);
            }

            try
            {
                var slidingTimeout = new SlidingTimeout(_settings.HeartbeatTimeout);

                var stopwatch = Stopwatch.StartNew();
                var isMasterCommand = new CommandWireProtocol(
                    "admin", 
                    new BsonDocument("isMaster", 1), 
                    true);

                var isMasterResult = new IsMasterResult(await isMasterCommand.ExecuteAsync(connection, slidingTimeout, cancellationToken));
                stopwatch.Stop();

                var buildInfoCommand = new CommandWireProtocol(
                    "admin", 
                    new BsonDocument("buildInfo", 1),
                    true);

                var buildInfoResult = new BuildInfoResult(await buildInfoCommand.ExecuteAsync(connection, slidingTimeout, cancellationToken));

                if (_listener != null)
                {
                    var args = new SentHeartbeatEventArgs(_endPoint, isMasterResult, buildInfoResult);
                    _listener.SentHeartbeat(args);
                }

                return new HeartbeatInfo 
                { 
                    RoundTripTime = stopwatch.Elapsed, 
                    IsMasterResult = isMasterResult, 
                    BuildInfoResult = buildInfoResult 
                };
            }
            catch (Exception exception)
            {
                if (_listener != null)
                {
                    var args = new SentHeartbeatEventArgs(_endPoint, exception);
                    _listener.SentHeartbeat(args);
                }
                throw;
            }
        }

        private void OnDescriptionChanged(ServerDescription newDescription)
        {
            var oldDescription = Interlocked.Exchange(ref _currentDescription, newDescription);
            if(oldDescription.Equals(newDescription))
            {
                return;
            }

            var args = new ServerDescriptionChangedEventArgs(oldDescription, newDescription);

            if (_listener != null)
            {
                _listener.ServerDescriptionChanged(args);
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                try { handler(this, args); }
                catch { } // ignore exceptions
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (_state.Value != State.Open)
            {
                ThrowIfDisposed();
                throw new InvalidOperationException("Server must be initialized.");
            }
        }

        // nested types
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }

        private class HeartbeatInfo
        {
            public TimeSpan RoundTripTime;
            public IsMasterResult IsMasterResult;
            public BuildInfoResult BuildInfoResult;
        }
    }
}
