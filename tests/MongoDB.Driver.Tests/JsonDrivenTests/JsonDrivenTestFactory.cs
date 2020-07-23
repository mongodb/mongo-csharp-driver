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
using MongoDB.Driver.Core;

namespace MongoDB.Driver.Tests.JsonDrivenTests
{
    public class JsonDrivenTestsStateHolder
    {
        private readonly IDictionary<string, object> _stateHolder = new Dictionary<string, object>();

        public T GetTestState<T>(string key) where T : class
        {
            if (!_stateHolder.ContainsKey(key))
            {
                _stateHolder.Add(key, Activator.CreateInstance<T>());
            }

            return (T)_stateHolder[key];
        }
    }

    public class JsonDrivenTestFactory
    {
        // private fields
        private readonly string _bucketName;
        private readonly IMongoClient _client;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly Dictionary<string, object> _objectMap;
        private readonly IJsonDrivenTestRunner _testRunner;
        private readonly EventCapturer _eventCapturer;
        private readonly JsonDrivenTestsStateHolder _stateHolder;

        // public constructors
        public JsonDrivenTestFactory(IMongoClient client, string databaseName, string collectionName, string bucketName, Dictionary<string, object> objectMap)
            : this(client, databaseName, collectionName, bucketName, objectMap, null)
        {
        }

        public JsonDrivenTestFactory(IMongoClient client, string databaseName, string collectionName, string bucketName, Dictionary<string, object> objectMap, EventCapturer eventCapturer)
            : this(null, client, databaseName, collectionName, bucketName, objectMap, eventCapturer)
        {
        }

        public JsonDrivenTestFactory(IJsonDrivenTestRunner testRunner, IMongoClient client, string databaseName, string collectionName, string bucketName, Dictionary<string, object> objectMap)
            : this(testRunner, client, databaseName, collectionName, bucketName, objectMap, null)
        {
        }

        public JsonDrivenTestFactory(IJsonDrivenTestRunner testRunner, IMongoClient client, string databaseName, string collectionName, string bucketName, Dictionary<string, object> objectMap, EventCapturer eventCapturer)
        {
            _client = client;
            _databaseName = databaseName;
            _collectionName = collectionName;
            _bucketName = bucketName;
            _objectMap = objectMap;
            _testRunner = testRunner;
            _eventCapturer = eventCapturer;
            _stateHolder = new JsonDrivenTestsStateHolder();
        }

        // public methods
        public JsonDrivenTest CreateTest(string receiver, string name)
        {
            IMongoDatabase database;
            switch (receiver)
            {
                case "testRunner":
                    switch (name)
                    {
                        case "assertCollectionExists": return new JsonDrivenAssertCollectionExistsTest(_testRunner, _objectMap);
                        case "assertCollectionNotExists": return new JsonDrivenAssertCollectionNotExistsTest(_testRunner, _objectMap);
                        case "assertDifferentLsidOnLastTwoCommands": return new JsonDrivenAssertDifferentLsidOnLastTwoCommandsTest(_testRunner, _eventCapturer, _objectMap);
                        case "assertIndexExists": return new JsonDrivenAssertIndexExistsTest(_testRunner, _objectMap);
                        case "assertIndexNotExists": return new JsonDrivenAssertIndexNotExistsTest(_testRunner, _objectMap);
                        case "assertSessionDirty": return new JsonDrivenAssertSessionDirtyTest(_testRunner, _objectMap);
                        case "assertSessionNotDirty": return new JsonDrivenAssertSessionNotDirtyTest(_testRunner, _objectMap);
                        case "assertSessionPinned": return new JsonDrivenAssertSessionPinnedTest(_testRunner, _objectMap);
                        case "assertSessionUnpinned": return new JsonDrivenAssertSessionUnpinnedTest(_testRunner, _objectMap);
                        case "assertSameLsidOnLastTwoCommands": return new JsonDrivenAssertSameLsidOnLastTwoCommandsTest(_testRunner, _eventCapturer, _objectMap);
                        case "assertSessionTransactionState": return new JsonDrivenAssertSessionTransactionStateTest(_testRunner, _objectMap);
                        case "assertEventCount": return new JsonDrivenAssertEventsCountTest(_testRunner, _objectMap, _eventCapturer);
                        case "configureFailPoint": return new JsonDrivenConfigureFailPointTest(_testRunner, _client, _objectMap);
                        case "recordPrimary": return new JsonDrivenRecordPrimaryTest(_stateHolder, _testRunner, _client, _objectMap);
                        case "runAdminCommand": return new JsonDrivenRunAdminCommandTest(_client, _testRunner, _objectMap);
                        case "runOnThread": return new JsonDrivenRunOnThreadTest(_stateHolder, _testRunner, _objectMap, this);
                        case "startThread": return new JsonDrivenStartThreadTest(_stateHolder, _testRunner, _objectMap);
                        case "targetedFailPoint": return new JsonDrivenTargetedFailPointTest(_testRunner, _objectMap);
                        case "wait": return new JsonDrivenWaitTest(_testRunner, _objectMap);
                        case "waitForEvent": return new JsonDrivenWaitForEventTest(_testRunner, _objectMap, _eventCapturer);
                        case "waitForPrimaryChange": return new JsonDrivenWaitForPrimaryChangeTest(_stateHolder, _testRunner, _client, _objectMap);
                        case "waitForThread": return new JsonDrivenWaitForThreadTest(_stateHolder, _testRunner, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case "client":
                    switch (name)
                    {
                        case "listDatabaseNames": return new JsonDrivenListDatabaseNamesTest(_client, _objectMap);
                        case "listDatabases": return new JsonDrivenListDatabasesTest(_client, _objectMap);
                        case "watch": return new JsonDrivenClientWatchTest(_client, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case var _ when receiver.StartsWith("session"):
                    switch (name)
                    {
                        case "abortTransaction": return new JsonDrivenAbortTransactionTest(_objectMap);
                        case "commitTransaction": return new JsonDrivenCommitTransactionTest(_objectMap);
                        case "endSession": return new JsonDrivenEndSessionTest(_objectMap);
                        case "startTransaction": return new JsonDrivenStartTransactionTest(_objectMap);
                        case "withTransaction": return new JsonDrivenWithTransactionTest(this, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case "database":
                    database = _client.GetDatabase(_databaseName);
                    switch (name)
                    {
                        case "aggregate": return new JsonDrivenDatabaseAggregateTest(database, _objectMap);
                        case "createCollection": return new JsonDrivenCreateCollectionTest(database, _objectMap);
                        case "dropCollection": return new JsonDrivenDropCollectionTest(database, _objectMap);
                        case "listCollectionNames": return new JsonDrivenListCollectionNamesTest(database, _objectMap);
                        case "listCollections": return new JsonDrivenListCollectionsTest(database, _objectMap);
                        case "runCommand": return new JsonDrivenRunCommandTest(database, _objectMap);
                        case "watch": return new JsonDrivenDatabaseWatchTest(database, _objectMap);
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
                        case "createIndex": return new JsonDrivenCreateIndexTest(collection, _objectMap);
                        case "deleteMany": return new JsonDrivenDeleteManyTest(collection, _objectMap);
                        case "deleteOne": return new JsonDrivenDeleteOneTest(collection, _objectMap);
                        case "distinct": return new JsonDrivenDistinctTest(collection, _objectMap);
                        case "dropIndex": return new JsonDrivenDropIndexTest(collection, _objectMap);
                        case "estimatedDocumentCount": return new JsonDrivenEstimatedCountTest(collection, _objectMap);
                        case "find":
                        case "findOne":
                            return new JsonDrivenFindTest(collection, _objectMap);
                        case "findOneAndDelete": return new JsonDrivenFindOneAndDeleteTest(collection, _objectMap);
                        case "findOneAndReplace": return new JsonDrivenFindOneAndReplaceTest(collection, _objectMap);
                        case "findOneAndUpdate": return new JsonDrivenFindOneAndUpdateTest(collection, _objectMap);
                        case "insertMany": return new JsonDrivenInsertManyTest(collection, _objectMap);
                        case "insertOne": return new JsonDrivenInsertOneTest(collection, _objectMap);
                        case "listIndexes": return new JsonDrivenListIndexesTest(collection, _objectMap);
                        case "mapReduce": return new JsonDrivenMapReduceTest(collection, _objectMap);
                        case "replaceOne": return new JsonDrivenReplaceOneTest(collection, _objectMap);
                        case "updateMany": return new JsonDrivenUpdateManyTest(collection, _objectMap);
                        case "updateOne": return new JsonDrivenUpdateOneTest(collection, _objectMap);
                        case "watch": return new JsonDrivenCollectionWatchTest(collection, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                case "gridfsbucket":
                    database = _client.GetDatabase(_databaseName);
                    switch (name)
                    {
                        case "download": return new JsonDrivenGridFSDownloadTest(database, _bucketName, _objectMap);
                        case "download_by_name": return new JsonDrivenGridFSDownloadByNameTest(database, _bucketName, _objectMap);
                        default: throw new FormatException($"Invalid method name: \"{name}\".");
                    }

                default:
                    throw new FormatException($"Invalid receiver: \"{receiver}\".");
            }
        }
    }
}
