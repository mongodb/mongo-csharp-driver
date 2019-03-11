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
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Tests.Specifications.transactions;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenTestFactory
    {
        // private fields
        private readonly IMongoClient _client;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly Dictionary<string, object> _objectMap;
        private readonly IJsonDrivenTestRunner _testRunner;

        // public constructors
        public JsonDrivenTestFactory(IMongoClient client, string databaseName, string collectionName, Dictionary<string, object> objectMap)
            : this(null, client, databaseName, collectionName, objectMap)
        {
        }

        public JsonDrivenTestFactory(IJsonDrivenTestRunner testRunner, IMongoClient client, string databaseName, string collectionName, Dictionary<string, object> objectMap)
        {
            _client = client;
            _databaseName = databaseName;
            _collectionName = collectionName;
            _objectMap = objectMap;
            _testRunner = testRunner;
        }

        // public methods
        public JsonDrivenTest CreateTest(string receiver, string name)
        {
            switch (receiver)
            {
                case "testRunner":
                    switch (name)
                    {
                        case "targetedFailPoint": return new JsonDrivenTargetedFailPointTest(_testRunner, _objectMap);
                        case "assertSessionPinned": return new JsonDrivenAssertSessionPinnedTest(_testRunner, _objectMap);
                        case "assertSessionUnpinned": return new JsonDrivenAssertSessionUnpinnedTest(_testRunner, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case var _ when receiver.StartsWith("session"):
                    switch (name)
                    {
                        case "abortTransaction": return new JsonDrivenAbortTransactionTest(_objectMap);
                        case "commitTransaction": return new JsonDrivenCommitTransactionTest(_objectMap);
                        case "startTransaction": return new JsonDrivenStartTransactionTest(_objectMap);
                        case "withTransaction": return new JsonDrivenWithTransactionTest(this, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case "database":
                    var database = _client.GetDatabase(_databaseName);
                    switch (name)
                    {
                        case "runCommand": return new JsonDrivenRunCommandTest(database, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case "collection":
                    var collection = _client.GetDatabase(_databaseName).GetCollection<BsonDocument>(_collectionName);
                    switch (name)
                    {
                        case "aggregate": return new JsonDrivenAggregateTest(collection, _objectMap);
                        case "bulkWrite": return new JsonDrivenBulkWriteTest(collection, _objectMap);
                        case "count": return new JsonDrivenCountTest(collection, _objectMap);
                        case "countDocuments": return new JsonDrivenCountDocumentsTest(collection, _objectMap);
                        case "deleteMany": return new JsonDrivenDeleteManyTest(collection, _objectMap);
                        case "deleteOne": return new JsonDrivenDeleteOneTest(collection, _objectMap);
                        case "distinct": return new JsonDrivenDistinctTest(collection, _objectMap);
                        case "find": return new JsonDrivenFindTest(collection, _objectMap);
                        case "findOneAndDelete": return new JsonDrivenFindOneAndDeleteTest(collection, _objectMap);
                        case "findOneAndReplace": return new JsonDrivenFindOneAndReplaceTest(collection, _objectMap);
                        case "findOneAndUpdate": return new JsonDrivenFindOneAndUpdateTest(collection, _objectMap);
                        case "insertMany": return new JsonDrivenInsertManyTest(collection, _objectMap);
                        case "insertOne": return new JsonDrivenInsertOneTest(collection, _objectMap);
                        case "replaceOne": return new JsonDrivenReplaceOneTest(collection, _objectMap);
                        case "updateMany": return new JsonDrivenUpdateManyTest(collection, _objectMap);
                        case "updateOne": return new JsonDrivenUpdateOneTest(collection, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                 default:
                     throw new FormatException($"Invalid receiver: \"{receiver}\".");
            }
        }
    }
}
