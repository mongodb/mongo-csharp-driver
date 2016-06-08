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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Helpers
{
    public class ServerDescriptionHelper
    {
        public static ServerDescription Disconnected(ClusterId clusterId, EndPoint endPoint = null)
        {
            endPoint = endPoint ?? new DnsEndPoint("localhost", 27017);
            return new ServerDescription(new ServerId(clusterId, endPoint), endPoint);
        }

        public static ServerDescription Connected(ClusterId clusterId, EndPoint endPoint = null, ServerType serverType = ServerType.Standalone, TagSet tags = null, TimeSpan? averageRoundTripTime = null, Range<int> wireVersionRange = null)
        {
            return Disconnected(clusterId, endPoint).With(
                averageRoundTripTime: averageRoundTripTime ?? TimeSpan.FromMilliseconds(1),
                replicaSetConfig: null,
                state: ServerState.Connected,
                tags: tags,
                type: serverType,
                version: new SemanticVersion(2, 6, 3),
                wireVersionRange: wireVersionRange ?? new Range<int>(0, 2));
        }
    }
}
