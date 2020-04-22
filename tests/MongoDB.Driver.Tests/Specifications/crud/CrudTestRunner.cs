/* Copyright 2010-present MongoDB Inc.
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
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.crud
{
    public class CrudTestRunner
    {
        #region static
        private static readonly HashSet<string> __commandsToNotCapture = new HashSet<string>
        {
            "configureFailPoint",
            "isMaster",
            "buildInfo",
            "getLastError",
            "authenticate",
            "saslStart",
            "saslContinue",
            "getnonce"
        };
        #endregion

        private readonly EventCapturer _capturedEvents = new EventCapturer()
            .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            RunTestDefinition(testCase.Shared, testCase.Test);
        }

        public void RunTestDefinition(BsonDocument definition, BsonDocument test)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(definition, "_path", "database_name", "collection_name", "runOn", "minServerVersion", "maxServerVersion", "data", "tests");
            JsonDrivenHelper.EnsureAllFieldsAreValid(test, "description", "skipReason", "operation", "operations", "expectations", "outcome", "async");
            SkipTestIfNeeded(definition, test);

            var databaseName = GetDatabaseName(definition);
            var collectionName = GetCollectionName(definition);
            RemoveCollectionData(databaseName, collectionName);
            PrepareData(databaseName, collectionName, definition);

            using (var client = CreateDisposableClient(_capturedEvents))
            {
                var database = client.GetDatabase(databaseName);
                var operations = test.Contains("operation")
                    ? new[] { test["operation"].AsBsonDocument }
                    : test["operations"].AsBsonArray.Cast<BsonDocument>();
                var outcome = (BsonDocument)test.GetValue("outcome", null);
                var async = test["async"].AsBoolean;

                foreach (var operation in operations)
                {
                    var collection = collectionName == null ? null : GetCollection(database, collectionName, operation);
                    ExecuteOperation(client, database, collection, operation, outcome, async);
                }

                AssertEventsIfNeeded(_capturedEvents, test);
            }
        }

        private void AssertEvent(object actualEvent, BsonDocument expectedEvent)
        {
            if (expectedEvent.ElementCount != 1)
            {
                throw new FormatException("Expected event must be a document with a single element with a name the specifies the type of the event.");
            }

            var eventType = expectedEvent.GetElement(0).Name;
            var eventAsserter = EventAsserterFactory.CreateAsserter(eventType);
            eventAsserter.AssertAspects(actualEvent, expectedEvent[0].AsBsonDocument);
        }

        private void AssertEventsIfNeeded(EventCapturer eventCapturer, BsonDocument test)
        {
            if (test.TryGetValue("expectations", out var expectations))
            {
                var actualEvents = eventCapturer.Events;
                var expectedEvents = expectations.AsBsonArray.Cast<BsonDocument>().ToList();
                RemoveExtraFindCommandsFromActualEvents(actualEvents, expectedEvents.Count);

                var minCount = Math.Min(actualEvents.Count, expectedEvents.Count);
                for (var i = 0; i < minCount; i++)
                {
                    AssertEvent(actualEvents[i], expectedEvents[i]);
                }

                if (actualEvents.Count < expectedEvents.Count)
                {
                    throw new Exception($"Missing event: {expectedEvents[actualEvents.Count]}.");
                }

                if (actualEvents.Count > expectedEvents.Count)
                {
                    throw new Exception($"Unexpected event of type: {actualEvents[expectedEvents.Count].GetType().Name}.");
                }
            }
        }

        private DisposableMongoClient CreateDisposableClient(EventCapturer eventCapturer)
        {
            return DriverTestConfiguration.CreateDisposableClient(
                (MongoClientSettings settings) =>
                {
                    settings.ClusterConfigurator = c =>
                    {
                        c = CoreTestConfiguration.ConfigureCluster(c);
                        c.Subscribe(eventCapturer);
                        c.ConfigureServer(ss => ss.With(heartbeatInterval: Timeout.InfiniteTimeSpan));
                    };
                });
        }

        private void ExecuteOperation(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument operation, BsonDocument outcome, bool async)
        {
            JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "object", "collectionOptions", "arguments", "error", "result");

            if (operation.TryGetValue("object", out var @object))
            {
                if (@object.AsString == "database")
                {
                    collection = null;
                }
            }

            var name = (string)operation["name"];
            var test = CrudOperationTestFactory.CreateTest(name);
            var isErrorExpected = false;
            if (operation.TryGetValue("error", out var error) ||
                (outcome != null && outcome.TryGetValue("error", out error)))
            {
                isErrorExpected = error.ToBoolean();
            }

            var arguments = (BsonDocument)operation.GetValue("arguments", new BsonDocument());
            test.SkipIfNotSupported(arguments);
            if (operation.TryGetValue("result", out var result))
            {
                outcome = outcome ?? new BsonDocument();
                outcome["result"] = result;
            }

            test.Execute(client.Cluster.Description, database, collection, arguments, outcome, isErrorExpected, async);

            var threwException = test.ActualException != null;
            if (isErrorExpected && !threwException)
            {
                throw new Exception("The test was expected to throw an exception, but no exception was thrown.");
            }
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName, BsonDocument operation)
        {
            var collectionSettings = ParseCollectionOptions(operation);

            var collection = database.GetCollection<BsonDocument>(
                collectionName,
                collectionSettings);

            return collection;
        }

        private string GetCollectionName(BsonDocument definition)
        {
            if (definition.TryGetValue("collection_name", out var collectionName))
            {
                return collectionName.AsString;
            }
            else
            {
                return DriverTestConfiguration.CollectionNamespace.CollectionName;
            }
        }

        private string GetDatabaseName(BsonDocument definition)
        {
            if (definition.TryGetValue("database_name", out var databaseName))
            {
                return databaseName.AsString;
            }
            else
            {
                return DriverTestConfiguration.DatabaseNamespace.DatabaseName;
            }
        }

        private void ParseCollectionOptions(MongoCollectionSettings settings, BsonDocument collectionOptions)
        {
            foreach (var collectionOption in collectionOptions.Elements)
            {
                switch (collectionOption.Name)
                {
                    case "readConcern":
                        settings.ReadConcern = ReadConcern.FromBsonDocument(collectionOption.Value.AsBsonDocument);
                        break;
                    case "writeConcern":
                        settings.WriteConcern = WriteConcern.FromBsonDocument(collectionOption.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Unexpected collection option: {collectionOption.Name}");
                }
            }
        }

        private MongoCollectionSettings ParseCollectionOptions(BsonDocument operation)
        {
            if (operation.TryGetValue("collectionOptions", out var collectionOptions))
            {
                var settings = new MongoCollectionSettings();
                ParseCollectionOptions(settings, collectionOptions.AsBsonDocument);
                return settings;
            }
            else
            {
                return null;
            }
        }

        private void PrepareData(string databaseName, string collectionName, BsonDocument operation)
        {
            if (operation.TryGetValue("data", out var data))
            {
                var documents = data.AsBsonArray.Cast<BsonDocument>().ToList();
                if (documents.Count > 0)
                {
                    DriverTestConfiguration
                        .Client
                        .GetDatabase(databaseName)
                        .GetCollection<BsonDocument>(collectionName, new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority })
                        .InsertMany(documents);
                }
            }
        }

        private void RemoveCollectionData(string databaseName, string collectionName)
        {
            var database = DriverTestConfiguration.Client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName).WithWriteConcern(WriteConcern.WMajority);
            collection.DeleteMany(FilterDefinition<BsonDocument>.Empty);
        }

        private void RemoveExtraFindCommandsFromActualEvents(List<object> actualEvents, int expectedCount)
        {
            while (actualEvents.Count > expectedCount)
            {
                var lastEvent = actualEvents[actualEvents.Count - 1];
                if (lastEvent is CommandStartedEvent commandStartedEvent &&
                    commandStartedEvent.CommandName == "find")
                {
                    actualEvents.RemoveAt(actualEvents.Count - 1);
                }
                else
                {
                    return;
                }
            }
        }

        private void SkipTestIfNeeded(BsonDocument definition, BsonDocument test)
        {
            if (definition.TryGetValue("runOn", out var runOn))
            {
                RequireServer.Check().RunOn(runOn.AsBsonArray);
            }

            if (definition.TryGetValue("minServerVersion", out var minServerVersion))
            {
                RequireServer.Check().VersionGreaterThanOrEqualTo(minServerVersion.AsString);
            }

            if (definition.TryGetValue("maxServerVersion", out var maxServerVersion))
            {
                RequireServer.Check().VersionLessThanOrEqualTo(maxServerVersion.AsString);
            }

            if (test.TryGetValue("skipReason", out var reason))
            {
                throw new SkipException(reason.AsString);
            }

            if (definition["_path"].AsString.EndsWith("aggregate-out-readConcern.json") &&
                test["description"].AsString == "invalid readConcern with out stage")
            {
                throw new SkipException("The C# driver does not support invalid read concerns.");
            }
        }

        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.crud.tests.";

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
        }
    }
}
