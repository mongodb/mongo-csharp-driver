/* Copyright 2013-2016 MongoDB Inc.
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
using System.Threading;
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
        private readonly TimeSpan? _maxStaleness; // with Zero and InfiniteTimespan converted to null
        private readonly ReadPreference _readPreference;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadPreferenceServerSelector"/> class.
        /// </summary>
        /// <param name="readPreference">The read preference.</param>
        public ReadPreferenceServerSelector(ReadPreference readPreference)
        {
            _readPreference = Ensure.IsNotNull(readPreference, nameof(readPreference));
            if (readPreference.MaxStaleness == TimeSpan.Zero || readPreference.MaxStaleness == Timeout.InfiniteTimeSpan)
            {
                _maxStaleness = null;
            }
            else
            {
                _maxStaleness = readPreference.MaxStaleness;
            }
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            if (_maxStaleness.HasValue)
            {
                if (cluster.Servers.Any(s => s.Type != ServerType.Unknown && !Feature.MaxStaleness.IsSupported(s.Version)))
                {
                    throw new NotSupportedException("All servers must be version 3.4 or newer to use max staleness.");
                }
            }

            if (cluster.ConnectionMode == ClusterConnectionMode.Direct)
            {
                return servers;
            }

            switch (cluster.Type)
            {
                case ClusterType.ReplicaSet: return SelectForReplicaSet(cluster, servers);
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

        private IEnumerable<ServerDescription> SelectForReplicaSet(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            if (_maxStaleness.HasValue)
            {
                var minHeartBeatIntervalTicks = servers.Select(s => s.HeartbeatInterval.Ticks).Min();
                if (_maxStaleness.Value.Ticks < 2 * minHeartBeatIntervalTicks)
                {
                    throw new MongoClientException("MaxStaleness must be at least twice the heartbeat frequency.");
                }

                servers = new CachedEnumerable<ServerDescription>(SelectFreshServers(cluster, servers)); // prevent multiple enumeration
            }
            else
            {
                servers = new CachedEnumerable<ServerDescription>(servers); // prevent multiple enumeration
            }

            switch (_readPreference.ReadPreferenceMode)
            {
                case ReadPreferenceMode.Primary:
                    return servers.Where(n => n.Type == ServerType.ReplicaSetPrimary);

                case ReadPreferenceMode.PrimaryPreferred:
                    var primary = servers.FirstOrDefault(n => n.Type == ServerType.ReplicaSetPrimary);
                    if (primary != null)
                    {
                        return new[] { primary };
                    }
                    else
                    {
                        return SelectByTagSets(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));
                    }

                case ReadPreferenceMode.Secondary:
                    return SelectByTagSets(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary));

                case ReadPreferenceMode.SecondaryPreferred:
                    var matchingSecondaries = SelectByTagSets(servers.Where(n => n.Type == ServerType.ReplicaSetSecondary)).ToList();
                    if (matchingSecondaries.Count != 0)
                    {
                        return matchingSecondaries;
                    }
                    else
                    {
                        return servers.Where(n => n.Type == ServerType.ReplicaSetPrimary);
                    }

                case ReadPreferenceMode.Nearest:
                    return SelectByTagSets(servers.Where(n => n.Type == ServerType.ReplicaSetPrimary || n.Type == ServerType.ReplicaSetSecondary));

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

        private IReadOnlyList<ServerDescription> SelectFreshServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            var primary = cluster.Servers.SingleOrDefault(s => s.Type == ServerType.ReplicaSetPrimary);
            if (primary == null)
            {
                return SelectFreshServersWithNoPrimary(cluster, servers);
            }
            else
            {
                return SelectFreshServersWithPrimary(cluster, primary, servers);
            }
        }

        private IReadOnlyList<ServerDescription> SelectFreshServersWithNoPrimary(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            var smax = servers
                .Where(s => s.Type == ServerType.ReplicaSetSecondary)
                .OrderByDescending(s => s.LastWriteTimestamp)
                .FirstOrDefault();
            return servers
                .Where(s =>
                {
                    var estimatedStaleness = smax.LastWriteTimestamp.Value - s.LastWriteTimestamp.Value + s.HeartbeatInterval;
                    return estimatedStaleness <= _maxStaleness;
                })
                .ToList();
        }

        private IReadOnlyList<ServerDescription> SelectFreshServersWithPrimary(ClusterDescription cluster, ServerDescription primary, IEnumerable<ServerDescription> servers)
        {
            var p = primary;
            return servers
                .Where(s =>
                {   
                    var estimatedStaleness = (s.LastUpdateTimestamp - s.LastWriteTimestamp.Value) - (p.LastUpdateTimestamp - p.LastWriteTimestamp.Value) + s.HeartbeatInterval;
                    return estimatedStaleness <= _maxStaleness;
                })
                .ToList();
        }
    }
}
