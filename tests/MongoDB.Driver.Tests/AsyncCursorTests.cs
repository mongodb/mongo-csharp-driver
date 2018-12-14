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
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AsyncCursorTests
    {
        //public methods
        [SkippableTheory]
        [ParameterAttributeData]
        public void Cursor_should_not_throw_exception_after_double_close([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.KillCursorsCommand);

            string testCollectionName = "test";
            string testDatabaseName = "test";
            var client = CreateClient();
            DropCollection(client, testDatabaseName, testCollectionName);
            var collection = client.GetDatabase(testDatabaseName).GetCollection<BsonDocument>(testCollectionName);
            collection.InsertOne(new BsonDocument("key", "value1"));
            collection.InsertOne(new BsonDocument("key", "value2"));

            var cursor = collection.Find(FilterDefinition<BsonDocument>.Empty, new FindOptions { BatchSize = 1 }).ToCursor().As<AsyncCursor<BsonDocument>>();
            if (async)
            {
                cursor.CloseAsync().Wait();
                cursor.CloseAsync().Wait();
            }
            else
            {
                cursor.Close();
                cursor.Close();
            }
        }

        //private methods
        private IMongoClient CreateClient()
        {
            var connectionString = CoreTestConfiguration.ConnectionString.ToString();
            return new MongoClient(connectionString);
        }

        private void DropCollection(IMongoClient client, string databaseName, string collectionName)
        {
            client.GetDatabase(databaseName).DropCollection(collectionName);
        }
    }
}