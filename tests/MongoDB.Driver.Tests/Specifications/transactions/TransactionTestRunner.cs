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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests.JsonDrivenTests;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.transactions
{
    public sealed class TransactionTestRunner : IJsonDrivenTestRunner, IDisposable
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

        // private fields
        private string _databaseName = "transaction-tests";
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private string _collectionName = "test";

        // public methods
        public void ConfigureFailPoint(IServer server, ICoreSessionHandle session, BsonDocument failCommand)
        {
            var failPoint = FailPoint.Configure(server, session, failCommand);
            _disposables.Add(failPoint);
        }

        public async Task ConfigureFailPointAsync(IServer server, ICoreSessionHandle session, BsonDocument failCommand)
        {
            var failPoint = await Task.Run(() => FailPoint.Configure(server, session, failCommand)).ConfigureAwait(false);
            _disposables.Add(failPoint);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void Run(JsonDrivenTestCase testCase)
        {
            Run(testCase.Shared, testCase.Test);
        }

        // private methods
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var disposable in _disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        private void Run(BsonDocument shared, BsonDocument test)
        {
            if (test.Contains("skipReason"))
            {
                throw new SkipException($"Test skipped because {test["skipReason"]}.");
            }

            if (shared.TryGetValue("runOn", out var runOn))
            {
                RequireServer.Check().RunOn(runOn.AsBsonArray);
            }

            JsonDrivenHelper.EnsureAllFieldsAreValid(shared,
                "_path",
                "database_name",
                "collection_name",
                "data",
                "tests",
                "runOn");
            JsonDrivenHelper.EnsureAllFieldsAreValid(test,
                "description",
                "clientOptions",
                "failPoint",
                "sessionOptions",
                "operations",
                "expectations",
                "outcome",
                "async",
                "useMultipleMongoses");

            _databaseName = shared["database_name"].AsString;
            _collectionName = shared["collection_name"].AsString;

            KillAllSessions();
            DropCollection();
            CreateCollection();
            InsertData(shared);

            if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded)
            {
                PrimeShardRoutersWithDistinctCommand();
            }

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));

            var useMultipleShardRouters = test.GetValue("useMultipleMongoses", false).ToBoolean();
            using (var client = CreateDisposableClient(test, eventCapturer, useMultipleShardRouters))
            using (ConfigureFailPointOnPrimaryOrShardRoutersIfNeeded(client, test))
            {
                Dictionary<string, BsonValue> sessionIdMap;

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

        private void KillAllSessions()
        {
            var client = DriverTestConfiguration.Client;
            var adminDatabase = client.GetDatabase("admin");
            var command = BsonDocument.Parse("{ killAllSessions : [] }");
            try
            {
                adminDatabase.RunCommand<BsonDocument>(command);
            }
            catch (MongoCommandException)
            {
                // ignore MongoCommandExceptions
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
            if (shared.Contains("data"))
            {
                var documents = shared["data"].AsBsonArray.Cast<BsonDocument>().ToList();
                if (documents.Count > 0)
                {
                    var client = DriverTestConfiguration.Client;
                    var database = client.GetDatabase(_databaseName);
                    var collection = database.GetCollection<BsonDocument>(_collectionName).WithWriteConcern(WriteConcern.WMajority);
                    collection.InsertMany(documents);
                }
            }
        }

        /// <summary>
        /// Temporary patch until SERVER-39704 is resolved.
        /// </summary>
        private void PrimeShardRoutersWithDistinctCommand()
        {
            foreach (var client in DriverTestConfiguration.DirectClientsToShardRouters)
            {
                var database = client.GetDatabase(_databaseName);
                var collection = database.GetCollection<BsonDocument>(_collectionName);
                collection.Distinct<BsonValue>("_id", "{ }");
            }
        }

        private DisposableMongoClient CreateDisposableClient(BsonDocument test, EventCapturer eventCapturer, bool useMultipleShardRouters)
        {
            return DriverTestConfiguration.CreateDisposableClient(
                (MongoClientSettings settings) =>
                {
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(5); // the default value for spec tests
                    ConfigureClientSettings(settings, test);
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                },
                useMultipleShardRouters);
        }

        private void ConfigureClientSettings(MongoClientSettings settings, BsonDocument test)
        {
            if (test.Contains("clientOptions"))
            {
                foreach (var option in test["clientOptions"].AsBsonDocument)
                {
                    switch (option.Name)
                    {
                        case "heartbeatFrequencyMS":
                            settings.HeartbeatInterval = TimeSpan.FromMilliseconds(option.Value.AsInt32);
                            break;

                        case "readConcernLevel":
                            var level = (ReadConcernLevel)Enum.Parse(typeof(ReadConcernLevel), option.Value.AsString, ignoreCase: true);
                            settings.ReadConcern = new ReadConcern(level);
                            break;

                        case "readPreference":
                            settings.ReadPreference = ReadPreferenceFromBsonValue(option.Value);
                            break;

                        case "retryWrites":
                            settings.RetryWrites = option.Value.ToBoolean();
                            break;

                        case "w":
                            if (option.Value.IsString)
                            {
                                settings.WriteConcern = new WriteConcern(option.Value.AsString);
                            }
                            else
                            {
                                settings.WriteConcern = new WriteConcern(option.Value.ToInt32());
                            }
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
            var options = ParseSessionOptions(test, sessionKey);
            return client.StartSession(options);
        }

        private ClientSessionOptions ParseSessionOptions(BsonDocument test, string sessionKey)
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

        private DisposableBundle ConfigureFailPointOnPrimaryOrShardRoutersIfNeeded(IMongoClient client, BsonDocument test)
        {
            if (!test.TryGetValue("failPoint", out var failPoint))
            {
                return null;
            }

            var cluster = client.Cluster;
            var timeOut = TimeSpan.FromSeconds(60);
            SpinWait.SpinUntil(() => cluster.Description.Type != ClusterType.Unknown, timeOut).Should().BeTrue();

            List<IServer> failPointServers;
            switch (cluster.Description.Type)
            {
                case ClusterType.ReplicaSet:
                    var primary = cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
                    failPointServers = new List<IServer> { primary };
                    break;

                case ClusterType.Sharded:
                    failPointServers =
                        cluster.Description.Servers
                        .Select(server => server.EndPoint)
                        .Select(endPoint => cluster.SelectServer(new EndPointServerSelector(endPoint), CancellationToken.None))
                        .ToList();
                    break;

                default:
                    throw new Exception($"Unsupported cluster type: {cluster.Description.Type}");
            }

            var session = NoCoreSession.NewHandle();
            var failPoints = failPointServers.Select(s => FailPoint.Configure(s, session, failPoint.AsBsonDocument)).ToList();

            return new DisposableBundle(failPoints);
        }

        private void ExecuteOperations(IMongoClient client, Dictionary<string, object> objectMap, BsonDocument test)
        {
            var factory = new JsonDrivenTestFactory(this, client, _databaseName, _collectionName, bucketName: null, objectMap);

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
            TimeSpan? maxCommitTimeMS = null;

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

                    case "maxCommitTimeMS":
                        maxCommitTimeMS = TimeSpan.FromMilliseconds(element.Value.ToInt32());
                        break;

                    default:
                        throw new ArgumentException($"Invalid field: {element.Name}.");
                }
            }

            return new TransactionOptions(readConcern, readPreference, writeConcern, maxCommitTimeMS);
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
            var database = DriverTestConfiguration.Client.GetDatabase(_databaseName).WithReadConcern(ReadConcern.Local);
            var collection = database.GetCollection<BsonDocument>(_collectionName);
            var actualDocuments = collection.Find("{}").ToList();
            actualDocuments.Should().BeEquivalentTo(expectedDocuments);
        }

        // nested types
        public class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix
            {
                get
                {
                    return "MongoDB.Driver.Tests.Specifications.transactions.tests.";
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
                return base.ShouldReadJsonDocument(path); // && path.EndsWith("commit.json");
            }
        }
    }
}
