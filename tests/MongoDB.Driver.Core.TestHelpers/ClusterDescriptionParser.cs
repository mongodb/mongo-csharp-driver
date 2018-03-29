/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class ClusterDescriptionParser
    {
        public static ClusterDescription Parse(BsonDocument args)
        {
            var clusterId = new ClusterId(args.GetValue("clusterId", 1).ToInt32());
            var connectionMode = (ClusterConnectionMode)Enum.Parse(typeof(ClusterConnectionMode), args.GetValue("connectionMode", "Automatic").AsString);
            var clusterType = (ClusterType)Enum.Parse(typeof(ClusterType), args["clusterType"].AsString);

            var numberOfServers = args["servers"].AsBsonArray.Count;
            var servers = new List<ServerDescription>(numberOfServers);
            for (var index = 0; index < numberOfServers; index++)
            {
                var serverArgs = args["servers"].AsBsonArray[index].AsBsonDocument;
                if (!serverArgs.Contains("clusterId"))
                {
                    serverArgs["clusterId"] = clusterId.Value;
                }
                if (!serverArgs.Contains("endPoint"))
                {
                    var port = 27017 + index;
                    serverArgs["endPoint"] = $"localhost:{port}";
                }

                var server = ServerDescriptionParser.Parse(serverArgs);
                servers.Add(server);
            }

            return new ClusterDescription(clusterId, connectionMode, clusterType, servers);
        }

        public static ClusterDescription Parse(string json)
        {
            var args = BsonDocument.Parse(json);
            return Parse(args);
        }
    }
}
