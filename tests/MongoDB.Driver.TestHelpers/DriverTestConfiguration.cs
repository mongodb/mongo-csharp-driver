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
using System.Threading;
using MongoDB.Driver.Authentication.AWS;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.Tests
{
    /// <summary>
    /// A static class to handle online test configuration.
    /// </summary>
    public static class DriverTestConfiguration
    {
        // private static fields
        private static Lazy<IMongoClient> __client;
        private static Lazy<IMongoClient> __clientWithMultipleShardRouters;
        private static CollectionNamespace __collectionNamespace;
        private static DatabaseNamespace __databaseNamespace;
        private static Lazy<IReadOnlyList<IMongoClient>> __directClientsToShardRouters;

        // static constructor
        static DriverTestConfiguration()
        {
            __client = new Lazy<IMongoClient>(CreateMongoClient, isThreadSafe: true);
            __clientWithMultipleShardRouters = new Lazy<IMongoClient>(() => CreateMongoClient(useMultipleShardRouters: true), true);
            __databaseNamespace = CoreTestConfiguration.DatabaseNamespace;
            __directClientsToShardRouters = new Lazy<IReadOnlyList<IMongoClient>>(
                () => CreateDirectClientsToHostsInConnectionString(CoreTestConfiguration.ConnectionStringWithMultipleShardRouters).ToList().AsReadOnly(),
                isThreadSafe: true);
            __collectionNamespace = new CollectionNamespace(__databaseNamespace, "testcollection");

            MongoClientSettings.Extensions.AddAWSAuthentication();
            MongoClientSettings.Extensions.AddAutoEncryption();
        }

        // public static properties
        /// <summary>
        /// Gets the test client.
        /// </summary>
        public static IMongoClient Client
        {
            get { return __client.Value; }
        }

        /// <summary>
        /// Gets the test client with multiple shard routers.
        /// </summary>
        public static IMongoClient ClientWithMultipleShardRouters
        {
            get { return __clientWithMultipleShardRouters.Value; }
        }

        /// <summary>
        /// Sequence of clients that connect directly to the shard routers
        /// </summary>
        public static IReadOnlyList<IMongoClient> DirectClientsToShardRouters
        {
            get => __directClientsToShardRouters.Value;
        }

        /// <summary>
        /// Gets the collection namespace.
        /// </summary>
        /// <value>
        /// The collection namespace.
        /// </value>
        public static CollectionNamespace CollectionNamespace
        {
            get { return __collectionNamespace; }
        }

        /// <summary>
        /// Gets the database namespace.
        /// </summary>
        /// <value>
        /// The database namespace.
        /// </value>
        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace; }
        }

        // public static methods
        public static IEnumerable<IMongoClient> CreateDirectClientsToServersInClientSettings(MongoClientSettings settings)
        {
            foreach (var server in settings.Servers)
            {
                var singleServerSettings = settings.Clone();
                singleServerSettings.Server = server;
                yield return new MongoClient(singleServerSettings);
            }
        }

        public static IEnumerable<IMongoClient> CreateDirectClientsToHostsInConnectionString(ConnectionString connectionString)
        {
            return CreateDirectClientsToServersInClientSettings(MongoClientSettings.FromConnectionString(connectionString.ToString()));
        }

        public static IMongoClient CreateMongoClient(LoggingSettings loggingSettings) =>
            CreateMongoClient((MongoClientSettings s) => { s.LoggingSettings = loggingSettings; });

        public static IMongoClient CreateMongoClient(Action<ClusterBuilder> clusterConfigurator) =>
            CreateMongoClient((MongoClientSettings s) => s.ClusterConfigurator = clusterConfigurator);

        public static IMongoClient CreateMongoClient(
            Action<MongoClientSettings> clientSettingsConfigurator = null,
            bool useMultipleShardRouters = false)
        {
            var clusterType = CoreTestConfiguration.Cluster.Description.Type;
            if (clusterType != ClusterType.Sharded && clusterType != ClusterType.LoadBalanced)
            {
                // This option has no effect for non-sharded/load balanced topologies.
                useMultipleShardRouters = false;
            }

            var connectionString = useMultipleShardRouters
                ? CoreTestConfiguration.ConnectionStringWithMultipleShardRouters.ToString()
                : CoreTestConfiguration.ConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            clientSettings.ServerApi = CoreTestConfiguration.ServerApi;
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            EnsureUniqueCluster(clientSettings);
            clientSettingsConfigurator?.Invoke(clientSettings);

            if (clientSettings.Credential?.Mechanism == OidcSaslMechanism.MechanismName)
            {
                OidcCallbackAdapterCachingFactory.Instance.Reset();
            }

            return new MongoClient(clientSettings);
        }

        public static IMongoClient CreateMongoClient(EventCapturer capturer, LoggingSettings loggingSettings = null) =>
            CreateMongoClient((ClusterBuilder c) =>
                {
                    c.Subscribe(capturer);
                    c.ConfigureLoggingSettings(_ => loggingSettings);
                });

        public static IMongoClient CreateMongoClient(MongoClientSettings settings)
        {
            EnsureUniqueCluster(settings);
            settings.ClusterSource = DisposingClusterSource.Instance;

            return new MongoClient(settings);
        }

        private static IMongoClient CreateMongoClient() =>
            CreateMongoClient(GetClientSettings());

        public static MongoClientSettings GetClientSettings()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

            var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
            if (serverSelectionTimeoutString == null)
            {
                serverSelectionTimeoutString = "30000";
            }
            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString));
            clientSettings.ClusterConfigurator = cb => CoreTestConfiguration.ConfigureLogging(cb);
            clientSettings.ServerApi = CoreTestConfiguration.ServerApi;
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            return clientSettings;
        }

        public static ConnectionDescription GetConnectionDescription()
        {
            var cluster = Client.GetClusterInternal();
            using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, NoCoreSession.NewHandle())))
            using (var channelSource = binding.GetWriteChannelSource(default))
            using (var channel = channelSource.GetChannel(default))
            {
                return channel.ConnectionDescription;
            }
        }

        public static bool IsReplicaSet(IMongoClient client)
        {
            var clusterTypeIsKnown = SpinWait.SpinUntil(() => client.Cluster.Description.Type != ClusterType.Unknown, TimeSpan.FromSeconds(10));
            if (!clusterTypeIsKnown)
            {
                throw new InvalidOperationException($"Unable to determine cluster type: {client.Cluster.Description}.");
            }
            return client.Cluster.Description.Type == ClusterType.ReplicaSet;
        }

        public static int GetReplicaSetNumberOfDataBearingMembers(IMongoClient client)
        {
            if (!IsReplicaSet(client))
            {
                throw new InvalidOperationException($"Cluster is not a replica set: {client.Cluster.Description}.");
            }

            var allServersAreConnected = SpinWait.SpinUntil(() => client.Cluster.Description.Servers.All(s => s.State == ServerState.Connected), TimeSpan.FromSeconds(10));
            if (!allServersAreConnected)
            {
                throw new InvalidOperationException($"Unable to connect to all members of the replica set: {client.Cluster.Description}.");
            }

            return client.Cluster.Description.Servers.Count(s => s.IsDataBearing);
        }

        private static void EnsureUniqueCluster(MongoClientSettings settings)
        {
            // make the settings unique (as far as the ClusterKey is concerned) by instantiating a new ClusterConfigurator
            var configurator = settings.ClusterConfigurator;
            settings.ClusterConfigurator = b => { configurator?.Invoke(b); };
        }
    }
}
