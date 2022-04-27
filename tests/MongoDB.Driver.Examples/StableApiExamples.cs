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
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Driver.Examples
{
    public class StableApiExamples
    {
        [Fact]
        public void ConfigureServerApi()
        {
            // Start Stable API Example 1
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Stable API Example 1
        }

        [Fact]
        public void ConfigureServerApiStrict()
        {
            // Start Stable API Example 2
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, strict: true);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Stable API Example 2
        }

        [Fact]
        public void ConfigureServerApiNonStrict()
        {
            // Start Stable API Example 3
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, strict: false);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Stable API Example 3
        }

        [Fact]
        public void ConfigureServerApiDeprecationErrors()
        {
            // Start Stable API Example 4
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, deprecationErrors: true);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            // End Stable API Example 4
        }

        [Fact]
        public void StableAPI_Strict_Migration_Example()
        {
            var connectionString = "mongodb://localhost";
            var serverApi = new ServerApi(ServerApiVersion.V1, strict: true);
            var mongoClientSettings = MongoClientSettings.FromConnectionString(connectionString);
            mongoClientSettings.ServerApi = serverApi;
            var mongoClient = new MongoClient(mongoClientSettings);
            var database = mongoClient.GetDatabase("test");
            database.DropCollection("coll");
            var collection = database.GetCollection<BsonDocument>("coll");

            // 1. Populate a test sales collection:
            collection.InsertMany(
                new[]
                {
                    BsonDocument.Parse(@"{ ""_id"" : 1, ""item"" : ""ab"", ""price"" : 10, ""quantity"" : 2, ""date"" : ISODate(""2021-01-01T08:00:00Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 2, ""item"" : ""jkl"", ""price"" : 20, ""quantity"" : 1, ""date"" : ISODate(""2021-02-03T09:00:00Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 3, ""item"" : ""xyz"", ""price"" : 5, ""quantity"" : 5, ""date"" : ISODate(""2021-02-03T09:05:00Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 4, ""item"" : ""abc"", ""price"" : 10, ""quantity"" : 10, ""date"" : ISODate(""2021-02-15T08:00:00Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 5, ""item"" : ""xyz"", ""price"" : 5, ""quantity"" : 10, ""date"" : ISODate(""2021-02-15T09:05:00Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 6, ""item"" : ""xyz"", ""price"" : 5, ""quantity"" : 5, ""date"" : ISODate(""2021-02-15T12:05:10Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 7, ""item"" : ""xyz"", ""price"" : 5, ""quantity"" : 10, ""date"" : ISODate(""2021-02-15T14:12:12Z"") }"),
                    BsonDocument.Parse(@"{ ""_id"" : 8, ""item"" : ""abc"", ""price"" : 10, ""quantity"" : 5, ""date"" : ISODate(""2021-03-16T20:20:13Z"") }")
                });

            // 2. The response from the server when running count using a strict client:
            try
            {
#pragma warning disable CS0618 // Type or member is obsolete
                collection.Count(FilterDefinition<BsonDocument>.Empty);
#pragma warning restore CS0618 // Type or member is obsolete
            }
            catch (MongoCommandException ex)
            {
                Console.WriteLine(ex.Code); // 323
                Console.WriteLine(ex.CodeName); // APIStrictError
                Console.WriteLine(ex.Message); // Command count failed: Provided apiStrict:true, but the command count is not in API Version 1. Information on supported commands and migrations in API Version 1 can be found at https://www.mongodb.com/docs/manual/reference/stable-api.
            }

            // 3. An alternative, accepted command to count the number of documents:
            var count = collection.CountDocuments(FilterDefinition<BsonDocument>.Empty);

            // 4. The output of the above command:
            Console.WriteLine(count); // 8
        }
    }
}
