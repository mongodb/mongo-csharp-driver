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
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    public static class CoreTestConfiguration
    {
        #region static
        // static fields
        private static Lazy<ICluster> __cluster = new Lazy<ICluster>(CreateCluster, isThreadSafe: true);
        private static Lazy<ConnectionString> __connectionString = new Lazy<ConnectionString>(CreateConnectionString, isThreadSafe: true);
        private static Lazy<ConnectionString> __connectionStringWithMultipleShardRouters = new Lazy<ConnectionString>(
            GetConnectionStringWithMultipleShardRouters, isThreadSafe: true);
        private static Lazy<DatabaseNamespace> __databaseNamespace = new Lazy<DatabaseNamespace>(GetDatabaseNamespace, isThreadSafe: true);
        private static Lazy<TimeSpan> __defaultServerSelectionTimeout = new Lazy<TimeSpan>(GetDefaultServerSelectionTimeout, isThreadSafe: true);
        private static Lazy<int> __maxWireVersion = new Lazy<int>(GetMaxWireVersion, isThreadSafe: true);
        private static MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static Lazy<int> __numberOfMongoses = new Lazy<int>(GetNumberOfMongoses, isThreadSafe: true);
        private static Lazy<ServerApi> __serverApi = new Lazy<ServerApi>(GetServerApi, isThreadSafe: true);
        private static Lazy<bool> __serverless = new Lazy<bool>(GetServerless, isThreadSafe: true);
        private static Lazy<SemanticVersion> __serverVersion = new Lazy<SemanticVersion>(GetServerVersion, isThreadSafe: true);
        private static Lazy<string> __storageEngine = new Lazy<string>(GetStorageEngine, isThreadSafe: true);
        private static TraceSource __traceSource;

        public static TimeSpan DefaultTestTimeout { get; } = TimeSpan.FromMinutes(3);

        // static properties
        public static ICluster Cluster
        {
            get { return __cluster.Value; }
        }

        public static ConnectionString ConnectionString
        {
            get { return __connectionString.Value; }
        }

        public static ConnectionString ConnectionStringWithMultipleShardRouters
        {
            get => __connectionStringWithMultipleShardRouters.Value;
        }

        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace.Value; }
        }

        public static ICluster ClusterWithConnectedPrimary
        {
            get
            {
                var timeout = __defaultServerSelectionTimeout.Value;
                var cluster = __cluster.Value;
                if (!SpinWait.SpinUntil(() => cluster.Description.Servers.Any(s => s.Type == ServerType.ReplicaSetPrimary), timeout))
                {
                    throw new Exception($"The cluster didn't find a primary during {timeout}. The current cluster description: {cluster.Description}.");
                }

                return cluster;
            }
        }

        public static MessageEncoderSettings MessageEncoderSettings
        {
            get { return __messageEncoderSettings; }
        }

        public static int NumberOfMongoses => __numberOfMongoses.Value;

        public static bool RequireApiVersion
        {
            get { return __serverApi.Value != null; }
        }

        public static ServerApi ServerApi
        {
            get { return __serverApi.Value; }
        }

        public static bool Serverless
        {
            get { return __serverless.Value; }
        }

        public static SemanticVersion ServerVersion => __serverVersion.Value;

        public static int MaxWireVersion => __maxWireVersion.Value;

        public static string StorageEngine => __storageEngine.Value;

        public static TraceSource TraceSource
        {
            get { return __traceSource; }
        }

        // static methods
        public static ClusterBuilder ConfigureCluster()
        {
            return ConfigureCluster(new ClusterBuilder());
        }

        public static ClusterBuilder ConfigureCluster(ClusterBuilder builder)
        {
            builder = builder
                .ConfigureWithConnectionString(__connectionString.Value, __serverApi.Value)
                .ConfigureCluster(c => c.With(serverSelectionTimeout: __defaultServerSelectionTimeout.Value));

            if (__connectionString.Value.Tls.HasValue &&
                __connectionString.Value.Tls.Value &&
                __connectionString.Value.AuthMechanism != null &&
                __connectionString.Value.AuthMechanism == MongoDBX509Authenticator.MechanismName)
            {
                var certificateFilename = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH");
                if (certificateFilename != null)
                {
                    builder.ConfigureSsl(ssl =>
                    {
                        var password = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD");
                        X509Certificate cert;
                        if (password == null)
                        {
                            cert = new X509Certificate2(certificateFilename);
                        }
                        else
                        {
                            cert = new X509Certificate2(certificateFilename, password);
                        }
                        return ssl.With(
                            clientCertificates: new[] { cert });
                    });
                }
            }

            return ConfigureLogging(builder);
        }

        public static ClusterBuilder ConfigureLogging(ClusterBuilder builder)
        {
            var environmentVariable = Environment.GetEnvironmentVariable("MONGO_LOGGING");
            if (environmentVariable == null)
            {
                return builder;
            }

            SourceLevels defaultLevel;
            if (!Enum.TryParse<SourceLevels>(environmentVariable, ignoreCase: true, result: out defaultLevel))
            {
                return builder;
            }

            __traceSource = new TraceSource("mongodb-tests", defaultLevel);
            __traceSource.Listeners.Clear(); // remove the default listener
            var listener = new TextWriterTraceListener(Console.Out);
            listener.TraceOutputOptions = TraceOptions.DateTime;
            __traceSource.Listeners.Add(listener);
            return builder.TraceWith(__traceSource);
        }

        public static ICluster CreateCluster()
        {
            return CreateCluster(b => b);
        }

        public static ICluster CreateCluster(Func<ClusterBuilder, ClusterBuilder> postConfigurator, bool allowDataBearingServers = false)
        {
            var builder = new ClusterBuilder();
            builder = ConfigureCluster(builder);
            builder = postConfigurator(builder);
            return CreateCluster(builder, allowDataBearingServers);
        }

        public static ICluster CreateCluster(ClusterBuilder builder, bool allowDataBearingServers = false)
        {
            var expectedServerKey = allowDataBearingServers ? "Databearing" : "Writable";

            bool hasExpectedServer = false;
            var cluster = builder.BuildCluster();
            cluster.DescriptionChanged += (o, e) =>
            {
                var anyExpectedServer = e.NewClusterDescription.Servers.Any(
                    description => allowDataBearingServers ? description.IsDataBearing : description.Type.IsWritable());
                if (__traceSource != null)
                {
                    __traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: DescriptionChanged event handler called.");
                    __traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: any{expectedServerKey}Server = {anyExpectedServer}.");
                    __traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: new description: {e.NewClusterDescription.ToString()}.");
                }
                Volatile.Write(ref hasExpectedServer, anyExpectedServer);
            };
            if (__traceSource != null)
            {
                __traceSource.TraceEvent(TraceEventType.Information, 0, "CreateCluster: initializing cluster.");
            }
            cluster.Initialize();

            // wait until the cluster has connected to the expected server
            if (!SpinWait.SpinUntil(() => Volatile.Read(ref hasExpectedServer), TimeSpan.FromSeconds(30)))
            {
                var message = $"Test cluster has no {expectedServerKey.ToLower()} server. Client view of the cluster is {cluster.Description}.";
                throw new Exception(message);
            }

            if (__traceSource != null)
            {
                __traceSource.TraceEvent(TraceEventType.Information, 0, $"CreateCluster: {expectedServerKey.ToLower()} server found.");
            }

            return cluster;
        }

        public static CollectionNamespace GetCollectionNamespaceForTestClass(Type testClassType)
        {
            var collectionName = TruncateCollectionNameIfTooLong(__databaseNamespace.Value, testClassType.Name);
            return new CollectionNamespace(__databaseNamespace.Value, collectionName);
        }

        public static CollectionNamespace GetCollectionNamespaceForTestMethod(string className, string methodName)
        {
            var collectionName = TruncateCollectionNameIfTooLong(__databaseNamespace.Value, $"{className}-{methodName}");
            return new CollectionNamespace(__databaseNamespace.Value, collectionName);
        }

        public static string GetCryptSharedLibPath() => Environment.GetEnvironmentVariable("CRYPT_SHARED_LIB_PATH");

        public static ConnectionString CreateConnectionString()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? Environment.GetEnvironmentVariable("MONGO_URI");
            if (uri == null)
            {
                uri = "mongodb://localhost";
                if (IsReplicaSet(uri))
                {
                    uri += "/?connect=replicaSet";
                }
            }

            return new ConnectionString(uri);
        }

        private static ConnectionString GetConnectionStringWithMultipleShardRouters()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_URI_WITH_MULTIPLE_MONGOSES") ?? "mongodb://localhost,localhost:27018";
            return new ConnectionString(uri);
        }

        private static DatabaseNamespace GetDatabaseNamespace()
        {
            if (!string.IsNullOrEmpty(__connectionString.Value.DatabaseName))
            {
                return new DatabaseNamespace(__connectionString.Value.DatabaseName);
            }

            var timestamp = DateTime.Now.ToString("MMddHHmm");
            return new DatabaseNamespace("Tests" + timestamp);
        }

        private static ServerApi GetServerApi()
        {
            var serverApiVersion = Environment.GetEnvironmentVariable("MONGODB_API_VERSION");

            if (serverApiVersion == null)
            {
                return null;
            }

            if (serverApiVersion != "1")
            {
                throw new ArgumentException($"Server API version \"{serverApiVersion}\" is not supported");
            }

            return new ServerApi(ServerApiVersion.V1);
        }

        private static bool GetServerless()
        {
            var serverless = Environment.GetEnvironmentVariable("SERVERLESS");

            return serverless?.ToLower() == "true";
        }

        public static DatabaseNamespace GetDatabaseNamespaceForTestClass(Type testClassType)
        {
            var databaseName = TruncateDatabaseNameIfTooLong(__databaseNamespace.Value.DatabaseName + "-" + testClassType.Name);
            if (databaseName.Length >= 64)
            {
                databaseName = databaseName.Substring(0, 63);
            }
            return new DatabaseNamespace(databaseName);
        }

        private static int GetMaxWireVersion()
        {
            using (var session = StartSession())
            using (var binding = CreateReadBinding(session))
            {
                var command = new BsonDocument("hello", 1);
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, __messageEncoderSettings);
                var response = operation.Execute(binding, CancellationToken.None);
                return response["maxWireVersion"].AsInt32;
            }
        }

        private static SemanticVersion GetServerVersion()
        {
            using (var session = StartSession())
            using (var binding = CreateReadBinding(session))
            {
                var command = new BsonDocument("buildinfo", 1);
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, __messageEncoderSettings);
                var response = operation.Execute(binding, CancellationToken.None);
                return SemanticVersion.Parse(response["version"].AsString);
            }
        }

        public static BsonDocument GetServerParameters()
        {
            using (var session = StartSession())
            using (var binding = CreateReadBinding(session))
            {
                var command = new BsonDocument("getParameter", new BsonString("*"));
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, __messageEncoderSettings);
                var serverParameters = operation.Execute(binding, CancellationToken.None);

                return serverParameters;
            }
        }

        public static ICoreSessionHandle StartSession()
        {
            return StartSession(__cluster.Value);
        }

        public static ICoreSessionHandle StartSession(ICluster cluster, CoreSessionOptions options = null)
        {
            if (AreSessionsSupported(cluster))
            {
                return cluster.StartSession(options);
            }
            else
            {
                return NoCoreSession.NewHandle();
            }
        }

        // private methods
        private static bool AreSessionsSupported(ICluster cluster)
        {
            SpinWait.SpinUntil(() => cluster.Description.Servers.Any(s => s.State == ServerState.Connected), TimeSpan.FromSeconds(30));
            return AreSessionsSupported(cluster.Description);
        }

        private static bool AreSessionsSupported(ClusterDescription clusterDescription)
        {
            return
                clusterDescription.Servers.Any(s => s.State == ServerState.Connected) &&
                (clusterDescription.LogicalSessionTimeout.HasValue || clusterDescription.Type == ClusterType.LoadBalanced);
        }

        private static IReadBindingHandle CreateReadBinding(ICoreSessionHandle session)
        {
            return CreateReadBinding(ReadPreference.Primary, session);
        }

        private static IReadBindingHandle CreateReadBinding(ReadPreference readPreference, ICoreSessionHandle session)
        {
            return CreateReadBinding(__cluster.Value, readPreference, session);
        }

        private static IReadBindingHandle CreateReadBinding(ICluster cluster, ReadPreference readPreference, ICoreSessionHandle session)
        {
            var binding = new ReadPreferenceBinding(cluster, readPreference, session.Fork());
            return new ReadBindingHandle(binding);
        }

        private static IReadWriteBindingHandle CreateReadWriteBinding(ICoreSessionHandle session)
        {
            var binding = new WritableServerBinding(__cluster.Value, session.Fork());
            return new ReadWriteBindingHandle(binding);
        }

        private static void DropDatabase()
        {
            var operation = new DropDatabaseOperation(__databaseNamespace.Value, __messageEncoderSettings);

            using (var session = StartSession())
            using (var binding = CreateReadWriteBinding(session))
            {
                operation.Execute(binding, CancellationToken.None);
            }
        }

        private static IEnumerable<BsonDocument> FindDocuments(ICluster cluster, CollectionNamespace collectionNamespace)
        {
            using (var session = StartSession(cluster))
            using (var binding = CreateReadBinding(cluster, ReadPreference.Primary, session))
            {
                var operation = new FindOperation<BsonDocument>(collectionNamespace, BsonDocumentSerializer.Instance, __messageEncoderSettings);

                return operation.Execute(binding, CancellationToken.None).ToList();
            }
        }

        private static int GetNumberOfMongoses()
        {
            var clusterType = __cluster.Value.Description.Type;
            if (clusterType == ClusterType.Sharded || clusterType == ClusterType.LoadBalanced)
            {
                var mongosCollection = new CollectionNamespace("config", "mongos");
                var mongosDocuments = FindDocuments(__cluster.Value, mongosCollection).ToList();
                return mongosDocuments.Count;
            }
            else
            {
                throw new InvalidOperationException("Cluster is not sharded or load balanced.");
            }
        }

        private static TimeSpan GetDefaultServerSelectionTimeout()
        {
            var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
            if (serverSelectionTimeoutString == null)
            {
                serverSelectionTimeoutString = "30000";
            }

            return TimeSpan.FromMilliseconds(int.Parse(serverSelectionTimeoutString));
        }

        private static string GetStorageEngine()
        {
            string result;

            var clusterType = __cluster.Value.Description.Type;
            switch (clusterType)
            {
                case ClusterType.LoadBalanced:
                case var _ when Serverless:
                    // Load balancing and serverless are only supported for servers higher than 50
                    result = "wiredTiger";
                    break;
                case ClusterType.Sharded:
                    {
                        // mongos cannot provide this data directly, so we need connection to a particular mongos shard
                        var shardsCollection = new CollectionNamespace("config", "shards");
                        var shards = FindDocuments(__cluster.Value, shardsCollection).FirstOrDefault();
                        if (shards != null)
                        {
                            var fullHosts = shards["host"].AsString; // for example: "shard01/localhost:27018,localhost:27019,localhost:27020"
                            var firstHost = fullHosts.Substring(fullHosts.IndexOf('/') + 1).Split(',')[0];
                            using (var cluster = CreateCluster(
                                configurator => configurator.ConfigureCluster(cs => cs.With(endPoints: new[] { EndPointHelper.Parse(firstHost) })),
                                allowDataBearingServers: true))
                            {
                                result = GetStorageEngineForCluster(cluster);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("mongos has not been found.");
                        }
                        break;
                    }

                default:
                    result = GetStorageEngineForCluster(__cluster.Value);
                    break;
            }

            return result ?? "mmapv1";

            string GetStorageEngineForCluster(ICluster cluster)
            {
                var command = new BsonDocument("serverStatus", 1);
                using (var session = StartSession(cluster))
                using (var binding = CreateReadBinding(cluster, ReadPreference.PrimaryPreferred, session))
                {
                    var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, __messageEncoderSettings);

                    var response = operation.Execute(binding, CancellationToken.None);
                    if (response.TryGetValue("storageEngine", out var storageEngine) && storageEngine.AsBsonDocument.TryGetValue("name", out var name))
                    {
                        return name.AsString;
                    }

                    return null;
                }
            }
        }

        private static bool IsReplicaSet(string uri)
        {
            var clusterBuilder = new ClusterBuilder().ConfigureWithConnectionString(uri, __serverApi.Value);

            using (var cluster = clusterBuilder.BuildCluster())
            {
                cluster.Initialize();

                var serverSelector = new ReadPreferenceServerSelector(ReadPreference.PrimaryPreferred);
                var server = cluster.SelectServer(serverSelector, CancellationToken.None);
                return server.Description.Type.IsReplicaSetMember();
            }
        }

        public static void TearDown()
        {
            if (__cluster.IsValueCreated)
            {
                // TODO: DropDatabase
                //DropDatabase();
                __cluster.Value.Dispose();
                __cluster = new Lazy<ICluster>(CreateCluster, isThreadSafe: true);
            }
        }

        private static string TruncateCollectionNameIfTooLong(DatabaseNamespace databaseNamespace, string collectionName)
        {
            var fullNameLength = databaseNamespace.DatabaseName.Length + 1 + collectionName.Length;
            if (fullNameLength <= 120)
            {
                return collectionName;
            }
            else
            {
                var maxCollectionNameLength = 120 - (databaseNamespace.DatabaseName.Length + 1);
                return collectionName.Substring(0, maxCollectionNameLength - 1);
            }
        }

        private static string TruncateDatabaseNameIfTooLong(string databaseName)
        {
            if (databaseName.Length < 64)
            {
                return databaseName;
            }
            else
            {
                return databaseName.Substring(0, 63);
            }
        }
        #endregion
    }
}
