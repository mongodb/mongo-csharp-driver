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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers;
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
            JsonDrivenHelper.EnsureAllFieldsAreValid(test, "description", "minServerVersion", "maxServerVersion", "topology", "target", "changeStreamPipeline", "changeStreamOptions", "operations", "expectations", "result", "async", "failPoint");

            RequireServer.Check().RunOn(EmulateRunOn());

            _databaseName = shared["database_name"].AsString;
            _database2Name = shared.GetValue("database2_name", null)?.AsString;
            _collectionName = shared["collection_name"].AsString;
            _collection2Name = shared.GetValue("collection2_name", null)?.AsString;

            CreateCollections();

            List<ChangeStreamDocument<BsonDocument>> actualResult = null;
            Exception actualException = null;
            List<CommandStartedEvent> actualEvents = null;

            var eventCapturer = CreateEventCapturer();
            using (ConfigureFailPoint(test))
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

            BsonArray EmulateRunOn()
            {
                var condition = new BsonDocument();
                if (test.TryGetElement("minServerVersion", out var minServerVersion))
                {
                    condition.Add(minServerVersion);
                }
                if (test.TryGetElement("maxServerVersion", out var maxServerVersion))
                {
                    condition.Add(maxServerVersion);
                }
                if (test.TryGetElement("topology", out var topology))
                {
                    condition.Add(topology);
                }

                return new BsonArray { condition };
            }
        }

        // private methods
        private FailPoint ConfigureFailPoint(BsonDocument test)
        {
            if (test.TryGetValue("failPoint", out var failPoint))
            {
                var cluster = DriverTestConfiguration.Client.Cluster;
                var server = cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
                var session = NoCoreSession.NewHandle();
                var command = failPoint.AsBsonDocument;
                return FailPoint.Configure(cluster, session, command);
            }

            return null;
        }

        private ChangeStreamOptions ParseChangeStreamOptions(BsonDocument document)
        {
            var options = new ChangeStreamOptions();

            foreach (var element in document)
            {
                switch (element.Name)
                {
                    case "batchSize": options.BatchSize = element.Value.ToInt32(); break;
                    default:
                        throw new FormatException($"Invalid change stream option: \"{element.Name}\".");
                }
            }

            return options;
        }

        private void CreateCollections()
        {
            var client = DriverTestConfiguration.Client;
            client.DropDatabase(_databaseName);
            if (_database2Name != null)
            {
                client.DropDatabase(_database2Name);
            }
            client.GetDatabase(_databaseName).CreateCollection(_collectionName);
            if (_collection2Name != null)
            {
                client.GetDatabase(_database2Name).CreateCollection(_collection2Name);
            }
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
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
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
            var resultDocument = test["result"].AsBsonDocument;
            var successNode = resultDocument.GetValue("success", null)?.AsBsonArray;

            var stopwatch = Stopwatch.StartNew();
            while (async ? cursor.MoveNextAsync().GetAwaiter().GetResult() : cursor.MoveNext())
            {
                result.AddRange(cursor.Current);

                if (successNode != null && result.Count >= successNode.Count)
                {
                    break;
                }

                if (stopwatch.Elapsed > TimeSpan.FromSeconds(10)) // 10 seconds is enough time to receive all the required change documents or for an exception to be thrown
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

            // filter out killCursors commands
            actualEvents = actualEvents.Where(e => e.CommandName != "killCursors").ToList();

            var n = Math.Min(actualEvents.Count, expectedEvents.Count);
            for (var i = 0; i < n; i++)
            {
                var actualEvent = MassageActualEvent(actualEvents[i]);
                var expectedEvent = expectedEvents[i];
                AssertEvent(i, actualEvent, expectedEvent);
            }

            if (actualEvents.Count < expectedEvents.Count)
            {
                throw new Exception($"Missing command started event: {expectedEvents[n]}.");
            }

            if (actualEvents.Count > expectedEvents.Count)
            {
                // the tests assume that a number of actual events can be bigger than expected.
                // So, skip this asserting
            }

            CommandStartedEvent MassageActualEvent(CommandStartedEvent commandStartedEvent)
            {
                var command = commandStartedEvent.Command;
                if (command.TryGetValue("pipeline", out var pipeline) &&
                    pipeline is BsonArray pipelineArray &&
                    pipelineArray.Count == 1 &&
                    pipelineArray[0].IsBsonDocument)
                {
                    if (pipelineArray[0].AsBsonDocument.TryGetValue("$changeStream", out var changeStream))
                    {
                        // this value is not a target of the tests, so it was skipped in the expectations
                        changeStream.AsBsonDocument.Remove("resumeAfter");
                    }
                }
                return commandStartedEvent;
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
            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedDocument, "_id", "documentKey", "operationType", "ns", "fullDocument", "updateDescription", "to");

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

            if (expectedDocument.Contains("updateDescription"))
            {
                var expectedUpdateDescription = expectedDocument["updateDescription"].AsBsonDocument;
                JsonDrivenHelper.EnsureAllFieldsAreValid(expectedUpdateDescription, "updatedFields", "removedFields");
                var actualUpdateDescription = actualDocument.UpdateDescription;
                actualUpdateDescription.UpdatedFields.Should().Be(expectedUpdateDescription["updatedFields"].AsBsonDocument);
                if (expectedUpdateDescription.Contains("removedFields"))
                {
                    var actualRemovedFields = new BsonArray(actualUpdateDescription.RemovedFields);
                    actualRemovedFields.Should().Be(expectedUpdateDescription["removedFields"].AsBsonArray);
                }
            }

            if (expectedDocument.Contains("to"))
            {
                var to = expectedDocument["to"].AsBsonDocument;
                JsonDrivenHelper.EnsureAllFieldsAreValid(to, "db", "coll");
                var expectedRenameToDatabaseName = to["db"].AsString;
                var expectedRenameToCollectionName = to["coll"].AsString;
                var expectedRenameToCollectionNamespace = new CollectionNamespace(new DatabaseNamespace(expectedRenameToDatabaseName), expectedRenameToCollectionName);
                actualDocument.RenameTo.Should().Be(expectedRenameToCollectionNamespace);
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

            var operationType = actualDocument.OperationType;
            operationType.ToString().ToLowerInvariant().Should().Be(backingDocument["operationType"].AsString);

            if (operationType == ChangeStreamOperationType.Invalidate)
            {
                return;
            }

            var collectionNamespace = actualDocument.CollectionNamespace;
            collectionNamespace.DatabaseNamespace.DatabaseName.Should().Be(backingDocument["ns"]["db"].AsString);
            collectionNamespace.CollectionName.Should().Be(backingDocument["ns"]["coll"].AsString);

            var documentKey = actualDocument.DocumentKey;
            if (operationType == ChangeStreamOperationType.Rename || operationType == ChangeStreamOperationType.Drop)
            {
                documentKey.Should().BeNull();
            }
            else
            {
                documentKey.Should().Be(backingDocument["documentKey"].AsBsonDocument);
            }

            var fullDocument = actualDocument.FullDocument;
            if (backingDocument.Contains("fullDocument"))
            {
                fullDocument.Should().Be(backingDocument["fullDocument"].AsBsonDocument);
            }
            else
            {
                fullDocument.Should().BeNull();
            }

            var resumeToken = actualDocument.ResumeToken;
            resumeToken.Should().Be(backingDocument["_id"].AsBsonDocument);

            var updateDescription = actualDocument.UpdateDescription;
            if (backingDocument.Contains("updateDescription"))
            {
                var removedFields = new BsonArray(updateDescription.RemovedFields);
                removedFields.Should().Be(backingDocument["updateDescription"]["removedFields"].AsBsonArray);
                updateDescription.UpdatedFields.Should().Be(backingDocument["updateDescription"]["updatedFields"].AsBsonDocument);
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

            var actualMongoException = actualException as MongoException;
            if (actualMongoException == null)
            {
                throw new Exception($"Expected a MongoCommandException to be thrown but instead a ${actualException.GetType().Name} was.");
            }

            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedResult, "error");
            var expectedError = expectedResult["error"].AsBsonDocument;

            JsonDrivenHelper.EnsureAllFieldsAreValid(expectedError, "code", "errorLabels");
            var expectedCode = expectedError["code"].ToInt32();

            int? actualCode = null;
            if (actualMongoException is MongoCommandException mongoCommandException)
            {
                actualCode = mongoCommandException.Code;
            }
            if (actualMongoException is MongoExecutionTimeoutException mongoExecutionTimeoutException)
            {
                actualCode = mongoExecutionTimeoutException.Code;
            }
            actualCode.Should().HaveValue();
            actualCode.Value.Should().Be(expectedCode);

            if (expectedError.TryGetValue("errorLabels", out var expectedLabels))
            {
                foreach (var expectedLabel in expectedLabels.AsBsonArray)
                {
                    actualMongoException.HasErrorLabel(expectedLabel.ToString()).Should().BeTrue();
                }
            }
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix
            {
                get
                {
                    return "MongoDB.Driver.Tests.Specifications.change_streams.tests.legacy.";
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
