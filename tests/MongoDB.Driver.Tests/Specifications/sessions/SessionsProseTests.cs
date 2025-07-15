﻿/* Copyright 2021-present MongoDB Inc.
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Encryption;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests.Specifications.sessions
{
    [Trait("Category", "Serverless")]
    [Trait("Category", "Integration")]
    public class SessionsProseTests : LoggableTestClass
    {
        public SessionsProseTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Snapshot_and_causal_consistent_session_is_not_allowed()
        {
            RequireServer.Check();

            var sessionOptions = new ClientSessionOptions()
            {
                Snapshot = true,
                CausalConsistency = true
            };

            var mongoClient = DriverTestConfiguration.Client;

            var exception = Record.Exception(() => mongoClient.StartSession(sessionOptions));
            exception.Should().BeOfType<NotSupportedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Ensure_explicit_session_raises_error_if_connection_does_not_support_sessions([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            CoreTestConfiguration.SkipMongocryptdTests_SERVER_106469();

            using var mongocryptdContext = GetMongocryptdContext();
            using var session = mongocryptdContext.MongoClient.StartSession();

            var exception = async ?
                await Record.ExceptionAsync(() => mongocryptdContext.MongocryptdCollection.FindAsync(session, FilterDefinition<BsonDocument>.Empty)) :
                Record.Exception(() => mongocryptdContext.MongocryptdCollection.Find(session, FilterDefinition<BsonDocument>.Empty).ToList());


            exception.Should().BeOfType<MongoClientException>().Subject.Message.Should().Be("Sessions are not supported.");
            exception = async ?
                await Record.ExceptionAsync(() => mongocryptdContext.MongocryptdCollection.InsertOneAsync(session, new BsonDocument())) :
                Record.Exception(() => mongocryptdContext.MongocryptdCollection.InsertOne(session, new BsonDocument()));

            exception.Should().BeOfType<MongoClientException>().Subject.Message.Should().Be("Sessions are not supported.");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Ensure_implicit_session_is_ignored_if_connection_does_not_support_sessions([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.ClientSideEncryption);
            CoreTestConfiguration.SkipMongocryptdTests_SERVER_106469();

            using var mongocryptdContext = GetMongocryptdContext();

            try
            {
                if (async)
                {
                    await mongocryptdContext.MongocryptdCollection.FindAsync(FilterDefinition<BsonDocument>.Empty);
                }
                else
                {
                    mongocryptdContext.MongocryptdCollection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                }
            }
            catch { } // Ignore command errors from mongocryptd

            try
            {
                if (async)
                {
                    await mongocryptdContext.MongocryptdCollection.InsertOneAsync(new BsonDocument());
                }
                else
                {
                    mongocryptdContext.MongocryptdCollection.InsertOne(new BsonDocument());
                }
            }
            catch { } // Ignore command errors from mongocryptd

            var commandEvents = mongocryptdContext.EventCapturer.Events.OfType<CommandStartedEvent>().ToArray();
            commandEvents.Single(c => c.CommandName == "find").Command.Contains("lsid").Should().BeFalse();
            commandEvents.Single(c => c.CommandName == "insert").Command.Contains("lsid").Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Ensure_server_session_are_allocated_only_on_connection_checkout([Values(true, false)]bool async)
        {
            var eventCapturer = new EventCapturer()
               .Capture<CommandStartedEvent>();

            using var client = DriverTestConfiguration.CreateMongoClient(
                (MongoClientSettings settings) =>
                {
                    settings.RetryWrites = true;
                    settings.MaxConnectionPoolSize = 1;
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.LoggingSettings = LoggingSettings;
                });

            var database = client.GetDatabase("test");

            database.DropCollection("inventory");
            var collection = database.GetCollection<BsonDocument>("inventory");

            const int operationsCount = 8;
            var singleSessionUsed = false;
            for (int i = 0; i < 5; i++)
            {
                eventCapturer.Clear();
                await ThreadingUtilities.ExecuteTasksOnNewThreads(operationsCount, async i =>
                {
                    switch (i)
                    {
                        case 0:
                            if (async)
                            {
                                await collection.InsertOneAsync(new BsonDocument("x", 0));
                            }
                            else
                            {
                                collection.InsertOne(new BsonDocument("x", 0));
                            }
                            break;
                        case 1:
                            if (async)
                            {
                                await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", 1));
                            }
                            else
                            {
                                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", 1));
                            }
                            break;
                        case 2:
                            if (async)
                            {
                                await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Empty, Builders<BsonDocument>.Update.Set("a", 1));
                            }
                            else
                            {
                                collection.UpdateOne(Builders<BsonDocument>.Filter.Empty, Builders<BsonDocument>.Update.Set("a", 1));
                            }
                            break;
                        case 3:
                            var bulkWriteRequests = new WriteModel<BsonDocument>[]
                            {
                                new UpdateOneModel<BsonDocument>(Builders<BsonDocument>.Filter.Empty, new BsonDocument("$set", new BsonDocument("1", 1)))
                            };

                            if (async)
                            {
                                await collection.BulkWriteAsync(bulkWriteRequests);
                            }
                            else
                            {
                                collection.BulkWrite(bulkWriteRequests);
                            }
                            break;
                        case 4:
                            if (async)
                            {
                                await collection.FindOneAndDeleteAsync(Builders<BsonDocument>.Filter.Empty);
                            }
                            else
                            {
                                collection.FindOneAndDelete(Builders<BsonDocument>.Filter.Empty);
                            }
                            break;
                        case 5:
                            if (async)
                            {
                                await collection.FindOneAndUpdateAsync(Builders<BsonDocument>.Filter.Empty, Builders<BsonDocument>.Update.Set("a", 1));
                            }
                            else
                            {
                                collection.FindOneAndUpdate(Builders<BsonDocument>.Filter.Empty, Builders<BsonDocument>.Update.Set("a", 1));
                            }

                            break;
                        case 6:
                            if (async)
                            {
                                await collection.FindOneAndReplaceAsync(Builders<BsonDocument>.Filter.Empty, new BsonDocument("x", 0));
                            }
                            else
                            {
                                collection.FindOneAndReplace(Builders<BsonDocument>.Filter.Empty, new BsonDocument("x", 0));
                            }
                            break;
                        case 7:
                            if (async)
                            {
                                var cursor = await collection.FindAsync(Builders<BsonDocument>.Filter.Empty);
                                _ = await cursor.ToListAsync();
                            }
                            else
                            {
                                _ = collection.Find(Builders<BsonDocument>.Filter.Empty).ToList();
                            }
                            break;
                    }
                });

                eventCapturer.WaitForOrThrowIfTimeout(e => e.OfType<CommandStartedEvent>().Count() >= operationsCount, TimeSpan.FromSeconds(10));
                var lsids = eventCapturer.Events.OfType<CommandStartedEvent>().Select(c => c.Command["lsid"]).ToArray();
                var distinctLsidsCount = lsids.Distinct().Count();

                distinctLsidsCount.Should().BeLessThan(operationsCount);
                if (distinctLsidsCount == 1)
                {
                    singleSessionUsed = true;
                    break;
                }
            }

            singleSessionUsed.Should().BeTrue("At least one iteration should use single session");
        }

        [Fact]
        public async Task Ensure_server_session_are_allocated_only_on_connection_checkout_deterministic()
        {
            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolCheckedOutConnectionEvent>()
               .Capture<CommandStartedEvent>();

            using var client = DriverTestConfiguration.CreateMongoClient(
                (MongoClientSettings settings) =>
                {
                    settings.MaxConnectionPoolSize = 1;
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.LoggingSettings = LoggingSettings;
                });

            var database = client.GetDatabase("test");
            database.DropCollection("inventory");
            var collection = database.GetCollection<BsonDocument>("inventory");

            collection.InsertOne(new BsonDocument("x", 0));

            var serverSessionPool = (CoreServerSessionPool)Reflector.GetFieldValue(client.Cluster, "_serverSessionPool");
            var serverSessionsList = (List<ICoreServerSession>)Reflector.GetFieldValue(serverSessionPool, "_pool");

            var serverSession = serverSessionsList.Single();
            eventCapturer.Clear();

            var eventsTask = eventCapturer.NotifyWhen(events =>
            {
                var connectionCheckedOutEvent = events.OfType<ConnectionPoolCheckedOutConnectionEvent>().FirstOrDefault();
                var commandStartedEvent = events.OfType<CommandStartedEvent>().FirstOrDefault();

                if (commandStartedEvent.ConnectionId != null)
                {
                    serverSessionsList.Count.Should().Be(0);
                    commandStartedEvent.Command["lsid"].Should().Be(serverSession.Id);

                    return true;
                }
                else if (connectionCheckedOutEvent.ConnectionId != null)
                {
                    serverSessionsList.Single().Should().Be(serverSession);
                }

                return false;
            });

            collection.InsertOne(new BsonDocument("x", 1));
            await eventsTask.WithTimeout(1000);
        }

        // https://specifications.readthedocs.io/en/latest/sessions/tests/#20-drivers-do-not-gossip-clustertime-on-sdam-commands
        [Fact]
        public void Ensure_cluster_times_are_not_gossiped_on_SDAM_commands()
        {
            RequireServer.Check().ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            var eventCapturer = new EventCapturer()
                .Capture<ServerHeartbeatStartedEvent>()
                .Capture<ServerHeartbeatSucceededEvent>()
                .Capture<CommandStartedEvent>();

            using var c1 = DriverTestConfiguration.CreateMongoClient(
                settings =>
                {
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.DirectConnection = true;
                    settings.HeartbeatInterval = TimeSpan.FromMilliseconds(10);
                    if (settings.Servers.Count() > 1)
                    {
                        settings.Servers = settings.Servers.Take(1);
                    }
                });

            var pingCommand = new BsonDocument("ping", 1);
            var pingResult = c1.GetDatabase("admin").RunCommand<BsonDocument>(pingCommand);

            var clusterTime = pingResult["$clusterTime"];

            var c2 = DriverTestConfiguration.Client;
            c2.GetDatabase("test").GetCollection<BsonDocument>("test").InsertOne(new BsonDocument("advance", "$clusterTime"));

            eventCapturer.Clear();

            eventCapturer.WaitForOrThrowIfTimeout(
                capturedEvents =>
                {
                    return capturedEvents
                        .SkipWhile(e => e is not ServerHeartbeatStartedEvent)
                        .Any(e => e is ServerHeartbeatSucceededEvent);
                }, TimeSpan.FromSeconds(1), "Didn't get any server heartbeat pairs");

            c1.GetDatabase("admin").RunCommand<BsonDocument>(pingCommand);

            var commandStartedEvents = eventCapturer.Events.OfType<CommandStartedEvent>().ToArray();
            commandStartedEvents.Length.Should().Be(1);
            commandStartedEvents[0].CommandName.Should().Be("ping");
            commandStartedEvents[0].Command["$clusterTime"].Should().Be(clusterTime);
        }

        private sealed class MongocryptdContext : IDisposable
        {
            public IMongoClient MongoClient { get; }
            public EventCapturer EventCapturer { get; }
            public IMongoCollection<BsonDocument> MongocryptdCollection { get; }

            public MongocryptdContext(IMongoClient mongoClient, IMongoCollection<BsonDocument> mongocryptdCollection, EventCapturer eventCapturer)
            {
                MongoClient = mongoClient;
                EventCapturer = eventCapturer;
                MongocryptdCollection = mongocryptdCollection;
            }

            public void Dispose() => MongoClient.Dispose();
        }

        private MongocryptdContext GetMongocryptdContext()
        {
            var extraOptions = new Dictionary<string, object>()
            {
                { "cryptSharedLibPath", "non_existing_path_to_use_mongocryptd" }
            };

            var eventCapturer = new EventCapturer();
            eventCapturer.Capture<CommandStartedEvent>();

            var mongocryptdFactory = new MongocryptdFactory(extraOptions, false, eventCapturer);
            var client = mongocryptdFactory.CreateMongocryptdClient();
            mongocryptdFactory.SpawnMongocryptdProcessIfRequired();

            var collection = client.GetDatabase("db").GetCollection<BsonDocument>("coll");
            return new MongocryptdContext(client, collection, eventCapturer);
        }
    }
}
