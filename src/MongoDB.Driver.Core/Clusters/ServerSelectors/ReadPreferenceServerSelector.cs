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
        public static ReadPreferenceServerSelector Primary
        {
            get { return __primary; }
        }
        #endregion

        // fields
        private readonly TimeSpan _allowedLatencyRange;
        private readonly ReadPreference _readPreference;

        // constructors
        public ReadPreferenceServerSelector(ReadPreference readPreference)
            : this(readPreference, TimeSpan.FromMilliseconds(15))
        {
        }

        public ReadPreferenceServerSelector(ReadPreference readPreference, TimeSpan allowedLatencyRange)
        {
            _readPreference = Ensure.IsNotNull(readPreference, "readPreference");
            _allowedLatencyRange = allowedLatencyRange;
        }

        // properties
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
        }

        // methods
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            IEnumerable<ServerDescription> selectedServers;
            switch (cluster.Type)
            {
                case ClusterType.ReplicaSet: selectedServers = SelectForReplicaSet(servers); break;
                case ClusterType.Sharded: selectedServers = SelectForShardedCluster(servers); break;
                case ClusterType.Standalone: selectedServers = SelectForStandaloneCluster(servers); break;
                default:
                    var message = string.Format("ReadPreferenceServerSelector is not implemented for cluster of type: {0}.", cluster.Type);
                    throw new NotImplementedException(message);
            }

            var minPingTime = selectedServers.Min(s => s.AveragePingTime);
            var maxPingTime = minPingTime.Add(_allowedLatencyRange);
            return selectedServers.Where(s => s.AveragePingTime <= maxPingTime);
        }

        private IReadOnlyList<ServerDescription> SelectByTagSets(IEnumerable<ServerDescription> servers)
        {
            foreach (var tagSet in (_readPreference.TagSets ?? new[] { new TagSet() }))
            {
                var matchingServers = new List<ServerDescription>();
                foreach (var server in servers)
                {
                    if (server.Tags.ContainsAll(tagSet))
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

            switch (_readPreference.Mode)
            {
                case ReadPreferenceMode.Primary:
                    return materializedList.Where(n => n.Type == ServerType.Primary);

                case ReadPreferenceMode.PrimaryPreferred:
                    var primary = materializedList.FirstOrDefault(n => n.Type == ServerType.Primary);
                    if (primary != null)
                    {
                        return new[] { primary };
                    }
                    else
                    {
                        return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.Secondary));
                    }

                case ReadPreferenceMode.Secondary:
                    return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.Secondary));

                case ReadPreferenceMode.SecondaryPreferred:
                    var matchingSecondaries = SelectByTagSets(materializedList.Where(n => n.Type == ServerType.Secondary));
                    if (matchingSecondaries.Count != 0)
                    {
                        return matchingSecondaries;
                    }
                    else
                    {
                        return materializedList.Where(n => n.Type == ServerType.Primary);
                    }

                case ReadPreferenceMode.Nearest:
                    return SelectByTagSets(materializedList.Where(n => n.Type == ServerType.Primary || n.Type == ServerType.Secondary));

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
