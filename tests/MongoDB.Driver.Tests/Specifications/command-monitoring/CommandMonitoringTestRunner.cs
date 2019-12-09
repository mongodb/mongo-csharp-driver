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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    public class CommandMonitoringTestRunner
    {
        private static MongoClient __client;
        private static EventCapturer __capturedEvents;
        private static Dictionary<string, Func<ICrudOperationTest>> __tests;
        private static string[] __commandsToCapture;
        private static bool __oneTimeSetupHasRun = false;
        private static object __oneTimeSetupLock = new object();

        static CommandMonitoringTestRunner()
        {
            __commandsToCapture = new string[]
            {
                "delete",
                "insert",
                "update",
                "find",
                "count",
                "killCursors",
                "getMore"
            };

            __tests = new Dictionary<string, Func<ICrudOperationTest>>
            {
                { "bulkWrite", () => new BulkWriteTest() },
                { "count", () => new CountTest() },
                { "deleteMany", () => new DeleteManyTest() },
                { "deleteOne", () => new DeleteOneTest() },
                { "find", () => new FindTest() },
                { "insertMany", () => new InsertManyTest() },
                { "insertOne", () => new InsertOneTest() },
                { "updateMany", () => new UpdateManyTest() },
                { "updateOne", () => new UpdateOneTest() },
            };
        }

        public CommandMonitoringTestRunner()
        {
            lock (__oneTimeSetupLock)
            {
                __oneTimeSetupHasRun = __oneTimeSetupHasRun || OneTimeSetup();
            }
        }

        public bool OneTimeSetup()
        {
            __capturedEvents = new EventCapturer()
                .Capture<CommandStartedEvent>(e => __commandsToCapture.Contains(e.CommandName))
                .Capture<CommandSucceededEvent>(e => __commandsToCapture.Contains(e.CommandName))
                .Capture<CommandFailedEvent>(e => __commandsToCapture.Contains(e.CommandName));

            var settings = new MongoClientSettings
            {
                ClusterConfigurator = cb =>
                {
                    cb = CoreTestConfiguration.ConfigureCluster(cb);
                    cb.Subscribe(__capturedEvents);

                    // never heartbeat...
                    cb.ConfigureServer(ss => ss.With(heartbeatInterval: Timeout.InfiniteTimeSpan));
                }
            };
            settings.RetryWrites = false;

            __client = new MongoClient(settings);

            return true;
        }

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var definition = testCase.Test;

            BsonValue bsonValue;
            if (definition.TryGetValue("ignore_if_server_version_greater_than", out bsonValue))
            {
                var serverVersion = GetServerVersion();
                var maxServerVersion = SemanticVersion.Parse(bsonValue.AsString);
                if (serverVersion > maxServerVersion)
                {
                    throw new SkipException($"Test ignored because server version {serverVersion} is greater than max server version {maxServerVersion}.");
                }
            }
            if (definition.TryGetValue("ignore_if_server_version_less_than", out bsonValue))
            {
                var serverVersion = GetServerVersion();
                var minServerVersion = SemanticVersion.Parse(bsonValue.AsString);
                if (serverVersion < minServerVersion)
                {
                    throw new SkipException($"Test ignored because server version {serverVersion} is less than min server version {minServerVersion}.");
                }
            }

            // TODO: re-enable these tests once a decision has been made about how to deal with unexpected fields in the server response (see: CSHARP-2444)
            if (CoreTestConfiguration.ServerVersion >= new SemanticVersion(4, 1, 5, ""))
            {
                switch (definition["description"].AsString)
                {
                    case "A successful insert one command with write errors":
                    case "A successful insert many command with write errors":
                        throw new SkipException("Test ignored because of CSHARP-2444");
                }
            }

            var data = testCase.Shared["data"].AsBsonArray.Cast<BsonDocument>().ToList();
            var databaseName = testCase.Shared["database_name"].AsString;
            var collectionName = testCase.Shared["collection_name"].AsString;

            var operation = (BsonDocument)definition["operation"];
            var database = __client.GetDatabase(databaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            database.DropCollection(collection.CollectionNamespace.CollectionName);
            collection.InsertMany(data);

            __capturedEvents.Clear();
            try
            {
                ExecuteOperation(database, collectionName, operation, definition["async"].AsBoolean);
            }
            catch (NotImplementedException)
            {
                throw;
            }
            catch (Exception)
            {
                // catch everything...
            }

            var expectations = (BsonArray)definition["expectations"];
            if (!SpinWait.SpinUntil(() => __capturedEvents.Count == expectations.Count, TimeSpan.FromSeconds(5)))
            {
                throw new Exception($"Expected {expectations.Count} events but only {__capturedEvents.Count} events were captured.");
            }

            long? operationId = null;
            foreach (BsonDocument expected in expectations)
            {
                if (expected.Contains("command_started_event"))
                {
                    var actual = (CommandStartedEvent)__capturedEvents.Next();
                    if (!operationId.HasValue)
                    {
                        operationId = actual.OperationId;
                    }
                    actual.OperationId.Should().Be(operationId);
                    VerifyCommandStartedEvent(actual, (BsonDocument)expected["command_started_event"], databaseName, collectionName);
                }
                else if (expected.Contains("command_succeeded_event"))
                {
                    var actual = (CommandSucceededEvent)__capturedEvents.Next();
                    actual.OperationId.Should().Be(operationId);
                    VerifyCommandSucceededEvent(actual, (BsonDocument)expected["command_succeeded_event"], databaseName, collectionName);
                }
                else if (expected.Contains("command_failed_event"))
                {
                    var actual = (CommandFailedEvent)__capturedEvents.Next();
                    actual.OperationId.Should().Be(operationId);
                    VerifyCommandFailedEvent(actual, (BsonDocument)expected["command_failed_event"], databaseName, collectionName);
                }
                else
                {
                    Assert.True(false, "Unknown event type.");
                }
            }
        }

        private IMongoCollection<BsonDocument> GetCollection(IMongoDatabase database, string collectionName, BsonDocument operation)
        {
            var collectionSettings = ParseOperationCollectionSettings(operation);

            var collection = database.GetCollection<BsonDocument>(
                collectionName,
                collectionSettings);

            return collection;
        }

        private SemanticVersion GetServerVersion()
        {
            var server = __client.Cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
            return server.Description.Version;
        }

        private void ExecuteOperation(IMongoDatabase database, string collectionName, BsonDocument operation, bool async)
        {
            var name = (string)operation["name"];
            Func<ICrudOperationTest> factory;
            if (!__tests.TryGetValue(name, out factory))
            {
                throw new NotImplementedException("The operation " + name + " has not been implemented.");
            }

            var arguments = (BsonDocument)operation.GetValue("arguments", new BsonDocument());
            var test = factory();
            string reason;
            if (!test.CanExecute(__client.Cluster.Description, arguments, out reason))
            {
                throw new SkipException(reason);
            }

            var collection = GetCollection(database, collectionName, operation);
            test.Execute(__client.Cluster.Description, database, collection, arguments, async);
        }

        private MongoCollectionSettings ParseCollectionSettings(BsonDocument collectionOptions)
        {
            var settings = new MongoCollectionSettings();
            foreach (var collectionOption in collectionOptions.Elements)
            {
                switch (collectionOption.Name)
                {
                    case "writeConcern":
                        settings.WriteConcern = WriteConcern.FromBsonDocument(collectionOption.Value.AsBsonDocument);
                        break;
                    default:
                        throw new FormatException($"Unexpected collection option: {collectionOption.Name}.");
                }
            }

            return settings;
        }

        private MongoCollectionSettings ParseOperationCollectionSettings(BsonDocument operation)
        {
            if (operation.TryGetValue("collectionOptions", out var collectionOptions))
            {
                return ParseCollectionSettings(collectionOptions.AsBsonDocument);
            }
            else
            {
                return null;
            }
        }

        private void VerifyCommandStartedEvent(CommandStartedEvent actual, BsonDocument expected, string databaseName, string collectionName)
        {
            actual.CommandName.Should().Be(expected["command_name"].ToString());
            actual.DatabaseNamespace.Should().Be(new DatabaseNamespace(databaseName));
            var actualCommand = MassageCommand(actual.CommandName, actual.Command);
            var expectedCommand = (BsonDocument)expected["command"];
            if (actualCommand.Contains("$db"))
            {
                expectedCommand["$db"] = databaseName;
            }
            actualCommand.Should().BeEquivalentTo(expectedCommand);
        }

        private void VerifyCommandSucceededEvent(CommandSucceededEvent actual, BsonDocument expected, string databaseName, string collectionName)
        {
            actual.CommandName.Should().Be(expected["command_name"].ToString());
            var expectedReply = (BsonDocument)expected["reply"];
            var reply = MassageReply(actual.CommandName, actual.Reply, expectedReply);
            reply.Should().BeEquivalentTo(expectedReply);
        }

        private void VerifyCommandFailedEvent(CommandFailedEvent actual, BsonDocument expected, string databaseName, string collectionName)
        {
            actual.CommandName.Should().Be(expected["command_name"].ToString());
        }

        private BsonDocument MassageCommand(string commandName, BsonDocument command)
        {
            var massagedCommand = (BsonDocument)command.DeepClone();
            switch (commandName)
            {
                case "delete":
                    massagedCommand["ordered"] = massagedCommand.GetValue("ordered", true);
                    break;
                case "getMore":
                    massagedCommand["getMore"] = 42L;
                    break;
                case "insert":
                    massagedCommand["ordered"] = massagedCommand.GetValue("ordered", true);
                    break;
                case "killCursors":
                    massagedCommand["cursors"][0] = 42L;
                    break;
                case "update":
                    massagedCommand["ordered"] = massagedCommand.GetValue("ordered", true);
                    break;
            }

            massagedCommand.Remove("$clusterTime");
            massagedCommand.Remove("lsid");

            return massagedCommand;
        }

        private BsonDocument MassageReply(string commandName, BsonDocument reply, BsonDocument expectedReply)
        {
            var massagedReply = (BsonDocument)reply.DeepClone();
            switch (commandName)
            {
                case "find":
                case "getMore":
                    if (massagedReply.Contains("cursor") && massagedReply["cursor"]["id"] != 0L)
                    {
                        massagedReply["cursor"]["id"] = 42L;
                    }
                    break;
                case "killCursors":
                    massagedReply["cursorsUnknown"][0] = 42L;
                    break;
                case "delete":
                case "insert":
                case "update":
                    if (massagedReply.Contains("writeErrors"))
                    {
                        foreach (BsonDocument writeError in (BsonArray)massagedReply["writeErrors"])
                        {
                            writeError["code"] = 42;
                            writeError["errmsg"] = "";
                            writeError.Remove("codeName");
                        }
                    }
                    break;
            }

            // add any fields in the actual reply into the expected reply that don't already exist
            expectedReply.Merge(reply, false);

            return massagedReply;
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            // protected properties
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.command_monitoring.tests.";

            // protected methods
            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var testCases = base.CreateTestCases(document);
                foreach (var testCase in testCases)
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
