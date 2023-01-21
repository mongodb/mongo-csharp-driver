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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Tests.Specifications.server_selection
{
    internal static class ServerSelectionTestHelper
    {
        private enum ClusterTypeTest
        {
            ReplicaSetWithPrimary,
            ReplicaSetNoPrimary,
            Sharded,
            Single,
            Unknown,
            LoadBalanced
        }

        private enum ServerTypeTest
        {
           RSPrimary = ServerType.ReplicaSetPrimary,
           RSSecondary = ServerType.ReplicaSetSecondary,
           RSArbiter = ServerType.ReplicaSetArbiter,
           RSGhost = ServerType.ReplicaSetGhost,
           RSOther = ServerType.ReplicaSetOther,
           Mongos = ServerType.ShardRouter,
           Standalone = ServerType.Standalone,
           Unknown = ServerType.Unknown,
           PossiblePrimary = ServerType.Unknown,
           LoadBalancer = ServerType.LoadBalanced
        }

        public enum ServerTagTest
        {
            data_center,
            rack,
            other_tag
        }

        private sealed class LastWrite
        {
            public long lastWriteDate { get; set; }
        }

        private sealed class ServerData
        {
            public string address { get; set; }
            public int avg_rtt_ms { get; set; }
            public int? lastUpdateTime { get; set; }
            public LastWrite lastWrite { get; set; }
            public int? maxWireVersion { get; set; }
            public ServerTypeTest type { get; set; }

            public Dictionary<ServerTagTest, string> tags { get; set; }
        }

        private sealed class TopologyDescription
        {
            public ServerData[] servers { get; set; }
            public ClusterTypeTest type { get; set; }
        }

        public static ClusterDescription BuildClusterDescription(
            BsonDocument topologyDescriptionDocument,
            TimeSpan? heartbeatInterval = null)
        {
            var clusterId = new ClusterId();

            heartbeatInterval = heartbeatInterval ?? TimeSpan.FromMilliseconds(500);

            var topologyDescription = BsonSerializer.Deserialize<TopologyDescription>(topologyDescriptionDocument);

            var (clusterType, clusterConnectionMode) = GetClusterType(topologyDescription);
            var servers = topologyDescription.servers.Select(x => BuildServerDescription(x, clusterId, heartbeatInterval.Value)).ToArray();

#pragma warning disable CS0618 // Type or member is obsolete
            return new ClusterDescription(clusterId, clusterConnectionMode, clusterType, servers);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // private methods
#pragma warning disable CS0618 // Type or member is obsolete
        private static (ClusterType, ClusterConnectionMode) GetClusterType(TopologyDescription topologyDescription) =>
            topologyDescription.type switch
            {
                ClusterTypeTest.ReplicaSetNoPrimary => (ClusterType.ReplicaSet, ClusterConnectionMode.ReplicaSet),
                ClusterTypeTest.ReplicaSetWithPrimary => (ClusterType.ReplicaSet, ClusterConnectionMode.ReplicaSet),
                ClusterTypeTest.Sharded => (ClusterType.Sharded, ClusterConnectionMode.Sharded),
                ClusterTypeTest.Single => (ClusterType.Standalone, ClusterConnectionMode.Standalone),
                ClusterTypeTest.Unknown => (ClusterType.Unknown, ClusterConnectionMode.Automatic),
                ClusterTypeTest.LoadBalanced => (ClusterType.LoadBalanced, ClusterConnectionMode.Automatic),
                _ => throw new NotSupportedException($"Unknown topology type: {topologyDescription.type}")
            };
#pragma warning restore CS0618 // Type or member is obsolete

        public static List<ServerDescription> BuildServerDescriptions(BsonArray bsonArray, ClusterId clusterId, TimeSpan heartbeatInterval) =>
            bsonArray.Select(x => BuildServerDescription(BsonSerializer.Deserialize<ServerData>((BsonDocument)x), clusterId, heartbeatInterval))
            .ToList();

        private static ServerDescription BuildServerDescription(
            ServerData serverData,
            ClusterId clusterId,
            TimeSpan heartbeatInterval)
        {
            var utcNow = DateTime.UtcNow;

            var endPoint = EndPointHelper.Parse(serverData.address);
            var averageRoundTripTime = TimeSpan.FromMilliseconds(serverData.avg_rtt_ms);
            var type = (ServerType)serverData.type;
            var tagSet = BuildTagSet(serverData);
            var lastWriteTimestamp = serverData.lastWrite != null ? BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(serverData.lastWrite.lastWriteDate) : utcNow;
            var lastUpdateTimestamp = serverData.lastUpdateTime != null ? BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(serverData.lastUpdateTime.Value) : utcNow;

            var maxWireVersion = serverData.maxWireVersion ?? 6;
            var wireVersionRange = new Range<int>(0, maxWireVersion);
            var serverVersion = maxWireVersion == 6 ? new SemanticVersion(3, 6, 0) : new SemanticVersion(3, 2, 0);

            var serverId = new ServerId(clusterId, endPoint);
            return new ServerDescription(
                serverId,
                endPoint,
                averageRoundTripTime: averageRoundTripTime,
                type: type,
                lastUpdateTimestamp: lastUpdateTimestamp,
                lastWriteTimestamp: lastWriteTimestamp,
                heartbeatInterval: heartbeatInterval,
                wireVersionRange: wireVersionRange,
                version: serverVersion,
                tags: tagSet,
                state: ServerState.Connected);
        }

        private static TagSet BuildTagSet(ServerData serverData)
        {
            TagSet result = null;

            if (serverData.tags != null)
            {
                return new TagSet(serverData.tags.Select(x => new Tag(x.Key.ToString(), x.Value)));
            }
            return result;
        }
    }
}
