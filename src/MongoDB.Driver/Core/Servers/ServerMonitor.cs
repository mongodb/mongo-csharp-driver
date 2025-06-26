/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Servers
{
    internal sealed class ServerMonitor : IServerMonitor
    {
        private readonly ServerDescription _baseDescription;
        private volatile IConnection _connection;
        private readonly IConnectionFactory _connectionFactory;
        private CancellationTokenSource _heartbeatCancellationTokenSource; // used to cancel an ongoing heartbeat
        private ServerDescription _currentDescription;
        private readonly EndPoint _endPoint;
        private HeartbeatDelay _heartbeatDelay;
        private readonly bool _isStreamingEnabled;
        private readonly object _lock = new object();
        private readonly IEnvironmentVariableProvider _environmentVariableProvider;
        private readonly EventLogger<LogCategories.SDAM> _eventLoggerSdam;
        private readonly ILogger<IServerMonitor> _logger;
        private readonly CancellationToken _monitorCancellationToken; // used to cancel the entire monitor
        private readonly CancellationTokenSource _monitorCancellationTokenSource; // used to cancel the entire monitor
        private readonly IRoundTripTimeMonitor _roundTripTimeMonitor;
        private readonly ServerApi _serverApi;
        private readonly ServerId _serverId;
        private readonly InterlockedInt32 _state;
        private readonly ServerMonitorSettings _serverMonitorSettings;

        private Thread _serverMonitorThread;

        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        public ServerMonitor(
            ServerId serverId,
            EndPoint endPoint,
            IConnectionFactory connectionFactory,
            ServerMonitorSettings serverMonitorSettings,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi,
            ILoggerFactory loggerFactory,
            IEnvironmentVariableProvider environmentVariableProvider = null)
            : this(
                serverId,
                endPoint,
                connectionFactory,
                serverMonitorSettings,
                eventSubscriber,
                roundTripTimeMonitor: new RoundTripTimeMonitor(
                    connectionFactory,
                    serverId,
                    endPoint,
                    Ensure.IsNotNull(serverMonitorSettings, nameof(serverMonitorSettings)).HeartbeatInterval,
                    serverApi,
                    loggerFactory?.CreateLogger<RoundTripTimeMonitor>()),
                serverApi,
                loggerFactory,
                environmentVariableProvider)
        {
        }

        public ServerMonitor(
            ServerId serverId,
            EndPoint endPoint,
            IConnectionFactory connectionFactory,
            ServerMonitorSettings serverMonitorSettings,
            IEventSubscriber eventSubscriber,
            IRoundTripTimeMonitor roundTripTimeMonitor,
            ServerApi serverApi,
            ILoggerFactory loggerFactory,
            IEnvironmentVariableProvider environmentVariableProvider = null)
        {
            _monitorCancellationTokenSource = new CancellationTokenSource();
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _endPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _serverMonitorSettings = Ensure.IsNotNull(serverMonitorSettings, nameof(serverMonitorSettings));

            _baseDescription = _currentDescription = new ServerDescription(_serverId, endPoint, reasonChanged: "InitialDescription", heartbeatInterval: serverMonitorSettings.HeartbeatInterval);
            _roundTripTimeMonitor = Ensure.IsNotNull(roundTripTimeMonitor, nameof(roundTripTimeMonitor));

            _state = new InterlockedInt32(State.Initial);
            _serverApi = serverApi;

            _monitorCancellationToken = _monitorCancellationTokenSource.Token;
            _heartbeatCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_monitorCancellationToken);

            _logger = loggerFactory?.CreateLogger<IServerMonitor>();
            _eventLoggerSdam = loggerFactory.CreateEventLogger<LogCategories.SDAM>(eventSubscriber);

            _environmentVariableProvider = environmentVariableProvider ?? EnvironmentVariableProvider.Instance;

            _isStreamingEnabled = serverMonitorSettings.ServerMonitoringMode switch
            {
                ServerMonitoringMode.Stream => true,
                ServerMonitoringMode.Poll => false,
                _ => !IsRunningInFaaS() // ServerMonitoringMode.Auto
            };
        }

        public ServerDescription Description => Interlocked.CompareExchange(ref _currentDescription, null, null);

        public object Lock => _lock;

        // public methods
        public void CancelCurrentCheck()
        {
            if (IsDisposed())
            {
                return;
            }

            IConnection toDispose = null;
            lock (_lock)
            {
                if (!_heartbeatCancellationTokenSource.IsCancellationRequested)
                {
                    // _heartbeatCancellationTokenSource instance might have been disposed in Dispose at this point
                    TryCancelAndDispose(_heartbeatCancellationTokenSource);

                    if (IsDisposed()) // Skip creating new CancellationTokenSource if disposed
                    {
                        return;
                    }

                    _heartbeatCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_monitorCancellationToken);
                    // the previous hello or legacy hello cancellation token is still cancelled

                    toDispose = _connection;
                    _connection = null;

                    _logger?.LogDebug(_serverId, "Heartbeat cancellation check requested");
                }
            }
            toDispose?.Dispose();
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                _logger?.LogDebug(_serverId, "Disposing");

                _monitorCancellationTokenSource.Cancel();
                _monitorCancellationTokenSource.Dispose();

                _connection?.Dispose();
                _roundTripTimeMonitor.Dispose();
                _heartbeatDelay?.Dispose();

                // _heartbeatCancellationTokenSource instance might have been disposed in CancelCurrentCheck at this point.
                TryCancelAndDispose(_heartbeatCancellationTokenSource);

                _logger?.LogDebug(_serverId, "Disposed");
            }
        }

        public void Initialize()
        {
            if (_state.TryChange(State.Initial, State.Open))
            {
                _logger?.LogDebug(_serverId, "Initializing");

                _serverMonitorThread = new Thread(new ParameterizedThreadStart(ThreadStart)) { IsBackground = true };
                _serverMonitorThread.Start(_monitorCancellationToken);

                _logger?.LogDebug(_serverId, "Initialized");
            }

            void ThreadStart(object monitorCancellationToken)
            {
                MonitorServer((CancellationToken)monitorCancellationToken);
            }
        }

        public void RequestHeartbeat()
        {
            ThrowIfNotOpen();

            // CSHARP-3302: Accessing _heartbeatDelay inside _lock can lead to deadlock when processing concurrent heartbeats from old and new primaries.
            // Accessing _heartbeatDelay outside of _lock avoids the deadlock and will at worst reference the previous delay
            _heartbeatDelay?.RequestHeartbeat();
        }

        // private methods
        private IConnection InitializeConnection(CancellationToken cancellationToken) // called setUpConnection in spec
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint);
            _eventLoggerSdam.LogAndPublish(new ServerHeartbeatStartedEvent(connection.ConnectionId, false));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // if we are cancelling, it's because the server has
                // been shut down and we really don't need to wait.
                // TODO: CSOT: Implement operation context support for Server Discovery and Monitoring
                connection.Open(new OperationContext(Timeout.InfiniteTimeSpan, cancellationToken));

                _eventLoggerSdam.LogAndPublish(new ServerHeartbeatSucceededEvent(connection.ConnectionId, stopwatch.Elapsed, false, connection.Description.HelloResult.Wrapped));
            }
            catch (Exception exception)
            {
                // dispose it here because the _connection is not initialized yet
                try { connection.Dispose(); } catch { }

                _eventLoggerSdam.LogAndPublish(new ServerHeartbeatFailedEvent(connection.ConnectionId, stopwatch.Elapsed, exception, false));

                throw;
            }
            stopwatch.Stop();

            _roundTripTimeMonitor.AddSample(stopwatch.Elapsed);
            return connection;
        }

        private CommandWireProtocol<BsonDocument> InitializeHelloProtocol(IConnection connection, bool helloOk)
        {
            BsonDocument helloCommand;
            var commandResponseHandling = CommandResponseHandling.Return;
            if (IsUsingStreamingProtocol(connection.Description.HelloResult))
            {
                connection.SetReadTimeout(_serverMonitorSettings.ConnectTimeout + _serverMonitorSettings.HeartbeatInterval);
                commandResponseHandling = CommandResponseHandling.ExhaustAllowed;

                var veryLargeHeartbeatInterval = TimeSpan.FromDays(1); // the server doesn't support Infinite value, so we set just a big enough value
                var maxAwaitTime = _serverMonitorSettings.HeartbeatInterval == Timeout.InfiniteTimeSpan ? veryLargeHeartbeatInterval : _serverMonitorSettings.HeartbeatInterval;
                helloCommand = HelloHelper.CreateCommand(_serverApi, helloOk, connection.Description.HelloResult.TopologyVersion, maxAwaitTime, connection.Settings.LoadBalanced);
            }
            else
            {
                helloCommand = HelloHelper.CreateCommand(_serverApi, helloOk, loadBalanced: connection.Settings.LoadBalanced);
            }

            return HelloHelper.CreateProtocol(helloCommand, _serverApi, commandResponseHandling);
        }

        private bool IsDisposed() => _state.Value == State.Disposed;

        private bool IsRunningInFaaS()
        {
            /* FaaS providers for each environment variable
             * aws.lambda: AWS_EXECUTION_ENV, AWS_LAMBDA_RUNTIME_API
             * azure.func: FUNCTIONS_WORKER_RUNTIME
             * gcp.func: K_SERVICE, FUNCTION_NAME
             * vercel: VERCEL
             */
            return (_environmentVariableProvider.GetEnvironmentVariable("AWS_EXECUTION_ENV")?.StartsWith("AWS_Lambda_") ?? false) ||
                   _environmentVariableProvider.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API") != null ||
                   _environmentVariableProvider.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") != null ||
                   _environmentVariableProvider.GetEnvironmentVariable("K_SERVICE") != null ||
                   _environmentVariableProvider.GetEnvironmentVariable("FUNCTION_NAME") != null ||
                   _environmentVariableProvider.GetEnvironmentVariable("VERCEL") != null;
        }

        private bool IsUsingStreamingProtocol(HelloResult helloResult) => _isStreamingEnabled && helloResult?.TopologyVersion != null;

        private HelloResult GetHelloResult(
            IConnection connection,
            CommandWireProtocol<BsonDocument> helloProtocol,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _eventLoggerSdam.LogAndPublish(new ServerHeartbeatStartedEvent(connection.ConnectionId, IsUsingStreamingProtocol(connection.Description.HelloResult)));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var helloResult = HelloHelper.GetResult(connection, helloProtocol, cancellationToken);
                stopwatch.Stop();

                // RTT check if using polling monitoring
                if (!IsUsingStreamingProtocol(connection.Description.HelloResult))
                {
                    _roundTripTimeMonitor.AddSample(stopwatch.Elapsed);
                }

                _eventLoggerSdam.LogAndPublish(new ServerHeartbeatSucceededEvent(connection.ConnectionId, stopwatch.Elapsed, IsUsingStreamingProtocol(connection.Description.HelloResult), helloResult.Wrapped));

                return helloResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _eventLoggerSdam.LogAndPublish(new ServerHeartbeatFailedEvent(connection.ConnectionId, stopwatch.Elapsed, ex, IsUsingStreamingProtocol(connection.Description.HelloResult)));

                throw;
            }
        }

        private void Heartbeat(CancellationToken cancellationToken)
        {
            CommandWireProtocol<BsonDocument> helloProtocol = null;
            bool processAnother = true;
            while (processAnother && !cancellationToken.IsCancellationRequested)
            {
                HelloResult heartbeatHelloResult = null;
                Exception heartbeatException = null;
                var previousDescription = _currentDescription;

                try
                {
                    IConnection connection;
                    lock (_lock)
                    {
                        connection = _connection;
                    }
                    if (connection == null)
                    {
                        var initializedConnection = InitializeConnection(cancellationToken);
                        lock (_lock)
                        {
                            if (_state.Value == State.Disposed)
                            {
                                try { initializedConnection.Dispose(); } catch { }
                                throw new OperationCanceledException("The ServerMonitor has been disposed.");
                            }
                            _connection = initializedConnection;
                            heartbeatHelloResult = _connection.Description.HelloResult;
                        }
                    }
                    else
                    {
                        // If MoreToCome is true, that means we are streaming hello or legacy hello results and must
                        // continue using the existing helloProtocol object.
                        // Otherwise helloProtocol has either not been initialized or we may need to switch between
                        // heartbeat commands based on the last heartbeat response.
                        if (helloProtocol == null || helloProtocol.MoreToCome == false)
                        {
                            helloProtocol = InitializeHelloProtocol(connection, previousDescription?.HelloOk ?? false);
                        }
                        heartbeatHelloResult = GetHelloResult(connection, helloProtocol, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    IConnection toDispose = null;

                    lock (_lock)
                    {
                        helloProtocol = null;

                        heartbeatException = ex;
                        _roundTripTimeMonitor.Reset();

                        toDispose = _connection;
                        _connection = null;
                    }
                    toDispose?.Dispose();
                }

                lock (_lock)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }

                ServerDescription newDescription;
                if (heartbeatHelloResult != null)
                {
                    newDescription = _baseDescription.With(
                        averageRoundTripTime: _roundTripTimeMonitor.Average,
                        canonicalEndPoint: heartbeatHelloResult.Me,
                        electionId: heartbeatHelloResult.ElectionId,
                        helloOk: heartbeatHelloResult.HelloOk,
                        lastWriteTimestamp: heartbeatHelloResult.LastWriteTimestamp,
                        logicalSessionTimeout: heartbeatHelloResult.LogicalSessionTimeout,
                        maxBatchCount: heartbeatHelloResult.MaxBatchCount,
                        maxDocumentSize: heartbeatHelloResult.MaxDocumentSize,
                        maxMessageSize: heartbeatHelloResult.MaxMessageSize,
                        replicaSetConfig: heartbeatHelloResult.GetReplicaSetConfig(),
                        state: ServerState.Connected,
                        tags: heartbeatHelloResult.Tags,
                        topologyVersion: heartbeatHelloResult.TopologyVersion,
                        type: heartbeatHelloResult.ServerType,
                        version: WireVersion.ToServerVersion(heartbeatHelloResult.MaxWireVersion),
                        wireVersionRange: new Range<int>(heartbeatHelloResult.MinWireVersion, heartbeatHelloResult.MaxWireVersion));
                }
                else
                {
                    newDescription = _baseDescription.With(lastUpdateTimestamp: DateTime.UtcNow);
                }

                if (heartbeatException != null)
                {
                    var topologyVersion = default(Optional<TopologyVersion>);
                    if (heartbeatException is MongoCommandException heartbeatCommandException)
                    {
                        topologyVersion = TopologyVersion.FromMongoCommandException(heartbeatCommandException);
                    }
                    newDescription = newDescription.With(heartbeatException: heartbeatException, topologyVersion: topologyVersion);
                }

                newDescription = newDescription.With(reasonChanged: "Heartbeat", lastHeartbeatTimestamp: DateTime.UtcNow);

                lock (_lock)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SetDescription(newDescription);
                }

                var serverSupportsStreaming = newDescription.Type != ServerType.Unknown &&
                    heartbeatHelloResult != null &&
                    heartbeatHelloResult.TopologyVersion != null;

                var connectionIsStreaming = helloProtocol != null && helloProtocol.MoreToCome;
                var transitionedWithNetworkError =
                    IsNetworkError(heartbeatException) && previousDescription.Type != ServerType.Unknown;

                if (_isStreamingEnabled && serverSupportsStreaming && !_roundTripTimeMonitor.IsStarted)
                {
                    // we are using streaming protocol and should start RTT monitoring on a separate thread as per the specs
                    _roundTripTimeMonitor.Start();
                }

                processAnother = (_isStreamingEnabled && (serverSupportsStreaming || connectionIsStreaming)) || transitionedWithNetworkError;
            }

            bool IsNetworkError(Exception ex)
            {
                return ex is MongoConnectionException mongoConnectionException && mongoConnectionException.IsNetworkException;
            }
        }

        private void MonitorServer(CancellationToken monitorCancellationToken)
        {
            var metronome = new Metronome(_serverMonitorSettings.HeartbeatInterval);

            while (!monitorCancellationToken.IsCancellationRequested)
            {
                try
                {
                    CancellationToken cachedHeartbeatCancellationToken;
                    lock (_lock)
                    {
                        cachedHeartbeatCancellationToken = _heartbeatCancellationTokenSource.Token; // we want to cache the current cancellation token in case the source changes
                    }

                    try
                    {
                        Heartbeat(cachedHeartbeatCancellationToken);
                    }
                    catch (OperationCanceledException) when (cachedHeartbeatCancellationToken.IsCancellationRequested)
                    {
                        // ignore OperationCanceledException when heartbeat cancellation is requested
                    }
                    catch (Exception unexpectedException)
                    {
                        // if we catch an exception here it's because of a bug in the driver (but we need to defend ourselves against that)
                        _eventLoggerSdam.LogAndPublish(
                            unexpectedException,
                            new SdamInformationEvent(
                                "Unexpected exception in ServerMonitor.MonitorServer: {0}",
                                unexpectedException));

                        // since an unexpected exception was thrown set the server description to Unknown (with the unexpected exception)
                        try
                        {
                            // keep this code as simple as possible to keep the surface area with any remaining possible bugs as small as possible
                            var newDescription = _baseDescription.WithHeartbeatException(unexpectedException); // not With in case the bug is in With
                            SetDescription(newDescription); // not SetDescriptionIfChanged in case the bug is in SetDescriptionIfChanged
                        }
                        catch
                        {
                            // if even the simple code in the try throws just give up (at least we've raised the unexpected exception via an SdamInformationEvent)
                        }
                    }

                    HeartbeatDelay newHeartbeatDelay;
                    lock (_lock)
                    {
                        newHeartbeatDelay = new HeartbeatDelay(metronome.GetNextTickDelay(), _serverMonitorSettings.MinHeartbeatInterval);

                        _heartbeatDelay?.Dispose();
                        _heartbeatDelay = newHeartbeatDelay;
                    }
                    newHeartbeatDelay.Wait(monitorCancellationToken); // corresponds to wait method in spec
                }
                catch
                {
                    // ignore these exceptions
                }
            }
        }

        private void OnDescriptionChanged(ServerDescription oldDescription, ServerDescription newDescription)
        {
            var handler = DescriptionChanged;
            if (handler != null)
            {
                var args = new ServerDescriptionChangedEventArgs(oldDescription, newDescription);
                try { handler(this, args); }
                catch { } // ignore exceptions
            }
        }

        private void SetDescription(ServerDescription newDescription)
        {
            var oldDescription = Interlocked.CompareExchange(ref _currentDescription, null, null);
            SetDescription(oldDescription, newDescription);
        }

        private void SetDescription(ServerDescription oldDescription, ServerDescription newDescription)
        {
            Interlocked.Exchange(ref _currentDescription, newDescription);
            OnDescriptionChanged(oldDescription, newDescription);
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed())
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfNotOpen()
        {
            if (_state.Value != State.Open)
            {
                ThrowIfDisposed();
                throw new InvalidOperationException("Server monitor must be initialized.");
            }
        }

        private void TryCancelAndDispose(CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore ObjectDisposedException
            }
        }

        // nested types
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}
