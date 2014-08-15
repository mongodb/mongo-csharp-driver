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

using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Clusters.ServerSelectors
{
    public class WritableServerSelector : IServerSelector
    {
        // static fields
        public static WritableServerSelector Instance = new WritableServerSelector();

        // constructors
        private WritableServerSelector()
        {
        }

        // methods
        public IEnumerable<ServerDescription> SelectServers(ClusterDescription cluster, IEnumerable<Servers.ServerDescription> servers)
        {
            return servers.Where(x =>
                x.Type == ServerType.ReplicaSetPrimary ||
                x.Type == ServerType.ShardRouter ||
                x.Type == ServerType.Standalone);
        }
    }
}
