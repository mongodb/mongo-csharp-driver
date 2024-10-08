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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters
{
    internal sealed class MultiServerCluster : Cluster, IDnsMonitoringCluster
    {
        private readonly IDnsMonitorFactory _dnsMonitorFactory;
        private Thread _dnsMonitorThread;
        private readonly CancellationTokenSource _monitorServersCancellationTokenSource;
        private volatile ElectionInfo _maxElectionInfo;
        private volatile string _replicaSetName;
        private readonly List<IClusterableServer> _servers;
        private readonly object _serversLock = new object();
        private readonly InterlockedInt32 _state;
        private readonly object _updateClusterDescriptionLock = new();

        public MultiServerCluster(
            ClusterSettings settings,
            IClusterableServerFactory serverFactory,
            IEventSubscriber eventSubscriber,
            ILoggerFactory loggerFactory,
            IDnsMonitorFactory dnsMonitorFactory = null)
            : base(settings, serverFactory, eventSubscriber, loggerFactory)
        {
            Ensure.IsGreaterThanZero(settings.EndPoints.Count, nameof(settings.EndPoints.Count));
            Ensure.That(!settings.DirectConnection, $"DirectConnection is not supported for a {nameof(MultiServerCluster)}.");

            _dnsMonitorFactory = dnsMonitorFactory ?? new DnsMonitorFactory(eventSubscriber, loggerFactory);
            _monitorServersCancellationTokenSource = new CancellationTokenSource();
            _servers = new List<IClusterableServer>();
            _state = new InterlockedInt32(State.Initial);
            _replicaSetName = settings.ReplicaSetName;
            _maxElectionInfo = new ElectionInfo(null, null);
        }

        protected override void Dispose(bool disposing)
        {
            Stopwatch stopwatch = null;
            if (_state.TryChange(State.Disposed))
            {
                if (disposing)
                {
                    _clusterEventLogger.LogAndPublish(new ClusterClosingEvent(ClusterId));

                    stopwatch = Stopwatch.StartNew();
                    _monitorServersCancellationTokenSource.Cancel();
                    _monitorServersCancellationTokenSource.Dispose();
                    var clusterDescription = Description;
                    lock (_serversLock)
                    {
                        foreach (var server in _servers.ToList())
                        {
                            RemoveServer(clusterDescription, server.EndPoint, "The cluster is closing.");
                        }
                    }
                    stopwatch.Stop();
                }
            }

            base.Dispose(disposing);

            if (stopwatch != null)
            {
                _clusterEventLogger.LogAndPublish(new ClusterClosedEvent(ClusterId, stopwatch.Elapsed));
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            if (_state.TryChange(State.Initial, State.Open))
            {
                _clusterEventLogger.LogAndPublish(new ClusterOpeningEvent(ClusterId, Settings));

                var stopwatch = Stopwatch.StartNew();

                var newServers = new List<IClusterableServer>();
                lock (_updateClusterDescriptionLock)
                {
                    // We lock here even though AddServer locks. Monitors
                    // are re-entrant such that this won't cause problems,
                    // but could prevent issues of conflicting reports
                    // from servers that are quick to respond.
                    var clusterDescription = Description.WithType(Settings.GetInitialClusterType());
                    if (Settings.Scheme != ConnectionStringScheme.MongoDBPlusSrv)
                    {
                        lock (_serversLock)
                        {
                            foreach (var endPoint in Settings.EndPoints)
                            {
                                clusterDescription = EnsureServer(clusterDescription, endPoint, newServers);
                            }
                        }
                    }

                    stopwatch.Stop();

                    UpdateClusterDescription(clusterDescription);
                }

                foreach (var server in newServers)
                {
                    server.Initialize();
                }

                _clusterEventLogger.LogAndPublish(new ClusterOpenedEvent(ClusterId, Settings, stopwatch.Elapsed));

                if (Settings.Scheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    var dnsEndPoint = (DnsEndPoint)Settings.EndPoints.Single();
                    var lookupDomainName = dnsEndPoint.Host;
                    var dnsMonitor = _dnsMonitorFactory.CreateDnsMonitor(this, Settings.SrvServiceName, lookupDomainName, _monitorServersCancellationTokenSource.Token);
                    _dnsMonitorThread = dnsMonitor.Start(); // store the Thread for use as evidence when testing that the DnsMonitor was started
                }
            }
        }

        private bool IsServerValidForCluster(ClusterType clusterType, ServerType serverType)
        {
            switch (clusterType)
            {
                case ClusterType.Standalone:
                    return serverType == ServerType.Standalone;
                case ClusterType.ReplicaSet:
                    return serverType.IsReplicaSetMember();
                case ClusterType.Sharded:
                    return serverType == ServerType.ShardRouter;
                case ClusterType.Unknown:
                    if (serverType == ServerType.Standalone)
                    {
                        return _servers.Count == 1 || Settings.Scheme == ConnectionStringScheme.MongoDBPlusSrv; // Standalone is only valid in MultiServerCluster when using MongoDBPlusSrv scheme
                    }
                    return serverType.IsReplicaSetMember() || serverType == ServerType.ShardRouter;
                default:
                    throw new MongoInternalException("Unexpected cluster type.");
            }
        }

        protected override void RequestHeartbeat()
        {
            List<IClusterableServer> servers;
            lock (_serversLock)
            {
                servers = _servers.ToList();
            }

            foreach (var server in servers)
            {
                if (server.IsInitialized)
                {
                    try
                    {
                        server.RequestHeartbeat();
                    }
                    catch (ObjectDisposedException)
                    {
                        // There is a possible race condition here
                        // due to the fact that we are working
                        // with the server outside of the lock,
                        // meaning another thread could remove
                        // the server and dispose of it before
                        // we invoke the method.
                    }
                }
            }
        }

        private void ServerDescriptionChangedHandler(object sender, ServerDescriptionChangedEventArgs args)
        {
            try
            {
                ProcessServerDescriptionChanged(args);
            }
            catch (Exception unexpectedException)
            {
                // if we catch an exception here it's because of a bug in the driver
                _clusterEventLogger.LogAndPublish(
                    unexpectedException,
                    new SdamInformationEvent("Unexpected exception in MultiServerCluster.ServerDescriptionChangedHandler: {0}", unexpectedException));
            }
            // TODO: should we reset the cluster state in some way? (the state is undefined since an unexpected exception was thrown)
        }

        private void ProcessServerDescriptionChanged(ServerDescriptionChangedEventArgs args)
        {
            var newServers = new List<IClusterableServer>();
            lock (_updateClusterDescriptionLock)
            {
                var newServerDescription = args.NewServerDescription;
                var newClusterDescription = Description;

                if (!_servers.Any(x => EndPointHelper.Equals(x.EndPoint, newServerDescription.EndPoint)))
                {
                    return;
                }

                if (newServerDescription.State == ServerState.Disconnected)
                {
                    newClusterDescription = newClusterDescription.WithServerDescription(newServerDescription);
                }
                else
                {
                    if (IsServerValidForCluster(newClusterDescription.Type, newServerDescription.Type))
                    {
                        if (newClusterDescription.Type == ClusterType.Unknown)
                        {
                            if (newServerDescription.Type != ServerType.ReplicaSetGhost)
                            {
                                // Unknown cluster description with ServerType.ReplicaSetGhost should be untouched
                                newClusterDescription = newClusterDescription.WithType(newServerDescription.Type.ToClusterType());
                            }
                        }

                        switch (newClusterDescription.Type)
                        {
                            case ClusterType.Standalone:
                                newClusterDescription = ProcessStandaloneChange(newClusterDescription, args);
                                break;

                            case ClusterType.ReplicaSet:
                            case ClusterType.Unknown when newServerDescription.Type == ServerType.ReplicaSetGhost: // update serverDescription in clusterDescription
                                newClusterDescription = ProcessReplicaSetChange(newClusterDescription, args, newServers);
                                break;

                            case ClusterType.Sharded:
                                newClusterDescription = ProcessShardedChange(newClusterDescription, args);
                                break;

                            default:
                                throw new MongoInternalException("Unexpected cluster type.");
                        }
                    }
                    else
                    {
                        var reason = $"The server {newServerDescription.EndPoint} with type {newServerDescription.Type} is not valid for cluster type {newClusterDescription.Type}.";
                        newClusterDescription = RemoveServer(newClusterDescription, newServerDescription.EndPoint, reason);
                    }
                }

                var shouldClusterDescriptionChangedEventBePublished = !args.OldServerDescription.SdamEquals(args.NewServerDescription);
                UpdateClusterDescription(newClusterDescription, shouldClusterDescriptionChangedEventBePublished);
            }

            foreach (var server in newServers)
            {
                server.Initialize();
            }
        }

        private ClusterDescription ProcessReplicaSetChange(ClusterDescription clusterDescription, ServerDescriptionChangedEventArgs args, List<IClusterableServer> newServers)
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
            clusterDescription = EnsureServers(clusterDescription, args.NewServerDescription, newServers);

            if (args.NewServerDescription.CanonicalEndPoint != null &&
                !EndPointHelper.Equals(args.NewServerDescription.CanonicalEndPoint, args.NewServerDescription.EndPoint) &&
                args.NewServerDescription.Type != ServerType.ReplicaSetPrimary)
            {
                return RemoveServer(clusterDescription, args.NewServerDescription.EndPoint, "CanonicalEndPoint is different than seed list EndPoint and server is not Primary.");
            }

            if (args.NewServerDescription.Type == ServerType.ReplicaSetPrimary)
            {
                var newElectionId = args.NewServerDescription.ElectionId;
                var newSetVersion = args.NewServerDescription.ReplicaSetConfig.Version;
                var maxWireVersion = args.NewServerDescription.MaxWireVersion;

                if (newElectionId != null || newSetVersion != null)
                {
                    var newMaxElectionInfo = _maxElectionInfo.Compare(newSetVersion, newElectionId, maxWireVersion);

                    if (newMaxElectionInfo == null) // reported stale primary
                    {
                        // we only invalidate the "newly" reported stale primary if electionId was used.
                        lock (_serversLock)
                        {
                            var server = _servers.SingleOrDefault(x => EndPointHelper.Equals(args.NewServerDescription.EndPoint, x.EndPoint));
                            server.Invalidate("ReportedPrimaryIsStale", args.NewServerDescription.TopologyVersion);

                            _clusterEventLogger.LogAndPublish(new SdamInformationEvent(
                                @"Invalidating server: Setting ServerType to ""Unknown"" for {0} because it " +
                                @"claimed to be the replica set primary for replica set ""{1}"" but sent a " +
                                @"(setVersion, electionId) tuple of ({2}, {3}) that was less than than the " +
                                @"largest tuple seen, (maxSetVersion, maxElectionId), of ({4}, {5}).",
                                args.NewServerDescription.EndPoint,
                                args.NewServerDescription.ReplicaSetConfig.Name,
                                newSetVersion,
                                newElectionId,
                                _maxElectionInfo.SetVersion,
                                _maxElectionInfo.ElectionId));

                            return clusterDescription.WithServerDescription(
                                new ServerDescription(server.ServerId, server.EndPoint, "ReportedPrimaryIsStale"));
                        }
                    }

                    if (!_maxElectionInfo.Equals(newMaxElectionInfo))
                    {
                        SdamInformationEvent sdamEvent;
                        if (_maxElectionInfo.SetVersion == null && _maxElectionInfo.ElectionId == null)
                        {
                            sdamEvent = new SdamInformationEvent(
                                @"Initializing (maxSetVersion, maxElectionId): Saving tuple " +
                                @"(setVersion, electionId) of ({0}, {1}) as (maxSetVersion, maxElectionId) for " +
                                @"replica set ""{2}"" because replica set primary {3} sent ({0}, {1}), the first " +
                                @"(setVersion, electionId) tuple ever seen for replica set ""{4}"".",
                                newMaxElectionInfo.SetVersion,
                                newMaxElectionInfo.ElectionId,
                                args.NewServerDescription.ReplicaSetConfig.Name,
                                args.NewServerDescription.EndPoint,
                                args.NewServerDescription.ReplicaSetConfig.Name);
                        }
                        else
                        {
                            sdamEvent = new SdamInformationEvent(
                                @"Updating stale electionId and setVersion: Updating the current " +
                                @"(maxSetVersion, maxElectionId) tuple from ({0}, {1}) to ({2}, {3}) for " +
                                @"replica set ""{4}"" because replica set primary {5} sent ({6}, {7})—a larger " +
                                @"(setVersion, electionId) tuple then the saved tuple, ({0}, {1}).",
                                _maxElectionInfo.SetVersion,
                                _maxElectionInfo.ElectionId,
                                newMaxElectionInfo.SetVersion,
                                newMaxElectionInfo.ElectionId,
                                args.NewServerDescription.ReplicaSetConfig.Name,
                                args.NewServerDescription.EndPoint,
                                args.NewServerDescription.ReplicaSetConfig.Version,
                                args.NewServerDescription.ElectionId);
                        }

                        _clusterEventLogger.LogAndPublish(sdamEvent);
                        _maxElectionInfo = newMaxElectionInfo;
                    }
                }

                var currentPrimaryEndPoints = clusterDescription.Servers
                    .Where(x => x.Type == ServerType.ReplicaSetPrimary)
                    .Where(x => !EndPointHelper.Equals(x.EndPoint, args.NewServerDescription.EndPoint))
                    .Select(x => x.EndPoint)
                    .ToList();

                if (currentPrimaryEndPoints.Count > 0)
                {
                    lock (_serversLock)
                    {
                        var currentPrimaries = _servers.Where(x => EndPointHelper.Contains(currentPrimaryEndPoints, x.EndPoint));
                        foreach (var currentPrimary in currentPrimaries)
                        {
                            // kick off the server to invalidate itself
                            currentPrimary.Invalidate("NoLongerPrimary", args.NewServerDescription.TopologyVersion);
                            // set it to disconnected in the cluster
                            clusterDescription = clusterDescription.WithServerDescription(
                                new ServerDescription(currentPrimary.ServerId, currentPrimary.EndPoint, "NoLongerPrimary"));
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

        private ClusterDescription ProcessStandaloneChange(ClusterDescription clusterDescription, ServerDescriptionChangedEventArgs args)
        {
            if (args.NewServerDescription.Type != ServerType.Unknown)
            {
                if (args.NewServerDescription.Type == ServerType.Standalone)
                {
                    foreach (var endPoint in clusterDescription.Servers.Select(s => s.EndPoint).ToList())
                    {
                        if (!EndPointHelper.Equals(endPoint, args.NewServerDescription.EndPoint))
                        {
                            clusterDescription = RemoveServer(clusterDescription, endPoint, "Removing all other end points once a standalone is discovered.");
                        }
                    }
                }
                else
                {
                    return RemoveServer(clusterDescription, args.NewServerDescription.EndPoint, "Server is not a standalone server.");
                }
            }

            return clusterDescription.WithServerDescription(args.NewServerDescription);
        }

        void IDnsMonitoringCluster.ProcessDnsException(Exception exception)
        {
            lock (_updateClusterDescriptionLock)
            {
                var newClusterDescription = Description.WithDnsMonitorException(exception);
                UpdateClusterDescription(newClusterDescription);
            }
        }

        void IDnsMonitoringCluster.ProcessDnsResults(List<DnsEndPoint> dnsEndPoints)
        {
            if (dnsEndPoints.Count == 0)
            {
                return;
            }

            // Assuming that before this method one from the following conditions has been validated:
            // 1. The cluster type is Unknown or Sharded.
            // 2. This method has been called the first time.
            // Otherwise, the below code should not be called.
            var newServers = new List<IClusterableServer>();
            lock (_updateClusterDescriptionLock)
            {
                var oldClusterDescription = Description;

                var newClusterDescription = oldClusterDescription;
                var currentEndPoints = oldClusterDescription.Servers.Select(serverDescription => serverDescription.EndPoint).ToList();

                var endPointsToRemove = currentEndPoints.Where(endPoint => !dnsEndPoints.Contains(endPoint));
                foreach (var endPoint in endPointsToRemove)
                {
                    newClusterDescription = RemoveServer(newClusterDescription, endPoint, "Server no longer appears in the DNS SRV records.");
                }

                var endPointsToAdd = dnsEndPoints.Where(endPoint => !currentEndPoints.Contains(endPoint)).ToList();
                var srvMaxHosts = Settings.SrvMaxHosts;
                if (srvMaxHosts > 0)
                {
                    FisherYatesShuffle.Shuffle(endPointsToAdd);
                    endPointsToAdd = endPointsToAdd.Take(srvMaxHosts - currentEndPoints.Count + endPointsToRemove.Count()).ToList();
                }
                foreach (var endPoint in endPointsToAdd)
                {
                    newClusterDescription = EnsureServer(newClusterDescription, endPoint, newServers);
                }

                newClusterDescription = newClusterDescription.WithDnsMonitorException(null);
                UpdateClusterDescription(newClusterDescription);
            }

            foreach (var addedServer in newServers)
            {
                addedServer.Initialize();
            }
        }

        bool IDnsMonitoringCluster.ShouldDnsMonitorStop()
        {
            var clusterType = Description.Type;
            return clusterType != ClusterType.Unknown && clusterType != ClusterType.Sharded;
        }

        private ClusterDescription EnsureServer(ClusterDescription clusterDescription, EndPoint endPoint, List<IClusterableServer> newServers)
        {
            if (_state.Value == State.Disposed)
            {
                return clusterDescription;
            }

            IClusterableServer server;
            Stopwatch stopwatch = new Stopwatch();
            lock (_serversLock)
            {
                if (_servers.Any(n => EndPointHelper.Equals(n.EndPoint, endPoint)))
                {
                    return clusterDescription;
                }

                _clusterEventLogger.LogAndPublish(new ClusterAddingServerEvent(ClusterId, endPoint));

                stopwatch.Start();
                server = CreateServer(endPoint);
                server.DescriptionChanged += ServerDescriptionChangedHandler;
                _servers.Add(server);
                newServers.Add(server);
            }

            clusterDescription = clusterDescription.WithServerDescription(server.Description);
            stopwatch.Stop();

            _clusterEventLogger.LogAndPublish(new ClusterAddedServerEvent(server.ServerId, stopwatch.Elapsed));

            return clusterDescription;
        }

        private ClusterDescription EnsureServers(ClusterDescription clusterDescription, ServerDescription serverDescription, List<IClusterableServer> newServers)
        {
            if (serverDescription.Type == ServerType.ReplicaSetPrimary ||
                !clusterDescription.Servers.Any(x => x.Type == ServerType.ReplicaSetPrimary))
            {
                foreach (var endPoint in serverDescription.ReplicaSetConfig.Members)
                {
                    clusterDescription = EnsureServer(clusterDescription, endPoint, newServers);
                }
            }

            if (serverDescription.Type == ServerType.ReplicaSetPrimary)
            {
                var requiredEndPoints = serverDescription.ReplicaSetConfig.Members;
                var extraEndPoints = clusterDescription.Servers.Where(x => !EndPointHelper.Contains(requiredEndPoints, x.EndPoint)).Select(x => x.EndPoint);
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
            var stopwatch = new Stopwatch();
            lock (_serversLock)
            {
                server = _servers.SingleOrDefault(x => EndPointHelper.Equals(x.EndPoint, endPoint));
                if (server == null)
                {
                    return clusterDescription;
                }

                _clusterEventLogger.LogAndPublish(new ClusterRemovingServerEvent(server.ServerId, reason));

                stopwatch.Start();
                _servers.Remove(server);
            }

            server.DescriptionChanged -= ServerDescriptionChangedHandler;
            server.Dispose();
            stopwatch.Stop();

            _clusterEventLogger.LogAndPublish(new ClusterRemovedServerEvent(server.ServerId, reason, stopwatch.Elapsed));

            return clusterDescription.WithoutServerDescription(endPoint);
        }

        protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
        {
            lock (_serversLock)
            {
                server = _servers.FirstOrDefault(s => EndPointHelper.Equals(s.EndPoint, endPoint));
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

        private sealed class ElectionInfo
        {
            private readonly int? _setVersion;
            private readonly ElectionId _electionId;

            public ElectionInfo(int? setVersion, ElectionId electionId)
            {
                _setVersion = setVersion;
                _electionId = electionId;
            }

            public int? SetVersion => _setVersion;
            public ElectionId ElectionId => _electionId;

            public ElectionInfo Compare(int? setVersion, ElectionId electionId, int maxWireVersion)
            {
                var newElectionId = _electionId;
                var newSetVersion = _setVersion;

                if (Feature.ElectionIdPriorityInSDAM.IsSupported(maxWireVersion))
                {
                    var electionIdOrder = Compare(electionId);
                    var isGreaterOrEqual = electionIdOrder < 0 || electionIdOrder == 0 && IsGreaterOrEqual(setVersion, _setVersion);

                    if (isGreaterOrEqual)
                    {
                        newElectionId = electionId;
                        newSetVersion = setVersion;
                    }
                    else
                    {
                        // stale primary
                        return null;
                    }
                }
                else
                {
                    if (setVersion != null && electionId != null)
                    {
                        if (_setVersion != null &&
                            _electionId != null &&
                            (_setVersion > setVersion || (_setVersion == setVersion && Compare(electionId) > 0)))
                        {
                            // stale primary
                            return null;
                        }

                        newElectionId = electionId;
                    }

                    if (IsGreater(setVersion, _setVersion))
                    {
                        newSetVersion = setVersion;
                    }
                }

                return new(newSetVersion, newElectionId);
            }

            public override bool Equals(object obj)
            {
                var other = obj as ElectionInfo;

                return other != null &&
                    _setVersion == other.SetVersion &&
                    object.Equals(_electionId, other.ElectionId);
            }

            private int Compare(ElectionId electionId)
            {
                if (_electionId == null)
                {
                    return electionId == null ? 0 : -1;
                }

                return _electionId.CompareTo(electionId);
            }

            public override int GetHashCode() => base.GetHashCode();

            private bool IsGreater(int? left, int? right) =>
                (left ?? int.MinValue) > (right ?? int.MinValue); // null is considered less than non null value

            private bool IsGreaterOrEqual(int? left, int? right) =>
                (left ?? int.MinValue) >= (right ?? int.MinValue); // null is considered less than non null value
        }
    }
}
