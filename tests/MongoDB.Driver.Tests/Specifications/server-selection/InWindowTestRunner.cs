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
using System.Net;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.server_selection
{
    public sealed class InWindowTestRunner : LoggableTestClass
    {
        private sealed class OperationsCount
        {
            public string address { get; set; }
            public int operation_count { get; set; }
        }

        private sealed class Outcome
        {
            public double tolerance { get; set; }
            public IDictionary<string, double> expected_frequencies  { get; set; }
        }

        private sealed class TestData
        {
            public string _path { get; set; }
            public BsonDocument topology_description { get; set; }
            public bool async { get; set; }
            public string description { get; set; }
            public int iterations { get; set; }

            public OperationsCount[] mocked_topology_state { get; set;}
            public Outcome outcome { get; set; }
        }

        public InWindowTestRunner(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var testDefinition = testCase.Test;
            var testData = BsonSerializer.Deserialize<TestData>(testDefinition);
            var clusterDescription = ServerSelectionTestHelper.BuildClusterDescription(testData.topology_description);

            using var cluster = CreateAndSetupCluster(clusterDescription, testData.mocked_topology_state);
            var readPreferenceSelector = new ReadPreferenceServerSelector(ReadPreference.Nearest);

            var selectionHistogram = testData.outcome.expected_frequencies.Keys
                .ToDictionary(s => clusterDescription.Servers.Single(d => d.EndPoint.ToString().EndsWith(s)).ServerId, s => 0);
            var selectionFrequenciesExpected = testData.outcome.expected_frequencies.
                ToDictionary(s => clusterDescription.Servers.Single(d => d.EndPoint.ToString().EndsWith(s.Key)).ServerId, s => s.Value);

            for (int i = 0; i < testData.iterations; i++)
            {
                var (selectedServer, _) = testData.async
                    ? cluster.SelectServerAsync(OperationContext.NoTimeout, readPreferenceSelector).GetAwaiter().GetResult()
                    : cluster.SelectServer(OperationContext.NoTimeout, readPreferenceSelector);

                selectionHistogram[selectedServer.ServerId]++;
            }

            foreach (var pair in selectionHistogram)
            {
                var expectedFrequency = selectionFrequenciesExpected[pair.Key];
                var actualFrequency = pair.Value / (double)testData.iterations;

                actualFrequency.Should().BeInRange(expectedFrequency - testData.outcome.tolerance, expectedFrequency + testData.outcome.tolerance);
            }
        }

        private MultiServerCluster CreateAndSetupCluster(ClusterDescription clusterDescription, OperationsCount[] operationsCounts)
        {
            var endpoints = clusterDescription.Servers.Select(s => s.EndPoint).ToArray();
            var clusterSettings = new ClusterSettings(
                directConnection: clusterDescription.DirectConnection,
                serverSelectionTimeout: TimeSpan.FromSeconds(30),
                endPoints: endpoints);

            var replicaSetConfig = new ReplicaSetConfig(
                endpoints,
                "rs_test",
                clusterDescription.Servers.SingleOrDefault(s => s.Type == ServerType.ReplicaSetPrimary)?.EndPoint,
                null);

            var mockServerFactory = new Mock<IClusterableServerFactory>();
            mockServerFactory
                .Setup(s => s.CreateServer(It.IsAny<ClusterType>(), It.IsAny<ClusterId>(), It.IsAny<IClusterClock>(), It.IsAny<EndPoint>()))
                .Returns<ClusterType, ClusterId, IClusterClock, EndPoint>((_, _, _, endpoint) =>
                {
                    var serverDescriptionDisconnected = clusterDescription.Servers
                        .Single(s => s.EndPoint == endpoint)
                        .With(state: ServerState.Disconnected);

                    if (serverDescriptionDisconnected.Type.IsReplicaSetMember())
                    {
                        serverDescriptionDisconnected = serverDescriptionDisconnected.With(replicaSetConfig: replicaSetConfig);
                    }
                    var serverDescriptionConnected = serverDescriptionDisconnected.With(state: ServerState.Connected);

                    var operationsCount = operationsCounts.Single(o => endpoint.ToString().EndsWith(o.address));

                    var server = new Mock<IClusterableServer>();
                    server.Setup(s => s.ServerId).Returns(serverDescriptionDisconnected.ServerId);
                    server.Setup(s => s.Description).Returns(serverDescriptionDisconnected);
                    server.Setup(s => s.EndPoint).Returns(endpoint);
                    server.Setup(s => s.OutstandingOperationsCount).Returns(operationsCount.operation_count);

                    server.Setup(s => s.Initialize()).Callback(() =>
                        {
                            server.Setup(s => s.Description).Returns(serverDescriptionConnected);
                            server.Raise(m => m.DescriptionChanged += null, new ServerDescriptionChangedEventArgs(serverDescriptionDisconnected, serverDescriptionConnected));
                        });

                    return server.Object;
                });

            var result = new MultiServerCluster(clusterSettings, mockServerFactory.Object, new EventCapturer(), LoggerFactory);
            result.Initialize();
            return result;
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string[] PathPrefixes => new[]
            {
                "MongoDB.Driver.Tests.Specifications.server_selection.tests.in_window.",
            };

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var name = GetTestCaseName(document, document, 0);

                foreach (var async in new[] { false, true })
                {
                    var testDecorated = document.DeepClone().AsBsonDocument.Add("async", async);
                    yield return new JsonDrivenTestCase($"{name}:async={async}", testDecorated, testDecorated);
                }
            }
        }
    }
}
