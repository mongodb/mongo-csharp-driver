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
using System.Net;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.TestHelpers
{
    public static class ServerDescriptionParser
    {
        public static ServerDescription Parse(BsonDocument args)
        {
            var clusterId = new ClusterId(args.GetValue("clusterId", 1).ToInt32());
            var endPoint = ParseEndPoint(args.GetValue("endPoint", "localhost:27017").AsString);
            var serverId = new ServerId(clusterId, endPoint);
            var logicalSessionTimeoutMinutes = args.GetValue("logicalSessionTimeoutMinutes", BsonNull.Value);
            var logicalSessionTimeout = logicalSessionTimeoutMinutes.IsBsonNull ? (TimeSpan?)null : TimeSpan.FromMinutes(logicalSessionTimeoutMinutes.ToInt32());
            var state = (ServerState)Enum.Parse(typeof(ServerState), args["state"].AsString);
            var serverType = (ServerType)Enum.Parse(typeof(ServerType), args["type"].AsString);

            return new ServerDescription(
                serverId,
                endPoint,
                logicalSessionTimeout: logicalSessionTimeout,
                state: state,
                type: serverType);
        }

        public static ServerDescription Parse(string json)
        {
            var args = BsonDocument.Parse(json);
            return Parse(args);
        }

        // private methods
        private static EndPoint ParseEndPoint(string value)
        {
            var colon = value.IndexOf(':');
            if (colon == -1)
            {
                return new DnsEndPoint(value, 27017);
            }
            else
            {
                var host = value.Substring(0, colon);
                var port = int.Parse(value.Substring(colon + 1));
                return new DnsEndPoint(host, port);
            }
        }
    }
}
