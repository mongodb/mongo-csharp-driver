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
using System.Collections.Generic;
using MongoDB.Bson;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenTestFactory
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<BsonDocument> _collection;
        private readonly Dictionary<string, IClientSessionHandle> _sessionMap;

        // public constructors
        public JsonDrivenTestFactory(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, Dictionary<string, IClientSessionHandle> sessionMap)
        {
            _client = client;
            _database = database;
            _collection = collection;
            _sessionMap = sessionMap;
        }

        // public methods
        public JsonDrivenClientTest CreateTest(string name)
        {
            switch (name)
            {
                case "abortTransaction": return new JsonDrivenAbortTransactionTest(_client, _sessionMap);
                case "aggregate": return new JsonDrivenAggregateTest(_client, _database, _collection, _sessionMap);
                case "bulkWrite": return new JsonDrivenBulkWriteTest(_client, _database, _collection, _sessionMap);
                case "commitTransaction": return new JsonDrivenCommitTransactionTest(_client, _sessionMap);
                case "count": return new JsonDrivenCountTest(_client, _database, _collection, _sessionMap);
                case "deleteMany": return new JsonDrivenDeleteManyTest(_client, _database, _collection, _sessionMap);
                case "deleteOne": return new JsonDrivenDeleteOneTest(_client, _database, _collection, _sessionMap);
                case "distinct": return new JsonDrivenDistinctTest(_client, _database, _collection, _sessionMap);
                case "find": return new JsonDrivenFindTest(_client, _database, _collection, _sessionMap);
                case "findOneAndDelete": return new JsonDrivenFindOneAndDeleteTest(_client, _database, _collection, _sessionMap);
                case "findOneAndReplace": return new JsonDrivenFindOneAndReplaceTest(_client, _database, _collection, _sessionMap);
                case "findOneAndUpdate": return new JsonDrivenFindOneAndUpdateTest(_client, _database, _collection, _sessionMap);
                case "insertMany": return new JsonDrivenInsertManyTest(_client, _database, _collection, _sessionMap);
                case "insertOne": return new JsonDrivenInsertOneTest(_client, _database, _collection, _sessionMap);
                case "replaceOne": return new JsonDrivenReplaceOneTest(_client, _database, _collection, _sessionMap);
                case "startTransaction": return new JsonDrivenStartTransactionTest(_client, _sessionMap);
                case "runCommand": return new JsonDrivenRunCommandTest(_client, _database, _sessionMap);
                case "updateMany": return new JsonDrivenUpdateManyTest(_client, _database, _collection, _sessionMap);
                case "updateOne": return new JsonDrivenUpdateOneTest(_client, _database, _collection, _sessionMap);
                default: throw new FormatException($"Invalid method name: \"{name}\".");
            }
        }
    }
}
