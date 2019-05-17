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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.Specifications.crud;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.change_streams
{
    public class ChangeStreamTestRunner
    {
        // private fields
        private string _databaseName;
        private string _database2Name;
        private string _collectionName;
        private string _collection2Name;

        // public methods
        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            Run(testCase.Shared, testCase.Test);
        }

        // private methods
        private void Run(BsonDocument shared, BsonDocument test)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(shared, "_path", "database_name", "database2_name", "collection_name", "collection2_name", "tests");
            JsonDrivenHelper.EnsureAllFieldsAreValid(test, "description", "minServerVersion", "topology", "target", "changeStreamPipeline", "changeStreamOptions", "operations", "expectations", "result", "async");

            if (test.Contains("minServerVersion"))
            {
                var minServerVersion = test["minServerVersion"].AsString;
                RequireServer.Check().VersionGreaterThanOrEqualTo(minServerVersion);
            }

            if (test.Contains("topology"))
            {
                var clusterTypes = MapTopologyToClusterTypes(test["topology"].AsBsonArray);
                RequireServer.Check().ClusterTypes(clusterTypes);
            }

            _databaseName = shared["database_name"].AsString;
            _database2Name = shared["database2_name"].AsString;
            _collectionName = shared["collection_name"].AsString;
            _collection2Name = shared["collection2_name"].AsString;

            CreateCollections();

            List<ChangeStreamDocument<BsonDocument>> actualResult = null;
            Exception actualException = null;
            List<CommandStartedEvent> actualEvents = null;

            var eventCapturer = CreateEventCapturer();
            using (var client = CreateDisposableClient(eventCapturer))
            {
                try
                {
                    var async = test["async"].AsBoolean;
                    using (var cursor = Watch(client, test, async))
                    {
                        var globalClient = DriverTestConfiguration.Client;
                        ExecuteOperations(globalClient, test["operations"].AsBsonArray);

                        actualResult = ReadChangeStreamDocuments(cursor, test, async);
                        actualEvents = GetEvents(eventCapturer);
                    }
                }
                catch (Exception exception)
                {
                    actualException = exception;
                }
            }

            if (test.Contains("expectations") && actualEvents != null)
            {
                var expectedEvents = test["expectations"].AsBsonArray.Cast<BsonDocument>().ToList();
                AssertEvents(actualEvents, expectedEvents);
            }

            if (test.Contains("result"))
            {
                var expectedResult = test["result"].AsBsonDocument;
                AssertResult(actualResult, actualException, expectedResult);
            }
        }

        // private methods
        private ClusterType[] MapTopologyToClusterTypes(BsonArray topologies)
        {
            var clusterTypes = new List<ClusterType>();
            foreach (var topology in topologies.Select(i => i.AsString))
            {
                switch (topology)
                {
                    case "single": clusterTypes.Add(ClusterType.Standalone); break;
                    case "replicaset": clusterTypes.Add(ClusterType.ReplicaSet); break;
                    case "sharded": clusterTypes.Add(ClusterType.Sharded); break;
                    default: throw new FormatException($"Invalid topology: \"{topology}\".");
                }
            }
            return clusterTypes.ToArray();
        }

        private ChangeStreamOptions ParseChangeStreamOptions(BsonDocument document)
        {
            var options = new ChangeStreamOptions();

            foreach (var element in document)
            {
                throw new FormatException($"Invalid change stream option: \"{element.Name}\".");
            }

            return options;
        }

        private void CreateCollections()
        {
            var client = DriverTestConfiguration.Client;
            client.DropDatabase(_databaseName);
            client.DropDatabase(_database2Name);
            client.GetDatabase(_databaseName).CreateCollection(_collectionName);
            client.GetDatabase(_database2Name).CreateCollection(_collection2Name);
        }

        private EventCapturer CreateEventCapturer()
        {
            var commandsToNotCapture = new HashSet<string>
            {
                "isMaster",
                "buildInfo",
                "getLastError",
                "authenticate",
                "saslStart",
                "saslContinue",
                "getnonce"
            };

            return
                new EventCapturer()
                .Capture<CommandStartedEvent>(e => !commandsToNotCapture.Contains(e.CommandName));
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
            {
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
            });
        }

        private IAsyncCursor<ChangeStreamDocument<BsonDocument>> Watch(IMongoClient client, BsonDocument test, bool async)
        {
            var target = test["target"].AsString;
            var stages = test["changeStreamPipeline"].AsBsonArray.Cast<BsonDocument>();
            var pipeline = new BsonDocumentStagePipelineDefinition<ChangeStreamDocument<BsonDocument>, ChangeStreamDocument<BsonDocument>>(stages);
            var options = ParseChangeStreamOptions(test["changeStreamOptions"].AsBsonDocument);

            if (target == "client")
            {
                if (async)
                {
                    return client.WatchAsync(pipeline, options).GetAwaiter().GetResult();
                }
                else
                {
                    return client.Watch(pipeline, options);
                }
            }

            var database = client.GetDatabase(_databaseName);
            if (target == "database")
            {
                if (async)
                {
                    return database.WatchAsync(pipeline, options).GetAwaiter().GetResult();
                }
                else
                {
                    return database.Watch(pipeline, options);
                }
            }

            var collection = database.GetCollection<BsonDocument>(_collectionName);
            if (target == "collection")
            {
                if (async)
                {
                    return collection.WatchAsync(pipeline, options).GetAwaiter().GetResult();
                }
                else
                {
                    return collection.Watch(pipeline, options);
                }
            }

            throw new FormatException($"Invalid target: \"{target}\".");
        }

        private void ExecuteOperations(IMongoClient client, BsonArray operations)
        {
            foreach (var operation in operations.Cast<BsonDocument>())
            {
                ExecuteOperation(client, operation);
            }
        }

        private void ExecuteOperation(IMongoClient client, BsonDocument operation)
        {
            var name = operation["name"].AsString;
            var test = CrudOperationTestFactory.CreateTest(name);

            var arguments = (BsonDocument)operation.GetValue("arguments", new BsonDocument());
            test.SkipIfNotSupported(arguments);

            var database = client.GetDatabase(operation["database"].AsString);
            var collection = database.GetCollection<BsonDocument>(operation["collection"].AsString);
            test.Execute(DriverTestConfiguration.Client.Cluster.Description, database, collection, arguments, outcome: null, isErrorExpected: false, async: false);
        }

        private List<CommandStartedEvent> GetEvents(EventCapturer eventCapturer)
        {
            var events = new List<CommandStartedEvent>();

            while (eventCapturer.Any())
            {
                events.Add((CommandStartedEvent)eventCapturer.Next());
            }

            return events;
        }

        private List<ChangeStreamDocument<BsonDocument>> ReadChangeStreamDocuments(IAsyncCursor<ChangeStreamDocument<BsonDocument>> cursor, BsonDocument test, bool async)
        {
            var result = new List<ChangeStreamDocument<BsonDocument>>();
            var expectedNumberOfDocuments = test["result"]["success"].AsBsonArray.Count;

            while (async ? cursor.MoveNextAsync().GetAwaiter().GetResult() : cursor.MoveNext())
            {
                result.AddRange(cursor.Current);

                if (result.Count >= expectedNumberOfDocuments)
                {
                    break;
                }
            }

            return result;
        }

        private void AssertEvents(List<CommandStartedEvent> actualEvents, List<BsonDocument> expectedEvents)
        {
            if (expectedEvents.Count == 0)
            {
                return;
            }

            // filter out getMore and killCursors commands
            actualEvents = actualEvents.Where(e => e.CommandName != "getMore" && e.CommandName != "killCursors").ToList();

            var n = Math.Min(actualEvents.Count, expectedEvents.Count);
            for (var i = 0; i < n; i++)
            {
                var actualEvent = actualEvents[i];
                var expectedEvent = expectedEvents[i];
                AssertEvent(i, actualEvent, expectedEvent);
            }

            if (actualEvents.Count < expectedEvents.Count)
            {
                throw new Exception($"Missing command started event: {expectedEvents[n]}.");
            }

            if (actualEvents.Count > expectedEvents.Count)
            {
                throw new Exception($"Unexpected command started event: {actualEvents[n].CommandName}.");
            }
        }

        private void AssertEvent(int i, CommandStartedEvent actualEvent, BsonDocument expectedEvent)
        {
            if (expectedEvent.ElementCount != 1)
            {
                throw new FormatException("Expected event must be a document with a single element with a name the specifies the type of the event.");
            }

            var eventType = expectedEvent.GetElement(0).Name;
            var eventAsserter = EventAsserterFactory.CreateAsserter(eventType);
            eventAsserter.AssertAspects(actualEvent, expectedEvent[0].AsBsonDocument);
        }

        private void AssertResult(List<ChangeStreamDocument<BsonDocument>> actualResult, Exception actualException, BsonDocument expectedResult)
        {
            if (expectedResult.Contains("success"))
            {
                AssertSuccess(actualResult, actualException, expectedResult);
            }
            else if (expectedResult.Contains("error"))
            {
                AssertError(actualException, expectedResult);
            }
        }

        private void AssertSuccess(List<ChangeStreamDocument<BsonDocument>> actualResult, Exception exception, BsonDocument expectedResult)
        {
            if (exception != null)
            {
                throw exception;
            }

            var expectedDocuments = expectedResult["success"].AsBsonArray.Cast<BsonDocument>().ToList();
            if (actualResult.Count != expectedDocuments.Count)
            {
                throw new Exception("Actual number of result documents does not match expected number of result documents.");
            }
            for (var i = 0; i < actualResult.Count; i++)
            {
                var actualDocument = actualResult[i];
                var expectedDocument = expectedDocuments[i];
                AssertChangeStreamDocument(actualDocument, expectedDocument);
            }
        }

        private void AssertChangeStreamDocument(ChangeStreamDocument<BsonDocument> actualDocument, BsonDocument expectedDocument)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedDocument, "_id", "documentKey", "operationType", "ns", "fullDocument");

            AssertChangeStreamDocumentPropertyValuesAgainstBackingDocument(actualDocument);

            if (expectedDocument.Contains("_id"))
            {
                actualDocument.ResumeToken.Should().NotBeNull();
            }

            if (expectedDocument.Contains("documentKey"))
            {
                actualDocument.DocumentKey.Should().NotBeNull();
            }

            if (expectedDocument.Contains("operationType"))
            {
                var expectedOperationType = (ChangeStreamOperationType)Enum.Parse(typeof(ChangeStreamOperationType), expectedDocument["operationType"].AsString, ignoreCase: true);
                actualDocument.OperationType.Should().Be(expectedOperationType);
            }

            if (expectedDocument.Contains("ns"))
            {
                var ns = expectedDocument["ns"].AsBsonDocument;
                JsonDrivenHelper.EnsureAllFieldsAreValid(ns, "db", "coll");
                var expectedDatabaseName = ns["db"].AsString;
                var expectedCollectionName = ns["coll"].AsString;
                var expectedCollectionNamespace = new CollectionNamespace(new DatabaseNamespace(expectedDatabaseName), expectedCollectionName);
                actualDocument.CollectionNamespace.Should().Be(expectedCollectionNamespace);
            }

            if (expectedDocument.Contains("fullDocument"))
            {
                var actualFullDocument = actualDocument.FullDocument;
                actualFullDocument.Remove("_id");
                var expectedFullDocument = expectedDocument["fullDocument"].AsBsonDocument;
                actualFullDocument.Should().Be(expectedFullDocument);
            }
        }

        private void AssertChangeStreamDocumentPropertyValuesAgainstBackingDocument(ChangeStreamDocument<BsonDocument> actualDocument)
        {
            var backingDocument = actualDocument.BackingDocument;
            backingDocument.Should().NotBeNull();

            var clusterTime = actualDocument.ClusterTime;
            if (backingDocument.Contains("clusterTime"))
            {
                clusterTime.Should().Be(backingDocument["clusterTime"].AsBsonTimestamp);
            }
            else
            {
                clusterTime.Should().BeNull();
            }

            var collectionNamespace = actualDocument.CollectionNamespace;
            collectionNamespace.DatabaseNamespace.DatabaseName.Should().Be(backingDocument["ns"]["db"].AsString);
            collectionNamespace.CollectionName.Should().Be(backingDocument["ns"]["coll"].AsString);

            var documentKey = actualDocument.DocumentKey;
            documentKey.Should().Be(backingDocument["documentKey"].AsBsonDocument);

            var fullDocument = actualDocument.FullDocument;
            if (backingDocument.Contains("fullDocument"))
            {
                fullDocument.Should().Be(backingDocument["fullDocument"].AsBsonDocument);
            }
            else
            {
                fullDocument.Should().BeNull();
            }

            var operationType = actualDocument.OperationType;
            operationType.ToString().ToLowerInvariant().Should().Be(backingDocument["operationType"].AsString);

            var resumeToken = actualDocument.ResumeToken;
            resumeToken.Should().Be(backingDocument["_id"].AsBsonDocument);

            var updateDescription = actualDocument.UpdateDescription;
            if (backingDocument.Contains("updateDescription"))
            {
                updateDescription.Should().Be(backingDocument["updateDescription"].AsBsonDocument);
            }
            else
            {
                updateDescription.Should().BeNull();
            }
        }

        private void AssertError(Exception actualException, BsonDocument expectedResult)
        {
            if (actualException == null)
            {
                throw new Exception("Expected an exception to be thrown but none was.");
            }

            var actualMongoCommandException = actualException as MongoCommandException;
            if (actualMongoCommandException == null)
            {
                throw new Exception($"Expected a MongoCommandException to be thrown but instead a ${actualException.GetType().Name} was.");
            }

            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedResult, "error");
            var expectedError = expectedResult["error"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid((BsonDocument)expectedError, "code");
            var code = expectedError["code"].ToInt32();

            actualMongoCommandException.Code.Should().Be(code);
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix
            {
                get
                {
                    return "MongoDB.Driver.Tests.Specifications.change_streams.tests.";
                }
            }

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                foreach (var testCase in base.CreateTestCases(document))
                {
                    foreach (var async in new[] { false, true })
                    {
                        var name = $"{testCase.Name}:async={async}";
                        var test = testCase.Test.DeepClone().AsBsonDocument.Add("async", async);
                        yield return new JsonDrivenTestCase(name, testCase.Shared, test);
                    }
                }
            }

            protected override bool ShouldReadJsonDocument(string path)
            {
                return base.ShouldReadJsonDocument(path); // && path.EndsWith("change-streams.json");
            }
        }
    }
}
