/* Copyright 2010-2014 MongoDB Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver
{
    public static class CoreTestConfiguration
    {
        #region static
        // static fields
        private static Lazy<ICluster> __cluster = new Lazy<ICluster>(CreateCluster, isThreadSafe: true);
        private static ConnectionString __connectionString = GetConnectionString();
        private static DatabaseNamespace __databaseNamespace = GetDatabaseNamespace();
        private static MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();

        // static properties
        public static ICluster Cluster
        {
            get { return __cluster.Value; }
        }

        public static ConnectionString ConnectionString
        {
            get { return __connectionString; }
        }

        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace; }
        }

        public static MessageEncoderSettings MessageEncoderSettings
        {
            get { return __messageEncoderSettings; }
        }

        public static SemanticVersion ServerVersion
        {
            get
            {
                var server = __cluster.Value.SelectServerAsync(WritableServerSelector.Instance, CancellationToken.None).GetAwaiter().GetResult();
                return server.Description.Version;
            }
        }

        // static methods
        public static ClusterBuilder ConfigureCluster()
        {
            var builder = new ClusterBuilder()
                .ConfigureWithConnectionString(__connectionString)
                .ConfigureCluster(c => c.With(serverSelectionTimeout: TimeSpan.FromMilliseconds(500)));

            if (__connectionString.Ssl.HasValue && __connectionString.Ssl.Value)
            {
                var certificateFilename = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_FILE");
                if (certificateFilename != null)
                {
                    builder.ConfigureSsl(ssl =>
                    {
                        var password = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_PASS");
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

            return builder;
        }

        public static ICluster CreateCluster()
        {
            var hasWritableServer = false;
            var builder = ConfigureCluster();
            var cluster = builder.BuildCluster();
            cluster.DescriptionChanged += (o, e) =>
            {
                hasWritableServer = e.NewClusterDescription.Servers.Any(
                    description => description.Type.IsWritable());
            };
            cluster.Initialize();

            // wait until the cluster has connected to a writable server
            SpinWait.SpinUntil(() => hasWritableServer, TimeSpan.FromSeconds(30));
            if (!hasWritableServer)
            {
                var message = string.Format(
                    "Test cluster has no writable server. Client view of the cluster is {0}.",
                    cluster.Description.ToString());
                throw new Exception(message);
            }

            return cluster;
        }

        public static CollectionNamespace GetCollectionNamespaceForTestFixture()
        {
            var testFixtureType = GetTestFixtureTypeFromCallStack();
            var collectionName = TruncateCollectionNameIfTooLong(__databaseNamespace, testFixtureType.Name);
            return new CollectionNamespace(__databaseNamespace, collectionName);
        }

        public static CollectionNamespace GetCollectionNamespaceForTestMethod()
        {
            var testMethodInfo = GetTestMethodInfoFromCallStack();
            var collectionName = TruncateCollectionNameIfTooLong(__databaseNamespace, testMethodInfo.DeclaringType.Name + "-" + testMethodInfo.Name);
            return new CollectionNamespace(__databaseNamespace, collectionName);
        }

        private static ConnectionString GetConnectionString()
        {
            return new ConnectionString(Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost");
        }

        private static DatabaseNamespace GetDatabaseNamespace()
        {
            var timestamp = DateTime.Now.ToString("MMddHHmm");
            return new DatabaseNamespace("Tests" + timestamp);
        }

        public static DatabaseNamespace GetDatabaseNamespaceForTestFixture()
        {
            var testFixtureType = GetTestFixtureTypeFromCallStack();
            var databaseName = TruncateDatabaseNameIfTooLong(__databaseNamespace.DatabaseName + "-" + testFixtureType.Name);
            return new DatabaseNamespace(databaseName);
        }

        public static IReadBinding GetReadBinding()
        {
            return GetReadBinding(ReadPreference.Primary);
        }

        public static IReadBinding GetReadBinding(ReadPreference readPreference)
        {
            return new ReadPreferenceBinding(__cluster.Value, readPreference);
        }

        public static IReadWriteBinding GetReadWriteBinding()
        {
            return new WritableServerBinding(__cluster.Value);
        }

        public static string GetStorageEngine()
        {
            using (var binding = GetReadWriteBinding())
            {
                var command = new BsonDocument("serverStatus", 1);
                var operation = new ReadCommandOperation<BsonDocument>(DatabaseNamespace.Admin, command, BsonDocumentSerializer.Instance, __messageEncoderSettings);
                var response = operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
                BsonValue storageEngine;
                if (response.TryGetValue("storageEngine", out storageEngine) && storageEngine.AsBsonDocument.Contains("name"))
                {
                    return storageEngine["name"].AsString;
                }
                else
                {
                    return "mmapv1";
                }
            }
        }

        private static Type GetTestFixtureTypeFromCallStack()
        {
            var stackTrace = new StackTrace();
            for (var index = 0; index < stackTrace.FrameCount; index++)
            {
                var frame = stackTrace.GetFrame(index);
                var methodInfo = frame.GetMethod();
                var declaringType = methodInfo.DeclaringType;
                var testFixtureAttribute = declaringType.GetCustomAttribute<TestFixtureAttribute>(inherit: false);
                if (testFixtureAttribute != null)
                {
                    return declaringType;
                }
            }

            throw new Exception("No [TestFixture] found on the call stack.");
        }

        private static MethodInfo GetTestMethodInfoFromCallStack()
        {
            var stackTrace = new StackTrace();
            for (var index = 0; index < stackTrace.FrameCount; index++)
            {
                var frame = stackTrace.GetFrame(index);
                var methodInfo = frame.GetMethod() as MethodInfo;
                if (methodInfo != null)
                {
                    var testAttribute = methodInfo.GetCustomAttribute<TestAttribute>(inherit: false);
                    if (testAttribute != null)
                    {
                        return methodInfo;
                    }
                    var testCaseAttribute = methodInfo.GetCustomAttribute<TestCaseAttribute>(inherit: false);
                    if (testCaseAttribute != null)
                    {
                        return methodInfo;
                    }
                }
            }

            throw new Exception("No [TestFixture] found on the call stack.");
        }

        private static string TruncateCollectionNameIfTooLong(DatabaseNamespace databaseNamespace, string collectionName)
        {
            var fullNameLength = databaseNamespace.DatabaseName.Length + 1 + collectionName.Length;
            if (fullNameLength < 123)
            {
                return collectionName;
            }
            else
            {
                var maxCollectionNameLength = 123 - (databaseNamespace.DatabaseName.Length + 1);
                return collectionName.Substring(0, maxCollectionNameLength);
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

        // methods
        private static void DropDatabase()
        {
            var operation = new DropDatabaseOperation(__databaseNamespace, __messageEncoderSettings);

            using (var binding = GetReadWriteBinding())
            {
                operation.ExecuteAsync(binding, CancellationToken.None).GetAwaiter().GetResult();
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
    }
}
