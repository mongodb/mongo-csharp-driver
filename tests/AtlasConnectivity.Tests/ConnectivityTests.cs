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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace AtlasConnectivity.Tests
{
    public class ConnectivityTests
    {
        [Theory]
        [InlineData("ATLAS_FREE")]
        [InlineData("ATLAS_FREE_SRV")]
        [InlineData("ATLAS_REPLICA")]
        [InlineData("ATLAS_REPLICA_SRV")]
        [InlineData("ATLAS_SHARDED")]
        [InlineData("ATLAS_SHARDED_SRV")]
        [InlineData("ATLAS_TLS11")]
        [InlineData("ATLAS_TLS11_SRV")]
        [InlineData("ATLAS_TLS12")]
        [InlineData("ATLAS_TLS12_SRV")]
        public void Connection_to_Atlas_should_work(string environmentVariableName)
        {
            var connectionString = Environment.GetEnvironmentVariable(environmentVariableName);
            connectionString.Should().NotBeNull();

            using (var client = CreateDisposableClient(connectionString))
            {
                // test that a command that doesn't require auth completes normally
                var adminDatabase = client.GetDatabase("admin");
                var isMasterCommand = new BsonDocument("ismaster", 1);
                var isMasterResult = adminDatabase.RunCommand<BsonDocument>(isMasterCommand);

                // test that a command that does require auth completes normally
                var database = client.GetDatabase("test");
                var collection = database.GetCollection<BsonDocument>("test");
                var emptyFilter = Builders<BsonDocument>.Filter.Empty;
                var count = collection.CountDocuments(emptyFilter);
            }
        }

        // private methods
        private DisposableMongoClient CreateDisposableClient(string connectionString)
        {
            var clientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            var client = new MongoClient(clientSettings);
            return new DisposableMongoClient(client);
        }
    }
}
