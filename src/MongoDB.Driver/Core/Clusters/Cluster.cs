/* Copyright 2013-present MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    internal abstract class Cluster : IClusterInternal
    {
        #region static

        private static readonly TimeSpan __minHeartbeatIntervalDefault = TimeSpan.FromMilliseconds(500);

        public static SemanticVersion MinSupportedServerVersion { get; } = WireVersion.ToServerVersion(WireVersion.SupportedWireVersionRange.Min);
        public static Range<int> SupportedWireVersionRange { get; } = WireVersion.SupportedWireVersionRange;

        #endregion

        private readonly TimeSpan _minHeartbeatInterval = __minHeartbeatIntervalDefault;
        private readonly IClusterClock _clusterClock = new ClusterClock();
        private readonly ClusterId _clusterId;
        private ExpirableClusterDescription _expirableClusterDescription;
        private readonly LatencyLimitingServerSelector _latencyLimitingServerSelector;
        protected readonly EventLogger<LogCategories.SDAM> _clusterEventLogger;
        protected readonly EventLogger<LogCategories.ServerSelection> _serverSelectionEventLogger;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ServerSelectionWaitQueue _serverSelectionWaitQueue;
        private readonly ICoreServerSessionPool _serverSessionPool;
        private readonly ClusterSettings _settings;
        private readonly InterlockedInt32 _state;

        // constructors
        protected Cluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IEventSubscriber eventSubscriber, ILoggerFactory loggerFactory)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            Ensure.That(!_settings.LoadBalanced, "LoadBalanced mode is not supported.");
            _serverFactory = Ensure.IsNotNull(serverFactory, nameof(serverFactory));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _state = new InterlockedInt32(State.Initial);
            _clusterId = new ClusterId();
            _expirableClusterDescription = new (this, ClusterDescription.CreateInitial(_clusterId, _settings.DirectConnection));
            _latencyLimitingServerSelector = new LatencyLimitingServerSelector(settings.LocalThreshold);
            _serverSelectionWaitQueue = new ServerSelectionWaitQueue(this);
            _serverSessionPool = new CoreServerSessionPool(this);
            _clusterEventLogger = loggerFactory.CreateEventLogger<LogCategories.SDAM>(eventSubscriber);
            _serverSelectionEventLogger = loggerFactory.CreateEventLogger<LogCategories.ServerSelection>(eventSubscriber);
        }

        // events
        public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        // properties
        public ClusterId ClusterId
        {
            get { return _clusterId; }
        }

        public ClusterDescription Description
        {
            get
            {
                return _expirableClusterDescription.ClusterDescription;
            }
        }

        public ClusterSettings Settings
        {
            get { return _settings; }
        }

        // methods
        public ICoreServerSession AcquireServerSession()
        {
            return _serverSessionPool.AcquireSession();
        }

        protected IClusterableServer CreateServer(EndPoint endPoint)
        {
            return _serverFactory.CreateServer(_settings.GetInitialClusterType(), _clusterId, _clusterClock, endPoint);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                _clusterEventLogger.Logger?.LogTrace(_clusterId, "Cluster disposing");

                var newClusterDescription = new ClusterDescription(
                    _clusterId,
                    _expirableClusterDescription.ClusterDescription.DirectConnection,
                    dnsMonitorException: null,
                    ClusterType.Unknown,
                    Enumerable.Empty<ServerDescription>());

                UpdateClusterDescription(newClusterDescription);

                _serverSelectionWaitQueue.Dispose();

                _clusterEventLogger.Logger?.LogTrace(_clusterId, "Cluster disposed");
            }
        }

        public virtual void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Initial, State.Open))
            {
                _clusterEventLogger.Logger?.LogTrace(_clusterId, "Cluster initialized");
            }
        }

        protected abstract void RequestHeartbeat();

        protected void OnDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription, bool shouldClusterDescriptionChangedEventBePublished)
        {
            if (shouldClusterDescriptionChangedEventBePublished)
            {
                _clusterEventLogger.LogAndPublish(new ClusterDescriptionChangedEvent(oldDescription, newDescription));
            }

            DescriptionChanged?.Invoke(this, new ClusterDescriptionChangedEventArgs(oldDescription, newDescription));
        }

        public IServer SelectServer(OperationContext operationContext, IServerSelector selector)
        {
            Ensure.IsNotNull(selector, nameof(selector));
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            ThrowIfDisposedOrNotOpen();

            using var serverSelectionOperationContext = operationContext.WithTimeout(Settings.ServerSelectionTimeout);
            var expirableClusterDescription = _expirableClusterDescription;
            IDisposable serverSelectionWaitQueueDisposer = null;
            (selector, var operationCountSelector, var stopwatch) = BeginServerSelection(expirableClusterDescription.ClusterDescription, selector);

            try
            {
                while (true)
                {
                    var server = SelectServer(expirableClusterDescription, selector, operationCountSelector);
                    if (server != null)
                    {
                        EndServerSelection(expirableClusterDescription.ClusterDescription, selector, server.Description, stopwatch);
                        return server;
                    }

                    serverSelectionWaitQueueDisposer ??= _serverSelectionWaitQueue.Enter(serverSelectionOperationContext, selector, expirableClusterDescription.ClusterDescription, EventContext.OperationId);

                    serverSelectionOperationContext.WaitTask(expirableClusterDescription.Expired);
                    expirableClusterDescription = _expirableClusterDescription;
                }
            }
            catch (Exception ex)
            {
                throw HandleServerSelectionException(expirableClusterDescription.ClusterDescription, selector, ex, stopwatch);
            }
            finally
            {
                serverSelectionWaitQueueDisposer?.Dispose();
            }
        }

        public async Task<IServer> SelectServerAsync(OperationContext operationContext, IServerSelector selector)
        {
            Ensure.IsNotNull(selector, nameof(selector));
            Ensure.IsNotNull(operationContext, nameof(operationContext));
            ThrowIfDisposedOrNotOpen();

            using var serverSelectionOperationContext = operationContext.WithTimeout(Settings.ServerSelectionTimeout);
            var expirableClusterDescription = _expirableClusterDescription;
            IDisposable serverSelectionWaitQueueDisposer = null;
            (selector, var operationCountSelector, var stopwatch) = BeginServerSelection(expirableClusterDescription.ClusterDescription, selector);

            try
            {
                while (true)
                {
                    var server = SelectServer(expirableClusterDescription, selector, operationCountSelector);
                    if (server != null)
                    {
                        EndServerSelection(expirableClusterDescription.ClusterDescription, selector, server.Description, stopwatch);
                        return server;
                    }

                    serverSelectionWaitQueueDisposer ??= _serverSelectionWaitQueue.Enter(serverSelectionOperationContext, selector, expirableClusterDescription.ClusterDescription, EventContext.OperationId);

                    await serverSelectionOperationContext.WaitTaskAsync(expirableClusterDescription.Expired).ConfigureAwait(false);
                    expirableClusterDescription = _expirableClusterDescription;
                }
            }
            catch (Exception ex)
            {
                throw HandleServerSelectionException(expirableClusterDescription.ClusterDescription, selector, ex, stopwatch);
            }
            finally
            {
                serverSelectionWaitQueueDisposer?.Dispose();
            }
        }

        public ICoreSessionHandle StartSession(CoreSessionOptions options)
        {
            options = options ?? new CoreSessionOptions();
            var session = new CoreSession(this, _serverSessionPool, options);
            return new CoreSessionHandle(session);
        }

        protected abstract bool TryGetServer(EndPoint endPoint, out IClusterableServer server);

        protected void UpdateClusterDescription(ClusterDescription newClusterDescription, bool shouldClusterDescriptionChangedEventBePublished = true)
        {
            var expiredClusterDescription = Interlocked.Exchange(ref _expirableClusterDescription, new(this, newClusterDescription));

            OnDescriptionChanged(expiredClusterDescription.ClusterDescription, newClusterDescription, shouldClusterDescriptionChangedEventBePublished);

            expiredClusterDescription.TrySetExpired();
        }

        private (IServerSelector Selector, OperationsCountServerSelector OperationCountSelector, Stopwatch Stopwatch) BeginServerSelection(ClusterDescription clusterDescription, IServerSelector selector)
        {
            _serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerEvent(
                clusterDescription,
                selector,
                EventContext.OperationId,
                EventContext.OperationName));

            var allSelectors = new List<IServerSelector>(5);
            if (Settings.PreServerSelector != null)
            {
                allSelectors.Add(Settings.PreServerSelector);
            }

            allSelectors.Add(selector);
            if (Settings.PostServerSelector != null)
            {
                allSelectors.Add(Settings.PostServerSelector);
            }

            allSelectors.Add(_latencyLimitingServerSelector);
            var operationCountSelector = new OperationsCountServerSelector(Array.Empty<IClusterableServer>());
            allSelectors.Add(operationCountSelector);

            return (new CompositeServerSelector(allSelectors), operationCountSelector, Stopwatch.StartNew());
        }

        private void EndServerSelection(ClusterDescription clusterDescription, IServerSelector selector, ServerDescription selectedServerDescription, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            _serverSelectionEventLogger.LogAndPublish(new ClusterSelectedServerEvent(
                clusterDescription,
                selector,
                selectedServerDescription,
                stopwatch.Elapsed,
                EventContext.OperationId,
                EventContext.OperationName));
        }

        private Exception HandleServerSelectionException(ClusterDescription clusterDescription, IServerSelector selector, Exception exception, Stopwatch stopwatch)
        {
            stopwatch.Stop();

            if (exception is TimeoutException)
            {
                var message = $"A timeout occurred after {stopwatch.ElapsedMilliseconds}ms selecting a server using {selector}. Client view of cluster state is {clusterDescription}.";
                exception = new TimeoutException(message);
            }

            _serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerFailedEvent(
                clusterDescription,
                selector,
                exception,
                EventContext.OperationId,
                EventContext.OperationName));

            return exception;
        }

        private SelectedServer SelectServer(ExpirableClusterDescription clusterDescriptionChangeSource, IServerSelector selector, OperationsCountServerSelector operationCountSelector)
        {
            MongoIncompatibleDriverException.ThrowIfNotSupported(clusterDescriptionChangeSource.ClusterDescription);

            operationCountSelector.PopulateServers(clusterDescriptionChangeSource.ConnectedServers);
            var selectedServerDescription = selector
                .SelectServers(clusterDescriptionChangeSource.ClusterDescription, clusterDescriptionChangeSource.ConnectedServerDescriptions)
                .SingleOrDefault();

            if (selectedServerDescription != null)
            {
                var selectedServer = clusterDescriptionChangeSource.ConnectedServers.FirstOrDefault(s => EndPointHelper.Equals(s.EndPoint, selectedServerDescription.EndPoint));
                if (selectedServer != null)
                {
                    return new(selectedServer, selectedServerDescription);
                }
            }

            return default;
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        private void ThrowIfDisposedOrNotOpen()
        {
            if (_state.Value != State.Open)
            {
                ThrowIfDisposed();
                throw new InvalidOperationException("Server must be initialized.");
            }
        }

        // nested classes
        internal sealed class ExpirableClusterDescription
        {
            private readonly Cluster _cluster;
            private readonly TaskCompletionSource<bool> _expireCompletionSource;
            private readonly ClusterDescription _clusterDescription;
            private readonly object _connectedServersLock = new();
            private IReadOnlyList<IClusterableServer> _connectedServers;
            private IReadOnlyList<ServerDescription> _connectedServerDescriptions;

            public ExpirableClusterDescription(Cluster cluster, ClusterDescription clusterDescription)
            {
                _cluster = cluster;
                _clusterDescription = clusterDescription;
                _expireCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public ClusterDescription ClusterDescription => _clusterDescription;

            public Task Expired => _expireCompletionSource.Task;

            public IReadOnlyList<IClusterableServer> ConnectedServers
            {
                get
                {
                    EnsureConnectedServersInitialized();
                    return _connectedServers;
                }
            }

            public IReadOnlyList<ServerDescription> ConnectedServerDescriptions
            {
                get
                {
                    EnsureConnectedServersInitialized();
                    return _connectedServerDescriptions;
                }
            }

            public bool TrySetExpired()
                => _expireCompletionSource.TrySetResult(true);

            private void EnsureConnectedServersInitialized()
            {
                if (_connectedServers != null)
                {
                    return;
                }

                lock (_connectedServersLock)
                {
                    if (_connectedServers != null)
                    {
                        return;
                    }

                    var connectedServerDescriptions = new List<ServerDescription>(ClusterDescription.Servers?.Count ?? 1);
                    var connectedServers = new List<IClusterableServer>(connectedServerDescriptions.Capacity);

                    if (ClusterDescription.Servers != null)
                    {
                        foreach (var description in ClusterDescription.Servers)
                        {
                            if (description.State == ServerState.Connected &&
                                _cluster.TryGetServer(description.EndPoint, out var server))
                            {
                                connectedServers.Add(server);
                                connectedServerDescriptions.Add(description);
                            }
                        }
                    }

                    _connectedServerDescriptions = connectedServerDescriptions;
                    _connectedServers = connectedServers;
                }
            }
        }

        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }

        private static class RapidHeartbeatTimerCallbackState
        {
            public const int NotRunning = 0;
            public const int Running = 1;
        }

        private sealed class ServerSelectionWaitQueue : IDisposable
        {
            private readonly Cluster _cluster;
            private readonly object _serverSelectionWaitQueueLock = new();
            private readonly Timer _rapidHeartbeatTimer;
            private readonly InterlockedInt32 _rapidHeartbeatTimerCallbackState;

            private int _serverSelectionWaitQueueSize;

            public ServerSelectionWaitQueue(Cluster cluster)
            {
                _cluster = cluster;
                _rapidHeartbeatTimerCallbackState = new InterlockedInt32(RapidHeartbeatTimerCallbackState.NotRunning);
                _rapidHeartbeatTimer = new Timer(RapidHeartbeatTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            public void Dispose()
            {
                _rapidHeartbeatTimer.Dispose();
            }

            public IDisposable Enter(OperationContext operationContext, IServerSelector selector, ClusterDescription clusterDescription, long? operationId)
            {
                lock (_serverSelectionWaitQueueLock)
                {
                    if (_serverSelectionWaitQueueSize >= _cluster._settings.MaxServerSelectionWaitQueueSize)
                    {
                        throw MongoWaitQueueFullException.ForServerSelection();
                    }

                    if (++_serverSelectionWaitQueueSize == 1)
                    {
                        _rapidHeartbeatTimer.Change(TimeSpan.Zero, _cluster._minHeartbeatInterval);
                    }

                    _cluster._serverSelectionEventLogger.LogAndPublish(new ClusterEnteredSelectionQueueEvent(
                        clusterDescription,
                        selector,
                        operationId,
                        EventContext.OperationName,
                        operationContext.RemainingTimeout));
                }

                return new ServerSelectionQueueDisposer(this);
            }

            private void ExitServerSelectionWaitQueue()
            {
                lock (_serverSelectionWaitQueueLock)
                {
                    if (--_serverSelectionWaitQueueSize == 0)
                    {
                        _rapidHeartbeatTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    }
                }
            }

            private void RapidHeartbeatTimerCallback(object args)
            {
                // avoid requesting heartbeat reentrantly
                if (_rapidHeartbeatTimerCallbackState.TryChange(RapidHeartbeatTimerCallbackState.NotRunning, RapidHeartbeatTimerCallbackState.Running))
                {
                    try
                    {
                        _cluster.RequestHeartbeat();
                    }
                    catch
                    {
                        // TODO: Trace this
                        // If we don't protect this call, we could
                        // take down the app domain.
                    }
                    finally
                    {
                        _rapidHeartbeatTimerCallbackState.TryChange(RapidHeartbeatTimerCallbackState.NotRunning);
                    }
                }
            }

            private sealed class ServerSelectionQueueDisposer : IDisposable
            {
                private readonly ServerSelectionWaitQueue _waitQueue;

                public ServerSelectionQueueDisposer(ServerSelectionWaitQueue waitQueue)
                {
                    _waitQueue = waitQueue;
                }

                public void Dispose()
                    => _waitQueue.ExitServerSelectionWaitQueue();
            }
        }
    }
}
