using System;
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    public class ClusterListenerPair : IClusterListener
    {
        // static
        public static IClusterListener Create(IClusterListener first, IClusterListener second)
        {
            if (first == null)
            {
                return second;
            }

            if (second == null)
            {
                return first;
            }

            return new ClusterListenerPair(first, second);
        }

        // fields
        private readonly IClusterListener _first;
        private readonly IClusterListener _second;

        // constructors
        public ClusterListenerPair(IClusterListener first, IClusterListener second)
        {
            _first = Ensure.IsNotNull(first, "first");
            _second = Ensure.IsNotNull(second, "second");
        }

        public void ClusterBeforeClosing(ClusterId clusterId)
        {
            _first.ClusterBeforeClosing(clusterId);
            _second.ClusterBeforeClosing(clusterId);
        }

        public void ClusterAfterClosing(ClusterId clusterId, TimeSpan elapsed)
        {
            _first.ClusterAfterClosing(clusterId, elapsed);
            _second.ClusterAfterClosing(clusterId, elapsed);
        }

        public void ClusterBeforeOpening(ClusterId clusterId, ClusterSettings settings)
        {
            _first.ClusterBeforeOpening(clusterId, settings);
            _second.ClusterBeforeOpening(clusterId, settings);
        }

        public void ClusterAfterOpening(ClusterId clusterId, ClusterSettings settings, TimeSpan elapsed)
        {
            _first.ClusterAfterOpening(clusterId, settings, elapsed);
            _second.ClusterAfterOpening(clusterId, settings, elapsed);
        }

        public void ClusterBeforeAddingServer(ClusterId clusterId, EndPoint endPoint)
        {
            _first.ClusterBeforeAddingServer(clusterId, endPoint);
            _second.ClusterBeforeAddingServer(clusterId, endPoint);
        }

        public void ClusterAfterAddingServer(ServerId serverId, TimeSpan elapsed)
        {
            _first.ClusterAfterAddingServer(serverId, elapsed);
            _second.ClusterAfterAddingServer(serverId, elapsed);
        }

        public void ClusterBeforeRemovingServer(ServerId serverId, string reason)
        {
            _first.ClusterBeforeRemovingServer(serverId, reason);
            _second.ClusterBeforeRemovingServer(serverId, reason);
        }

        public void ClusterAfterRemovingServer(ServerId serverId, string reason, TimeSpan elapsed)
        {
            _first.ClusterAfterRemovingServer(serverId, reason, elapsed);
            _second.ClusterAfterRemovingServer(serverId, reason, elapsed);
        }

        public void ClusterDescriptionChanged(ClusterDescription oldClusterDescription, ClusterDescription newClusterDescription)
        {
            _first.ClusterDescriptionChanged(oldClusterDescription, newClusterDescription);
            _second.ClusterDescriptionChanged(oldClusterDescription, newClusterDescription);
        }
    }
}