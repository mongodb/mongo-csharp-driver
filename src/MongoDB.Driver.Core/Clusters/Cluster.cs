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
    public abstract class Cluster : ICluster
    {
        // fields
        private readonly ClusterId _clusterId;
        private ClusterDescription _description;
        private TaskCompletionSource<bool> _descriptionChangedTaskCompletionSource;
        private bool _disposed;
        private readonly IClusterListener _listener;
        private readonly object _lock = new object();
        private readonly IServerSelector _randomServerSelector = new RandomServerSelector();
        private readonly IServerFactory _serverFactory;
        private readonly ClusterSettings _settings;

        // constructors
        protected Cluster(ClusterSettings settings, IServerFactory serverFactory, IClusterListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _serverFactory = Ensure.IsNotNull(serverFactory, "serverFactory");
            _listener = listener;

            _clusterId = new ClusterId();
            _description = ClusterDescription.CreateUninitialized(_clusterId, settings.RequiredClusterType ?? ClusterType.Unknown);
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
                lock (_lock)
                {
                    return _description;
                }
            }
        }

        protected bool Disposed
        {
            get { return _disposed; }
        }

        protected IClusterListener Listener
        {
            get { return _listener; }
        }

        protected object Lock
        {
            get { return _lock; }
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
            _disposed = true;
        }

        public async Task<ClusterDescription> GetDescriptionAsync(int minimumRevision = 0, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var slidingTimeout = new SlidingTimeout(timeout);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ClusterDescription description;
                Task descriptionChangedTask;
                lock (_lock)
                {
                    description = _description;
                    descriptionChangedTask = _descriptionChangedTaskCompletionSource.Task;
                }

                if (description.Revision >= minimumRevision)
                {
                    return description;
                }

                await descriptionChangedTask.WithTimeout(slidingTimeout, cancellationToken);
            }
        }

        public abstract IServer GetServer(EndPoint endPoint);

        protected virtual void OnDescriptionChanged(ClusterDescription oldDescription, ClusterDescription newDescription)
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
            Ensure.IsNotNull(selector, "selector");
            var slidingTimeout = new SlidingTimeout(timeout);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ClusterDescription description;
                Task descriptionChangedTask;
                lock (_lock)
                {
                    description = _description;
                    descriptionChangedTask = _descriptionChangedTaskCompletionSource.Task;
                }

                var connectedServers = description.Servers.Where(s => s.State == ServerState.Connected);
                var selectedServers = selector.SelectServers(_description, connectedServers).ToList();

                while (selectedServers.Any())
                {
                    var server = selectedServers.Count == 1 ?
                        selectedServers[0] :
                        _randomServerSelector.SelectServers(_description, selectedServers).Single();

                    IClusterableServer rootServer;
                    if (TryGetServer(server.EndPoint, out rootServer))
                    {
                        return rootServer;
                    }

                    selectedServers.Remove(server);
                }

                await descriptionChangedTask.WithTimeout(slidingTimeout, cancellationToken);
            }
        }

        protected abstract bool TryGetServer(EndPoint endPoint, out IClusterableServer server);

        protected void UpdateClusterDescription(ClusterDescription newClusterDescription)
        {
            ClusterDescription oldClusterDescription = null;
            TaskCompletionSource<bool> oldDescriptionChangedTaskCompletionSource = null;

            lock (_lock)
            {
                oldClusterDescription = _description;
                newClusterDescription = newClusterDescription.WithRevision(oldClusterDescription.Revision + 1);
                _description = newClusterDescription;

                oldDescriptionChangedTaskCompletionSource = _descriptionChangedTaskCompletionSource;
                _descriptionChangedTaskCompletionSource = new TaskCompletionSource<bool>();
            }

            OnDescriptionChanged(oldClusterDescription, newClusterDescription);
            oldDescriptionChangedTaskCompletionSource.TrySetResult(true);
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
