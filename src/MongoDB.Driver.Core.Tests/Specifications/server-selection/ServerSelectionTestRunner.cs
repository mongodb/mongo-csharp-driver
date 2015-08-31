/* Copyright 2013-2015 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;

namespace MongoDB.Driver.Specifications.server_selection
{
    [TestFixture]
    public class ServerSelectionTestRunner
    {
        private ClusterId _clusterId = new ClusterId();

        [TestCaseSource(typeof(TestCaseFactory), "GetTestCases")]
        public void RunTestDefinition(BsonDocument definition)
        {
            var clusterDescription = BuildClusterDescription((BsonDocument)definition["topology_description"]);
            IServerSelector selector;
            if (definition["operation"].ToString() == "write")
            {
                selector = WritableServerSelector.Instance;
            }
            else
            {
                selector = BuildServerSelector((BsonDocument)definition["read_preference"]);
            }
            var suitableServers = BuildServerDescriptions((BsonArray)definition["suitable_servers"]).ToList();
            var selectedServers = selector.SelectServers(clusterDescription, clusterDescription.Servers).ToList();
            AssertServers(suitableServers, selectedServers);

            selector = new CompositeServerSelector(new[] { selector, new LatencyLimitingServerSelector(TimeSpan.FromMilliseconds(15)) });
            var inLatencyWindowServers = BuildServerDescriptions((BsonArray)definition["in_latency_window"]).ToList();
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

        private IServerSelector BuildServerSelector(BsonDocument readPreference)
        {
            return new ReadPreferenceServerSelector(BuildReadPreference(readPreference));
        }

        private ReadPreference BuildReadPreference(BsonDocument readPreferenceDescription)
        {
            var tagSets = ((BsonArray)readPreferenceDescription["tag_sets"]).Select(x => BuildTagSet((BsonDocument)x));

            ReadPreference readPreference;
            switch (readPreferenceDescription["mode"].ToString())
            {
                case "Nearest":
                    readPreference = ReadPreference.Nearest;
                    break;
                case "Primary":
                    // tag sets can't be used with Primary
                    return ReadPreference.Primary;
                case "PrimaryPreferred":
                    readPreference = ReadPreference.PrimaryPreferred;
                    break;
                case "Secondary":
                    readPreference = ReadPreference.Secondary;
                    break;
                case "SecondaryPreferred":
                    readPreference = ReadPreference.SecondaryPreferred;
                    break;
                default:
                    throw new NotSupportedException("Unknown read preference mode: " + readPreferenceDescription["mode"]);
            }

            return readPreference.With(tagSets: tagSets);
        }

        private ClusterDescription BuildClusterDescription(BsonDocument topologyDescription)
        {
            var clusterType = GetClusterType(topologyDescription["type"].ToString());
            var servers = BuildServerDescriptions((BsonArray)topologyDescription["servers"]);

            return new ClusterDescription(_clusterId, ClusterConnectionMode.Automatic, clusterType, servers);
        }

        private IEnumerable<ServerDescription> BuildServerDescriptions(BsonArray serverDescriptions)
        {
            return serverDescriptions.Select(x => BuildServerDescription((BsonDocument)x));
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

        private ServerDescription BuildServerDescription(BsonDocument serverDescription)
        {
            var endPoint = EndPointHelper.Parse(serverDescription["address"].ToString());
            var averageRoundTripTime = TimeSpan.FromMilliseconds(serverDescription["avg_rtt_ms"].ToDouble());
            var type = GetServerType(serverDescription["type"].ToString());
            var tagSet = BuildTagSet((BsonDocument)serverDescription["tag_sets"][0]);

            var serverId = new ServerId(_clusterId, endPoint);
            return new ServerDescription(serverId, endPoint,
                averageRoundTripTime: averageRoundTripTime,
                type: type,
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

        private static class TestCaseFactory
        {
            public static IEnumerable<ITestCaseData> GetTestCases()
            {
                const string prefix = "MongoDB.Driver.Specifications.server_selection.tests.server_selection.";
                return Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path =>
                    {
                        var definition = ReadDefinition(path);
                        var fullName = path.Remove(0, prefix.Length);
                        var data = new TestCaseData(definition);
                        data.Categories.Add("Specifications");
                        data.Categories.Add("server-selection");
                        return data.SetName(fullName.Remove(fullName.Length - 5).Replace(".", "_"));
                    });
            }

            private static BsonDocument ReadDefinition(string path)
            {
                using (var definitionStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
