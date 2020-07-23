/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Driver.Tests.JsonDrivenTests;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_reads
{
    public sealed class RetryableReadsTestRunner
    {
        #region static
        private static readonly HashSet<string> __commandsToNotCapture = new HashSet<string>
        {
            "isMaster",
            "buildInfo",
            "getLastError",
            "authenticate",
            "saslStart",
            "saslContinue",
            "getnonce"
        };
        #endregion

        // private fields
        private string _databaseName = "retryable-reads-tests";
        private string _collectionName = "coll";
        private string _bucketName = "fs";

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
            JsonDrivenHelper.EnsureAllFieldsAreValid(
                shared,
                "_path",
                "runOn",
                "data",
                "tests",
                "database_name",
                "collection_name",
                "bucket_name");
            JsonDrivenHelper.EnsureAllFieldsAreValid(
                test,
                "description",
                "clientOptions",
                "retryableReads",
                "failPoint",
                "operations",
                "result",
                "expectations",
                "async");

            if (shared.TryGetValue("runOn", out var runOn))
            {
                RequireServer.Check().RunOn(runOn.AsBsonArray);
            }
            if (test.TryGetValue("skipReason", out var skipReason))
            {
                throw new SkipException(skipReason.AsString);
            }

            DropCollection();
            CreateCollection();
            InsertData(shared);

            using (ConfigureFailPoint(test))
            {
                var eventCapturer = new EventCapturer()
                    .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));

                Dictionary<string, BsonValue> sessionIdMap;

                using (var client = CreateDisposableClient(test, eventCapturer))
                using (var session0 = StartSession(client, test, "session0"))
                using (var session1 = StartSession(client, test, "session1"))
                {
                    var objectMap = new Dictionary<string, object>
                    {
                        { "session0", session0 },
                        { "session1", session1 }
                    };
                    sessionIdMap = new Dictionary<string, BsonValue>
                    {
                        { "session0", session0.ServerSession.Id },
                        { "session1", session1.ServerSession.Id }
                    };

                    ExecuteOperations(client, objectMap, test);
                }

                AssertEvents(eventCapturer, test, sessionIdMap);
                AssertOutcome(test);
            }
        }

        private void DropCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            database.DropCollection(_collectionName);
        }

        private void CreateCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            database.CreateCollection(_collectionName);
        }

        private void InsertData(BsonDocument shared)
        {
            if (!shared.Contains("data"))
            {
                return;
            }

            if (shared.Contains("bucket_name"))
            {
                InsertGridFsData(shared);
                return;
            }
            var documents = shared["data"].AsBsonArray.Cast<BsonDocument>().ToList();
            if (documents.Count <= 0)
            {
                return;
            }
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<BsonDocument>(_collectionName).WithWriteConcern(WriteConcern.WMajority);
            collection.InsertMany(documents);
        }

        private void InsertGridFsData(BsonDocument shared)
        {
            var bucketName = shared["bucket_name"].AsString;
            var filesCollectionName = $"{bucketName}.files";
            var chunksCollectionName = $"{bucketName}.chunks";
            var filesDocuments = shared["data"][filesCollectionName].AsBsonArray.Cast<BsonDocument>().ToList();
            var chunksDocuments = shared["data"][chunksCollectionName].AsBsonArray.Cast<BsonDocument>().ToList();
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName);

            database.DropCollection(filesCollectionName);
            database.DropCollection(chunksCollectionName);
            database.GetCollection<BsonDocument>(filesCollectionName)
                .WithWriteConcern(WriteConcern.WMajority)
                .InsertMany(filesDocuments);
            database.GetCollection<BsonDocument>(chunksCollectionName)
                .WithWriteConcern(WriteConcern.WMajority)
                .InsertMany(chunksDocuments);
        }

        private DisposableMongoClient CreateDisposableClient(BsonDocument test, EventCapturer eventCapturer)
        {
            return DriverTestConfiguration.CreateDisposableClient((MongoClientSettings settings) =>
            {
                settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
                ConfigureClientSettings(settings, test);
                settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
            });
        }

        private void ConfigureClientSettings(MongoClientSettings settings, BsonDocument test)
        {
            if (test.Contains("clientOptions"))
            {
                foreach (var option in test["clientOptions"].AsBsonDocument)
                {
                    switch (option.Name)
                    {
                        case "retryReads":
                            settings.RetryReads = option.Value.ToBoolean();
                            break;

                        default:
                            throw new FormatException($"Unexpected client option: \"{option.Name}\".");
                    }
                }
            }
        }

        private ReadPreference ReadPreferenceFromBsonValue(BsonValue value)
        {
            if (value.BsonType == BsonType.String)
            {
                var mode = (ReadPreferenceMode)Enum.Parse(typeof(ReadPreferenceMode), value.AsString, ignoreCase: true);
                return new ReadPreference(mode);
            }

            return ReadPreference.FromBsonDocument(value.AsBsonDocument);
        }

        private IClientSessionHandle StartSession(IMongoClient client, BsonDocument test, string sessionKey)
        {
            var options = CreateSessionOptions(test, sessionKey);
            return client.StartSession(options);
        }

        private ClientSessionOptions CreateSessionOptions(BsonDocument test, string sessionKey)
        {
            var options = new ClientSessionOptions();
            if (test.Contains("sessionOptions"))
            {
                var sessionOptions = test["sessionOptions"].AsBsonDocument;
                if (sessionOptions.Contains(sessionKey))
                {
                    foreach (var option in sessionOptions[sessionKey].AsBsonDocument)
                    {
                        switch (option.Name)
                        {
                            case "causalConsistency":
                                options.CausalConsistency = option.Value.ToBoolean();
                                break;

                            case "defaultTransactionOptions":
                                options.DefaultTransactionOptions = ParseTransactionOptions(option.Value.AsBsonDocument);
                                break;

                            default:
                                throw new FormatException($"Unexpected session option: \"{option.Name}\".");
                        }
                    }
                }
            }
            return options;
        }

        private FailPoint ConfigureFailPoint(BsonDocument test)
        {
            BsonValue failPoint;
            if (test.TryGetValue("failPoint", out failPoint))
            {
                var cluster = DriverTestConfiguration.Client.Cluster;
                var server = cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
                var session = NoCoreSession.NewHandle();
                var command = failPoint.AsBsonDocument;
                return FailPoint.Configure(cluster, session, command);
            }

            return null;
        }

        private void ExecuteOperations(IMongoClient client, Dictionary<string, object> objectMap, BsonDocument test)
        {
            var factory = new JsonDrivenTestFactory(client, _databaseName, _collectionName, _bucketName, objectMap);

            foreach (var operation in test["operations"].AsBsonArray.Cast<BsonDocument>())
            {
                var receiver = operation["object"].AsString;
                var name = operation["name"].AsString;
                var jsonDrivenTest = factory.CreateTest(receiver, name);

                jsonDrivenTest.Arrange(operation);
                if (test["async"].AsBoolean)
                {
                    jsonDrivenTest.ActAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    jsonDrivenTest.Act(CancellationToken.None);
                }
                jsonDrivenTest.Assert();
            }
        }

        private void AssertEvents(EventCapturer actualEvents, BsonDocument test, Dictionary<string, BsonValue> sessionIdMap)
        {
            if (test.Contains("expectations"))
            {
                var expectedEvents = test["expectations"].AsBsonArray.Cast<BsonDocument>().GetEnumerator();

                while (actualEvents.Any())
                {
                    var actualEvent = actualEvents.Next();

                    if (!expectedEvents.MoveNext())
                    {
                        throw new Exception($"Unexpected event of type: {actualEvent.GetType().Name}.");
                    }
                    var expectedEvent = expectedEvents.Current;
                    RecursiveFieldSetter.SetAll(expectedEvent, "lsid", value => sessionIdMap[value.AsString]);

                    AssertEvent(actualEvent, expectedEvent);
                }

                if (expectedEvents.MoveNext())
                {
                    var expectedEvent = expectedEvents.Current;
                    throw new Exception($"Missing event: {expectedEvent}.");
                }
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

        private void AssertOutcome(BsonDocument test)
        {
            if (test.Contains("outcome"))
            {
                foreach (var aspect in test["outcome"].AsBsonDocument)
                {
                    switch (aspect.Name)
                    {
                        case "collection":
                            VerifyCollectionOutcome(aspect.Value.AsBsonDocument);
                            break;

                        default:
                            throw new FormatException($"Unexpected outcome aspect: {aspect.Name}.");
                    }
                }
            }
        }

        private TransactionOptions ParseTransactionOptions(BsonDocument document)
        {
            ReadConcern readConcern = null;
            ReadPreference readPreference = null;
            WriteConcern writeConcern = null;

            foreach (var element in document)
            {
                switch (element.Name)
                {
                    case "readConcern":
                        readConcern = ReadConcern.FromBsonDocument(element.Value.AsBsonDocument);
                        break;

                    case "readPreference":
                        readPreference = ReadPreference.FromBsonDocument(element.Value.AsBsonDocument);
                        break;

                    case "writeConcern":
                        writeConcern = WriteConcern.FromBsonDocument(element.Value.AsBsonDocument);
                        break;

                    default:
                        throw new ArgumentException($"Invalid field: {element.Name}.");
                }
            }

            return new TransactionOptions(readConcern, readPreference, writeConcern);
        }

        private void VerifyCollectionOutcome(BsonDocument outcome)
        {
            foreach (var aspect in outcome)
            {
                switch (aspect.Name)
                {
                    case "data":
                        VerifyCollectionData(aspect.Value.AsBsonArray.Cast<BsonDocument>());
                        break;

                    default:
                        throw new FormatException($"Unexpected collection outcome aspect: {aspect.Name}.");
                }
            }
        }

        private void VerifyCollectionData(IEnumerable<BsonDocument> expectedDocuments)
        {
            var database = DriverTestConfiguration.Client.GetDatabase(_databaseName);
            var collection = database.GetCollection<BsonDocument>(_collectionName);
            var actualDocuments = collection.Find("{}").ToList();
            actualDocuments.Should().BeEquivalentTo(expectedDocuments);
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            // the path is "retryable-reads" but the namespace is "retryable_reads"
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.retryable_reads.tests.";

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
