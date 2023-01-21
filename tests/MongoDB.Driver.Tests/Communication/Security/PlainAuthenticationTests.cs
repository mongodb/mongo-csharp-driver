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
using System.Linq;
using MongoDB.Bson;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver;
using Xunit;

namespace MongoDB.Driver.Tests.Communication.Security
{
    [Trait("Category", "Authentication")]
    [Trait("Category", "PlainMechanism")]
    public class PlainAuthenticationTests
    {
        private static readonly string __collectionName = "test";

        private MongoClientSettings _settings;

        public PlainAuthenticationTests()
        {
            _settings = MongoClientSettings.FromUrl(new MongoUrl(CoreTestConfiguration.ConnectionString.ToString()));
        }

        [Fact]
        public void TestNoCredentials()
        {
            RequireEnvironment.Check().EnvironmentVariable("PLAIN_AUTH_TESTS_ENABLED");
            _settings.Credential = null;
            var client = new MongoClient(_settings);

            Assert.Throws<MongoCommandException>(() =>
            {
#pragma warning disable 618
                client
                    .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(__collectionName)
                    .Count(new BsonDocument());
#pragma warning restore
            });
        }

        [Fact]
        public void TestSuccessfulAuthentication()
        {
            RequireEnvironment.Check().EnvironmentVariable("PLAIN_AUTH_TESTS_ENABLED");
            var client = new MongoClient(_settings);

            var result = client
                .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonDocument>(__collectionName)
                .FindSync(new BsonDocument())
                .ToList();

            Assert.NotNull(result);
        }

        [Fact]
        public void TestBadPassword()
        {
            RequireEnvironment.Check().EnvironmentVariable("PLAIN_AUTH_TESTS_ENABLED");
            var currentCredential = _settings.Credential;
            _settings.Credential = MongoCredential.CreatePlainCredential(currentCredential.Source, currentCredential.Username, "wrongPassword");

            var client = new MongoClient(_settings);

            Assert.Throws<MongoAuthenticationException>(() =>
            {
                client
                    .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(__collectionName)
                    .FindSync(new BsonDocument())
                    .ToList();
            });
        }
    }
}
