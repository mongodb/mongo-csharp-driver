/* Copyright 2016-present MongoDB Inc.
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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Specifications.sdam_monitoring
{
    public class MonitoringTestRunner : LoggableTestClass
    {
        private ICluster _cluster;
        private EventCapturer _eventSubscriber;
        private MockClusterableServerFactory _serverFactory;

        public MonitoringTestRunner(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;

            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "description", "_path", "phases", "uri");

            _cluster = BuildCluster(definition);
            _cluster.Initialize();

            var phases = definition["phases"].AsBsonArray;
            foreach (BsonDocument phase in phases)
            {
                ApplyPhase(phase);
            }
        }

        private void ApplyPhase(BsonDocument phase)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(phase, "outcome", "responses");

            var responses = phase.GetValue("responses", new BsonArray()).AsBsonArray;
            foreach (BsonArray response in responses)
            {
                ApplyResponse(response);
            }

            var outcome = phase["outcome"].AsBsonDocument;
            VerifyOutcome(outcome);
        }

        private void ApplyResponse(BsonArray response)
        {
            if (response.Count != 2)
            {
                throw new FormatException($"Invalid response count: {response.Count}.");
            }

            var address = response[0].AsString;
            var helloDocument = response[1].AsBsonDocument;
            JsonDrivenHelper.EnsureAllFieldsAreValid(helloDocument, "hosts", "isWritablePrimary", OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, "helloOk", "maxWireVersion", "minWireVersion", "ok", "primary", "secondary", "setName", "setVersion");

            var endPoint = EndPointHelper.Parse(address);
            var helloResult = new HelloResult(helloDocument);
            var currentServerDescription = _serverFactory.GetServerDescription(endPoint);
            var newServerDescription = currentServerDescription.With(
                canonicalEndPoint: helloResult.Me,
                electionId: helloResult.ElectionId,
                replicaSetConfig: helloResult.GetReplicaSetConfig(),
                state: helloResult.Wrapped.GetValue("ok", false).ToBoolean() ? ServerState.Connected : ServerState.Disconnected,
                type: helloResult.ServerType,
                wireVersionRange: new Range<int>(helloResult.MinWireVersion, helloResult.MaxWireVersion));

            var currentClusterDescription = _cluster.Description;
            _serverFactory.PublishDescription(newServerDescription);
            SpinWait.SpinUntil(() => !object.ReferenceEquals(_cluster.Description, currentClusterDescription), 100); // sometimes returns false and that's OK
        }

        private void VerifyOutcome(BsonDocument outcome)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(outcome, "events");

            var expectedEvents = outcome["events"].AsBsonArray;
            foreach (BsonDocument expectedEvent in expectedEvents)
            {
                var actualEvent = _eventSubscriber.Next();
                VerifyEvent(actualEvent, expectedEvent);
            }

            while (_eventSubscriber.Any())
            {
                var extraEvent = _eventSubscriber.Next();
                throw new AssertionException($"Found an extra event of type: {extraEvent.GetType().FullName}.");
            }
        }

        private void VerifyEvent(object actualEvent, BsonDocument expectedEvent)
        {
            if (expectedEvent.ElementCount != 1)
            {
                throw new FormatException($"Invalid event element count: {expectedEvent.ElementCount}.");
            }

            var expectedEventType = expectedEvent.GetElement(0).Name;
            var expectedEventProperties = expectedEvent[0].AsBsonDocument;

            switch (expectedEventType)
            {
                case "topology_opening_event":
                    actualEvent.Should().BeOfType<ClusterOpeningEvent>();
                    VerifyEvent((ClusterOpeningEvent)actualEvent, expectedEventProperties);
                    break;
                case "topology_description_changed_event":
                    actualEvent.Should().BeOfType<ClusterDescriptionChangedEvent>();
                    VerifyEvent((ClusterDescriptionChangedEvent)actualEvent, expectedEventProperties);
                    break;
                case "server_opening_event":
                    actualEvent.Should().BeOfType<ServerOpeningEvent>();
                    VerifyEvent((ServerOpeningEvent)actualEvent, expectedEventProperties);
                    break;
                case "server_closed_event":
                    actualEvent.Should().BeOfType<ServerClosedEvent>();
                    VerifyEvent((ServerClosedEvent)actualEvent, expectedEventProperties);
                    break;
                case "server_description_changed_event":
                    actualEvent.Should().BeOfType<ServerDescriptionChangedEvent>();
                    VerifyEvent((ServerDescriptionChangedEvent)actualEvent, expectedEventProperties);
                    break;
                default:
                    throw new FormatException($"Invalid event type: \"{expectedEventType}\".");
            }
        }

        private void VerifyEvent(ClusterOpeningEvent actualEvent, BsonDocument expectedEvent)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "topologyId");
            actualEvent.ClusterId.Should().Be(_cluster.ClusterId);
        }

        private void VerifyEvent(ClusterDescriptionChangedEvent actualEvent, BsonDocument expectedEvent)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "newDescription", "previousDescription", "topologyId");
            actualEvent.ClusterId.Should().Be(_cluster.ClusterId);
            VerifyClusterDescription(actualEvent.OldDescription, expectedEvent["previousDescription"].AsBsonDocument);
            VerifyClusterDescription(actualEvent.NewDescription, expectedEvent["newDescription"].AsBsonDocument);
        }

        private void VerifyEvent(ServerOpeningEvent actualEvent, BsonDocument expectedEvent)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "address", "topologyId");
            var expectedEndPoint = EndPointHelper.Parse(expectedEvent["address"].AsString);
            actualEvent.ClusterId.Should().Be(_cluster.ClusterId);
            actualEvent.ServerId.EndPoint.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().Be(expectedEndPoint);
        }

        private void VerifyEvent(ServerClosedEvent actualEvent, BsonDocument expectedEvent)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "address", "topologyId");
            var expectedEndPoint = EndPointHelper.Parse(expectedEvent["address"].AsString);
            actualEvent.ClusterId.Should().Be(_cluster.ClusterId);
            actualEvent.ServerId.EndPoint.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().Be(expectedEndPoint);
        }

        private void VerifyEvent(ServerDescriptionChangedEvent actualEvent, BsonDocument expectedEvent)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "address", "newDescription", "previousDescription", "topologyId");
            var expectedEndPoint = EndPointHelper.Parse(expectedEvent["address"].AsString);
            actualEvent.ClusterId.Should().Be(_cluster.ClusterId);
            actualEvent.ServerId.EndPoint.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().Be(expectedEndPoint);
            VerifyServerDescription(actualEvent.OldDescription, expectedEvent["previousDescription"].AsBsonDocument);
            VerifyServerDescription(actualEvent.NewDescription, expectedEvent["newDescription"].AsBsonDocument);
        }

        private void VerifyClusterDescription(ClusterDescription actualDescription, BsonDocument expectedDescription)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedDescription, "servers", "setName", "topologyType");

            var expectedTopologyType = expectedDescription["topologyType"].AsString;
            VerifyTopology(actualDescription, expectedTopologyType);

            var actualEndPoints = actualDescription.Servers.Select(x => x.EndPoint);
            var expectedEndPointDescriptionPairs = expectedDescription["servers"].AsBsonArray.Select(x => new
            {
                EndPoint = EndPointHelper.Parse(x["address"].AsString),
                Description = x.AsBsonDocument
            });
            actualEndPoints.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().BeEquivalentTo(expectedEndPointDescriptionPairs.Select(x => x.EndPoint).WithComparer(EndPointHelper.EndPointEqualityComparer));

            foreach (var actualServerDescription in actualDescription.Servers)
            {
                var expectedServerDescription = expectedEndPointDescriptionPairs.Single(x => EndPointHelper.EndPointEqualityComparer.Equals(x.EndPoint, actualServerDescription.EndPoint)).Description;
                VerifyServerDescription(actualServerDescription, expectedServerDescription);
            }

            if (expectedDescription.Contains("setName"))
            {
                // TODO: assert something against setName
            }
        }

        private void VerifyTopology(ClusterDescription actualDescription, string expectedType)
        {
            switch (expectedType)
            {
                case "Single":
                    break;
                case "ReplicaSetWithPrimary":
                    actualDescription.Type.Should().Be(ClusterType.ReplicaSet);
                    actualDescription.Servers.Should().ContainSingle(x => x.Type == ServerType.ReplicaSetPrimary);
                    break;
                case "ReplicaSetNoPrimary":
                    actualDescription.Type.Should().Be(ClusterType.ReplicaSet);
                    actualDescription.Servers.Should().NotContain(x => x.Type == ServerType.ReplicaSetPrimary);
                    break;
                case "Sharded":
                    actualDescription.Type.Should().Be(ClusterType.Sharded);
                    break;
                case "Unknown":
                    actualDescription.Type.Should().Be(ClusterType.Unknown);
                    break;
                case "LoadBalanced":
                    actualDescription.Type.Should().Be(ClusterType.LoadBalanced);
                    break;
                default:
                    throw new FormatException($"Invalid topology type: \"{expectedType}\".");
            }
        }

        private void VerifyServerDescription(ServerDescription actualDescription, BsonDocument expectedDescription)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedDescription, "address", "arbiters", "hosts", "passives", "primary", "setName", "type");

            var expectedType = expectedDescription["type"].AsString;
            switch (expectedType)
            {
                case "RSPrimary":
                    actualDescription.Type.Should().Be(ServerType.ReplicaSetPrimary);
                    break;
                case "RSSecondary":
                    actualDescription.Type.Should().Be(ServerType.ReplicaSetSecondary);
                    break;
                case "RSArbiter":
                    actualDescription.Type.Should().Be(ServerType.ReplicaSetArbiter);
                    break;
                case "RSGhost":
                    actualDescription.Type.Should().Be(ServerType.ReplicaSetGhost);
                    break;
                case "RSOther":
                    actualDescription.Type.Should().Be(ServerType.ReplicaSetOther);
                    break;
                case "Mongos":
                    actualDescription.Type.Should().Be(ServerType.ShardRouter);
                    break;
                case "Standalone":
                    actualDescription.Type.Should().Be(ServerType.Standalone);
                    break;
                case "LoadBalancer":
                    actualDescription.Type.Should().Be(ServerType.LoadBalanced);
                    break;
                default:
                    actualDescription.Type.Should().Be(ServerType.Unknown);
                    break;
            }

            if (expectedDescription.Contains("setName"))
            {
                string expectedSetName;
                switch (expectedDescription["setName"].BsonType)
                {
                    case BsonType.Null: expectedSetName = null; break;
                    case BsonType.String: expectedSetName = expectedDescription["setName"].AsString; break;
                    default: throw new FormatException($"Invalid setName BSON type: {expectedDescription["setName"].BsonType}.");
                }
                actualDescription.ReplicaSetConfig?.Name.Should().Be(expectedSetName);
            }

            var expectedAddress = expectedDescription["address"].AsString;
            var expectedEndpoint = EndPointHelper.Parse(expectedAddress);
            actualDescription.EndPoint.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().Be(expectedEndpoint);

            if (expectedDescription.Contains("arbiters"))
            {
                // TODO: assert something against arbiters
            }

            if (expectedDescription.Contains("hosts"))
            {
                // TODO: assert something against hosts
            }

            if (expectedDescription.Contains("passives"))
            {
                // TODO: assert something against passives
            }

            if (expectedDescription.Contains("primary"))
            {
                var actualPrimary = actualDescription.ReplicaSetConfig.Primary;
                var expectedPrimary = EndPointHelper.Parse(expectedDescription["primary"].AsString);
                actualPrimary.Should().Be(expectedPrimary);
            }
        }

        private ICluster BuildCluster(BsonDocument definition)
        {
            var connectionString = new ConnectionString(definition["uri"].AsString);
            var settings = new ClusterSettings(
#pragma warning disable CS0618
                connectionModeSwitch: connectionString.ConnectionModeSwitch,
#pragma warning restore CS0618
                endPoints: Optional.Enumerable(connectionString.Hosts),
                replicaSetName: connectionString.ReplicaSet,
                loadBalanced: connectionString.LoadBalanced);
#pragma warning disable CS0618
            if (connectionString.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
            {
                settings = settings.With(directConnection: connectionString.DirectConnection);
            }
            else
            {
                settings = settings.With(connectionMode: connectionString.Connect);
            }
#pragma warning restore CS0618

            _eventSubscriber = new EventCapturer();
            _eventSubscriber.Capture<ClusterOpeningEvent>(e => true);
            _eventSubscriber.Capture<ClusterDescriptionChangedEvent>(e => true);
            _eventSubscriber.Capture<ServerOpeningEvent>(e => true);
            _eventSubscriber.Capture<ServerDescriptionChangedEvent>(e => true);
            _eventSubscriber.Capture<ServerClosedEvent>(e => true);
            _serverFactory = new MockClusterableServerFactory(LoggerFactory, _eventSubscriber);
            return new ClusterFactory(settings, _serverFactory, _eventSubscriber, LoggerFactory)
                .CreateCluster();
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Core.Tests.Specifications.server_discovery_and_monitoring.tests.monitoring.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var name = GetTestCaseName(document, document, 0);
                yield return new JsonDrivenTestCase(name, document, document);
            }

            protected override bool ShouldReadJsonDocument(string path)
            {
                // load balancer support not yet implemented
                return base.ShouldReadJsonDocument(path) && !path.EndsWith("load_balancer.json");
            }
        }
    }
}
