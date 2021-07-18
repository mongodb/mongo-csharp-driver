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
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver
{
    public static class CoreTestConfiguration
    {
        #region static
        // static fields
        private static readonly Lazy<ICluster> __cluster;
        private static readonly ClusterTestWrapper __clusterTestWrapper;
        private static readonly CoreEnvironmentConfiguration __coreEnvironmentConfiguration;
        private static readonly Lazy<DatabaseNamespace> __databaseNamespace;
        private static TraceSource __traceSource = null;
        private static readonly MessageEncoderSettings __messageEncoderSettings;

        static CoreTestConfiguration()
        {
            __coreEnvironmentConfiguration = new CoreEnvironmentConfiguration();
            __cluster = new Lazy<ICluster>(CreateCluster, isThreadSafe: true);
            __clusterTestWrapper = new ClusterTestWrapper(__cluster.Value);
            __databaseNamespace = new Lazy<DatabaseNamespace>(GetDatabaseNamespace, isThreadSafe: true);
            __messageEncoderSettings = new MessageEncoderSettings();
        }

        // static properties
        public static ICluster Cluster
        {
            get { return __cluster.Value; }
        }

        public static ConnectionString ConnectionString
        {
            get { return __coreEnvironmentConfiguration.DefaultConnectionString; }
        }

        public static CoreEnvironmentConfiguration DefaultCoreEnvironmentConfiguration
        {
            get { return __coreEnvironmentConfiguration; }
        }

        public static ConnectionString ConnectionStringWithMultipleShardRouters
        {
            get => __coreEnvironmentConfiguration.MultipleShardRoutersConnectionString;
        }

        public static DatabaseNamespace DatabaseNamespace
        {
            get { return __databaseNamespace.Value; }
        }

        public static MessageEncoderSettings MessageEncoderSettings
        {
            get { return __messageEncoderSettings; }
        }

        public static bool RequireApiVersion
        {
            get { return __coreEnvironmentConfiguration.ServerApi != null; }
        }

        public static ServerApi ServerApi
        {
            get { return __coreEnvironmentConfiguration.ServerApi; }
        }

        public static SemanticVersion ServerVersion => __clusterTestWrapper.ServerVersion;

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
            builder = ClusterBuilderHelper.BaseConfigureCluster(__coreEnvironmentConfiguration, builder);
            if (ClusterBuilderHelper.TryCreateTraceSourceIfConfigured(__coreEnvironmentConfiguration, out var traceSource))
            {
                __traceSource = traceSource;
                builder = ClusterBuilderHelper.ConfigureLogging(builder, traceSource);
            }
            return builder;
        }

        public static ICluster CreateCluster()
        {
            return CreateCluster(b => b);
        }

        public static ICluster CreateCluster(ClusterBuilder builder)
        {
            return ClusterBuilderHelper.CreateCluster(builder, __traceSource);
        }

        public static ICluster CreateCluster(Func<ClusterBuilder, ClusterBuilder> postConfigurator)
        {
            var builder = new ClusterBuilder();
            builder = ConfigureCluster(builder);
            builder = postConfigurator(builder);
            return CreateCluster(builder);
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

        private static DatabaseNamespace GetDatabaseNamespace()
        {
            if (!string.IsNullOrEmpty(__coreEnvironmentConfiguration.DefaultConnectionString.DatabaseName))
            {
                return new DatabaseNamespace(__coreEnvironmentConfiguration.DefaultConnectionString.DatabaseName);
            }

            var timestamp = DateTime.Now.ToString("MMddHHmm");
            return new DatabaseNamespace("Tests" + timestamp);
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

        public static BsonDocument GetServerParameters() => __clusterTestWrapper.GetServerParameters();

        public static string GetStorageEngine() => __clusterTestWrapper.GetStorageEngine();

        public static ICoreSessionHandle StartSession(ICluster cluster, CoreSessionOptions options = null)
        {
            return new ClusterTestWrapper(cluster).StartSession(options);
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
