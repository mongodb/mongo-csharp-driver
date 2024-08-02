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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    /// <summary>
    /// Represents a server selector that selects servers based on a collection of servers to deprioritize.
    /// </summary>
    public sealed class PriorityServerSelector : IServerSelector
    {
        private readonly IReadOnlyCollection<ServerDescription> _deprioritizedServers;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityServerSelector"/> class.
        /// </summary>
        /// <param name="deprioritizedServers">The collection of servers to deprioritize.</param>
        public PriorityServerSelector(IReadOnlyCollection<ServerDescription> deprioritizedServers)
        {
            _deprioritizedServers = Ensure.IsNotNullOrEmpty(deprioritizedServers, nameof(deprioritizedServers)) as IReadOnlyCollection<ServerDescription>;
        }

        /// <inheritdoc />
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            // according to spec, we only do deprioritization in a sharded cluster.
            if (cluster.Type != ClusterType.Sharded)
            {
                return servers;
            }

            var filteredServers = servers.Where(description => _deprioritizedServers.All(d => d.EndPoint != description.EndPoint)).ToList();

            return filteredServers.Any() ? filteredServers : servers;
        }

        /// <inheritdoc/>
        public override string ToString() => $"PriorityServerSelector{{{{ Deprioritized servers: {string.Join(", ", _deprioritizedServers.Select(s => s.EndPoint))} }}}}";
    }
}
