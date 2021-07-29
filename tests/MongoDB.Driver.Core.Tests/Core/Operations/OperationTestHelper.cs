/* Copyright 2017-present MongoDB Inc.
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

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;

namespace MongoDB.Driver.Core.Operations
{
    public static class OperationTestHelper
    {
        public static IChannelHandle CreateChannel(
            ConnectionDescription connectionDescription = null,
            SemanticVersion serverVersion = null,
            bool supportsSessions = true)
        {
            if (connectionDescription == null)
            {
                connectionDescription = CreateConnectionDescription(
                    serverVersion: serverVersion,
                    supportsSessions: supportsSessions);
            }

            var mock = new Mock<IChannelHandle>();
            mock.SetupGet(m => m.ConnectionDescription).Returns(connectionDescription);
            return mock.Object;
        }

        public static ConnectionDescription CreateConnectionDescription(
            SemanticVersion serverVersion = null,
            ServerType serverType = ServerType.Standalone,
            bool supportsSessions = true)
        {
            if (serverVersion == null)
            {
                serverVersion = new SemanticVersion(3, 6, 0);
            }

            var clusterId = new ClusterId();
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId);
            var buildInfoResult = new BuildInfoResult(new BsonDocument("ok", 1).Add("version", serverVersion.ToString()));
            var helloResult = CreateHelloResult(serverVersion, serverType, supportsSessions);

            return new ConnectionDescription(connectionId, helloResult, buildInfoResult);
        }

        public static ICoreSessionHandle CreateSession(
            bool isCausallyConsistent = false,
            BsonTimestamp operationTime = null,
            bool supportsSession = true)
        {
            var mock = new Mock<ICoreSessionHandle>();
            mock.SetupGet(x => x.IsCausallyConsistent).Returns(isCausallyConsistent);
            mock.SetupGet(x => x.OperationTime).Returns(operationTime);
            return mock.Object;
        }

        private static HelloResult CreateHelloResult(
            SemanticVersion version = null,
            ServerType serverType = ServerType.Standalone,
            bool supportsSessions = true)
        {
            var helloDocument = BsonDocument.Parse("{ ok: 1 }");
            if (supportsSessions)
            {
                helloDocument.Add("logicalSessionTimeoutMinutes", 10);
            }
            switch (serverType)
            {
                case ServerType.ReplicaSetArbiter:
                    helloDocument.Add("setName", "rs");
                    helloDocument.Add("arbiterOnly", true);
                    break;
                case ServerType.ReplicaSetGhost:
                    helloDocument.Add("isreplicaset", true);
                    break;
                case ServerType.ReplicaSetOther:
                    helloDocument.Add("setName", "rs");
                    break;
                case ServerType.ReplicaSetPrimary:
                    helloDocument.Add("setName", "rs");
                    helloDocument.Add(OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, true);
                    break;
                case ServerType.ReplicaSetSecondary:
                    helloDocument.Add("setName", "rs");
                    helloDocument.Add("secondary", true);
                    break;
                case ServerType.ShardRouter:
                    helloDocument.Add("msg", "isdbgrid");
                    break;
            }
            return new HelloResult(helloDocument);
        }
    }
}
