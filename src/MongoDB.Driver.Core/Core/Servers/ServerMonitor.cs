/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
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
        private BuildInfoResult _handshakeBuildInfoResult;
        private HeartbeatDelay _heartbeatDelay;
        private readonly object _lock = new object();
        private readonly CancellationTokenSource _monitorCancellationTokenSource; // used to cancel the entire monitor
        private readonly IRoundTripTimeMonitor _roundTripTimeMonitor;
        private readonly ServerId _serverId;
        private readonly InterlockedInt32 _state;
        private readonly ServerMonitorSettings _serverMonitorSettings;

        private readonly Action<ServerHeartbeatStartedEvent> _heartbeatStartedEventHandler;
        private readonly Action<ServerHeartbeatSucceededEvent> _heartbeatSucceededEventHandler;
        private readonly Action<ServerHeartbeatFailedEvent> _heartbeatFailedEventHandler;
        private readonly Action<SdamInformationEvent> _sdamInformationEventHandler;

        public event EventHandler<ServerDescriptionChangedEventArgs> DescriptionChanged;

        public ServerMonitor(ServerId serverId, EndPoint endPoint, IConnectionFactory connectionFactory, ServerMonitorSettings serverMonitorSettings, IEventSubscriber eventSubscriber)
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
                    Ensure.IsNotNull(serverMonitorSettings, nameof(serverMonitorSettings)).HeartbeatInterval))
        {
        }

        public ServerMonitor(ServerId serverId, EndPoint endPoint, IConnectionFactory connectionFactory, ServerMonitorSettings serverMonitorSettings, IEventSubscriber eventSubscriber, IRoundTripTimeMonitor roundTripTimeMonitor)
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
            eventSubscriber.TryGetEventHandler(out _heartbeatStartedEventHandler);
            eventSubscriber.TryGetEventHandler(out _heartbeatSucceededEventHandler);
            eventSubscriber.TryGetEventHandler(out _heartbeatFailedEventHandler);
            eventSubscriber.TryGetEventHandler(out _sdamInformationEventHandler);

            _heartbeatCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_monitorCancellationTokenSource.Token);
        }

        public ServerDescription Description => Interlocked.CompareExchange(ref _currentDescription, null, null);

        public object Lock => _lock;

        // public methods
        public void CancelCurrentCheck()
        {
            IConnection toDispose = null;
            lock (_lock)
            {
                if (!_heartbeatCancellationTokenSource.IsCancellationRequested)
                {
                    _heartbeatCancellationTokenSource.Cancel();
                    _heartbeatCancellationTokenSource.Dispose();
                    _heartbeatCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_monitorCancellationTokenSource.Token);
                    // the previous isMaster cancelation token is still cancelled

                    toDispose = _connection;
                    _connection = null;
                }
            }
            toDispose.Dispose();
        }

        public void Dispose()
        {
            if (_state.TryChange(State.Disposed))
            {
                _monitorCancellationTokenSource.Cancel();
                _monitorCancellationTokenSource.Dispose();
                if (_connection != null)
                {
                    _connection.Dispose();
                }
                _roundTripTimeMonitor.Dispose();
            }
        }

        public void Initialize()
        {
            if (_state.TryChange(State.Initial, State.Open))
            {
                // the call to Task.Factory.StartNew is not normally recommended or necessary
                // we are using it temporarily to work around a race condition in some of our tests
                // the issue is that we set up some mocked async methods to return results immediately synchronously
                // which results in the MonitorServerAsync method making more progress synchronously than the test expected
                // by using Task.Factory.StartNew we introduce a short delay before the MonitorServerAsync Task starts executing
                // the delay is whatever time it takes for the new Task to be activated and scheduled
                // and the delay is usually long enough for the test to get past the race condition (though not guaranteed)
                _ = Task.Factory.StartNew(() => _ = MonitorServerAsync().ConfigureAwait(false)).ConfigureAwait(false);
                _ = _roundTripTimeMonitor.RunAsync().ConfigureAwait(false);
            }
        }

        public void RequestHeartbeat()
        {
            ThrowIfNotOpen();
            lock (_lock)
            {
                _heartbeatDelay?.RequestHeartbeat();
            }
        }

        // private methods
        private CommandWireProtocol<BsonDocument> InitializeIsMasterProtocol(IConnection connection)
        {
            BsonDocument isMasterCommand;
            var commandResponseHandling = CommandResponseHandling.Return;
            if (connection.Description.IsMasterResult.TopologyVersion != null)
            {
                connection.SetReadTimeout(_serverMonitorSettings.ConnectTimeout + _serverMonitorSettings.HeartbeatInterval);
                commandResponseHandling = CommandResponseHandling.ExhaustAllowed;

                var veryLargeHeartbeatInterval = TimeSpan.FromDays(1); // the server doesn't support Infinite value, so we set just a big enough value
                var maxAwaitTime = _serverMonitorSettings.HeartbeatInterval == Timeout.InfiniteTimeSpan ? veryLargeHeartbeatInterval : _serverMonitorSettings.HeartbeatInterval;
                isMasterCommand = IsMasterHelper.CreateCommand(connection.Description.IsMasterResult.TopologyVersion, maxAwaitTime);
            }
            else
            {
                isMasterCommand = IsMasterHelper.CreateCommand();
            }

            return IsMasterHelper.CreateProtocol(isMasterCommand, commandResponseHandling);
        }

        private async Task<IConnection> InitializeConnectionAsync(CancellationToken cancellationToken) // called setUpConnection in spec
        {
            var connection = _connectionFactory.CreateConnection(_serverId, _endPoint);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // if we are cancelling, it's because the server has
                // been shut down and we really don't need to wait.
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // dispose it here because the _connection is not initialized yet
                try { connection.Dispose(); } catch { }
                throw;
            }
            stopwatch.Stop();

            _roundTripTimeMonitor.AddSample(stopwatch.Elapsed);
            return connection;
        }

        private async Task MonitorServerAsync()
        {
            var metronome = new Metronome(_serverMonitorSettings.HeartbeatInterval);
            var monitorCancellationToken = _monitorCancellationTokenSource.Token;

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
                        await HeartbeatAsync(cachedHeartbeatCancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cachedHeartbeatCancellationToken.IsCancellationRequested)
                    {
                        // ignore OperationCanceledException when heartbeat cancellation is requested
                    }
                    catch (Exception unexpectedException)
                    {
                        // if we catch an exception here it's because of a bug in the driver (but we need to defend ourselves against that)

                        var handler = _sdamInformationEventHandler;
                        if (handler != null)
                        {
                            try
                            {
                                handler.Invoke(new SdamInformationEvent(() =>
                                    string.Format(
                                        "Unexpected exception in ServerMonitor.MonitorServerAsync: {0}",
                                        unexpectedException.ToString())));
                            }
                            catch
                            {
                                // ignore any exceptions thrown by the handler (note: event handlers aren't supposed to throw exceptions)
                            }
                        }

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
                        if (_heartbeatDelay != null)
                        {
                            _heartbeatDelay.Dispose();
                        }
                        _heartbeatDelay = newHeartbeatDelay;
                    }
                    await newHeartbeatDelay.Task.ConfigureAwait(false); // corresponds to wait method in spec
                }
                catch
                {
                    // ignore these exceptions
                }
            }
        }

        private async Task HeartbeatAsync(CancellationToken cancellationToken)
        {
            CommandWireProtocol<BsonDocument> isMasterProtocol = null;

            bool processAnother = true;
            while (processAnother && !cancellationToken.IsCancellationRequested)
            {
                IsMasterResult heartbeatIsMasterResult = null;
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
                        var initializedConnection = await InitializeConnectionAsync(cancellationToken).ConfigureAwait(false);
                        lock (_lock)
                        {
                            if (_state.Value == State.Disposed)
                            {
                                try { initializedConnection.Dispose(); } catch { }
                                throw new OperationCanceledException("The ServerMonitor has been disposed.");
                            }
                            _connection = initializedConnection;
                            _handshakeBuildInfoResult = _connection.Description.BuildInfoResult;
                            heartbeatIsMasterResult = _connection.Description.IsMasterResult;
                        }
                    }
                    else
                    {
                        isMasterProtocol = isMasterProtocol ?? InitializeIsMasterProtocol(connection);
                        heartbeatIsMasterResult = await GetIsMasterResultAsync(connection, isMasterProtocol, cancellationToken).ConfigureAwait(false);
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
                        isMasterProtocol = null;

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
                if (heartbeatIsMasterResult != null)
                {
                    if (_handshakeBuildInfoResult == null)
                    {
                        // we can be here only if there is a bug in the driver
                        throw new ArgumentNullException("BuildInfo has been lost.");
                    }

                    var averageRoundTripTime = _roundTripTimeMonitor.Average;
                    var averageRoundTripTimeRounded = TimeSpan.FromMilliseconds(Math.Round(averageRoundTripTime.TotalMilliseconds));

                    newDescription = _baseDescription.With(
                        averageRoundTripTime: averageRoundTripTimeRounded,
                        canonicalEndPoint: heartbeatIsMasterResult.Me,
                        electionId: heartbeatIsMasterResult.ElectionId,
                        lastWriteTimestamp: heartbeatIsMasterResult.LastWriteTimestamp,
                        logicalSessionTimeout: heartbeatIsMasterResult.LogicalSessionTimeout,
                        maxBatchCount: heartbeatIsMasterResult.MaxBatchCount,
                        maxDocumentSize: heartbeatIsMasterResult.MaxDocumentSize,
                        maxMessageSize: heartbeatIsMasterResult.MaxMessageSize,
                        replicaSetConfig: heartbeatIsMasterResult.GetReplicaSetConfig(),
                        state: ServerState.Connected,
                        tags: heartbeatIsMasterResult.Tags,
                        topologyVersion: heartbeatIsMasterResult.TopologyVersion,
                        type: heartbeatIsMasterResult.ServerType,
                        version: _handshakeBuildInfoResult.ServerVersion,
                        wireVersionRange: new Range<int>(heartbeatIsMasterResult.MinWireVersion, heartbeatIsMasterResult.MaxWireVersion));
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

                processAnother =
                    // serverSupportsStreaming
                    (newDescription.Type != ServerType.Unknown && heartbeatIsMasterResult != null && heartbeatIsMasterResult.TopologyVersion != null) ||
                    // connectionIsStreaming
                    (isMasterProtocol != null && isMasterProtocol.MoreToCome) ||
                    // transitionedWithNetworkError
                    (IsNetworkError(heartbeatException) && previousDescription.Type != ServerType.Unknown);
            }

            bool IsNetworkError(Exception ex)
            {
                return ex is MongoConnectionException mongoConnectionException && mongoConnectionException.IsNetworkException;
            }
        }

        private async Task<IsMasterResult> GetIsMasterResultAsync(
            IConnection connection,
            CommandWireProtocol<BsonDocument> isMasterProtocol,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_heartbeatStartedEventHandler != null)
            {
                _heartbeatStartedEventHandler(new ServerHeartbeatStartedEvent(connection.ConnectionId, connection.Description.IsMasterResult.TopologyVersion != null));
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var isMasterResult = await IsMasterHelper.GetResultAsync(connection, isMasterProtocol, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                if (_heartbeatSucceededEventHandler != null)
                {
                    _heartbeatSucceededEventHandler(new ServerHeartbeatSucceededEvent(connection.ConnectionId, stopwatch.Elapsed, connection.Description.IsMasterResult.TopologyVersion != null));
                }

                return isMasterResult;
            }
            catch (Exception ex)
            {
                if (_heartbeatFailedEventHandler != null)
                {
                    _heartbeatFailedEventHandler(new ServerHeartbeatFailedEvent(connection.ConnectionId, ex, connection.Description.IsMasterResult.TopologyVersion != null));
                }
                throw;
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
                throw new InvalidOperationException("Server monitor must be initialized.");
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
