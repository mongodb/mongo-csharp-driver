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

        public GssapiAuthenticationTests()
        {
            RequireEnvironment.Check().EnvironmentVariable("GSSAPI_TESTS_ENABLED");
        }

        [Fact]
        public void TestNoCredentials()
        {
            var clientSettings = CreateMongoClientSettings();
            clientSettings.Credential = null;
            var client = new MongoClient(clientSettings);
            var collection = GetTestCollection(client);

            var exception = Record.Exception(() => { collection.FindSync(new BsonDocument()).ToList(); });
            var e = exception.Should().BeOfType<MongoCommandException>().Subject;
            e.CodeName.Should().Be("Unauthorized");
        }


        [Fact]
        public void TestSuccessfulAuthentication()
        {
            var clientSettings = CreateMongoClientSettings();
            var client = new MongoClient(clientSettings);

            var collection = GetTestCollection(client);
            var result = collection
                .FindSync(new BsonDocument())
                .ToList();

            result.Should().NotBeNull();
        }

        [Fact]
        public void TestBadPassword()
        {
            var clientSettings = CreateMongoClientSettings();
            clientSettings.Credential = MongoCredential.CreateGssapiCredential(clientSettings.Credential.Username, "wrongPassword");

            var client = new MongoClient(clientSettings);
            var collection = GetTestCollection(client);

            var exception = Record.Exception(() => { collection.FindSync(new BsonDocument()).ToList(); });
            exception.Should().BeOfType<MongoAuthenticationException>();
        }

        // private methods
        private MongoClientSettings CreateMongoClientSettings()
        {
            var authHost = GetEnvironmentVariable("AUTH_HOST");
            var authDb = GetEnvironmentVariable("AUTH_DATABASE");
            var username = GetEnvironmentVariable("GSSAPI_PRINCIPAL");
            var password = GetEnvironmentVariable("GSSAPI_PASS");

            var connectionString = $"mongodb://{authHost}/{authDb}";

            var result = MongoClientSettings.FromConnectionString(connectionString);
            result.Credential = MongoCredential.CreateGssapiCredential(username, password);

            return result;
        }

        private string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name) ?? throw new Exception($"{name} has not been configured.");

        private IMongoCollection<BsonDocument> GetTestCollection(MongoClient client)
        {
            return client
                .GetDatabase(GetEnvironmentVariable("AUTH_DATABASE"))
                .GetCollection<BsonDocument>(__collectionName);
        }
    }
}
