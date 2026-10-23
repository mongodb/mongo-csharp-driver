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
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace AtlasConnectivity.Tests
{
    [Trait("Category", "Integration")]
    public class ConnectivityTests : LoggableTestClass
    {
        public ConnectivityTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_CONNECTIVITY_TESTS_ENABLED");
        }

        [Theory]
        [InlineData("ATLAS_REPL")]
        [InlineData("ATLAS_SHRD")]
        [InlineData("ATLAS_FREE")]
        [InlineData("ATLAS_TLS11")]
        [InlineData("ATLAS_TLS12")]
        [InlineData("ATLAS_SRV_REPL")]
        [InlineData("ATLAS_SRV_SHRD")]
        [InlineData("ATLAS_SRV_FREE")]
        [InlineData("ATLAS_SRV_TLS11")]
        [InlineData("ATLAS_SRV_TLS12")]
        public void Connection_to_Atlas_should_work(string environmentVariableName)
        {
            var connectionString = GetRequiredEnvironmentVariable(environmentVariableName);

            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            ExecuteTestCase(mongoClientSettings, true);
        }

        [Fact]
        public void Secure_Frontend_Processor_Unauthenticated_Tests()
        {
            var connectionString = GetRequiredEnvironmentVariable("SFP_ATLAS_URI");

            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            ExecuteTestCase(mongoClientSettings, false);
        }

        [Theory]
        [InlineData("SCRAM-SHA-256", CompressorType.Noop, false)]
        [InlineData("SCRAM-SHA-256", CompressorType.Zlib, false)]
        [InlineData("SCRAM-SHA-256", CompressorType.Noop, true)]
        [InlineData("X.509", CompressorType.Noop, false)]
        [InlineData("X.509", CompressorType.Zlib, false)]
        [InlineData("X.509", CompressorType.Noop, true)]
        public void Secure_Frontend_Processor_Authenticated_Tests(string authMechanism, CompressorType compressor, bool useServerApi)
        {
            string connectionString;
            MongoCredential credential;
            X509Certificate2 clientCertificate = null;

            switch (authMechanism)
            {
                case "SCRAM-SHA-256":
                    connectionString = GetRequiredEnvironmentVariable("SFP_ATLAS_URI");
                    credential = MongoCredential.CreateCredential(null,
                        GetRequiredEnvironmentVariable("SFP_ATLAS_USER"), GetRequiredEnvironmentVariable("SFP_ATLAS_PASSWORD"));
                    break;
                case "X.509":
                    connectionString = GetRequiredEnvironmentVariable("SFP_ATLAS_X509_URI");
                    credential = MongoCredential.CreateMongoX509Credential();

                    var certPath = GetRequiredEnvironmentVariable("SFP_ATLAS_X509_CERT_PATH");
                    var certPassword = GetRequiredEnvironmentVariable("SFP_ATLAS_X509_CERT_PASSWORD");

                    clientCertificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(authMechanism), authMechanism, null);
            }

            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.Credential = credential;
            if (clientCertificate != null)
            {
                mongoClientSettings.UseTls = true;
                mongoClientSettings.SslSettings ??= new SslSettings();
                mongoClientSettings.SslSettings.ClientCertificates = [clientCertificate];
            }
            if (compressor != CompressorType.Noop)
            {
                mongoClientSettings.Compressors = new List<CompressorConfiguration>
                {
                    new CompressorConfiguration(compressor)
                };
            }
            if (useServerApi)
            {
                mongoClientSettings.ServerApi = new ServerApi(ServerApiVersion.V1);
            }

            try
            {
                ExecuteTestCase(mongoClientSettings, true);
            }
            finally
            {
                clientCertificate?.Dispose();
            }
        }

        private static string GetRequiredEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Environment variable '{name}' must be set.");
            }

            return value;
        }

        private void ExecuteTestCase(MongoClientSettings mongoClientSettings, bool isAuthenticated)
        {
            mongoClientSettings.ClusterSource = DisposingClusterSource.Instance;
            using var client = new MongoClient(mongoClientSettings);

            var db = client.GetDatabase("db");
            var collectionName = $"sfp_test_{ObjectId.GenerateNewId()}";

            try
            {
                AssertPing(client);
                AssertConnectionStatus(client, isAuthenticated);
                if (isAuthenticated)
                {
                    AssertCrud(db.GetCollection<BsonDocument>(collectionName));
                }
            }
            finally
            {
                if (isAuthenticated)
                {
                    try
                    {
                        db.DropCollection(collectionName);
                    }
                    catch
                    {
                        // suppress any exceptions here to not mask the real test result in case of exception while dropping the collection
                    }
                }
            }
        }

        private void AssertPing(IMongoClient client)
        {
            var adminDatabase = client.GetDatabase("admin");
            var command = new BsonDocument("ping", 1);
            var response = adminDatabase.RunCommand<BsonDocument>(command);
            response["ok"].ToDouble().Should().Be(1);
        }

        private void AssertConnectionStatus(IMongoClient client, bool isAuthenticated)
        {
            var adminDatabase = client.GetDatabase("admin");
            var command = new BsonDocument("connectionStatus", 1);
            var response = adminDatabase.RunCommand<BsonDocument>(command);
            response["ok"].ToDouble().Should().Be(1);
            var authenticatedUsers = response["authInfo"].AsBsonDocument["authenticatedUsers"].AsBsonArray;
            if (isAuthenticated)
            {
                authenticatedUsers.Count.Should().BeGreaterOrEqualTo(1);
            }
            else
            {
                authenticatedUsers.Count.Should().Be(0);
            }
        }

        private void AssertCrud(IMongoCollection<BsonDocument> collection)
        {
            var randomFieldValue = Guid.NewGuid().ToString();

            collection.InsertOne(new BsonDocument()
            {
                { "_id", 1 },
                { "field", randomFieldValue }
            });

            var filter = Builders<BsonDocument>.Filter.Eq("_id", 1);
            var findResult = collection.Find(filter).Single();

            findResult.Should().NotBeNull();
            findResult["field"].AsString.Should().Be(randomFieldValue);
        }
    }
}
