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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents a server in a MongoDB cluster.
    /// </summary>
    internal sealed class Server : IClusterableServer
    {
        // fields
        private readonly ExponentiallyWeightedMovingAverage _averageRoundTripTimeCalculator = new ExponentiallyWeightedMovingAverage(0.2);
        private readonly ServerDescription _baseDescription;
        private IConnectionPool _connectionPool;
        private readonly IConnectionPoolFactory _connectionPoolFactory;
        private ServerDescription _currentDescription;
        private readonly EndPoint _endPoint;
        private readonly CancellationTokenSource _heartbeatCancellationTokenSource = new CancellationTokenSource();
        private readonly IConnectionFactory _heartbeatConnectionFactory;
        private IConnection _heartbeatConnection;
        private InterruptibleDelay _heartbeatDelay;
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
            _connectionPoolFactory = Ensure.IsNotNull(connectionPoolFactory, "connectionPoolFactory");
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
                var metronome = new Metronome(_settings.HeartbeatInterval);
                AsyncBackgroundTask.Start(
                    HeartbeatAsync,
                    ct =>
                    {
                        var newDelay = new InterruptibleDelay(metronome.GetNextTickDelay(), ct);
                        Interlocked.Exchange(ref _heartbeatDelay, newDelay);
                        return newDelay.Task;
                    },
                    _heartbeatCancellationTokenSource.Token)
                    .LogUnobservedExceptions();
            }
        }

        public void Invalidate()
        {
            ThrowIfNotOpen();
            Interlocked.CompareExchange(ref _heartbeatDelay, null, null).Interrupt();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    _heartbeatCancellationTokenSource.Cancel();
                    _heartbeatCancellationTokenSource.Dispose();
                    _connectionPool.Dispose();
                }
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
                return new ServerConnection(this, connection);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private async Task HeartbeatAsync(CancellationToken cancellationToken)
        {
            const int maxRetryCount = 2;
            HeartbeatInfo heartbeatInfo = null;
            for (var attempt = 1; attempt <= maxRetryCount; attempt++)
            {
                try
                {
                    if (_heartbeatConnection == null)
                    {
                        _heartbeatConnection = _heartbeatConnectionFactory.CreateConnection(_serverId, _endPoint);
                        await _heartbeatConnection.OpenAsync(TimeSpan.FromMinutes(1), cancellationToken);
                    }

                    heartbeatInfo = await GetHeartbeatInfoAsync(_heartbeatConnection, cancellationToken);
                    break;
                }
                catch
                {
                    _heartbeatConnection.Dispose();
                    _heartbeatConnection = null;

                    if (attempt == maxRetryCount)
                    {
                        _connectionPool.Clear();
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
                    isMasterResult.GetReplicaSetConfig(_settings.AddressFamily),
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

                var isMasterCommand = new CommandWireProtocol(
                    "admin",
                    new BsonDocument("isMaster", 1),
                    true);

                var stopwatch = Stopwatch.StartNew();
                var isMasterResultDocument = await isMasterCommand.ExecuteAsync(connection, slidingTimeout, cancellationToken);
                stopwatch.Stop();
                var isMasterResult = new IsMasterResult(isMasterResultDocument);

                var buildInfoCommand = new CommandWireProtocol(
                    "admin",
                    new BsonDocument("buildInfo", 1),
                    true);

                var buildInfoResultRocument = await buildInfoCommand.ExecuteAsync(connection, slidingTimeout, cancellationToken);
                var buildInfoResult = new BuildInfoResult(buildInfoResultRocument);

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
            var oldDescription = Interlocked.CompareExchange(ref _currentDescription, null, null);
            if (oldDescription.Equals(newDescription))
            {
                return;
            }
            newDescription = newDescription.WithRevision(oldDescription.Revision + 1);
            Interlocked.Exchange(ref _currentDescription, newDescription);

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

        private void HandleConnectionException(IConnection connection, Exception ex)
        {
            if (_state.Value == State.Open)
            {
                // For any connection exception, we are going to immediately
                // invalidate the server
                Invalidate();
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

        private sealed class ServerConnection : ConnectionWrapper, IConnectionHandle
        {
            private readonly Server _server;
            private readonly IConnectionHandle _wrappedHandle;
            
            public ServerConnection(Server server, IConnectionHandle wrapped)
                : base(wrapped)
            {
                _wrappedHandle = wrapped;
                _server = server;
            }

            public override async Task OpenAsync(TimeSpan timeout, CancellationToken cancellationToken)
            {
                try
                {
                    await base.OpenAsync(timeout, cancellationToken);
                }
                catch(Exception ex)
                {
                    _server.HandleConnectionException(this, ex);
                    throw;
                }
            }

            public override async Task<ReplyMessage<TDocument>> ReceiveMessageAsync<TDocument>(int responseTo, IBsonSerializer<TDocument> serializer, TimeSpan timeout, CancellationToken cancellationToken)
            {
                try
                {
                    return await base.ReceiveMessageAsync<TDocument>(responseTo, serializer, timeout, cancellationToken);
                }
                catch (Exception ex)
                {
                    _server.HandleConnectionException(this, ex);
                    throw;
                }
            }

            public override async Task SendMessagesAsync(IEnumerable<RequestMessage> messages, TimeSpan timeout, CancellationToken cancellationToken)
            {
                try
                {
                    await base.SendMessagesAsync(messages, timeout, cancellationToken);
                }
                catch (Exception ex)
                {
                    _server.HandleConnectionException(this, ex);
                    throw;
                }
            }

            public IConnectionHandle Fork()
            {
                return new ServerConnection(_server, _wrappedHandle.Fork());
            }
        }
    }
}