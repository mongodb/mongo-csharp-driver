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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring
{
    [Trait("Category", "SDAM")]
    public class ServerDiscoveryAndMonitoringTestRunner : LoggableTestClass
    {
        private IClusterInternal _cluster;
        private IEventSubscriber _eventSubscriber;
        private MockClusterableServerFactory _serverFactory;

        public ServerDiscoveryAndMonitoringTestRunner(ITestOutputHelper output) : base(output)
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

        private void ApplyApplicationError(BsonDocument applicationError)
        {
            var expectedKeys = new[]
            {
                "address",
                "generation", // optional
                "maxWireVersion",
                "when",
                "type",
                "response" // optional
            };
            JsonDrivenHelper.EnsureAllFieldsAreValid(applicationError, expectedKeys);
            var address = applicationError["address"].AsString;
            var endPoint = EndPointHelper.Parse(address);
            var server = (Server)_serverFactory.GetServer(endPoint);
            var connectionId = new ConnectionId(server.ServerId);
            var type = applicationError["type"].AsString;
            var maxWireVersion = applicationError["maxWireVersion"].AsInt32;
            Exception simulatedException = null;
            switch (type)
            {
                case "command":
                    var response = applicationError["response"].AsBsonDocument;
                    var command = new BsonDocument("Link", "start!");
                    simulatedException = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, command, response, "errmsg"); // can return null
                    break;
                case "network":
                    {
                        var innerException = CoreExceptionHelper.CreateException("IOExceptionWithNetworkUnreachableSocketException");
                        simulatedException = new MongoConnectionException(connectionId, "Ignorance, yet knowledge.", innerException);
                        break;
                    }
                case "timeout":
                    {
                        var innerException = CoreExceptionHelper.CreateException("IOExceptionWithTimedOutSocketException");
                        simulatedException = new MongoConnectionException(connectionId, "Chaos, yet harmony.", innerException);
                        break;
                    }
                default:
                    throw new ArgumentException($"Unsupported value of {type} for type");
            }

            var mockConnection = new Mock<IConnectionHandle>();

            var helloResult = new HelloResult(
                new BsonDocument
                {
                    { "compressors", new BsonArray() },
                    { "maxWireVersion", maxWireVersion }
                });
            mockConnection
                .SetupGet(c => c.Description)
                .Returns(new ConnectionDescription(connectionId, helloResult));

            int generation = 0;
            if (applicationError.TryGetValue("generation", out var generationBsonValue))
            {
                generation = generationBsonValue.AsInt32;

                if (simulatedException is MongoConnectionException mongoConnectionException)
                {
                    mongoConnectionException.Generation = generation;
                }
            }

            mockConnection.SetupGet(c => c.Generation).Returns(generation);
            mockConnection
                .SetupGet(c => c.Generation)
                .Returns(generation);

            if (simulatedException != null)
            {
                var when = applicationError["when"].AsString;
                switch (when)
                {
                    case "beforeHandshakeCompletes":
                        server.HandleBeforeHandshakeCompletesException(simulatedException);
                        break;
                    case "afterHandshakeCompletes":
                        server.HandleChannelException(mockConnection.Object, simulatedException);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported value of {when} for when.");
                }
            }
        }

        private void ApplyPhase(BsonDocument phase)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(phase, "applicationErrors", "description", "outcome", "responses");

            if (phase.Contains("responses"))
            {
                var responses = phase["responses"].AsBsonArray;
                foreach (BsonArray response in responses)
                {
                    ApplyResponse(response);
                }
            }

            if (phase.TryGetValue("applicationErrors", out var applicationErrors))
            {
                foreach (BsonDocument applicationError in applicationErrors.AsBsonArray)
                {
                    ApplyApplicationError(applicationError);
                }
            }

            var outcome = (BsonDocument)phase["outcome"];
            var description = (string)phase.GetValue("description", defaultValue: null);
            VerifyOutcome(outcome, description);
        }

        private void ApplyResponse(BsonArray response)
        {
            if (response.Count != 2)
            {
                throw new FormatException($"Invalid response count: {response.Count}.");
            }

            var address = response[0].AsString;
            var helloDocument = response[1].AsBsonDocument;
            var expectedNames = new[]
            {
                "arbiterOnly",
                "arbiters",
                "electionId",
                "hidden",
                "hosts",
                "helloOk",
                "isWritablePrimary",
                OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName,
                "isreplicaset",
                "logicalSessionTimeoutMinutes",
                "maxWireVersion",
                "me",
                "minWireVersion",
                "msg",
                "ok",
                "passive",
                "passives",
                "primary",
                "secondary",
                "setName",
                "setVersion",
                "topologyVersion"
            };
            JsonDrivenHelper.EnsureAllFieldsAreValid(helloDocument, expectedNames);

            var endPoint = EndPointHelper.Parse(address);
            var helloResult = new HelloResult(helloDocument);
            var currentServerDescription = _serverFactory.GetServerDescription(endPoint);
            var newServerDescription = currentServerDescription.With(
                canonicalEndPoint: helloResult.Me,
                electionId: helloResult.ElectionId,
                logicalSessionTimeout: helloResult.LogicalSessionTimeout,
                replicaSetConfig: helloResult.GetReplicaSetConfig(),
                state: helloResult.Wrapped.GetValue("ok", false).ToBoolean() ? ServerState.Connected : ServerState.Disconnected,
                topologyVersion: helloResult.TopologyVersion,
                type: helloResult.ServerType,
                wireVersionRange: new Range<int>(helloResult.MinWireVersion, helloResult.MaxWireVersion));

            var currentClusterDescription = _cluster.Description;
            _serverFactory.PublishDescription(newServerDescription);
            SpinWait.SpinUntil(() => !object.ReferenceEquals(_cluster.Description, currentClusterDescription), 100); // sometimes returns false and that's OK
        }

        private void VerifyTopology(IClusterInternal cluster, string expectedType, string phaseDescription)
        {
            var clusterDescription = cluster.Description;
            switch (expectedType)
            {
                case "Single":
                    if (cluster is SingleServerCluster || cluster is MultiServerCluster)
                    {
                        cluster.Settings.DirectConnection.Should().Be(clusterDescription.DirectConnection);
                        cluster.Description.Type.Should().Be(ClusterType.Standalone);
                    }
                    else
                    {
                        throw new Exception($"Unexpected cluster type {cluster.GetType().Name}.");
                    }
                    break;
                case "ReplicaSetWithPrimary":
                    cluster.Should().BeOfType<MultiServerCluster>();
                    cluster.Description.Type.Should().Be(ClusterType.ReplicaSet);
                    cluster.Description.Servers.Should().ContainSingle(x => x.Type == ServerType.ReplicaSetPrimary, phaseDescription);
                    break;
                case "ReplicaSetNoPrimary":
                    cluster.Should().BeOfType<MultiServerCluster>(phaseDescription);
                    cluster.Description.Type.Should().Be(ClusterType.ReplicaSet);
                    cluster.Description.Servers.Should().NotContain(x => x.Type == ServerType.ReplicaSetPrimary, because: $"because of {phaseDescription}");
                    break;
                case "Sharded":
                    cluster.Should().BeOfType<MultiServerCluster>();
                    cluster.Description.Type.Should().Be(ClusterType.Sharded);
                    break;
                case "Unknown":
                    cluster.Description.Type.Should().Be(ClusterType.Unknown);
                    break;
                case "LoadBalanced":
                    cluster.Should().BeOfType<LoadBalancedCluster>();
                    cluster.Description.Type.Should().Be(ClusterType.LoadBalanced);
                    cluster.Description.Servers.Should().ContainSingle(c => c.Type == ServerType.LoadBalanced);
                    break;
                default:
                    throw new FormatException($"Invalid topology type: \"{expectedType}\".");
            }
        }

        private void VerifyOutcome(BsonDocument outcome, string phaseDescription)
        {
            var expectedNames = new[]
            {
                "compatible",
                "logicalSessionTimeoutMinutes",
                "pool",
                "servers",
                "setName",
                "topologyType",
                "topologyVersion",
                "maxSetVersion",
                "maxElectionId"
            };
            JsonDrivenHelper.EnsureAllFieldsAreValid(outcome, expectedNames);

            var expectedTopologyType = (string)outcome["topologyType"];
            VerifyTopology(_cluster, expectedTopologyType, phaseDescription);

            var actualDescription = _cluster.Description;

            var actualServersEndpoints = actualDescription.Servers.Select(x => x.EndPoint).ToList();
            var expectedServers = outcome["servers"].AsBsonDocument.Elements.Select(x => new
            {
                EndPoint = EndPointHelper.Parse(x.Name),
                Description = (BsonDocument)x.Value
            });
            actualServersEndpoints.WithComparer(EndPointHelper.EndPointEqualityComparer).Should().BeEquivalentTo(expectedServers.Select(x => x.EndPoint).WithComparer(EndPointHelper.EndPointEqualityComparer));

            var actualServers = actualServersEndpoints.Select(endpoint => _serverFactory.GetServer(endpoint));
            foreach (var actualServerDescription in actualDescription.Servers)
            {
                var expectedServer = expectedServers.Single(x => EndPointHelper.EndPointEqualityComparer.Equals(x.EndPoint, actualServerDescription.EndPoint));
                VerifyServerDescription(actualServerDescription, expectedServer.Description, phaseDescription);
                VerifyServerPropertiesNotInServerDescription(_serverFactory.GetServer(actualServerDescription.EndPoint), expectedServer.Description, phaseDescription);
            }
            if (outcome.TryGetValue("maxSetVersion", out var maxSetVersion))
            {
                switch (_cluster)
                {
                    case MultiServerCluster multiServerCluster:
                        multiServerCluster._maxElectionInfo_setVersion().Should().Be(maxSetVersion.AsInt32);
                        break;
                    case LoadBalancedCluster:
                        // LoadBalancedCluster doesn't support maxSetVersion, so assert that there is no expected value
                        maxSetVersion.Should().BeOfType<BsonNull>();
                        break;
                    default:
                        throw new Exception($"Unsupported cluster type {_cluster.GetType()}.");
                }
            }
            if (outcome.TryGetValue("maxElectionId", out var maxElectionId))
            {
                switch (_cluster)
                {
                    case MultiServerCluster multiServerCluster:
                        multiServerCluster._maxElectionInfo_electionId().Should().Be(new ElectionId((ObjectId)maxElectionId));
                        break;
                    case LoadBalancedCluster:
                        // LoadBalancedCluster doesn't support maxElectionId, so assert that there is no expected value
                        maxElectionId.Should().BeOfType<BsonNull>();
                        break;
                    default:
                        throw new Exception($"Unsupported cluster type {_cluster.GetType()}.");
                }
            }

            if (outcome.TryGetValue("setName", out var setName))
            {
                // TODO: assert something against setName for non LoadBalancedCluster

                if (_cluster is LoadBalancedCluster)
                {
                    // LoadBalancedCluster doesn't support setName, so assert that there is no expected value
                    setName.Should().BeOfType<BsonNull>();
                }
            }

            if (outcome.Contains("logicalSessionTimeoutMinutes"))
            {
                TimeSpan? expectedLogicalSessionTimeout;
                switch (outcome["logicalSessionTimeoutMinutes"].BsonType)
                {
                    case BsonType.Null:
                        expectedLogicalSessionTimeout = null;
                        break;
                    case BsonType.Int32:
                    case BsonType.Int64:
                        expectedLogicalSessionTimeout = TimeSpan.FromMinutes(outcome["logicalSessionTimeoutMinutes"].ToDouble());
                        break;
                    default:
                        throw new FormatException($"Invalid logicalSessionTimeoutMinutes BSON type: {outcome["setName"].BsonType}.");
                }
                actualDescription.LogicalSessionTimeout.Should().Be(expectedLogicalSessionTimeout);
            }

            if (outcome.Contains("compatible"))
            {
                var expectedIsCompatibleWithDriver = outcome["compatible"].ToBoolean();
                actualDescription.IsCompatibleWithDriver.Should().Be(expectedIsCompatibleWithDriver);
            }
        }

        private void VerifyServerDescription(ServerDescription actualDescription, BsonDocument expectedDescription, string phaseDescription)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedDescription, "electionId", "pool", "setName", "setVersion", "topologyVersion", "type", "logicalSessionTimeoutMinutes", "minWireVersion", "maxWireVersion");

            var expectedType = (string)expectedDescription["type"];
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
                    case BsonType.String: expectedSetName = expectedDescription["setName"].AsString; ; break;
                    default: throw new FormatException($"Invalid setName BSON type: {expectedDescription["setName"].BsonType}.");
                }
                actualDescription.ReplicaSetConfig?.Name.Should().Be(expectedSetName);
            }

            if (expectedDescription.Contains("setVersion"))
            {
                int? expectedSetVersion;
                switch (expectedDescription["setVersion"].BsonType)
                {
                    case BsonType.Null:
                        expectedSetVersion = null;
                        break;
                    case BsonType.Int32:
                    case BsonType.Int64:
                        expectedSetVersion = expectedDescription["setVersion"].ToInt32();
                        break;
                    default:
                        throw new FormatException($"Invalid setVersion BSON type: {expectedDescription["setVersion"].BsonType}.");
                }
                actualDescription.ReplicaSetConfig?.Version.Should().Be(expectedSetVersion);
            }

            if (expectedDescription.Contains("electionId"))
            {
                ElectionId expectedElectionId;
                switch (expectedDescription["electionId"].BsonType)
                {
                    case BsonType.Null: expectedElectionId = null; break;
                    case BsonType.ObjectId: expectedElectionId = new ElectionId(expectedDescription["electionId"].AsObjectId); break;
                    default: throw new FormatException($"Invalid electionId BSON type: {expectedDescription["electionId"].BsonType}.");
                }
                actualDescription.ElectionId.Should().Be(expectedElectionId);
            }

            if (expectedDescription.TryGetValue("topologyVersion", out var topologyVersionValue))
            {
                switch (topologyVersionValue)
                {
                    case BsonDocument topologyVersion:
                        TopologyVersion expectedTopologyType = TopologyVersion.FromBsonDocument(topologyVersion);
                        expectedTopologyType.Should().NotBeNull();
                        actualDescription.TopologyVersion.Should().Be(expectedTopologyType, phaseDescription);
                        break;
                    case BsonNull _:
                        actualDescription.TopologyVersion.Should().BeNull();
                        break;
                    default: throw new FormatException($"Invalid topologyVersion BSON type: {topologyVersionValue.BsonType}.");
                }
            }

            if (expectedDescription.TryGetValue("logicalSessionTimeoutMinutes", out var logicalSessionTimeoutMinutes))
            {
                if (logicalSessionTimeoutMinutes is BsonNull)
                {
                    actualDescription.LogicalSessionTimeout.Should().NotHaveValue();
                }
                else
                {
                    actualDescription.LogicalSessionTimeout.Should().Be(TimeSpan.FromMinutes(logicalSessionTimeoutMinutes.ToInt32()));
                }
            }

            if (expectedDescription.TryGetValue("minWireVersion", out var minWireVersion))
            {
                if (minWireVersion is BsonNull)
                {
                    actualDescription.WireVersionRange.Should().BeNull();
                }
                else
                {
                    actualDescription.WireVersionRange.Min.Should().Be(minWireVersion.ToInt32());
                }
            }

            if (expectedDescription.TryGetValue("maxWireVersion", out var maxWireVersion))
            {
                if (maxWireVersion is BsonNull)
                {
                    actualDescription.WireVersionRange.Should().BeNull();
                }
                else
                {
                    actualDescription.WireVersionRange.Max.Should().Be(maxWireVersion.ToInt32());
                }
            }
        }

        private void VerifyServerPropertiesNotInServerDescription(IClusterableServer actualServer, BsonDocument expectedServer, string phaseDescription)
        {
            if (expectedServer.TryGetValue("pool", out var poolValue))
            {
                switch (poolValue)
                {
                    case BsonDocument poolDocument:
                        if (poolDocument.Values.Count() == 1 &&
                            poolDocument.TryGetValue("generation", out var generationValue) &&
                            generationValue is BsonInt32 generation)
                        {
                            VerifyServerGeneration(actualServer, generation.Value, phaseDescription);
                            break;
                        }
                        throw new FormatException($"Invalid schema for pool.");
                    default: throw new FormatException($"Invalid topologyVersion BSON type: {poolValue.BsonType}.");
                }
            }
        }

        private void VerifyServerGeneration(IClusterableServer actualServer, int poolGeneration, string phaseDescription)
        {
            switch (actualServer)
            {
                case Server server:
                    server._connectionPool().Generation.Should().Be(poolGeneration, phaseDescription);
                    break;
                default: throw new Exception("Verifying pool generation with mock servers is currently unsupported.");
            }
        }

        private IClusterInternal BuildCluster(BsonDocument definition)
        {
            var connectionString = new ConnectionString((string)definition["uri"]);
            var settings = new ClusterSettings(
                directConnection: connectionString.DirectConnection,
                endPoints: Optional.Enumerable(connectionString.Hosts),
                replicaSetName: connectionString.ReplicaSet,
                loadBalanced: connectionString.LoadBalanced);

            // Passing in an eventCapturer results in Server being used instead of a Mock
            _serverFactory = new MockClusterableServerFactory(LoggerFactory, new EventCapturer());
            _eventSubscriber = new Mock<IEventSubscriber>().Object;
            return new ClusterFactory(settings, _serverFactory, _eventSubscriber, LoggerFactory)
                .CreateCluster();
        }

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // private constants
            private readonly string[] MonitoringPrefixes =
            {
                "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.monitoring.",
                "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.legacy_hello.monitoring."
            };

            // Integration tests are run by ServerDiscoveryAndMonitoringIntegrationTestRunner in MongoDB.Driver.Tests
            private const string IntegrationTestPrefix = "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.integration.";
            private const string UnifiedTestPrefix = "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.unified.";

            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.tests.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var name = GetTestCaseName(document, document, 0);
                yield return new JsonDrivenTestCase(name, document, document);
            }

            protected override bool ShouldReadJsonDocument(string path)
            {
                return base.ShouldReadJsonDocument(path) &&
                    !MonitoringPrefixes.Any(prefix => path.StartsWith(prefix)) &&
                    !path.StartsWith(IntegrationTestPrefix) &&
                    !path.StartsWith(UnifiedTestPrefix);
            }
        }
    }

    internal static class MultiServerClusterReflector
    {
        public static int _maxElectionInfo_setVersion(this MultiServerCluster obj)
        {
            var maxElectionInfo = _maxElectionInfo(obj);
            return (int)Reflector.GetFieldValue(maxElectionInfo, "_setVersion");
        }

        public static ElectionId _maxElectionInfo_electionId(this MultiServerCluster obj)
        {
            var maxElectionInfo = _maxElectionInfo(obj);
            return (ElectionId)Reflector.GetFieldValue(maxElectionInfo, "_electionId");
        }

        private static object _maxElectionInfo(MultiServerCluster obj)
        {
            return Reflector.GetFieldValue(obj, nameof(_maxElectionInfo));
        }
    }

    internal static class ServerReflector
    {
        public static IConnectionPool _connectionPool(this Server server)
        {
            return (IConnectionPool)Reflector.GetFieldValue(server, nameof(_connectionPool));
        }

        public static IServerMonitor _monitor(this IServer server)
        {
            return (IServerMonitor)Reflector.GetFieldValue(server, nameof(_monitor));
        }

        public static void HandleBeforeHandshakeCompletesException(this Server server, Exception ex)
        {
            Reflector.Invoke(server, nameof(HandleBeforeHandshakeCompletesException), ex);
        }

        public static void HandleChannelException(this Server server, IConnection connection, Exception ex)
        {
            Reflector.Invoke(server, nameof(HandleChannelException), connection, ex, checkBaseClass: true);
        }
    }
}
