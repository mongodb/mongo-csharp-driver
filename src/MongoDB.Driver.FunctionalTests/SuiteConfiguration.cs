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
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using NUnit.Framework;

namespace MongoDB.Driver
{
    [SetUpFixture]
    public class SuiteConfiguration
    {
        // fields
        private static MongoClient __client;
        private static string __databaseName;
        private static MongoUrl __url;

        // properties
        public static MongoClient Client
        {
            get { return __client; }
        }

        public static string DatabaseName
        {
            get { return __databaseName; }
        }

        public static MongoUrl Url
        {
            get { return __url; }
        }

        // methods
        public static MongoClient CreateClient()
        {
            var settings = MongoClientSettings.FromUrl(__url);

            if (settings.UseSsl)
            {
                var certificateFilename = Environment.GetEnvironmentVariable("MONGO_SSL_CERT_FILE");
                if (certificateFilename != null)
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
                    settings.SslSettings = new SslSettings
                    {
                        ClientCertificates = new[] { cert }
                    };
                }
            }

            return new MongoClient(settings);
        }

        private void DropDatabase()
        {
            // TODO: implement DropDatabase
        }

        [SetUp]
        public void SuiteConfigurationSetup()
        {
            __url = new MongoUrl(Environment.GetEnvironmentVariable("MONGO_URI") ?? "mongodb://localhost");
            __client = CreateClient();

            __databaseName = string.Format("MongoDB-Driver-FunctionalTests-{0}", DateTime.Now.ToString("s").Replace(':', '-'));
        }

        [TearDown]
        public void SuiteConfigurationTearDown()
        {
            DropDatabase();
        }
    }
}
