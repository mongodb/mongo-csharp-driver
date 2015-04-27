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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a cluster.
    /// </summary>
    internal abstract class Cluster : ICluster
    {
        #region static
        // static fields
        private static readonly TimeSpan __minHeartbeatInterval = TimeSpan.FromMilliseconds(500);
        private static readonly Range<int> __supportedWireVersionRange = new Range<int>(0, 3);
        private static readonly IServerSelector __randomServerSelector = new RandomServerSelector();
        #endregion

        // fields
        private readonly ClusterId _clusterId;
        private ClusterDescription _description;
        private TaskCompletionSource<bool> _descriptionChangedTaskCompletionSource;
        private readonly object _descriptionLock = new object();
        private readonly IClusterListener _listener;
        private Timer _rapidHeartbeatTimer;
        private readonly object _serverSelectionWaitQueueLock = new object();
        private int _serverSelectionWaitQueueSize;
        private readonly IClusterableServerFactory _serverFactory;
        private readonly ClusterSettings _settings;
        private readonly InterlockedInt32 _state;

        // constructors
        protected Cluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IClusterListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _serverFactory = Ensure.IsNotNull(serverFactory, "serverFactory");
            _listener = listener;
            _state = new InterlockedInt32(State.Initial);

            _clusterId = new ClusterId();
            _description = ClusterDescription.CreateInitial(_clusterId, _settings.ConnectionMode.ToClusterType());
            _descriptionChangedTaskCompletionSource = new TaskCompletionSource<bool>();

            _rapidHeartbeatTimer = new Timer(RapidHeartbeatTimerCallback, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
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

        protected IClusterListener Listener
        {
            get { return _listener; }
        }

        public ClusterSettings Settings
        {
            get { return _settings; }
        }

        // methods
        protected IClusterableServer CreateServer(EndPoint endPoint)
        {
            return _serverFactory.CreateServer(_clusterId, endPoint);
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
                var newClusterDescription = new ClusterDescription(
                    _clusterId,
                    ClusterType.Unknown,
                    Enumerable.Empty<ServerDescription>());

                UpdateClusterDescription(newClusterDescription);

                _rapidHeartbeatTimer.Dispose();
            }
        }

        private void EnterServerSelectionWaitQueue()
        {
            lock (_serverSelectionWaitQueueLock)
            {
                if (_serverSelectionWaitQueueSize >= _settings.MaxServerSelectionWaitQueueSize)
                {
                    throw MongoWaitQueueFullException.ForServerSelection();
                }

                if (++_serverSelectionWaitQueueSize == 1)
                {
                    _rapidHeartbeatTimer.Change(TimeSpan.Zero, __minHeartbeatInterval);
                }
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
            _state.TryChange(State.Initial, State.Open);
        }

        private void RapidHeartbeatTimerCallback(object args)
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
        }

        protected abstract void RequestHeartbeat();

        protected void OnDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription)
        {
            if (_listener != null)
            {
                _listener.ClusterAfterDescriptionChanged(new ClusterAfterDescriptionChangedEvent(oldDescription, newDescription));
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                var args = new ClusterDescriptionChangedEventArgs(oldDescription, newDescription);
                handler(this, args);
            }
        }

        public async Task<IServer> SelectServerAsync(IServerSelector selector, CancellationToken cancellationToken)
        {
            ThrowIfDisposedOrNotOpen();
            Ensure.IsNotNull(selector, "selector");

            var timeoutAt = DateTime.UtcNow + _settings.ServerSelectionTimeout;

            var serverSelectionWaitQueueEntered = false;

            if (_settings.PreServerSelector != null || _settings.PostServerSelector != null)
            {
                var allSelectors = new List<IServerSelector>();
                if (_settings.PreServerSelector != null)
                {
                    allSelectors.Add(_settings.PreServerSelector);
                }

                allSelectors.Add(selector);

                if (_settings.PostServerSelector != null)
                {
                    allSelectors.Add(_settings.PostServerSelector);
                }

                selector = new CompositeServerSelector(allSelectors);
            }

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task descriptionChangedTask;
                    ClusterDescription description;
                    lock (_descriptionLock)
                    {
                        descriptionChangedTask = _descriptionChangedTaskCompletionSource.Task;
                        description = _description;
                    }

                    ThrowIfIncompatible(description);

                    var connectedServers = description.Servers.Where(s => s.State == ServerState.Connected);
                    var selectedServers = selector.SelectServers(description, connectedServers).ToList();

                    while (selectedServers.Count > 0)
                    {
                        var server = selectedServers.Count == 1 ?
                            selectedServers[0] :
                            __randomServerSelector.SelectServers(description, selectedServers).Single();

                        IClusterableServer selectedServer;
                        if (TryGetServer(server.EndPoint, out selectedServer))
                        {
                            return selectedServer;
                        }

                        selectedServers.Remove(server);
                    }

                    if (!serverSelectionWaitQueueEntered)
                    {
                        EnterServerSelectionWaitQueue();
                        serverSelectionWaitQueueEntered = true;
                    }

                    var timeoutRemaining = timeoutAt - DateTime.UtcNow;
                    if (timeoutRemaining <= TimeSpan.Zero)
                    {
                        ThrowTimeoutException(selector, description);
                    }

                    await WaitForDescriptionChangedAsync(selector, description, descriptionChangedTask, timeoutRemaining, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (serverSelectionWaitQueueEntered)
                {
                    ExitServerSelectionWaitQueue();
                }
            }
        }

        protected abstract bool TryGetServer(EndPoint endPoint, out IClusterableServer server);

        protected void UpdateClusterDescription(ClusterDescription newClusterDescription)
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

            OnDescriptionChanged(oldClusterDescription, newClusterDescription);
            oldDescriptionChangedTaskCompletionSource.TrySetResult(true);
        }

        private string BuildTimeoutExceptionMessage(TimeSpan timeout, IServerSelector selector, ClusterDescription clusterDescription)
        {
            var ms = (int)Math.Round(timeout.TotalMilliseconds);
            return string.Format(
                "A timeout occured after {0}ms selecting a server using {1}. Client view of cluster state is {2}.",
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

        private void ThrowIfIncompatible(ClusterDescription description)
        {
            var isIncompatible = description.Servers
                .Any(sd => sd.WireVersionRange != null && !sd.WireVersionRange.Overlaps(__supportedWireVersionRange));

            if (isIncompatible)
            {
                throw new MongoIncompatibleDriverException(description);
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

        private async Task WaitForDescriptionChangedAsync(IServerSelector selector, ClusterDescription description, Task descriptionChangedTask, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancellationTaskCompletionSource.TrySetCanceled()))
            using (var timeoutCancellationTokenSource = new CancellationTokenSource())
            {
                var timeoutTask = Task.Delay(timeout, timeoutCancellationTokenSource.Token);
                var completedTask = await Task.WhenAny(descriptionChangedTask, timeoutTask, cancellationTaskCompletionSource.Task).ConfigureAwait(false);

                if (completedTask == timeoutTask)
                {
                    ThrowTimeoutException(selector, description);
                }
                timeoutCancellationTokenSource.Cancel();

                if (completedTask == cancellationTaskCompletionSource.Task)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await descriptionChangedTask.ConfigureAwait(false); // propagate exceptions
            }
        }

        private void ThrowTimeoutException(IServerSelector selector, ClusterDescription description)
        {
            var message = BuildTimeoutExceptionMessage(_settings.ServerSelectionTimeout, selector, description);
            throw new TimeoutException(message);
        }

        // nested classes
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}
