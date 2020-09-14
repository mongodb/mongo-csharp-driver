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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring.prose_tests
{
    public class ServerDiscoveryProseTests
    {
        [SkippableTheory]
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
            using (var client = new DisposableMongoClient(new MongoClient(CreateConnectionString(dnsEndpoint, directConnection, replicaSetName))))
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument()));
                if (directConnection.GetValueOrDefault())
                {
                    exception.Should().BeOfType<MongoNotPrimaryException>();
                    exception.Message.Should().Contain("Server returned not master error");
                }
                else
                {
                    exception.Should().BeNull();
                }
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
