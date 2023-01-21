/* Copyright 2013-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.server_selection
{
    public class ServerSelectionTestRunner
    {
        private ClusterId _clusterId = new ClusterId();

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;

            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "_path", "in_latency_window", "operation", "read_preference", "suitable_servers", "topology_description", "heartbeatFrequencyMS", "error");

            var error = definition.GetValue("error", false).ToBoolean();
            var heartbeatInterval = TimeSpan.FromMilliseconds(definition.GetValue("heartbeatFrequencyMS", 10000).ToInt64());
            var clusterDescription = ServerSelectionTestHelper.BuildClusterDescription((BsonDocument)definition["topology_description"], heartbeatInterval);

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
            var suitableServers = ServerSelectionTestHelper.BuildServerDescriptions((BsonArray)definition["suitable_servers"], _clusterId, heartbeatInterval);
            var selectedServers = selector.SelectServers(clusterDescription, clusterDescription.Servers).ToList();
            AssertServers(suitableServers, selectedServers);

            selector = new CompositeServerSelector(new[] { selector, new LatencyLimitingServerSelector(TimeSpan.FromMilliseconds(15)) });
            var inLatencyWindowServers = ServerSelectionTestHelper.BuildServerDescriptions((BsonArray)definition["in_latency_window"], _clusterId, heartbeatInterval);
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
            JsonDrivenHelper.EnsureAllFieldsAreValid(readPreferenceDescription, "mode", "tag_sets", "maxStalenessSeconds");

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

        private TagSet BuildTagSet(BsonDocument tagSet)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(tagSet, "data_center", "rack", "other_tag");

            return new TagSet(tagSet.Elements.Select(x => new Tag(x.Name, x.Value.ToString())));
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string[] PathPrefixes => new[]
            {
                "MongoDB.Driver.Tests.Specifications.server_selection.tests.server_selection.",
                "MongoDB.Driver.Tests.Specifications.max_staleness.tests."
            };

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var name = GetTestCaseName(document, document, 0);
                yield return new JsonDrivenTestCase(name, document, document);
            }
        }
    }
}
