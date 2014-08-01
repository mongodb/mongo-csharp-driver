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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Clusters.Monitoring;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a multi server cluster.
    /// </summary>
    public class MultiServerCluster : Cluster
    {
        // fields
        private readonly CancellationTokenSource _backgroundTaskCancellationTokenSource = new CancellationTokenSource();
        private readonly AsyncQueue<ServerDescriptionChangedEventArgs> _serverDescriptionChangedQueue = new AsyncQueue<ServerDescriptionChangedEventArgs>();
        private readonly List<IRootServer> _servers = new List<IRootServer>();

        // constructors
        public MultiServerCluster(ClusterSettings settings, IServerFactory serverFactory, IClusterListener listener)
            : base(settings, serverFactory, listener)
        {
        }

        // properties
        // methods
        internal void AddServer(IRootServer server)
        {
            lock (Lock)
            {
                if (_servers.Any(n => n.EndPoint.Equals(server.EndPoint)))
                {
                    var message = string.Format("The cluster already contains a server for end point: {0}.", server.EndPoint);
                    throw new ArgumentException(message, "server");
                }

                _servers.Add(server);
            }

            server.DescriptionChanged += ServerDescriptionChangedHandler;
            server.Initialize();

            if (Listener != null)
            {
                var args = new ServerAddedEventArgs(server.Description);
                Listener.ServerAdded(args);
            }
        }

        private void AddServerAction(AddServerAction action)
        {
            throw new NotImplementedException();
        }

        private async Task BackgroundTask()
        {
            var cancellationToken = _backgroundTaskCancellationTokenSource.Token;
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var eventArgs = await _serverDescriptionChangedQueue.DequeueAsync(); // TODO: add timeout and cancellationToken to DequeueAsync
                    ProcessServerDescriptionChanged(eventArgs);
                }
            }
            catch (TaskCanceledException)
            {
                // ignore TaskCanceledException
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (Lock)
            {
                if (disposing)
                {
                    if (!Disposed)
                    {
                        _backgroundTaskCancellationTokenSource.Cancel();
                        foreach (var server in _servers)
                        {
                            server.DescriptionChanged -= ServerDescriptionChangedHandler;
                            server.Dispose();
                        }
                        _backgroundTaskCancellationTokenSource.Dispose();
                    }
                }
            }
            base.Dispose();
        }

        public override IServer GetServer(EndPoint endPoint)
        {
            lock (Lock)
            {
                return _servers.Where(s => s.EndPoint.Equals(endPoint)).FirstOrDefault();
            }
        }

        public void Initialize()
        {
            foreach (var endPoint in Settings.EndPoints)
            {
                var server = CreateServer(endPoint);
                AddServer(server);
            }
            BackgroundTask().LogUnobservedExceptions();
        }

        private void ServerDescriptionChangedHandler(object sender, ServerDescriptionChangedEventArgs args)
        {
            var server = (IServer)sender;
            _serverDescriptionChangedQueue.Enqueue(args);
        }

        private void ProcessServerDescriptionChanged(ServerDescriptionChangedEventArgs eventArgs)
        {
            ClusterDescription newClusterDescription = null;

            var clusterMonitorLogic = new ClusterMonitorLogic(Description, eventArgs.NewServerDescription);
            var actions = clusterMonitorLogic.Transition();
            lock (Lock)
            {
                foreach (var action in actions)
                {
                    switch (action.Type)
                    {
                        case TransitionActionType.AddServer:
                            AddServerAction((AddServerAction)action);
                            break;
                        case TransitionActionType.RemoveServer:
                            RemoveServerAction((RemoveServerAction)action);
                            break;
                        case TransitionActionType.UpdateClusterDescription:
                            UpdateClusterDescriptionAction((UpdateClusterDescriptionAction)action);
                            break;
                    }
                }
            }

            UpdateClusterDescription(newClusterDescription);
        }

        protected void RemoveServer(IRootServer server)
        {
            server.DescriptionChanged -= ServerDescriptionChangedHandler;
            var endPoint = server.EndPoint;

            lock (Lock)
            {
                _servers.Remove(server);
            }

            server.Dispose();

            if (Listener != null)
            {
                var args = new ServerRemovedEventArgs(endPoint);
                Listener.ServerRemoved(args);
            }
        }

        private void RemoveServerAction(RemoveServerAction action)
        {
            throw new NotImplementedException();
        }

        protected override bool TryGetServer(EndPoint endPoint, out IRootServer server)
        {
            lock (Lock)
            {
                server = _servers.FirstOrDefault(s => s.EndPoint.Equals(endPoint));
                return server != null;
            }
        }

        private void UpdateClusterDescriptionAction(UpdateClusterDescriptionAction action)
        {
            throw new NotImplementedException();
        }
    }
}
