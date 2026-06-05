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
            __client = new Lazy<IMongoClient>(() => CreateMongoClient(), isThreadSafe: true);
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
            bool useMultipleShardRouters = false,
            bool waitForAllServersToBeConnected = false)
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
            clientSettings.TranslationOptions = GetTranslationOptions();
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            EnsureUniqueCluster(clientSettings);
            clientSettingsConfigurator?.Invoke(clientSettings);

            if (clientSettings.Credential?.Mechanism == OidcSaslMechanism.MechanismName)
            {
                OidcCallbackAdapterCachingFactory.Instance.Reset();
            }

            var client = new MongoClient(clientSettings);
            if (waitForAllServersToBeConnected)
            {
                WaitForAllServersToBeConnected(client.GetClusterInternal());
            }

            return client;
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

        public static MongoClientSettings GetClientSettings()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));

            var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
            if (serverSelectionTimeoutString == null)
            {
                serverSelectionTimeoutString = "30000";
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                clientSettings.HeartbeatTimeout = TimeSpan.FromDays(1);
                clientSettings.ServerMonitoringMode = ServerMonitoringMode.Poll;
            }

            clientSettings.ServerSelectionTimeout = TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString));
            clientSettings.ClusterConfigurator = cb => CoreTestConfiguration.ConfigureLogging(cb);
            clientSettings.ServerApi = CoreTestConfiguration.ServerApi;
            clientSettings.TranslationOptions = GetTranslationOptions();
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            return clientSettings;
        }

        public static ConnectionDescription GetConnectionDescription()
        {
            var cluster = Client.GetClusterInternal();
            using (var binding = new ReadWriteBindingHandle(new WritableServerBinding(cluster, NoCoreSession.NewHandle())))
            using (var channelSource = binding.GetWriteChannelSource(OperationContext.NoTimeout))
            using (var channel = channelSource.GetChannel(OperationContext.NoTimeout))
            {
                return channel.ConnectionDescription;
            }
        }

        public static ExpressionTranslationOptions GetTranslationOptions()
        {
            var compatibilityLevel = CoreTestConfiguration.MaxWireVersion.ToServerVersion();
            return new ExpressionTranslationOptions
            {
                CompatibilityLevel = compatibilityLevel
            };
        }

        // Returns true if any server reports an RS member type (Primary, Secondary, Arbiter,
        // Other, or Ghost). Callers that need specifically a data-bearing member should use
        // GetReplicaSetNumberOfDataBearingMembers instead.
        internal static bool IsReplicaSet(IClusterInternal cluster)
        {
            var serverIsKnown = SpinWait.SpinUntil(
                () => cluster.Description.Servers.Any(s => s.Type != ServerType.Unknown),
                TimeSpan.FromSeconds(10));
            if (!serverIsKnown)
            {
                throw new InvalidOperationException($"Unable to determine cluster type: {cluster.Description}.");
            }
            return cluster.Description.Servers.Any(s => s.Type.IsReplicaSetMember());
        }

        internal static int GetReplicaSetNumberOfDataBearingMembers(IClusterInternal cluster)
        {
            if (!IsReplicaSet(cluster))
            {
                throw new InvalidOperationException($"Cluster is not a replica set: {cluster.Description}.");
            }

            WaitForAllServersToBeConnected(cluster);
            // Under a directConnect the description only includes the single node being targeted,
            // so this count reflects only that server. RequireServer.ReplicaSetDataBearingMembers(2)
            // therefore correctly skips tests that need multiple members when connected directly.
            return cluster.Description.Servers.Count(s => s.IsDataBearing);
        }

        // Returns the effective cluster type. For direct connections Description.Type is always
        // Standalone regardless of the actual server type, so the server's reported type is used
        // instead. Throws (rather than skips) if the type cannot be determined: an undeterminable
        // topology is an environment failure, not a "this topology isn't applicable" skip.
        internal static ClusterType GetActualClusterType(IClusterInternal cluster)
        {
            var description = cluster.Description;
            if (description.DirectConnection)
            {
                var serverIsKnown = SpinWait.SpinUntil(
                    () => cluster.Description.Servers.Any(s => s.Type != ServerType.Unknown),
                    TimeSpan.FromSeconds(10));
                if (!serverIsKnown)
                {
                    throw new InvalidOperationException($"Unable to determine cluster type: {cluster.Description}.");
                }
                return cluster.Description.Servers.First(s => s.Type != ServerType.Unknown).Type.ToClusterType();
            }
            // For non-direct connections the cluster machinery resolves Description.Type before tests
            // run, but spin briefly here as well so early-startup invocations don't race against the
            // initial SDAM rounds.
            var typeIsKnown = SpinWait.SpinUntil(
                () => cluster.Description.Type != ClusterType.Unknown,
                TimeSpan.FromSeconds(10));
            if (!typeIsKnown)
            {
                throw new InvalidOperationException($"Unable to determine cluster type: {cluster.Description}.");
            }
            return cluster.Description.Type;
        }

        internal static bool IsSingleNodeReplicaSet(IClusterInternal cluster)
        {
            // IsReplicaSet spins until at least one server type is known (throwing if it never
            // resolves), so a transient empty / all-Unknown snapshot is never mistaken for "not a
            // replica set".
            if (!IsReplicaSet(cluster))
            {
                return false;
            }
            // Matches both a directConnection to a single RS member and a non-directConnection
            // single-node RS.
            var servers = cluster.Description.Servers;
            return servers.Count == 1 && servers[0].Type.IsReplicaSetMember();
        }

        internal static void WaitForAllServersToBeConnected(IClusterInternal cluster)
        {
            var allServersAreConnected = SpinWait.SpinUntil(() => cluster.Description.Servers.All(s => s.State == ServerState.Connected), TimeSpan.FromSeconds(10));
            if (!allServersAreConnected)
            {
                throw new InvalidOperationException($"Unable to connect to all members of the cluster: {cluster.Description}.");
            }
        }

        private static void EnsureUniqueCluster(MongoClientSettings settings)
        {
            // make the settings unique (as far as the ClusterKey is concerned) by instantiating a new ClusterConfigurator
            var configurator = settings.ClusterConfigurator;
            settings.ClusterConfigurator = b => { configurator?.Invoke(b); };
        }
    }
}
