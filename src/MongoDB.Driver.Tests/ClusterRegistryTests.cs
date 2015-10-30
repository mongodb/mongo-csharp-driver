/* Copyright 2010-2015 MongoDB Inc.
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
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class ClusterRegistryTests
    {
        [Test]
        public void Instance_should_return_the_same_instance_every_time()
        {
            var subject1 = ClusterRegistry.Instance;
            var subject2 = ClusterRegistry.Instance;
            subject2.Should().BeSameAs(subject1);
        }

        [Test]
        public void GetOrCreateCluster_should_return_a_cluster_with_the_correct_settings()
        {
            var credentials = new[] { MongoCredential.CreateMongoCRCredential("source", "username", "password") };
            var servers = new[] { new MongoServerAddress("localhost"), new MongoServerAddress("127.0.0.1", 30000), new MongoServerAddress("[::1]", 27018) };

            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Ssl3
            };

            var clientSettings = new MongoClientSettings
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ConnectTimeout = TimeSpan.FromSeconds(1),
                Credentials = credentials,
                GuidRepresentation = GuidRepresentation.Standard,
                IPv6 = true,
                MaxConnectionIdleTime = TimeSpan.FromSeconds(2),
                MaxConnectionLifeTime = TimeSpan.FromSeconds(3),
                MaxConnectionPoolSize = 10,
                MinConnectionPoolSize = 5,
                ReplicaSetName = "rs",
                LocalThreshold = TimeSpan.FromMilliseconds(20),
                Servers = servers,
                ServerSelectionTimeout = TimeSpan.FromSeconds(5),
                SocketTimeout = TimeSpan.FromSeconds(4),
                SslSettings = sslSettings,
                UseSsl = true,
                VerifySslCertificate = true,
                WaitQueueSize = 20,
                WaitQueueTimeout = TimeSpan.FromSeconds(5)
            };

            var subject = new ClusterRegistry();

            using (var cluster = subject.GetOrCreateCluster(clientSettings.ToClusterKey()))
            {
                var endPoints = new EndPoint[]
                {
                    new DnsEndPoint("localhost", 27017),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000),
                    new IPEndPoint(IPAddress.Parse("[::1]"), 27018)
                };
                cluster.Settings.ConnectionMode.Should().Be(ClusterConnectionMode.ReplicaSet);
                cluster.Settings.EndPoints.Equals(endPoints);
                cluster.Settings.ReplicaSetName.Should().Be("rs");
                cluster.Settings.ServerSelectionTimeout.Should().Be(clientSettings.ServerSelectionTimeout);
                cluster.Settings.PostServerSelector.Should().NotBeNull().And.Subject.Should().BeOfType<LatencyLimitingServerSelector>();
                cluster.Settings.MaxServerSelectionWaitQueueSize.Should().Be(20);

                cluster.Description.Servers.Select(s => s.EndPoint).Should().Contain(endPoints);

                // TODO: don't know how to test the rest of the settings because they are all private to the cluster
            }
        }

        [Test]
        public void GetOrCreateCluster_should_return_a_different_cluster_if_client_settings_are_not_equal()
        {
            var clientSettings1 = new MongoClientSettings();
            var clientSettings2 = new MongoClientSettings() { IPv6 = true };

            var subject = new ClusterRegistry();

            using (var cluster1 = subject.GetOrCreateCluster(clientSettings1.ToClusterKey()))
            using (var cluster2 = subject.GetOrCreateCluster(clientSettings2.ToClusterKey()))
            {
                cluster2.Should().NotBeSameAs(cluster1);
            }
        }

        [Test]
        public void GetOrCreateCluster_should_return_the_same_cluster_if_client_settings_are_equal()
        {
            var clientSettings1 = new MongoClientSettings();
            var clientSettings2 = new MongoClientSettings();

            var subject = new ClusterRegistry();

            using (var cluster1 = subject.GetOrCreateCluster(clientSettings1.ToClusterKey()))
            using (var cluster2 = subject.GetOrCreateCluster(clientSettings2.ToClusterKey()))
            {
                cluster2.Should().BeSameAs(cluster1);
            }
        }
    }
}