/* Copyright 2013-2014 MongoDB Inc.
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
using System.Threading;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [SetUpFixture]
    public class SuiteConfiguration
    {
        // fields
        private static ICluster __cluster;
        private static ConnectionString __connectionString;
        private static string __databaseName;

        // properties
        public static ICluster Cluster
        {
            get { return __cluster; }
        }

        public static ConnectionString ConnectionString
        {
            get { return __connectionString; }
        }

        public static string DatabaseName
        {
            get { return __databaseName; }
        }

        // methods
        public static ClusterBuilder ConfigureCluster()
        {
            var builder = new ClusterBuilder().ConfigureWithConnectionString(__connectionString);

            if (__connectionString.Ssl.HasValue && __connectionString.Ssl.Value)
            {
                var certificateFilename = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_FILE");
                if (certificateFilename != null)
                {
                    // TODO: configure SSL
                    //builder.ConfigureSsl(ssl =>
                    //{
                    //    var password = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_PASS");
                    //    X509Certificate cert;
                    //    if (password == null)
                    //    {
                    //        cert = new X509Certificate2(certificateFilename);
                    //    }
                    //    else
                    //    {
                    //        cert = new X509Certificate2(certificateFilename, password);
                    //    }
                    //    ssl.AddClientCertificate(cert);
                    //});
                }
            }

            return builder;
        }

        public static ICluster CreateCluster()
        {
            var builder = ConfigureCluster();
            var cluster = builder.BuildCluster();
            cluster.Initialize();
            return cluster;
        }

        private void DropDatabase()
        {
            var operation = new DropDatabaseOperation(
                new DatabaseNamespace(__databaseName),
                new MessageEncoderSettings());

            using(var binding = new WritableServerBinding(__cluster))
            {
                operation.ExecuteAsync(binding, TimeSpan.FromSeconds(10), CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        [SetUp]
        public void SuiteConfigurationSetup()
        {
            __connectionString = new ConnectionString(Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost");

            __cluster = CreateCluster();

            __databaseName = string.Format("FunctionalTests-{0}", DateTime.Now.ToString("s").Replace(':', '-'));
        }

        [TearDown]
        public void SuiteConfigurationTearDown()
        {
            DropDatabase();
            __cluster.Dispose();
        }
    }
}
