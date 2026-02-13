/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.prose_tests
{
    [Trait("Category", "Integration")]
    public class ServerDiscoveryProseTests : LoggableTestClass
    {
        // public constructors
        public ServerDiscoveryProseTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // public methods
        [Theory]
        [ParameterAttributeData]
        public void Topology_secondary_discovery_with_directConnection_false_should_work_as_expected([Values(false, true, null)] bool? directConnection)
        {
            RequireServer
                .Check()
                .Supports(Feature.DirectConnectionSetting)
                .ClusterTypes(ClusterType.ReplicaSet)
                .Authentication(false); // we don't use auth connection string in this test

            var setupClient = DriverTestConfiguration.Client;
            setupClient.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName).RunCommand<BsonDocument>("{ ping : 1 }");

            var setupCluster = setupClient.Cluster;
            SpinWait.SpinUntil(() => setupCluster.Description.State == ClusterState.Connected, TimeSpan.FromSeconds(3));

            var clusterDescription = setupCluster.Description;
            var secondary = clusterDescription.Servers.FirstOrDefault(s => s.State == ServerState.Connected && s.Type == ServerType.ReplicaSetSecondary);
            if (secondary == null)
            {
                throw new Exception("No secondary was found.");
            }

            var dnsEndpoint = (DnsEndPoint)secondary.EndPoint;
            var replicaSetName = secondary.ReplicaSetConfig.Name;
            var settings = MongoClientSettings.FromConnectionString(CreateConnectionString(dnsEndpoint, directConnection, replicaSetName));
            settings.LoggingSettings = LoggingSettings;

            using (var client = new MongoClient(settings))
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument()));
                if (directConnection.GetValueOrDefault())
                {
                    exception.Should().BeOfType<MongoNotPrimaryException>();
                    exception.Message.Should().Contain("Server returned not primary error");
                }
                else
                {
                    exception.Should().BeNull();
                }
            }
        }

        // https://github.com/mongodb/specifications/blob/a8d34be0df234365600a9269af5a463f581562fd/source/server-discovery-and-monitoring/server-discovery-and-monitoring-tests.md?plain=1#L176
        [Theory]
        [ParameterAttributeData]
        public async Task Connection_Pool_Backpressure([Values(true, false)]bool async)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("7.0.23");

            var setupClient = DriverTestConfiguration.Client;
            var adminDatabase = setupClient.GetDatabase(DatabaseNamespace.Admin.DatabaseName);

            adminDatabase.RunCommand<BsonDocument>(
                @"{
                    setParameter : 1,
                    ingressConnectionEstablishmentRateLimiterEnabled: true,
                    ingressConnectionEstablishmentRatePerSec: 20,
                    ingressConnectionEstablishmentBurstCapacitySecs: 1,
                    ingressConnectionEstablishmentMaxQueueDepth: 1
                }");

            try
            {
                var eventCapturer = new EventCapturer()
                    .Capture<ConnectionPoolCheckingOutConnectionFailedEvent>()
                    .Capture<ConnectionPoolClearedEvent>();

                using var client = DriverTestConfiguration.CreateMongoClient(settings =>
                {
                    settings.MaxConnecting = 100;
                    settings.LoggingSettings = LoggingSettings;
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                });

                var collection = client.GetDatabase("test").GetCollection<BsonDocument>("test");
                collection.InsertOne(new BsonDocument());

                var filter = "{ $where : \"function() { sleep(2000); return true; }\" }";
                _ = await ThreadingUtilities.ExecuteTasksOnNewThreadsCollectExceptions(
                    100,
                    _ => async ? collection.FindAsync(filter) : Task.FromResult(collection.FindSync(filter)), Timeout.Infinite);

                eventCapturer.Events.Count(e => e is ConnectionPoolCheckingOutConnectionFailedEvent).Should().BeGreaterOrEqualTo(10);
                eventCapturer.Events.Should().NotContain(e => e is ConnectionPoolClearedEvent);
            }
            finally
            {
                Thread.Sleep(1000);

                adminDatabase.RunCommand<BsonDocument>(
                    @"{
                        setParameter : 1,
                        ingressConnectionEstablishmentRateLimiterEnabled: false
                    }");
            }
        }

        // private methods
        private string CreateConnectionString(DnsEndPoint endpoint, bool? directConnection, string replicaSetName)
        {
            var connectionString = $"mongodb://{endpoint.Host}:{endpoint.Port}/?replicaSet={replicaSetName}";
            if (directConnection.HasValue)
            {
                connectionString += $"&directConnection={directConnection.Value}";
            }
            return connectionString;
        }
    }
}
