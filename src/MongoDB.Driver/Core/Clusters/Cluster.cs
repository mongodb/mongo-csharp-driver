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
        private static readonly IServerSelector __randomServerSelector = new RandomServerSelector();

        public static SemanticVersion MinSupportedServerVersion { get; } = WireVersion.ToServerVersion(WireVersion.SupportedWireVersionRange.Min);
        public static Range<int> SupportedWireVersionRange { get; } = WireVersion.SupportedWireVersionRange;

        #endregion

        private readonly TimeSpan _minHeartbeatInterval = __minHeartbeatIntervalDefault;
        private readonly IClusterClock _clusterClock = new ClusterClock();
        private readonly ClusterId _clusterId;
        private ClusterDescription _description;
        private TaskCompletionSource<bool> _descriptionChangedTaskCompletionSource;
        private readonly object _descriptionLock = new object();
        private readonly LatencyLimitingServerSelector _latencyLimitingServerSelector;
        protected readonly EventLogger<LogCategories.SDAM> _clusterEventLogger;
        protected readonly EventLogger<LogCategories.ServerSelection> _serverSelectionEventLogger;
        private Timer _rapidHeartbeatTimer;
        private readonly object _serverSelectionWaitQueueLock = new object();
        private int _serverSelectionWaitQueueSize;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ICoreServerSessionPool _serverSessionPool;
        private readonly ClusterSettings _settings;
        private readonly InterlockedInt32 _state;
        private readonly InterlockedInt32 _rapidHeartbeatTimerCallbackState;

        // constructors
        protected Cluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IEventSubscriber eventSubscriber, ILoggerFactory loggerFactory)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            Ensure.That(!_settings.LoadBalanced, "LoadBalanced mode is not supported.");
            _serverFactory = Ensure.IsNotNull(serverFactory, nameof(serverFactory));
            Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _state = new InterlockedInt32(State.Initial);
            _rapidHeartbeatTimerCallbackState = new InterlockedInt32(RapidHeartbeatTimerCallbackState.NotRunning);

            _clusterId = new ClusterId();
            _description = ClusterDescription.CreateInitial(_clusterId, _settings.DirectConnection);
            _descriptionChangedTaskCompletionSource = new TaskCompletionSource<bool>();
            _latencyLimitingServerSelector = new LatencyLimitingServerSelector(settings.LocalThreshold);

            _rapidHeartbeatTimer = new Timer(RapidHeartbeatTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

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
                lock (_descriptionLock)
                {
                    return _description;
                }
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
                    _description.DirectConnection,
                    dnsMonitorException: null,
                    ClusterType.Unknown,
                    Enumerable.Empty<ServerDescription>());

                UpdateClusterDescription(newClusterDescription);

                _rapidHeartbeatTimer.Dispose();

                _clusterEventLogger.Logger?.LogTrace(_clusterId, "Cluster disposed");
            }
        }

        private void EnterServerSelectionWaitQueue(IServerSelector selector, ClusterDescription clusterDescription, long? operationId, TimeSpan remainingTime)
        {
            lock (_serverSelectionWaitQueueLock)
            {
                if (_serverSelectionWaitQueueSize >= _settings.MaxServerSelectionWaitQueueSize)
                {
                    throw MongoWaitQueueFullException.ForServerSelection();
                }

                if (++_serverSelectionWaitQueueSize == 1)
                {
                    _rapidHeartbeatTimer.Change(TimeSpan.Zero, _minHeartbeatInterval);
                }

                _serverSelectionEventLogger.LogAndPublish(new ClusterEnteredSelectionQueueEvent(
                    clusterDescription,
                    selector,
                    operationId,
                    EventContext.OperationName,
                    remainingTime));
            }
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

        public virtual void Initialize()
        {
            ThrowIfDisposed();
            if (_state.TryChange(State.Initial, State.Open))
            {
                _clusterEventLogger.Logger?.LogTrace(_clusterId, "Cluster initialized");
            }
        }

        private void RapidHeartbeatTimerCallback(object args)
        {
            // avoid requesting heartbeat reentrantly
            if (_rapidHeartbeatTimerCallbackState.TryChange(RapidHeartbeatTimerCallbackState.NotRunning, RapidHeartbeatTimerCallbackState.Running))
            {
                try
                {
                    RequestHeartbeat();
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

        protected abstract void RequestHeartbeat();

        protected void OnDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription, bool shouldClusterDescriptionChangedEventBePublished)
        {
            if (shouldClusterDescriptionChangedEventBePublished)
            {
                _clusterEventLogger.LogAndPublish(new ClusterDescriptionChangedEvent(oldDescription, newDescription));
            }

            DescriptionChanged?.Invoke(this, new ClusterDescriptionChangedEventArgs(oldDescription, newDescription));
        }

        public IServer SelectServer(IServerSelector selector, CancellationToken cancellationToken)
        {
            ThrowIfDisposedOrNotOpen();
            Ensure.IsNotNull(selector, nameof(selector));

            using (var helper = new SelectServerHelper(this, selector))
            {
                try
                {
                    while (true)
                    {
                        var server = helper.SelectServer();
                        if (server != null)
                        {
                            return server;
                        }

                        helper.WaitingForDescriptionToChange();
                        WaitForDescriptionChanged(helper.Selector, helper.Description, helper.DescriptionChangedTask, helper.TimeoutRemaining, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    helper.HandleException(ex);
                    throw;
                }
            }
        }

        public async Task<IServer> SelectServerAsync(IServerSelector selector, CancellationToken cancellationToken)
        {
            ThrowIfDisposedOrNotOpen();
            Ensure.IsNotNull(selector, nameof(selector));

            using (var helper = new SelectServerHelper(this, selector))
            {
                try
                {
                    while (true)
                    {
                        var server = helper.SelectServer();
                        if (server != null)
                        {
                            return server;
                        }

                        helper.WaitingForDescriptionToChange();
                        await WaitForDescriptionChangedAsync(helper.Selector, helper.Description, helper.DescriptionChangedTask, helper.TimeoutRemaining, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    helper.HandleException(ex);
                    throw;
                }
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
            ClusterDescription oldClusterDescription = null;
            TaskCompletionSource<bool> oldDescriptionChangedTaskCompletionSource = null;

            lock (_descriptionLock)
            {
                oldClusterDescription = _description;
                _description = newClusterDescription;

                oldDescriptionChangedTaskCompletionSource = _descriptionChangedTaskCompletionSource;
                _descriptionChangedTaskCompletionSource = new TaskCompletionSource<bool>();
            }

            OnDescriptionChanged(oldClusterDescription, newClusterDescription, shouldClusterDescriptionChangedEventBePublished);

            // TODO: use RunContinuationsAsynchronously instead once we require a new enough .NET Framework
            Task.Run(() => oldDescriptionChangedTaskCompletionSource.TrySetResult(true));
        }

        private string BuildTimeoutExceptionMessage(TimeSpan timeout, IServerSelector selector, ClusterDescription clusterDescription)
        {
            var ms = (int)Math.Round(timeout.TotalMilliseconds);
            return string.Format(
                "A timeout occurred after {0}ms selecting a server using {1}. Client view of cluster state is {2}.",
                ms.ToString(),
                selector.ToString(),
                clusterDescription.ToString());
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

        private void WaitForDescriptionChanged(IServerSelector selector, ClusterDescription description, Task descriptionChangedTask, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var helper = new WaitForDescriptionChangedHelper(this, selector, description, descriptionChangedTask, timeout, cancellationToken))
            {
                var index = Task.WaitAny(helper.Tasks);
                helper.HandleCompletedTask(helper.Tasks[index]);
            }
        }

        private async Task WaitForDescriptionChangedAsync(IServerSelector selector, ClusterDescription description, Task descriptionChangedTask, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var helper = new WaitForDescriptionChangedHelper(this, selector, description, descriptionChangedTask, timeout, cancellationToken))
            {
                var completedTask = await Task.WhenAny(helper.Tasks).ConfigureAwait(false);
                helper.HandleCompletedTask(completedTask);
            }
        }

        private void ThrowTimeoutException(IServerSelector selector, ClusterDescription description)
        {
            var message = BuildTimeoutExceptionMessage(_settings.ServerSelectionTimeout, selector, description);
            throw new TimeoutException(message);
        }

        // nested classes
        private class SelectServerHelper : IDisposable
        {
            private readonly Cluster _cluster;
            private readonly List<IClusterableServer> _connectedServers;
            private readonly List<ServerDescription> _connectedServerDescriptions;
            private ClusterDescription _description;
            private Task _descriptionChangedTask;
            private bool _serverSelectionWaitQueueEntered;
            private readonly IServerSelector _selector;
            private readonly OperationsCountServerSelector _operationCountServerSelector;
            private readonly Stopwatch _stopwatch;
            private readonly DateTime _timeoutAt;

            public SelectServerHelper(Cluster cluster, IServerSelector selector)
            {
                _cluster = cluster;

                _connectedServers = new List<IClusterableServer>(_cluster._description?.Servers?.Count ?? 1);
                _connectedServerDescriptions = new List<ServerDescription>(_connectedServers.Count);
                _operationCountServerSelector = new OperationsCountServerSelector(_connectedServers);

                _selector = DecorateSelector(selector);
                _stopwatch = Stopwatch.StartNew();
                _timeoutAt = DateTime.UtcNow + _cluster.Settings.ServerSelectionTimeout;
            }

            public ClusterDescription Description
            {
                get { return _description; }
            }

            public Task DescriptionChangedTask
            {
                get { return _descriptionChangedTask; }
            }

            public IServerSelector Selector
            {
                get { return _selector; }
            }

            public TimeSpan TimeoutRemaining
            {
                get { return _timeoutAt - DateTime.UtcNow; }
            }

            public void Dispose()
            {
                if (_serverSelectionWaitQueueEntered)
                {
                    _cluster.ExitServerSelectionWaitQueue();
                }
            }

            public void HandleException(Exception exception)
            {
                _cluster._serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerFailedEvent(
                    _description,
                    _selector,
                    exception,
                    EventContext.OperationId,
                    EventContext.OperationName));
            }

            public IServer SelectServer()
            {
                lock (_cluster._descriptionLock)
                {
                    _descriptionChangedTask = _cluster._descriptionChangedTaskCompletionSource.Task;
                    _description = _cluster._description;
                }

                if (!_serverSelectionWaitQueueEntered)
                {
                    // this is our first time through...
                    _cluster._serverSelectionEventLogger.LogAndPublish(new ClusterSelectingServerEvent(
                        _description,
                        _selector,
                        EventContext.OperationId,
                        EventContext.OperationName));
                }

                MongoIncompatibleDriverException.ThrowIfNotSupported(_description);

                _connectedServers.Clear();
                _connectedServerDescriptions.Clear();

                foreach (var description in _description.Servers)
                {
                    if (description.State == ServerState.Connected &&
                        _cluster.TryGetServer(description.EndPoint, out var server))
                    {
                        _connectedServers.Add(server);
                        _connectedServerDescriptions.Add(description);
                    }
                }

                var selectedServersDescriptions = _selector
                    .SelectServers(_description, _connectedServerDescriptions)
                    .ToList();

                IServer selectedServer = null;

                if (selectedServersDescriptions.Count > 0)
                {
                    var selectedServerDescription = selectedServersDescriptions.Count == 1
                        ? selectedServersDescriptions[0]
                        : __randomServerSelector.SelectServers(_description, selectedServersDescriptions).Single();

                    selectedServer = _connectedServers.FirstOrDefault(s => EndPointHelper.Equals(s.EndPoint, selectedServerDescription.EndPoint));
                }

                if (selectedServer != null)
                {
                    _stopwatch.Stop();

                    _cluster._serverSelectionEventLogger.LogAndPublish(new ClusterSelectedServerEvent(
                        _description,
                        _selector,
                        selectedServer.Description,
                        _stopwatch.Elapsed,
                        EventContext.OperationId,
                        EventContext.OperationName));
                }

                return selectedServer;
            }

            public void WaitingForDescriptionToChange()
            {
                if (!_serverSelectionWaitQueueEntered)
                {
                    _cluster.EnterServerSelectionWaitQueue(_selector, _description, EventContext.OperationId, _timeoutAt - DateTime.UtcNow);
                    _serverSelectionWaitQueueEntered = true;
                }

                var timeoutRemaining = _timeoutAt - DateTime.UtcNow;
                if (timeoutRemaining <= TimeSpan.Zero)
                {
                    _cluster.ThrowTimeoutException(_selector, _description);
                }
            }

            private IServerSelector DecorateSelector(IServerSelector selector)
            {
                var settings = _cluster.Settings;
                var allSelectors = new List<IServerSelector>();

                if (settings.PreServerSelector != null)
                {
                    allSelectors.Add(settings.PreServerSelector);
                }

                allSelectors.Add(selector);

                if (settings.PostServerSelector != null)
                {
                    allSelectors.Add(settings.PostServerSelector);
                }

                allSelectors.Add(_cluster._latencyLimitingServerSelector);
                allSelectors.Add(_operationCountServerSelector);

                return new CompositeServerSelector(allSelectors);
            }
        }

        private sealed class WaitForDescriptionChangedHelper : IDisposable
        {
            private readonly CancellationToken _cancellationToken;
            private readonly TaskCompletionSource<bool> _cancellationTaskCompletionSource;
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;
            private readonly Cluster _cluster;
            private readonly ClusterDescription _description;
            private readonly Task _descriptionChangedTask;
            private readonly IServerSelector _selector;
            private readonly CancellationTokenSource _timeoutCancellationTokenSource;
            private readonly Task _timeoutTask;

            public WaitForDescriptionChangedHelper(Cluster cluster, IServerSelector selector, ClusterDescription description, Task descriptionChangedTask, TimeSpan timeout, CancellationToken cancellationToken)
            {
                _cluster = cluster;
                _description = description;
                _selector = selector;
                _descriptionChangedTask = descriptionChangedTask;
                _cancellationToken = cancellationToken;
                _cancellationTaskCompletionSource = new TaskCompletionSource<bool>();
                _cancellationTokenRegistration = cancellationToken.Register(() => _cancellationTaskCompletionSource.TrySetCanceled());
                _timeoutCancellationTokenSource = new CancellationTokenSource();
                _timeoutTask = Task.Delay(timeout, _timeoutCancellationTokenSource.Token);
            }

            public Task[] Tasks
            {
                get
                {
                    return new Task[]
                    {
                        _descriptionChangedTask,
                        _timeoutTask,
                        _cancellationTaskCompletionSource.Task
                    };
                }
            }

            public void Dispose()
            {
                _cancellationTokenRegistration.Dispose();
                _timeoutCancellationTokenSource.Dispose();
            }

            public void HandleCompletedTask(Task completedTask)
            {
                if (completedTask == _timeoutTask)
                {
                    _cluster.ThrowTimeoutException(_selector, _description);
                }
                _timeoutCancellationTokenSource.Cancel();

                if (completedTask == _cancellationTaskCompletionSource.Task)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                }

                _descriptionChangedTask.GetAwaiter().GetResult(); // propagate exceptions
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
    }
}
