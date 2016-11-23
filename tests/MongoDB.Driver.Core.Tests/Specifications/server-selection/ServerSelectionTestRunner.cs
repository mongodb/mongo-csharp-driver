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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Specifications.server_selection
{
    public class ServerSelectionTestRunner
    {
        private ClusterId _clusterId = new ClusterId();
        private DateTime _utcNow = DateTime.UtcNow;

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(BsonDocument definition)
        {
            var error = definition.GetValue("error", false).ToBoolean();
            var heartbeatInterval = TimeSpan.FromMilliseconds(definition.GetValue("heartbeatFrequencyMS", 10000).ToInt64());
            var clusterDescription = BuildClusterDescription((BsonDocument)definition["topology_description"], heartbeatInterval);
            IServerSelector selector;
            if (definition.GetValue("operation", "read").AsString == "write")
            {
                selector = WritableServerSelector.Instance;
            }
            else
            {
                ReadPreference readPreference;
                try
                {
                    readPreference = BuildReadPreference(definition["read_preference"].AsBsonDocument);
                }
                catch
                {
                    if (error)
                    {
                        return;
                    }
                    throw;
                }

                selector = new ReadPreferenceServerSelector(readPreference);
            }

            if (error)
            {
                RunErrorTest(clusterDescription, selector);
            }
            else
            {
                RunNonErrorTest(definition, clusterDescription, selector, heartbeatInterval);
            }
        }

        private void RunErrorTest(ClusterDescription clusterDescription, IServerSelector selector)
        {
            var exception = Record.Exception(() => selector.SelectServers(clusterDescription, clusterDescription.Servers).ToList());

            exception.Should().NotBeNull();
        }

        private void RunNonErrorTest(BsonDocument definition, ClusterDescription clusterDescription, IServerSelector selector, TimeSpan heartbeatInterval)
        {
            var suitableServers = BuildServerDescriptions((BsonArray)definition["suitable_servers"], heartbeatInterval).ToList();
            var selectedServers = selector.SelectServers(clusterDescription, clusterDescription.Servers).ToList();
            AssertServers(suitableServers, selectedServers);

            selector = new CompositeServerSelector(new[] { selector, new LatencyLimitingServerSelector(TimeSpan.FromMilliseconds(15)) });
            var inLatencyWindowServers = BuildServerDescriptions((BsonArray)definition["in_latency_window"], heartbeatInterval).ToList();
            selectedServers = selector.SelectServers(clusterDescription, clusterDescription.Servers).ToList();
            AssertServers(inLatencyWindowServers, selectedServers);
        }

        private void AssertServers(List<ServerDescription> actual, List<ServerDescription> expected)
        {
            if (expected.Count == 0)
            {
                actual.Count.Should().Be(0);
            }
            else
            {
                actual.Should().OnlyContain(x => expected.Any(y => EndPointHelper.Equals(x.EndPoint, y.EndPoint)));
            }
        }

        private ReadPreference BuildReadPreference(BsonDocument readPreferenceDescription)
        {
            var mode = (ReadPreferenceMode)Enum.Parse(typeof(ReadPreferenceMode), readPreferenceDescription["mode"].AsString);

            IEnumerable<TagSet> tagSets = null;
            if (readPreferenceDescription.Contains("tag_sets"))
            {
                tagSets = ((BsonArray)readPreferenceDescription["tag_sets"]).Select(x => BuildTagSet((BsonDocument)x));
            }

            TimeSpan? maxStaleness = null;
            if (readPreferenceDescription.Contains("maxStalenessSeconds"))
            {
                maxStaleness = TimeSpan.FromSeconds(readPreferenceDescription["maxStalenessSeconds"].ToDouble());
            }

            // work around minor issue in test files
            if (mode == ReadPreferenceMode.Primary && tagSets != null)
            {
                if (tagSets.Count() == 1 && tagSets.First().Tags.Count == 0)
                {
                    tagSets = null;
                }
            }

            return new ReadPreference(mode, tagSets, maxStaleness);
        }

        private ClusterDescription BuildClusterDescription(BsonDocument topologyDescription, TimeSpan heartbeatInterval)
        {
            var clusterType = GetClusterType(topologyDescription["type"].ToString());
            var servers = BuildServerDescriptions((BsonArray)topologyDescription["servers"], heartbeatInterval);

            return new ClusterDescription(_clusterId, ClusterConnectionMode.Automatic, clusterType, servers);
        }

        private IEnumerable<ServerDescription> BuildServerDescriptions(BsonArray serverDescriptions, TimeSpan heartbeatInterval)
        {
            return serverDescriptions.Select(x => BuildServerDescription((BsonDocument)x, heartbeatInterval));
        }

        private ClusterType GetClusterType(string type)
        {
            if (type.StartsWith("ReplicaSet"))
            {
                return ClusterType.ReplicaSet;
            }

            if (type == "Sharded")
            {
                return ClusterType.Sharded;
            }

            if (type == "Single")
            {
                return ClusterType.Standalone;
            }

            if (type == "Unknown")
            {
                return ClusterType.Unknown;
            }

            throw new NotSupportedException("Unknown topology type: " + type);
        }

        private ServerDescription BuildServerDescription(BsonDocument serverDescription, TimeSpan heartbeatInterval)
        {
            var endPoint = EndPointHelper.Parse(serverDescription["address"].ToString());
            var averageRoundTripTime = TimeSpan.FromMilliseconds(serverDescription.GetValue("avg_rtt_ms", 0.0).ToDouble());
            var type = GetServerType(serverDescription["type"].ToString());
            TagSet tagSet = null;
            if (serverDescription.Contains("tags"))
            {
                tagSet = BuildTagSet((BsonDocument)serverDescription["tags"]);
            }
            DateTime lastWriteTimestamp;
            if (serverDescription.Contains("lastWrite"))
            {
                lastWriteTimestamp = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(serverDescription["lastWrite"]["lastWriteDate"].ToInt64());
            }
            else
            {
                lastWriteTimestamp = _utcNow;
            }
            var maxWireVersion = serverDescription.GetValue("maxWireVersion", 5).ToInt32();
            var wireVersionRange = new Range<int>(0, maxWireVersion);
            var serverVersion = maxWireVersion == 5 ? new SemanticVersion(3, 4, 0) : new SemanticVersion(3, 2, 0);
            DateTime lastUpdateTimestamp;
            if (serverDescription.Contains("lastUpdateTime"))
            {
                lastUpdateTimestamp = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(serverDescription.GetValue("lastUpdateTime", 0).ToInt64());
            }
            else
            {
                lastUpdateTimestamp = _utcNow;
            }

            var serverId = new ServerId(_clusterId, endPoint);
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

        private ServerType GetServerType(string type)
        {
            switch (type)
            {
                case "RSPrimary":
                    return ServerType.ReplicaSetPrimary;
                case "RSSecondary":
                    return ServerType.ReplicaSetSecondary;
                case "RSArbiter":
                    return ServerType.ReplicaSetArbiter;
                case "RSGhost":
                    return ServerType.ReplicaSetGhost;
                case "RSOther":
                    return ServerType.ReplicaSetOther;
                case "Mongos":
                    return ServerType.ShardRouter;
                case "Standalone":
                    return ServerType.Standalone;
                default:
                    return ServerType.Unknown;
            }
        }

        private TagSet BuildTagSet(BsonDocument tagSet)
        {
            return new TagSet(tagSet.Elements.Select(x => new Tag(x.Name, x.Value.ToString())));
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.Specifications.server_selection.tests.server_selection.";
                const string maxStalenessPrefix = "MongoDB.Driver.Specifications.max_staleness.tests.";
#else
                const string prefix = "MongoDB.Driver.Core.Tests.Dotnet.Specifications.server_selection.tests.server_selection.";
                const string maxStalenessPrefix = "MongoDB.Driver.Core.Tests.Dotnet.Specifications.max_staleness.tests.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var enumerable = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => (path.StartsWith(prefix) || path.StartsWith(maxStalenessPrefix)) && path.EndsWith(".json"))
                    .Select(path =>
                    {
                        var definition = ReadDefinition(path);
                        definition.InsertAt(0, new BsonElement("path", path));
                        //var data = new TestCaseData(definition);
                        //data.SetCategory("Specifications");
                        //data.SetCategory("server-selection");
                        //var fullName = path.Remove(0, prefix.Length);
                        //data = data.SetName(fullName.Remove(fullName.Length - 5).Replace(".", "_"));
                        var data = new object[] { definition };
                        return data;
                    });
                return enumerable.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static BsonDocument ReadDefinition(string path)
            {
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                using (var definitionStream = executingAssembly.GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
