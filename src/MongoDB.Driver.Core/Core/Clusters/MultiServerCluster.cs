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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    /// <summary>
    /// Represents a multi server cluster.
    /// </summary>
    internal sealed class MultiServerCluster : Cluster
    {
        // fields
        private readonly CancellationTokenSource _monitorServersCancellationTokenSource;
        private volatile string _replicaSetName;
        private readonly AsyncQueue<ServerDescriptionChangedEventArgs> _serverDescriptionChangedQueue;
        private readonly List<IClusterableServer> _servers;
        private readonly object _serversLock = new object();
        private readonly InterlockedInt32 _state;

        // constructors
        public MultiServerCluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IClusterListener listener)
            : base(settings, serverFactory, listener)
        {
            Ensure.IsGreaterThanZero(settings.EndPoints.Count, "settings.EndPoints.Count");
            if (settings.ConnectionMode == ClusterConnectionMode.Standalone)
            {
                throw new ArgumentException("ClusterConnectionMode.StandAlone is not supported for a MultiServerCluster.");
            }
            if (settings.ConnectionMode == ClusterConnectionMode.Direct)
            {
                throw new ArgumentException("ClusterConnectionMode.Direct is not supported for a MultiServerCluster.");
            }

            _monitorServersCancellationTokenSource = new CancellationTokenSource();
            _serverDescriptionChangedQueue = new AsyncQueue<ServerDescriptionChangedEventArgs>();
            _servers = new List<IClusterableServer>();
            _state = new InterlockedInt32(State.Initial);
            _replicaSetName = settings.ReplicaSetName;
        }

        // methods
        protected override void Dispose(bool disposing)
        {
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    if (Listener != null)
                    {
                        Listener.ClusterBeforeClosing(ClusterId);
                    }

                    var stopwatch = Stopwatch.StartNew();
                    _monitorServersCancellationTokenSource.Cancel();
                    _monitorServersCancellationTokenSource.Dispose();
                    var clusterDescription = Description;
                    lock (_serversLock)
                    {
                        foreach (var server in _servers.ToList())
                        {
                            clusterDescription = RemoveServer(clusterDescription, server.EndPoint, "The cluster is closing.");
                        }
                    }
                    UpdateClusterDescription(clusterDescription);
                    stopwatch.Stop();

                    if (Listener != null)
                    {
                        Listener.ClusterAfterClosing(ClusterId, stopwatch.Elapsed);
                    }
                }
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_state.TryChange(State.Initial, State.Open))
            {
                if (Listener != null)
                {
                    Listener.ClusterBeforeOpening(ClusterId, Settings);
                }

                var stopwatch = Stopwatch.StartNew();
                AsyncBackgroundTask.Start(
                    MonitorServersAsync,
                    TimeSpan.Zero,
                    _monitorServersCancellationTokenSource.Token)
                    .HandleUnobservedException(ex => { }); // TODO: do we need to handle any error here?

                // We lock here even though AddServer locks. Monitors
                // are re-entrant such that this won't cause problems,
                // but could prevent issues of conflicting reports
                // from servers that are quick to respond.
                var clusterDescription = Description;
                lock (_serversLock)
                {
                    foreach (var endPoint in Settings.EndPoints)
                    {
                        clusterDescription = EnsureServer(clusterDescription, endPoint);
                    }
                }

                UpdateClusterDescription(clusterDescription);
                stopwatch.Stop();

                if (Listener != null)
                {
                    Listener.ClusterAfterOpening(ClusterId, Settings, stopwatch.Elapsed);
                }
            }
        }

        protected override void Invalidate()
        {
            lock (_serversLock)
            {
                foreach (var server in _servers)
                {
                    server.Invalidate();
                }
            }
        }

        private async Task<bool> MonitorServersAsync(CancellationToken cancellationToken)
        {
            try
            {
                var eventArgs = await _serverDescriptionChangedQueue.DequeueAsync().ConfigureAwait(false); // TODO: add timeout and cancellationToken to DequeueAsync
                ProcessServerDescriptionChanged(eventArgs);
            }
            catch
            {
                // TODO: log this somewhere...
            }

            return true;
        }

        private void ServerDescriptionChangedHandler(object sender, ServerDescriptionChangedEventArgs args)
        {
            _serverDescriptionChangedQueue.Enqueue(args);
        }

        private void ProcessServerDescriptionChanged(ServerDescriptionChangedEventArgs args)
        {
            var currentClusterDescription = Description;
            var currentServerDescription = args.OldServerDescription;
            var newServerDescription = args.NewServerDescription;

            var currentServer = _servers.SingleOrDefault(x => x.EndPoint.Equals(newServerDescription.EndPoint));
            if (currentServer == null)
            {
                return;
            }

            ClusterDescription newClusterDescription;
            if (newServerDescription.State == ServerState.Disconnected)
            {
                newClusterDescription = currentClusterDescription.WithServerDescription(args.NewServerDescription);
            }
            else if (newServerDescription.Type == ServerType.Standalone)
            {
                newClusterDescription = currentClusterDescription.WithoutServerDescription(args.NewServerDescription.EndPoint);
            }
            else
            {
                if (currentClusterDescription.Type == ClusterType.Unknown)
                {
                    currentClusterDescription = currentClusterDescription.WithType(args.NewServerDescription.Type.ToClusterType());
                }

                switch (currentClusterDescription.Type)
                {
                    case ClusterType.ReplicaSet:
                        newClusterDescription = ProcessReplicaSetChange(currentClusterDescription, args);
                        break;
                    case ClusterType.Sharded:
                        newClusterDescription = ProcessShardedChange(currentClusterDescription, args);
                        break;
                    case ClusterType.Standalone:
                        throw new MongoInternalException("MultiServerCluster does not support a standalone state.");
                    default:
                        newClusterDescription = currentClusterDescription.WithServerDescription(newServerDescription);
                        break;
                }
            }

            UpdateClusterDescription(newClusterDescription);
        }

        private ClusterDescription ProcessReplicaSetChange(ClusterDescription clusterDescription, ServerDescriptionChangedEventArgs args)
        {
            if (!args.NewServerDescription.Type.IsReplicaSetMember())
            {
                return RemoveServer(clusterDescription, args.NewServerDescription.EndPoint, string.Format("Server is a {0}, not a replica set member.", args.NewServerDescription.Type));
            }

            if (args.NewServerDescription.Type == ServerType.ReplicaSetGhost)
            {
                return clusterDescription.WithServerDescription(args.NewServerDescription);
            }

            if (_replicaSetName == null)
            {
                _replicaSetName = args.NewServerDescription.ReplicaSetConfig.Name;
            }

            if (_replicaSetName != args.NewServerDescription.ReplicaSetConfig.Name)
            {
                return RemoveServer(clusterDescription, args.NewServerDescription.EndPoint, string.Format("Server was a member of the '{0}' replica set, but should be '{1}'.", args.NewServerDescription.ReplicaSetConfig.Name, _replicaSetName));
            }

            clusterDescription = clusterDescription.WithServerDescription(args.NewServerDescription);
            clusterDescription = EnsureServers(clusterDescription, args.NewServerDescription);

            if (args.NewServerDescription.Type == ServerType.ReplicaSetPrimary &&
                args.OldServerDescription.Type != ServerType.ReplicaSetPrimary)
            {
                var currentPrimaryEndPoints = clusterDescription.Servers
                    .Where(x => x.Type == ServerType.ReplicaSetPrimary)
                    .Where(x => !x.EndPoint.Equals(args.NewServerDescription.EndPoint))
                    .Select(x => x.EndPoint)
                    .ToList();

                if (currentPrimaryEndPoints.Count > 0)
                {
                    lock (_serversLock)
                    {
                        var currentPrimaries = _servers.Where(x => currentPrimaryEndPoints.Contains(x.EndPoint));
                        foreach (var currentPrimary in currentPrimaries)
                        {
                            // kick off the server to invalidate itself
                            currentPrimary.Invalidate();
                            // set it to disconnected in the cluster
                            clusterDescription = clusterDescription.WithServerDescription(
                                new ServerDescription(currentPrimary.ServerId, currentPrimary.EndPoint));
                        }
                    }
                }
            }

            return clusterDescription;
        }

        private ClusterDescription ProcessShardedChange(ClusterDescription clusterDescription, ServerDescriptionChangedEventArgs args)
        {
            if (args.NewServerDescription.Type != ServerType.ShardRouter)
            {
                return RemoveServer(clusterDescription, args.NewServerDescription.EndPoint, "Server is not a shard router.");
            }

            return clusterDescription.WithServerDescription(args.NewServerDescription);
        }

        private ClusterDescription EnsureServer(ClusterDescription clusterDescription, EndPoint endPoint)
        {
            if (_state.Value == State.Disposed)
            {
                return clusterDescription;
            }

            IClusterableServer server;
            Stopwatch stopwatch = new Stopwatch();
            lock (_serversLock)
            {
                if (_servers.Any(n => n.EndPoint.Equals(endPoint)))
                {
                    return clusterDescription;
                }

                if (Listener != null)
                {
                    Listener.ClusterBeforeAddingServer(ClusterId, endPoint);
                }

                stopwatch.Start();
                server = CreateServer(endPoint);
                server.DescriptionChanged += ServerDescriptionChangedHandler;
                _servers.Add(server);
            }

            clusterDescription = clusterDescription.WithServerDescription(server.Description);
            server.Initialize();
            stopwatch.Stop();

            if (Listener != null)
            {
                Listener.ClusterAfterAddingServer(server.ServerId, stopwatch.Elapsed);
            }

            return clusterDescription;
        }

        private ClusterDescription EnsureServers(ClusterDescription clusterDescription, ServerDescription serverDescription)
        {
            if (serverDescription.Type == ServerType.ReplicaSetPrimary ||
                !clusterDescription.Servers.Any(x => x.Type == ServerType.ReplicaSetPrimary))
            {
                foreach (var endPoint in serverDescription.ReplicaSetConfig.Members)
                {
                    clusterDescription = EnsureServer(clusterDescription, endPoint);
                }
            }

            if (serverDescription.Type == ServerType.ReplicaSetPrimary)
            {
                var requiredEndPoints = serverDescription.ReplicaSetConfig.Members;
                var extraEndPoints = clusterDescription.Servers.Where(x => !requiredEndPoints.Contains(x.EndPoint)).Select(x => x.EndPoint);
                foreach (var endPoint in extraEndPoints)
                {
                    clusterDescription = RemoveServer(clusterDescription, endPoint, "Server is not in the host list of the primary.");
                }
            }

            return clusterDescription;
        }

        private ClusterDescription RemoveServer(ClusterDescription clusterDescription, EndPoint endPoint, string reason)
        {
            IClusterableServer server;
            lock (_serversLock)
            {
                server = _servers.SingleOrDefault(x => x.EndPoint.Equals(endPoint));
                if (server == null)
                {
                    return clusterDescription;
                }

                if (Listener != null)
                {
                    Listener.ClusterBeforeRemovingServer(server.ServerId, reason);
                }

                _servers.Remove(server);
            }

            var stopwatch = new Stopwatch();
            server.DescriptionChanged -= ServerDescriptionChangedHandler;
            server.Dispose();
            stopwatch.Stop();

            if (Listener != null)
            {
                Listener.ClusterAfterRemovingServer(server.ServerId, reason, stopwatch.Elapsed);
            }

            return clusterDescription.WithoutServerDescription(endPoint);
        }

        protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
        {
            lock (_serversLock)
            {
                server = _servers.FirstOrDefault(s => s.EndPoint.Equals(endPoint));
                return server != null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_state.Value == State.Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
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
