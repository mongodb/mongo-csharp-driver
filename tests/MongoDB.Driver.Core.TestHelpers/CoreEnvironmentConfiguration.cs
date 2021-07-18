/* Copyright 2021-present MongoDB Inc.
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
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.TestHelpers
{
    public class CoreEnvironmentConfiguration
    {
        private readonly Lazy<ConnectionString> _defaultConnectionString;
        private readonly Lazy<SourceLevels?> _logging;
        private readonly Lazy<ConnectionString> _multipleShardRoutersConnectionString;
        private readonly Lazy<ServerApi> _serverApi;
        private readonly Lazy<int> _defaultServerSelection;
        private readonly Lazy<X509Certificate2> _x509Certificate2;

        public CoreEnvironmentConfiguration()
        {
            _defaultConnectionString = new Lazy<ConnectionString>(() => GetDefaultConnectionString(), isThreadSafe: true);
            _logging = new Lazy<SourceLevels?>(() => GetLogging(), isThreadSafe: true);
            _multipleShardRoutersConnectionString = new Lazy<ConnectionString>(() => GetMultipleShardRoutersConnectionString(), isThreadSafe: true);
            _serverApi = new Lazy<ServerApi>(() => GetServerApi(), isThreadSafe: true);
            _defaultServerSelection = new Lazy<int>(() => GetServerSelection(), isThreadSafe: true);
            _x509Certificate2 = new Lazy<X509Certificate2>(() => GetX509Certificate2(), isThreadSafe: true);
        }

        public int DefaultServerSelection => _defaultServerSelection.Value;
#pragma warning disable CA1721 // Property names should not match get methods
        public ConnectionString DefaultConnectionString => _defaultConnectionString.Value;
#pragma warning restore CA1721 // Property names should not match get methods
        public SourceLevels? Logging => _logging.Value;
#pragma warning disable CA1721 // Property names should not match get methods
        public ConnectionString MultipleShardRoutersConnectionString => _multipleShardRoutersConnectionString.Value;
#pragma warning restore CA1721 // Property names should not match get methods
        public ServerApi ServerApi => _serverApi.Value;
        public X509Certificate2 X509Certificate2 => _x509Certificate2.Value;

        // protected methods
        protected virtual ConnectionString GetDefaultConnectionString()
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

            var connectionString = new ConnectionString(uri);
            if (connectionString.LoadBalanced)
            {
                // TODO: temporary solution until server will actually support serviceId
                ServiceIdHelper.IsServiceIdEmulationEnabled = true;
            }
            return connectionString;
        }

        protected virtual ConnectionString GetMultipleShardRoutersConnectionString()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_URI_WITH_MULTIPLE_MONGOSES") ?? "mongodb://localhost,localhost:27018";
            var connectionString = new ConnectionString(uri);
            if (connectionString.LoadBalanced)
            {
                // TODO: temporary solution until server will actually support serviceId
                ServiceIdHelper.IsServiceIdEmulationEnabled = true;
            }
            return connectionString;
        }

        // private methods
        private SourceLevels? GetLogging()
        {
            var mongoLogging = Environment.GetEnvironmentVariable("MONGO_LOGGING");
            if (mongoLogging != null && Enum.TryParse<SourceLevels>(mongoLogging, ignoreCase: true, result: out var defaultLevel))
            {
                return defaultLevel;
            }

            return null;
        }

        private ServerApi GetServerApi()
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

        private int GetServerSelection()
        {
            var serverSelectionTimeoutString = Environment.GetEnvironmentVariable("MONGO_SERVER_SELECTION_TIMEOUT_MS");
            if (serverSelectionTimeoutString == null)
            {
                serverSelectionTimeoutString = "30000";
            }
            return int.Parse(serverSelectionTimeoutString);
        }

        private X509Certificate2 GetX509Certificate2()
        {
            var certificateFilename = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PATH");
            if (certificateFilename != null)
            {
                var password = Environment.GetEnvironmentVariable("MONGO_X509_CLIENT_CERTIFICATE_PASSWORD");
                return password == null
                    ? new X509Certificate2(certificateFilename)
                    : new X509Certificate2(certificateFilename, password);
            }

            return null;
        }

        private bool IsReplicaSet(string uri)
        {
            var clusterBuilder = new ClusterBuilder().ConfigureWithConnectionString(uri, _serverApi.Value);

            using (var cluster = clusterBuilder.BuildCluster())
            {
                cluster.Initialize();

                var serverSelector = new ReadPreferenceServerSelector(ReadPreference.PrimaryPreferred);
                var server = cluster.SelectServer(serverSelector, CancellationToken.None);
                return server.Description.Type.IsReplicaSetMember();
            }
        }
    }
}
