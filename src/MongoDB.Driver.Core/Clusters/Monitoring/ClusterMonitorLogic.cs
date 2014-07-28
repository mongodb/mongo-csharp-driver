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
using System.Collections.Generic;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Misc;
using System.Net;

namespace MongoDB.Driver.Core.Clusters.Monitoring
{
    public class ClusterMonitorLogic
    {
        // fields
        private ClusterDescription _newClusterDescription;
        private readonly ServerDescription _newServerDescription;
        private readonly ClusterDescription _oldClusterDescription;
        private List<DnsEndPoint> _serversToAdd = new List<DnsEndPoint>();
        private List<DnsEndPoint> _serversToRemove = new List<DnsEndPoint>();

        // constructors
        public ClusterMonitorLogic(ClusterDescription oldClusterDescription, ServerDescription newServerDescription)
        {
            Ensure.IsNotNull(oldClusterDescription, "oldClusterDescription");
            switch (oldClusterDescription.Type)
            {
                case ClusterType.ReplicaSet:
                case ClusterType.Sharded:
                case ClusterType.Unknown:
                    break;
                default:
                    var message = string.Format("Unexpected cluster type: {0}.", oldClusterDescription.Type);
                    throw new ArgumentException(message, "oldClusterDescription.Type");
            }

            Ensure.IsNotNull(newServerDescription, "newServerDescription");
            if (!oldClusterDescription.Servers.Any(s => s.EndPoint.Equals(newServerDescription.EndPoint)))
            {
                var message = string.Format("Server is not a member of the cluster: '{0}'.", DnsEndPointParser.ToString(newServerDescription.EndPoint));
                throw new ArgumentException(message, "newServerDescription");
            }

            _oldClusterDescription = oldClusterDescription;
            _newServerDescription = newServerDescription;
        }

        // methods
        private IEnumerable<TransitionAction> CreateActions()
        {
            var actions = new List<TransitionAction>();
            if (!_newClusterDescription.Equals(_oldClusterDescription))
            {
                actions.Add(new UpdateClusterDescriptionAction(_newClusterDescription));
            }
            foreach (var server in _serversToAdd)
            {
                actions.Add(new AddServerAction(server));
            }
            foreach (var server in _serversToRemove)
            {
                actions.Add(new RemoveServerAction(server));
            }

            return actions;
        }

        private ClusterType DeduceClusterType(ServerType serverType)
        {
            switch (serverType)
            {
                case ServerType.Arbiter:
                case ServerType.Ghost:
                case ServerType.Other:
                case ServerType.Passive:
                case ServerType.Primary:
                case ServerType.Secondary:
                    return ClusterType.ReplicaSet;
                case ServerType.ShardRouter:
                    return ClusterType.Sharded;
                case ServerType.Standalone:
                    return ClusterType.Standalone;
                default:
                    return ClusterType.Unknown;
            }
        }

        public IEnumerable<TransitionAction> Transition()
        {
            _newClusterDescription = _oldClusterDescription.WithServerDescription(_newServerDescription);

            var clusterType = _newClusterDescription.Type;
            if (clusterType == ClusterType.Unknown)
            {
                clusterType = DeduceClusterType(_newServerDescription.Type);
                _newClusterDescription = _newClusterDescription.WithType(clusterType);
            }

            switch (clusterType)
            {
                case ClusterType.ReplicaSet:
                    TransitionReplicaSet();
                    break;
                case ClusterType.Sharded:
                    TransitionShardedCluster();
                    break;
            }

            return CreateActions();
        }

        private void TransitionReplicaSet()
        {
            throw new NotImplementedException();
        }

        private void TransitionShardedCluster()
        {
            throw new NotImplementedException();
        }
    }
}
