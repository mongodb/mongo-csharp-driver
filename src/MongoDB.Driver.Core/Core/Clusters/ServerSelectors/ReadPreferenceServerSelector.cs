/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    /// <summary>
    /// Represents a selector that selects servers based on a read preference.
    /// </summary>
    public class ReadPreferenceServerSelector : IServerSelector
    {
        #region static
        // static fields
        private static readonly ServerDescription[] __noServers = new ServerDescription[0];
        private static readonly ReadPreferenceServerSelector __primary = new ReadPreferenceServerSelector(ReadPreference.Primary);

        // static properties
        /// <summary>
        /// Gets a ReadPreferenceServerSelector that selects the Primary.
        /// </summary>
        /// <value>
        /// A server selector.
        /// </value>
        public static ReadPreferenceServerSelector Primary
        {
            get { return __primary; }
        }
        #endregion

        // fields
        private readonly ReadPreference _readPreference;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadPreferenceServerSelector"/> class.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        public ReadPreferenceServerSelector(ReadPreference readPreference)
        {
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            if (cluster.ConnectionMode == ClusterConnectionMode.Direct)
            {
                return servers;
            }

            switch (cluster.Type)
            {
                case ClusterType.ReplicaSet: return SelectForReplicaSet(servers);
                case ClusterType.Sharded: return SelectForShardedCluster(servers);
                case ClusterType.Standalone: return SelectForStandaloneCluster(servers);
                case ClusterType.Unknown: return __noServers;
                default:
                    var message = string.Format("ReadPreferenceServerSelector is not implemented for cluster of type: {0}.", cluster.Type);
                    throw new NotImplementedException(message);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("ReadPreferenceServerSelector{{ ReadPreference = {0} }}", _readPreference);
        }

        private IEnumerable<ServerDescription> SelectByTagSets(IEnumerable<ServerDescription> servers)
        {
            var tagSets = _readPreference.TagSets;
            if (tagSets == null || !tagSets.Any())
            {
                return servers;
            }

            foreach (var tagSet in _readPreference.TagSets)
            {
                var matchingServers = new List<ServerDescription>();
                foreach (var server in servers)
                {
                    if (server.Tags != null && server.Tags.ContainsAll(tagSet))
                    {
                        matchingServers.Add(server);
                    }
                }

                if (matchingServers.Count > 0)
                {
                    return matchingServers;
                }
            }

            return __noServers;
        }

        private IEnumerable<ServerDescription> SelectForReplicaSet(IEnumerable<ServerDescription> servers)
        {
            var materializedList = servers as IReadOnlyList<ServerDescription> ?? servers.ToList();

            switch (_readPreference.ReadPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    return materializedList.Where(n => n.Type == ServerType.ReplicaSetPrimary);

                case ReadPreferenceMode.PrimaryPreferred:
                    var primary = materializedList.FirstOrDefault(n => n.Type == ServerType.ReplicaSetPrimary);
                    if (primary != null)
                    {
                        return new[] { primary };
                    }
                    else
                    {
                        return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    }

                case ReadPreferenceMode.Secondary:
                    return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.ReplicaSetSecondary));

                case ReadPreferenceMode.SecondaryPreferred:
                    var matchingSecondaries = SelectByTagSets(materializedList.Where(n => n.Type == ServerType.ReplicaSetSecondary)).ToList();
                    if (matchingSecondaries.Count != 0)
                    {
                        return matchingSecondaries;
                    }
                    else
                    {
                        return materializedList.Where(n => n.Type == ServerType.ReplicaSetPrimary);
                    }

                case ReadPreferenceMode.Nearest:
                    return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.ReplicaSetPrimary || n.Type == ServerType.ReplicaSetSecondary));

                default:
                    throw new ArgumentException("Invalid ReadPreferenceMode.");
            }
        }

        private IEnumerable<ServerDescription> SelectForShardedCluster(IEnumerable<ServerDescription> servers)
        {
            return servers.Where(n => n.Type == ServerType.ShardRouter); // ReadPreference will be sent to mongos
        }

        private IEnumerable<ServerDescription> SelectForStandaloneCluster(IEnumerable<ServerDescription> servers)
        {
            return servers.Where(n => n.Type == ServerType.Standalone); // standalone servers match any ReadPreference (to facilitate testing)
        }
    }
}
