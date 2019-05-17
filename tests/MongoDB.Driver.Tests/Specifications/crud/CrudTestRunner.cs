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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using MongoDB.Bson;
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
            "getnonce",
            "find"
        };
        #endregion

        private readonly EventCapturer _capturedEvents = new EventCapturer()
            .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(BsonDocument definition, BsonDocument test, bool async)
        {
            SkipTestIfNeeded(definition, test);

            var databaseName = DriverTestConfiguration.DatabaseNamespace.DatabaseName;
            var collectionName = GetCollectionName(definition);
            DropCollection(databaseName, collectionName);
            PrepareData(databaseName, collectionName, definition);

            using (var client = CreateDisposableClient(_capturedEvents))
            {
                var database = client.GetDatabase(databaseName);
                var operations = test.Contains("operation")
                    ? new[] { test["operation"].AsBsonDocument }
                    : test["operations"].AsBsonArray.Cast<BsonDocument>();

                foreach (var operation in operations)
                {
                    var collection = GetCollection(database, collectionName, operation);
                    ExecuteOperation(client, database, collection, operation, (BsonDocument)test.GetValue("outcome", null), async);
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

        private void DropCollection(string databaseName, string collectionName)
        {
            DriverTestConfiguration.Client.GetDatabase(databaseName).DropCollection(collectionName);
        }

        private void ExecuteOperation(IMongoClient client, IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument operation, BsonDocument outcome, bool async)
        {
            var name = (string)operation["name"];
            var test = CrudOperationTestFactory.CreateTest(name);
            bool isErrorExpected = operation.GetValue("error", false).AsBoolean;

            var arguments = (BsonDocument)operation.GetValue("arguments", new BsonDocument());
            test.SkipIfNotSupported(arguments);

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

        private string GetCollectionName(BsonDocument operation)
        {
            if (operation.TryGetValue("collection_name", out var collectionName))
            {
                return collectionName.AsString;
            }
            else
            {
                return DriverTestConfiguration.CollectionNamespace.CollectionName;
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
            var data = operation["data"].AsBsonArray.Cast<BsonDocument>();
            DriverTestConfiguration
                .Client
                .GetDatabase(databaseName)
                .GetCollection<BsonDocument>(collectionName)
                .InsertMany(data);
        }

        private void SkipTestIfNeeded(BsonDocument definition, BsonDocument test)
        {
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
        }

        private class TestCaseFactory : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                const string prefix = "MongoDB.Driver.Tests.Specifications.crud.tests.";
                var definitions = typeof(TestCaseFactory).GetTypeInfo().Assembly
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .Select(path => ReadDefinition(path));

                var testCases = new List<object[]>();
                foreach (var definition in definitions)
                {
                    foreach (BsonDocument test in definition["tests"].AsBsonArray)
                    {
                        foreach (var async in new[] { false, true })
                        {
                            //var testCase = new TestCaseData(definition, test, async);
                            //testCase.SetCategory("Specifications");
                            //testCase.SetCategory("crud");
                            //testCase.SetName($"{test["description"]}({async})");
                            var testCase = new object[] { definition, test, async };
                            testCases.Add(testCase);
                        }
                    }
                }

                return testCases.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static BsonDocument ReadDefinition(string path)
            {
                using (var definitionStream = typeof(TestCaseFactory).GetTypeInfo().Assembly.GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    var definition = BsonDocument.Parse(definitionString);
                    definition.InsertAt(0, new BsonElement("path", path));
                    return definition;
                }
            }
        }
    }
}
