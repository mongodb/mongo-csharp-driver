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

namespace MongoDB.Driver.Core.Clusters.Monitoring
{
    public class ClusterMonitor
    {
        // methods
        private ClusterType DeduceClusterType(ServerDescription serverDescription)
        {
            switch (serverDescription.Type)
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

        public IEnumerable<TransitionAction> Transition(ClusterDescription oldClusterDescription, ServerDescription newServerDescription)
        {
            switch (oldClusterDescription.Type)
            {
                case ClusterType.ReplicaSet:
                    return TransitionReplicaSet(oldClusterDescription, newServerDescription);
                case ClusterType.Sharded:
                    return TransitionShardedCluster(oldClusterDescription, newServerDescription);
                case ClusterType.Unknown:
                    return TransitionUnknownCluster(oldClusterDescription, newServerDescription);
                default:
                    var message = string.Format("Unexpected cluster type: {0}.", oldClusterDescription.Type);
                    throw new ApplicationException(message);
            }
        }

        public IEnumerable<TransitionAction> TransitionReplicaSet(ClusterDescription oldClusterDescription, ServerDescription newServerDescription)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransitionAction> TransitionShardedCluster(ClusterDescription oldClusterDescription, ServerDescription newServerDescription)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransitionAction> TransitionUnknownCluster(ClusterDescription oldClusterDescription, ServerDescription newServerDescription)
        {
            var clusterType = DeduceClusterType(newServerDescription);
            if (clusterType == ClusterType.Unknown)
            {
                var newClusterDescription = oldClusterDescription.WithServerDescription(newServerDescription);
                var action = new UpdateClusterDescriptionAction(newClusterDescription);
                return new TransitionAction[] { action };
            }

            var knownTypeClusterDescription = oldClusterDescription.WithType(clusterType);
            return Transition(knownTypeClusterDescription, newServerDescription);
        }
    }
}
