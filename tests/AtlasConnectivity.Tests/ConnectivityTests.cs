/* Copyright 2018-present MongoDB Inc.
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
using MongoDB.Driver;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Xunit;
using Xunit.Abstractions;

namespace AtlasConnectivity.Tests
{
    public class ConnectivityTests : LoggableTestClass
    {
        // public constructors
        public ConnectivityTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        // public methods
        [Theory]
        [InlineData("ATLAS_REPL")]
        [InlineData("ATLAS_SHRD")]
        [InlineData("ATLAS_FREE")]
        [InlineData("ATLAS_TLS11")]
        [InlineData("ATLAS_TLS12")]
        [InlineData("ATLAS_SERVERLESS")]
        [InlineData("ATLAS_SRV_REPL")]
        [InlineData("ATLAS_SRV_SHRD")]
        [InlineData("ATLAS_SRV_FREE")]
        [InlineData("ATLAS_SRV_TLS11")]
        [InlineData("ATLAS_SRV_TLS12")]
        [InlineData("ATLAS_SRV_SERVERLESS")]
        public void Connection_to_Atlas_should_work(string environmentVariableName)
        {
            var connectionString = Environment.GetEnvironmentVariable(environmentVariableName);
            connectionString.Should().NotBeNull();

            using (var client = CreateMongoClient(connectionString))
            {
                // test that a command that doesn't require auth completes normally
                var adminDatabase = client.GetDatabase("admin");
                var pingCommand = new BsonDocument("ping", 1);
                var pingResult = adminDatabase.RunCommand<BsonDocument>(pingCommand);

                // test that a command that does require auth completes normally
                var database = client.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("test");
                var emptyFilter = Builders<BsonDocument>.Filter.Empty;
                var count = collection.CountDocuments(emptyFilter);
            }
        }

        // private methods
        private IMongoClient CreateMongoClient(string connectionString)
        {
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            clientSettings.ClusterSource = DisposingClusterSource.Instance;

            return new MongoClient(clientSettings);
        }
    }
}
