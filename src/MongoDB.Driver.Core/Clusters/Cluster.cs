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
using System.Linq;
using System.Net;
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
        // static fields
        private static readonly Range<int> __supportedWireVersionRange = new Range<int>(0, 2);
        private static readonly IServerSelector __randomServerSelector = new RandomServerSelector();

        // fields
        private readonly ClusterId _clusterId;
        private ClusterDescription _description;
        private TaskCompletionSource<bool> _descriptionChangedTaskCompletionSource;
        private readonly object _descriptionLock = new object();
        private readonly IClusterListener _listener;
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
        }

        // events
        public event EventHandler<ClusterDescriptionChangedEventArgs> DescriptionChanged;

        // properties
        protected ClusterId ClusterId
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
                    Enumerable.Empty<ServerDescription>(),
                    null);

                UpdateClusterDescription(newClusterDescription);
            }
        }

        public virtual void Initialize()
        {
            ThrowIfDisposed();
            _state.TryChange(State.Initial, State.Open);
        }

        protected abstract void Invalidate();

        protected void OnDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription)
        {
            ClusterDescriptionChangedEventArgs args = null;

            if (_listener != null)
            {
                args = new ClusterDescriptionChangedEventArgs(oldDescription, newDescription);
                _listener.ClusterDescriptionChanged(args);
            }

            var handler = DescriptionChanged;
            if (handler != null)
            {
                if (args == null)
                {
                    args = new ClusterDescriptionChangedEventArgs(oldDescription, newDescription);
                }
                handler(this, args);
            }
        }

        public async Task<IServer> SelectServerAsync(IServerSelector selector, TimeSpan timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposedOrNotOpen();
            Ensure.IsNotNull(selector, "selector");
            var slidingTimeout = new SlidingTimeout(timeout);

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

                Invalidate();

                await descriptionChangedTask.WithTimeout(slidingTimeout, cancellationToken);
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
                var message = string.Format(
                    "This version of the driver is incompatible with one or more of the " +
                    "servers to which it is connected: {0}.", description);
                throw new MongoDBException(message);
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
        private static class State
        {
            public const int Initial = 0;
            public const int Open = 1;
            public const int Disposed = 2;
        }
    }
}
