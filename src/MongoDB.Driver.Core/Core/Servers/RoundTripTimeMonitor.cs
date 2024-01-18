/* Copyright 2020-present MongoDB Inc.
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
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    internal interface IRoundTripTimeMonitor : IDisposable
    {
        TimeSpan Average { get; }
        bool IsStarted { get; }
        void AddSample(TimeSpan roundTripTime);
        void Reset();
        void Start();
    }

    internal sealed class RoundTripTimeMonitor : IRoundTripTimeMonitor
    {
        private readonly ExponentiallyWeightedMovingAverage _averageRoundTripTimeCalculator = new ExponentiallyWeightedMovingAverage(0.2);
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IConnectionFactory _connectionFactory;
        private bool _disposed;
        private readonly EndPoint _endPoint;
        private readonly TimeSpan _heartbeatInterval;
        private readonly object _lock = new object();
        private IConnection _roundTripTimeConnection;
        private Thread _roundTripTimeMonitorThread;
        private readonly ServerApi _serverApi;
        private readonly ServerId _serverId;
        private readonly ILogger<RoundTripTimeMonitor> _logger;

        public RoundTripTimeMonitor(
            IConnectionFactory connectionFactory,
            ServerId serverId,
            EndPoint endpoint,
            TimeSpan heartbeatInterval,
            ServerApi serverApi,
            ILogger<RoundTripTimeMonitor> logger)
        {
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endpoint, nameof(endpoint));
            _heartbeatInterval = heartbeatInterval;
            _serverApi = serverApi;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            _logger = logger;
        }

        public TimeSpan Average
        {
            get
            {
                lock (_lock)
                {
                    return _averageRoundTripTimeCalculator.Average;
                }
            }
        }

        public bool IsStarted => _roundTripTimeMonitorThread != null;

        // public methods
        public void Dispose()
        {
            if (!_disposed)
            {
                _logger?.LogDebug(_serverId, "Disposing");

                _disposed = true;
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();

                try { _roundTripTimeConnection?.Dispose(); } catch { }

                _logger?.LogDebug(_serverId, "Disposed");
            }
        }

        public void Start()
        {
            _roundTripTimeMonitorThread = new Thread(ThreadStart) { IsBackground = true };
            _roundTripTimeMonitorThread.Start();

            void ThreadStart()
            {
                try
                {
                    MonitorServer();
                }
                catch (OperationCanceledException)
                {
                    // ignore OperationCanceledException
                }
            }
        }

        // private methods
        private void MonitorServer()
        {
            _logger?.LogDebug(_serverId, "Monitoring started");

            var helloOk = false;
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_roundTripTimeConnection == null)
                    {
                        InitializeConnection(); // sets _roundTripTimeConnection
                    }
                    else
                    {
                        var helloCommand = HelloHelper.CreateCommand(_serverApi, helloOk, loadBalanced: _roundTripTimeConnection.Settings.LoadBalanced);
                        var helloProtocol = HelloHelper.CreateProtocol(helloCommand, _serverApi);

                        var stopwatch = Stopwatch.StartNew();
                        var helloResult = HelloHelper.GetResult(_roundTripTimeConnection, helloProtocol, _cancellationToken);
                        stopwatch.Stop();
                        AddSample(stopwatch.Elapsed);
                        helloOk = helloResult.HelloOk;
                    }
                }
                catch (Exception ex)
                {
                    IConnection toDispose;
                    lock (_lock)
                    {
                        toDispose = _roundTripTimeConnection;
                        _roundTripTimeConnection = null;
                    }
                    var connectionId = toDispose?.ConnectionId;
                    toDispose?.Dispose();

                    _logger?.LogDebug(ex, StructuredLogTemplateProviders.DriverConnectionId_Message, connectionId?.LongLocalValue, "Monitoring exception");
                }
                ThreadHelper.Sleep(_heartbeatInterval, _cancellationToken);
            }
        }

        private void InitializeConnection()
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var roundTripTimeConnection = _connectionFactory.CreateConnection(_serverId, _endPoint);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // if we are cancelling, it's because the server has
                // been shut down and we really don't need to wait.
                roundTripTimeConnection.Open(_cancellationToken);
                _cancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                // dispose it here because the _connection is not initialized yet
                try { roundTripTimeConnection.Dispose(); } catch { }
                throw;
            }
            stopwatch.Stop();

            lock (_lock)
            {
                _roundTripTimeConnection = roundTripTimeConnection;
            }
            AddSample(stopwatch.Elapsed);
        }

        public void AddSample(TimeSpan roundTripTime)
        {
            lock (_lock)
            {
                _averageRoundTripTimeCalculator.AddSample(roundTripTime);
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _averageRoundTripTimeCalculator.Reset();
            }
        }
    }
}
