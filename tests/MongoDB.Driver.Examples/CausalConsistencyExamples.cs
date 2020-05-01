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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class CausalConsistencyExamples
    {
        [Fact]
        public void Causal_Consistency_Example_1()
        {
            RequireServer.Check().SupportsCausalConsistency();

            string testDatabaseName = "test";
            string itemsCollectionName = "items";
            var client = CreateClient();
            DropCollection(client, testDatabaseName, itemsCollectionName);

            // Start Causal Consistency Example 1
            using (var session1 = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
            {
                var currentDate = DateTime.UtcNow.Date;
                var items = client.GetDatabase(
                    "test",
                    new MongoDatabaseSettings
                    {
                        ReadConcern = ReadConcern.Majority,
                        WriteConcern = new WriteConcern(
                                WriteConcern.WMode.Majority,
                                TimeSpan.FromMilliseconds(1000))
                    })
                    .GetCollection<BsonDocument>("items");

                items.UpdateOne(session1,
                    Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("sku", "111"),
                        Builders<BsonDocument>.Filter.Eq("end", BsonNull.Value)),
                    Builders<BsonDocument>.Update.Set("end", currentDate));

                items.InsertOne(session1, new BsonDocument
                {
                    {"sku", "nuts-111"},
                    {"name", "Pecans"},
                    {"start", currentDate}
                });
            }
            // End Causal Consistency Example 1

            var result = client.GetDatabase(testDatabaseName).GetCollection<BsonDocument>(itemsCollectionName).Find("{}").FirstOrDefault();
            RemoveIds(new[] { result });
            result["start"].Should().NotBeNull();
            result.Remove("start");
            result.Should().Be("{ \"sku\" : \"nuts-111\", \"name\" : \"Pecans\" }");
        }

        [Fact]
        public void Causal_Consistency_Example_2()
        {
            RequireServer.Check().SupportsCausalConsistency();

            string testDatabaseName = "test";
            string itemsCollectionName = "items";
            var client = CreateClient();
            DropCollection(client, testDatabaseName, itemsCollectionName);

            using (var session1 = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
            {
                client.GetDatabase(testDatabaseName).RunCommand<BsonDocument>(session1, "{ ping : 1 }");

                // Start Causal Consistency Example 2
                using (var session2 = client.StartSession(new ClientSessionOptions { CausalConsistency = true }))
                {
                    session2.AdvanceClusterTime(session1.ClusterTime);
                    session2.AdvanceOperationTime(session1.OperationTime);

                    var items = client.GetDatabase(
                        "test",
                        new MongoDatabaseSettings
                        {
                            ReadPreference = ReadPreference.Secondary,
                            ReadConcern = ReadConcern.Majority,
                            WriteConcern = new WriteConcern(WriteConcern.WMode.Majority, TimeSpan.FromMilliseconds(1000))
                        })
                        .GetCollection<BsonDocument>("items");

                    var filter = Builders<BsonDocument>.Filter.Eq("end", BsonNull.Value);
                    foreach (var item in items.Find(session2, filter).ToEnumerable())
                    {
                        // process item
                    }
                }
                // End Causal Consistency Example 2
            }
        }

        [Fact]
        public void Causal_Consistency_Example_3()
        {
            RequireServer.Check().SupportsCausalConsistency();

            DropCollection(CreateClient(), "myDatabase", "myCollection");

            // Start Tunable Consistency Controls Example
            var connectionString = "mongodb://localhost/?readPreference=secondaryPreferred";

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("myDatabase");
            var collection = database.GetCollection<BsonDocument>("myCollection");

            // Start client session, which is causally consistent by default
            using (var session = client.StartSession())
            {
                // Run causally related operations within the session
                collection.InsertOne(session, new BsonDocument("name", "Tom"));
                collection.UpdateOne(session,
                    Builders<BsonDocument>.Filter.Eq("name", "Tom"),
                    Builders<BsonDocument>.Update.Set("name", "John"));

                foreach (var document in collection.Find(session, FilterDefinition<BsonDocument>.Empty).ToEnumerable())
                {
                    // process document
                }
            }
            // End Tunable Consistency Controls Example

            var result = collection.Find("{}").FirstOrDefault();
            RemoveIds(new[] { result });
            result.Should().Be("{ name: 'John' }");
        }

        private IMongoClient CreateClient()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            return new MongoClient(connectionString);
        }

        private void DropCollection(IMongoClient client, string databaseName, string collectionName)
        {
            client.GetDatabase(databaseName).DropCollection(collectionName);
        }

        private void RemoveIds(IEnumerable<BsonDocument> documents)
        {
            foreach (var document in documents)
            {
                document.Remove("_id");
            }
        }
    }
}
