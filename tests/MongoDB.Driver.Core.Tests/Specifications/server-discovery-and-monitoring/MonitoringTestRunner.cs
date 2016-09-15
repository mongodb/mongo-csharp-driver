/* Copyright 2016 MongoDB Inc.
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
using System.Net;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Xunit;

namespace MongoDB.Driver.Specifications.sdam_monitoring
{
    public class MonitoringTestRunner
    {
        private static Type[] __testedEventTypes = new[]
        {
            typeof(ClusterOpeningEvent),
            typeof(ClusterDescriptionChangedEvent),
            typeof(ServerOpeningEvent),
            typeof(ServerDescriptionChangedEvent),
            typeof(ServerClosedEvent)
        };

        private ICluster _cluster;
        private EventCapturer _eventSubscriber;
        private MockClusterableServerFactory _serverFactory;

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(BsonDocument definition)
        {
            _cluster = BuildCluster(definition);
            _cluster.Initialize();

            var phases = definition["phases"].AsBsonArray;
            foreach (var phase in phases)
            {
                ApplyPhase(phase);
            }
        }

        private void ApplyPhase(BsonValue phase)
        {
            var responses = phase["responses"].AsBsonArray;
            foreach (var response in responses)
            {
                ApplyResponse(response);
            }

            var outcome = (BsonDocument)phase["outcome"];

            VerifyOutcome(outcome);
        }

        private void ApplyResponse(BsonValue response)
        {
            var server = (string)response[0];
            var endPoint = EndPointHelper.Parse(server);
            var isMasterResult = new IsMasterResult((BsonDocument)response[1]);
            var currentServerDescription = _serverFactory.GetServerDescription(endPoint);
            var newServerDescription = currentServerDescription.With(
                state: isMasterResult.Wrapped.GetValue("ok", false).ToBoolean() ? ServerState.Connected : ServerState.Disconnected,
                type: isMasterResult.ServerType,
                canonicalEndPoint: isMasterResult.Me,
                electionId: isMasterResult.ElectionId,
                replicaSetConfig: isMasterResult.GetReplicaSetConfig());

            var currentClusterDescription = _cluster.Description;
            _serverFactory.PublishDescription(newServerDescription);
            SpinWait.SpinUntil(() => !object.ReferenceEquals(_cluster.Description, currentClusterDescription), 100); // sometimes returns false and that's OK
        }

        private void VerifyOutcome(BsonDocument outcome)
        {
            var events = (BsonArray)outcome["events"];
            foreach (var @event in events.OfType<BsonDocument>().Select(x => x.GetElement(0)))
            {
                var next = _eventSubscriber.Next();
                while (!__testedEventTypes.Contains(next.GetType()))
                {
                    next = _eventSubscriber.Next();
                }

                var content = (BsonDocument)@event.Value;
                switch (@event.Name.ToLowerInvariant())
                {
                    case "topology_opening_event":
                        next.Should().BeOfType<ClusterOpeningEvent>();
                        VerifyEvent(content, (ClusterOpeningEvent)next);
                        break;
                    case "topology_description_changed_event":
                        next.Should().BeOfType<ClusterDescriptionChangedEvent>();
                        VerifyEvent(content, (ClusterDescriptionChangedEvent)next);
                        break;
                    case "server_opening_event":
                        next.Should().BeOfType<ServerOpeningEvent>();
                        VerifyEvent(content, (ServerOpeningEvent)next);
                        break;
                    case "server_closed_event":
                        next.Should().BeOfType<ServerClosedEvent>();
                        VerifyEvent(content, (ServerClosedEvent)next);
                        break;
                    case "server_description_changed_event":
                        next.Should().BeOfType<ServerDescriptionChangedEvent>();
                        VerifyEvent(content, (ServerDescriptionChangedEvent)next);
                        break;
                }
            }

            while (_eventSubscriber.Any())
            {
                var next = _eventSubscriber.Next();
                if (!__testedEventTypes.Contains(next.GetType()))
                {
                    continue;
                }

                throw new AssertionException($"Found an extra {next.GetType()}");
            }
        }

        private void VerifyEvent(BsonDocument content, ClusterOpeningEvent @event)
        {
            @event.ClusterId.Should().Be(_cluster.ClusterId);
        }

        private void VerifyEvent(BsonDocument content, ClusterDescriptionChangedEvent @event)
        {
            @event.ClusterId.Should().Be(_cluster.ClusterId);
            VerifyClusterDescription((BsonDocument)content["previousDescription"], @event.OldDescription);
            VerifyClusterDescription((BsonDocument)content["newDescription"], @event.NewDescription);
        }

        private void VerifyEvent(BsonDocument content, ServerOpeningEvent @event)
        {
            @event.ClusterId.Should().Be(_cluster.ClusterId);
            var expectedEndPoint = EndPointHelper.Parse(content["address"].ToString());

            EndPointHelper.EndPointEqualityComparer.Equals(@event.ServerId.EndPoint, expectedEndPoint)
                .Should().BeTrue();
        }

        private void VerifyEvent(BsonDocument content, ServerClosedEvent @event)
        {
            @event.ClusterId.Should().Be(_cluster.ClusterId);
            var expectedEndPoint = EndPointHelper.Parse(content["address"].ToString());

            EndPointHelper.EndPointEqualityComparer.Equals(@event.ServerId.EndPoint, expectedEndPoint)
                .Should().BeTrue();
        }

        private void VerifyEvent(BsonDocument content, ServerDescriptionChangedEvent @event)
        {
            @event.ClusterId.Should().Be(_cluster.ClusterId);
            var expectedEndPoint = EndPointHelper.Parse(content["address"].ToString());

            EndPointHelper.EndPointEqualityComparer.Equals(@event.ServerId.EndPoint, expectedEndPoint)
                .Should().BeTrue();
        }

        private void VerifyClusterDescription(BsonDocument content, ClusterDescription description)
        {
            VerifyTopology((string)content["topologyType"], description);

            var expectedServers = ((BsonArray)content["servers"]).Select(x => new
            {
                EndPoint = EndPointHelper.Parse((string)x["address"]),
                Description = (BsonDocument)x
            });

            var actualServers = description.Servers.Select(x => x.EndPoint);

            actualServers.Should().BeEquivalentTo(expectedServers.Select(x => x.EndPoint));

            foreach (var actualServer in description.Servers)
            {
                var expectedServer = expectedServers.Single(x => x.EndPoint.Equals(actualServer.EndPoint));
                VerifyServer(actualServer, expectedServer.Description);
            }
        }

        private void VerifyTopology(string topologyType, ClusterDescription description)
        {
            switch (topologyType)
            {
                case "Single":
                    break;
                case "ReplicaSetWithPrimary":
                    description.Type.Should().Be(ClusterType.ReplicaSet);
                    description.Servers.Should().ContainSingle(x => x.Type == ServerType.ReplicaSetPrimary);
                    break;
                case "ReplicaSetNoPrimary":
                    description.Type.Should().Be(ClusterType.ReplicaSet);
                    description.Servers.Should().NotContain(x => x.Type == ServerType.ReplicaSetPrimary);
                    break;
                case "Sharded":
                    description.Type.Should().Be(ClusterType.Sharded);
                    break;
                case "Unknown":
                    description.Type.Should().Be(ClusterType.Unknown);
                    break;
                default:
                    throw new AssertionException($"Unexpected topology type {topologyType}.");
            }
        }

        private void VerifyServer(ServerDescription actualServer, BsonDocument expectedServer)
        {
            var type = (string)expectedServer["type"];
            switch (type)
            {
                case "RSPrimary":
                    actualServer.Type.Should().Be(ServerType.ReplicaSetPrimary);
                    break;
                case "RSSecondary":
                    actualServer.Type.Should().Be(ServerType.ReplicaSetSecondary);
                    break;
                case "RSArbiter":
                    actualServer.Type.Should().Be(ServerType.ReplicaSetArbiter);
                    break;
                case "RSGhost":
                    actualServer.Type.Should().Be(ServerType.ReplicaSetGhost);
                    break;
                case "RSOther":
                    actualServer.Type.Should().Be(ServerType.ReplicaSetOther);
                    break;
                case "Mongos":
                    actualServer.Type.Should().Be(ServerType.ShardRouter);
                    break;
                case "Standalone":
                    actualServer.Type.Should().Be(ServerType.Standalone);
                    break;
                default:
                    actualServer.Type.Should().Be(ServerType.Unknown);
                    break;
            }

            BsonValue setName;
            if (expectedServer.TryGetValue("setName", out setName) && !setName.IsBsonNull)
            {
                actualServer.ReplicaSetConfig.Should().NotBeNull();
                actualServer.ReplicaSetConfig.Name.Should().Be(setName.ToString());
            }
        }

        private ICluster BuildCluster(BsonDocument definition)
        {
            var connectionString = new ConnectionString((string)definition["uri"]);
            var settings = new ClusterSettings(
                endPoints: Optional.Enumerable(connectionString.Hosts),
                connectionMode: connectionString.Connect,
                replicaSetName: connectionString.ReplicaSet);

            _eventSubscriber = new EventCapturer();
            _serverFactory = new MockClusterableServerFactory(_eventSubscriber);
            return new ClusterFactory(settings, _serverFactory, _eventSubscriber)
                .CreateCluster();
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
#if NET45
                const string prefix = "MongoDB.Driver.Specifications.server_discovery_and_monitoring.tests.monitoring.";
#else
                const string prefix = "MongoDB.Driver.Core.Tests.Dotnet.Specifications.server_discovery_and_monitoring.tests.monitoring.";
#endif
                var executingAssembly = typeof(TestCaseFactory).GetTypeInfo().Assembly;
                var enumerable = executingAssembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path =>
                    {
                        var definition = ReadDefinition(path);
                        //var fullName = path.Remove(0, prefix.Length);
                        //var data = new TestCaseData(definition);
                        //data.SetCategory("Specifications");
                        //data.SetCategory("sdam-monitoring");
                        //return data
                        //    .SetName(fullName.Remove(fullName.Length - 5).Replace(".", "_"))
                        //    .SetDescription(definition["description"].ToString());
                        return new object[] { definition };
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
                using (var definitionStreamReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStreamReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
