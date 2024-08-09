/* Copyright 2021-present MongoDB Inc.
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
    internal sealed class OperationsCountServerSelector : IServerSelector
    {
        private readonly IEnumerable<IClusterableServer> _clusterableServers;

        public OperationsCountServerSelector(IEnumerable<IClusterableServer> clusterableServers)
        {
            _clusterableServers = clusterableServers;
        }

        // methods
        /// <inheritdoc/>
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<ServerDescription> servers)
        {
            var list = servers.ToList();
            switch (list.Count)
            {
                case 0:
                case 1:
                    return list;
                default:
                    {
                        // Follow the "Power of Two Choices" approach
                        // https://web.archive.org/web/20191212194243/https://www.nginx.com/blog/nginx-power-of-two-choices-load-balancing-algorithm/
                        var index1 = ThreadStaticRandom.Next(list.Count);
                        var index2 = (index1 + 1 + ThreadStaticRandom.Next(list.Count - 1)) % list.Count;

                        var endpoint1 = list[index1].EndPoint;
                        var endpoint2 = list[index2].EndPoint;
                        var server1 = _clusterableServers.First(s => EndPointHelper.Equals(s.Description.EndPoint, endpoint1));
                        var server2 = _clusterableServers.First(s => EndPointHelper.Equals(s.Description.EndPoint, endpoint2));

                        var selectedServer = server1.OutstandingOperationsCount < server2.OutstandingOperationsCount ? server1 : server2;

                        return new[] { selectedServer.Description };
                    }
            }
        }

        /// <inheritdoc/>
        public override string ToString() =>
            nameof(OperationsCountServerSelector);
    }
}
