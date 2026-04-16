/* Copyright 2010-present MongoDB Inc.
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
using System.Security.Authentication;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    public class ClusterRegistryTests : LoggableTestClass
    {
        public ClusterRegistryTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void DefaultClusterSource_should_use_cluster_registry_and_not_return_cluster()
        {
            var settings = new MongoClientSettings();
            var clusterKey = settings.ToClusterKey();

            var clusterSource = DefaultClusterSource.Instance;

            var cluster = clusterSource.Get(clusterKey);

            var clusterInRegistry = ClusterRegistry.Instance._registry()[clusterKey];
            clusterInRegistry.Should().BeSameAs(cluster);

            clusterSource.Return(cluster);
            ClusterRegistry.Instance._registry().Keys.Should().Contain(clusterKey);
        }

        [Fact]
        public void DisposingClusterSource_should_use_cluster_registry_and_return_cluster()
        {
            var settings = new MongoClientSettings();
            var clusterKey = settings.ToClusterKey();

            var clusterSource = DisposingClusterSource.Instance;

            var cluster = clusterSource.Get(clusterKey);

            var clusterInRegistry = ClusterRegistry.Instance._registry()[clusterKey];
            clusterInRegistry.Should().BeSameAs(cluster);

            clusterSource.Return(cluster);
            ClusterRegistry.Instance._registry().Keys.Should().NotContain(clusterKey);
        }

        [Fact]
        public void Instance_should_return_the_same_instance_every_time()
        {
            var subject1 = ClusterRegistry.Instance;
            var subject2 = ClusterRegistry.Instance;
            subject2.Should().BeSameAs(subject1);
        }

        [Fact]
        public void GetOrCreateCluster_should_return_a_cluster_with_the_correct_settings()
        {
            var clusterConfigurator = new Action<ClusterBuilder>(b => { });
            var credential = MongoCredential.CreateCredential("source", "username", "password");
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            var servers = new[] { new MongoServerAddress("localhost"), new MongoServerAddress("127.0.0.1", 30000), new MongoServerAddress("[::1]", 27018) };
            var sslSettings = new SslSettings
            {
                CheckCertificateRevocation = true,
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var kmsProviders = new Dictionary<string, IReadOnlyDictionary<string, object>>()
            {
                { "local", new Dictionary<string, object>() { { "key" , new byte[96] } } }
            };
            var dummyMap = new Dictionary<string, BsonDocument>()
            {
                { "db.coll", new BsonDocument() }
            };

            var cryptClientSettings = new CryptClientSettings(
                bypassQueryAnalysis: true,
                null,
                null,
                dummyMap,
                false,
                kmsProviders,
                dummyMap);

            var clusterKey = new ClusterKey(
                allowInsecureTls: false,
                applicationName: "app1",
                clusterConfigurator: clusterConfigurator,
                compressors: new[] { new CompressorConfiguration(CompressorType.Zlib) },
                connectTimeout: TimeSpan.FromSeconds(1),
                credential: credential,
                cryptClientSettings: cryptClientSettings,
                directConnection: false,
                libraryInfo: null,
                heartbeatInterval: TimeSpan.FromSeconds(2),
                heartbeatTimeout: TimeSpan.FromSeconds(3),
                ipv6: true,
                loadBalanced: false,
                localThreshold: TimeSpan.FromSeconds(4),
                loggingSettings: null,
                tracingOptions: null,
                maxConnecting: 3,
                maxConnectionIdleTime: TimeSpan.FromSeconds(5),
                maxConnectionLifeTime: TimeSpan.FromSeconds(6),
                maxConnectionPoolSize: 7,
                minConnectionPoolSize: 8,
                receiveBufferSize: 9,
                replicaSetName: "rs",
                scheme: ConnectionStringScheme.MongoDB,
                sendBufferSize: 10,
                serverApi: serverApi,
                servers: servers,
                serverMonitoringMode: ServerMonitoringMode.Stream,
                serverSelectionTimeout: TimeSpan.FromSeconds(11),
                socketTimeout: TimeSpan.FromSeconds(12),
                socks5ProxySettings: null,
                srvMaxHosts: 0,
                srvServiceName: "mongodb",
                sslSettings: sslSettings,
                useTls: true,
                waitQueueSize: 13,
                waitQueueTimeout: TimeSpan.FromSeconds(14));

            var subject = new ClusterRegistry();

            using (var cluster = subject.GetOrCreateCluster(clusterKey))
            {
                var expectedEndPoints = new EndPoint[]
                {
                    new DnsEndPoint("localhost", 27017),
                    new IPEndPoint(IPAddress.Parse("127.0.0.1"), 30000),
                    new IPEndPoint(IPAddress.Parse("[::1]"), 27018)
                };
                cluster.Settings.CryptClientSettings.EncryptedFieldsMap.Should().BeEquivalentTo(dummyMap);
                cluster.Settings.CryptClientSettings.KmsProviders.Should().BeEquivalentTo(kmsProviders);
                cluster.Settings.DirectConnection.Should().Be(clusterKey.DirectConnection);
                cluster.Settings.EndPoints.Should().Equal(expectedEndPoints);
                cluster.Settings.LoadBalanced.Should().Be(clusterKey.LoadBalanced);
                cluster.Settings.MaxServerSelectionWaitQueueSize.Should().Be(clusterKey.WaitQueueSize);
                cluster.Settings.ReplicaSetName.Should().Be(clusterKey.ReplicaSetName);
                cluster.Settings.CryptClientSettings.SchemaMap.Should().BeEquivalentTo(dummyMap);
                cluster.Settings.Scheme.Should().Be(clusterKey.Scheme);
                cluster.Settings.ServerApi.Should().Be(clusterKey.ServerApi);
                cluster.Settings.ServerSelectionTimeout.Should().Be(clusterKey.ServerSelectionTimeout);

                cluster.Description.Servers.Select(s => s.EndPoint).Should().BeEquivalentTo(expectedEndPoints);

                // TODO: don't know how to test the rest of the settings because they are all private to the cluster
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void UnregisterAndDisposeCluster_should_unregister_and_dispose_the_cluster()
        {
            var subject = new ClusterRegistry();
            var settings = new MongoClientSettings();
            var clusterKey = settings.ToClusterKey();
            var cluster = subject.GetOrCreateCluster(clusterKey);

            subject.UnregisterAndDisposeCluster(cluster);

            subject._registry().Count.Should().Be(0);
            cluster._state().Should().Be(2);
        }
    }

    internal static class ClusterRegistryReflector
    {
        public static Dictionary<ClusterKey, IClusterInternal> _registry(this ClusterRegistry clusterRegistry) => (Dictionary<ClusterKey, IClusterInternal>)Reflector.GetFieldValue(clusterRegistry, nameof(_registry));

        public static int _state(this IClusterInternal cluster) => (int)((InterlockedInt32)Reflector.GetFieldValue(cluster, nameof(_state))).Value;
    }
}
