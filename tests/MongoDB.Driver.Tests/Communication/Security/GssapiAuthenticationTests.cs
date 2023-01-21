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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "GssapiMechanism")]
    public class GssapiAuthenticationTests
    {
        private static readonly string __collectionName = "test";

        [Fact]
        public void TestNoCredentials()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var mongoUrl = CreateMongoUrl();
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            clientSettings.Credential = null;
            var client = new MongoClient(clientSettings);
            var collection = GetTestCollection(client, mongoUrl.DatabaseName);

            var exception = Record.Exception(() => { collection.CountDocuments(new BsonDocument()); });
            var e = exception.Should().BeOfType<MongoCommandException>().Subject;
            e.CodeName.Should().Be("Unauthorized");
        }


        [Fact]
        public void TestSuccessfulAuthentication()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var mongoUrl = CreateMongoUrl();
            var client = new MongoClient(mongoUrl);

            var collection = GetTestCollection(client, mongoUrl.DatabaseName);
            var result = collection
                .FindSync(new BsonDocument())
                .ToList();

            result.Should().NotBeNull();
        }

        [Fact]
        public void TestBadPassword()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");

            var mongoUrl = CreateMongoUrl();
            var currentCredentialUsername = mongoUrl.Username;
            var clientSettings = MongoClientSettings.FromUrl(mongoUrl);
            clientSettings.Credential = MongoCredential.CreateGssapiCredential(currentCredentialUsername, "wrongPassword");

            var client = new MongoClient(clientSettings);
            var collection = GetTestCollection(client, mongoUrl.DatabaseName);

            var exception = Record.Exception(() => { collection.FindSync(new BsonDocument()).ToList(); });
            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        // private methods
        private string CreateGssapiConnectionString(string authHost, string mechanismProperty = null)
        {
            var authGssapi = GetEnvironmentVariable("AUTH_GSSAPI");

            return $"mongodb://{authGssapi}@{authHost}/kerberos?authMechanism=GSSAPI{mechanismProperty}";
        }

        private MongoUrl CreateMongoUrl()
        {
            var authHost = GetEnvironmentVariable("AUTH_HOST");
            var connectionString = CreateGssapiConnectionString(authHost);
            return MongoUrl.Create(connectionString);
        }

        private string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? throw new Exception($"{name} has not been configured.");

        private IMongoCollection<BsonDocument> GetTestCollection(MongoClient client, string databaseName)
        {
            return client
                .GetDatabase(databaseName)
                .GetCollection<BsonDocument>(__collectionName);
        }
    }
}
