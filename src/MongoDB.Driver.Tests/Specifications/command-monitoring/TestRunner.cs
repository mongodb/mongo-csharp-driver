/* Copyright 2010-2015 MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Specifications.command_monitoring
{
    [TestFixture]
    public class TestRunner
    {
        private static MongoClient __client;
        private static EventCapturer __capturedEvents;
        private static Dictionary<string, Func<ICrudOperationTest>> __tests;

        private static string[] __commandsToCapture;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
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

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
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

            __client = new MongoClient(settings);
        }

        [TestCaseSource(typeof(TestCaseFactory), "GetTestCases")]
        public async Task RunTestDefinitionAsync(IEnumerable<BsonDocument> data, string databaseName, string collectionName, BsonDocument definition)
        {
            BsonValue bsonValue;
            if (definition.TryGetValue("ignore_if_server_version_greater_than", out bsonValue))
            {
                var serverVersion = CoreTestConfiguration.ServerVersion;
                var maxServerVersion = SemanticVersion.Parse(bsonValue.AsString);
                if (serverVersion > maxServerVersion)
                {
                    Assert.Ignore($"Test ignored because server version {serverVersion} is greater than max server version {maxServerVersion}.");
                }
            }
            if (definition.TryGetValue("ignore_if_server_version_less_than", out bsonValue))
            {
                var serverVersion = CoreTestConfiguration.ServerVersion;
                var minServerVersion = SemanticVersion.Parse(bsonValue.AsString);
                if (serverVersion < minServerVersion)
                {
                    Assert.Ignore($"Test ignored because server version {serverVersion} is less than min server version {minServerVersion}.");
                }
            }

            var database = __client
                .GetDatabase(databaseName);
            var collection = database
                .GetCollection<BsonDocument>(collectionName);

            await database.DropCollectionAsync(collection.CollectionNamespace.CollectionName);
            await collection.InsertManyAsync(data);

            __capturedEvents.Clear();
            try
            {
                await ExecuteOperationAsync(database, collection, (BsonDocument)definition["operation"]);
            }
            catch (NotImplementedException)
            {
                throw;
            }
            catch (Exception)
            {
                // catch everything...
            }

            long? operationId = null;
            foreach (BsonDocument expected in (BsonArray)definition["expectations"])
            {
                if (!__capturedEvents.Any())
                {
                    Assert.Fail("Expected an event, but no events were captured.");
                }

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
                    Assert.Fail("Unknown event type.");
                }
            }
        }

        private Task ExecuteOperationAsync(IMongoDatabase database, IMongoCollection<BsonDocument> collection, BsonDocument operation)
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
                Assert.Ignore(reason);
                return Task.FromResult(false);
            }

            return factory().ExecuteAsync(__client.Cluster.Description, database, collection, arguments);
        }

        private void VerifyCommandStartedEvent(CommandStartedEvent actual, BsonDocument expected, string databaseName, string collectionName)
        {
            actual.CommandName.Should().Be(expected["command_name"].ToString());
            actual.DatabaseNamespace.Should().Be(new DatabaseNamespace(databaseName));
            var command = MassageCommand(actual.CommandName, actual.Command);
            command.Should().BeEquivalentTo((BsonDocument)expected["command"]);
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
            var massagedCommand = (BsonDocument)DeepCopy(command);
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
                    foreach (BsonDocument update in (BsonArray)massagedCommand["updates"])
                    {
                        update["multi"] = update.GetValue("multi", false);
                        update["upsert"] = update.GetValue("upsert", false);
                    }

                    break;
            }

            return massagedCommand;
        }

        private BsonDocument MassageReply(string commandName, BsonDocument reply, BsonDocument expectedReply)
        {
            var massagedReply = (BsonDocument)DeepCopy(reply);
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
                        }
                    }
                    break;
            }

            // add any fields in the actual reply into the expected reply that don't already exist
            expectedReply.Merge(reply, false);

            return massagedReply;
        }

        private BsonValue DeepCopy(BsonValue value)
        {
            if (value.BsonType == BsonType.Document)
            {
                var document = new BsonDocument();
                foreach (var element in (BsonDocument)value)
                {
                    document.Add(element.Name, DeepCopy(element.Value));
                }

                return document;
            }
            else if (value.BsonType == BsonType.Array)
            {
                var array = new BsonArray();
                foreach (var element in (BsonArray)value)
                {
                    array.Add(DeepCopy(element));
                }
                return array;
            }

            return value;
        }

        private static class TestCaseFactory
        {
            public static IEnumerable<ITestCaseData> GetTestCases()
            {
                const string prefix = "MongoDB.Driver.Tests.Specifications.command_monitoring.tests.";
                return Assembly
                    .GetExecutingAssembly()
                    .GetManifestResourceNames()
                    .Where(path => path.StartsWith(prefix) && path.EndsWith(".json"))
                    .SelectMany(path =>
                    {
                        var doc = ReadDocument(path);
                        var data = ((BsonArray)doc["data"]).Select(x => (BsonDocument)x).ToList();
                        var databaseName = doc["database_name"].ToString();
                        var collectionName = doc["collection_name"].ToString();

                        return ((BsonArray)doc["tests"]).Select(def =>
                        {
                            var testCase = new TestCaseData(data, databaseName, collectionName, (BsonDocument)def);
                            testCase.Categories.Add("Specifications");
                            testCase.Categories.Add("command-monitoring");
                            return testCase.SetName((string)def["description"]);
                        });
                    });
            }

            private static BsonDocument ReadDocument(string path)
            {
                using (var definitionStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
                using (var definitionStringReader = new StreamReader(definitionStream))
                {
                    var definitionString = definitionStringReader.ReadToEnd();
                    return BsonDocument.Parse(definitionString);
                }
            }
        }
    }
}
