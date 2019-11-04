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
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.TestHelpers.JsonDrivenTests;

namespace MongoDB.Driver.Tests.Specifications.Runner
{
    public abstract class MongoClientJsonDrivenSessionsTestRunner : MongoClientJsonDrivenTestRunnerBase
    {
        private const string SessionIdKeySuffix = "__ClientSessionId";

        protected override string[] ExpectedTestColumns => new[] { "description", "clientOptions", "useMultipleMongoses", "failPoint", "sessionOptions", "operations", "expectations", "outcome", "async" };

        // protected methods
        protected override void TestInitialize(MongoClient client, BsonDocument test, BsonDocument shared)
        {
            base.TestInitialize(client, test, shared);
            KillAllSessions();
        }

        protected override void AssertEvent(object actualEvent, BsonDocument expectedEvent)
        {
            base.AssertEvent(
                actualEvent,
                expectedEvent,
                (actual, expected) =>
                {
                    RecursiveFieldSetter.SetAll(
                        expected,
                        "lsid",
                        value =>
                        {
                            if (ObjectMap.TryGetValue(value.AsString + SessionIdKeySuffix, out var sessionId))
                            {
                                return (BsonValue)sessionId;
                            }
                            else
                            {
                                return value;
                            }
                        });
                });
        }

        protected override void ExecuteOperations(IMongoClient client, Dictionary<string, object> objectMap, BsonDocument test, EventCapturer eventCapturer = null)
        {
            var newItems = new Dictionary<string, object>();
            foreach (var mapItem in objectMap)
            {
                // Save session ids to have their values when the session will be disposed.
                if (mapItem.Value is IClientSessionHandle clientSessionHandle)
                {
                    newItems.Add(mapItem.Key + SessionIdKeySuffix, clientSessionHandle.ServerSession.Id);
                }
            }

            foreach (var newItem in newItems)
            {
                objectMap.Add(newItem.Key, newItem.Value);
            }

            base.ExecuteOperations(client, objectMap, test, eventCapturer);
        }

        protected void KillAllSessions()
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

        protected IClientSessionHandle StartSession(IMongoClient client, BsonDocument test, string sessionKey)
        {
            var options = ParseSessionOptions(test, sessionKey);
            return client.StartSession(options);
        }

        // private methods
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
    }
}