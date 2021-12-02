/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.Legacy.Tests
{
    public class WireProtocolUsageTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Insert(
            [Values(true, false)] bool isLegacy,
            [Values(true, false)] bool isAcknowledged)
        {
            Console.Write(isAcknowledged);
            WithConfiguredServer((client) =>
            {
                Insert(client, isAcknowledged, isLegacy);
            },
            (eventCapturer) =>
            {
                var @event = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
                if (isAcknowledged)
                {
                    @event.WireProtocol.Should().Be("Command");
                }
                else
                {
                    if (isLegacy)
                    {
                        // We use InsertOpcodeOperation explicitly that uses emulator(based on Command) for Acknowledged
                        @event.WireProtocol.Should().Be("Insert");
                    }
                    else
                    {
                        @event.WireProtocol.Should().Be("Command");
                    }
                }
                eventCapturer.Any().Should().BeFalse();
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Update(
            [Values(true, false)] bool isLegacy,
            [Values(true, false)] bool isAcknowledged)
        {
            Console.Write(isAcknowledged);
            WithConfiguredServer((client) =>
            {
                Update(client, isAcknowledged, isLegacy);
            },
            (eventCapturer) =>
            {
                var @event = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
                if (isAcknowledged)
                {
                    @event.WireProtocol.Should().Be("Command");
                }
                else
                {
                    if (isLegacy)
                    {
                        // We use UpdateOpcodeOperation explicitly that uses emulator(based on Command) for Acknowledged
                        @event.WireProtocol.Should().Be("Update");
                    }
                    else
                    {
                        @event.WireProtocol.Should().Be("Command");
                    }
                }
                eventCapturer.Any().Should().BeFalse();
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Delete(
            [Values(true, false)] bool isLegacy,
            [Values(true, false)] bool isAcknowledged)
        {
            Console.Write(isAcknowledged);
            WithConfiguredServer((client) =>
            {
                Delete(client, isAcknowledged, isLegacy);
            },
            (eventCapturer) =>
            {
                var @event = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
                if (isAcknowledged)
                {
                    @event.WireProtocol.Should().Be("Command");
                }
                else
                {
                    if (isLegacy)
                    {
                        // We use DeleteOpcodeOperation explicitly that uses emulator(based on Command) for Acknowledged
                        @event.WireProtocol.Should().Be("Delete");
                    }
                    else
                    {
                        @event.WireProtocol.Should().Be("Command");
                    }
                }
                eventCapturer.Any().Should().BeFalse();
            });
        }

        [Theory]
        [ParameterAttributeData]
        public void Find(
            [Values(true, false)] bool isLegacy,
            [Values(true, false)] bool explain,
            [Values(true, false)] bool isAcknowledged)
        {
            Console.Write(isAcknowledged);
            WithConfiguredServer((client) =>
            {
                Read(client, isAcknowledged, isLegacy, explain);
            },
            (eventCapturer) =>
            {
                var @event = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
                if (explain)
                {
                    @event.WireProtocol.Should().Be("Query");
                }
                else 
                {
                    // We always use FindOperation explicitly (as in the Core/High)
                    @event.WireProtocol.Should().Be("Command");
                }
                eventCapturer.Any().Should().BeFalse();
            });
        }

        private void WithConfiguredServer(Action<MongoClient> testCase, Action<EventCapturer> assert)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName is not ("hello" or "buildInfo" or OppressiveLanguageConstants.LegacyHelloCommandName or "getLastError"));
            using (var client = DriverTestConfiguration.CreateDisposableClient(configurator => configurator.Subscribe(eventCapturer)))
            {
                testCase(client.Wrapped as MongoClient);
                assert(eventCapturer);
            }
        }

        private void Insert(MongoClient client, bool isAcknowledged, bool isLegacy)
        {
            if (isLegacy)
            {
                var coll = GetLegacyCollection(client, isAcknowledged);
                coll.Insert(new BsonDocument());
            }
            else
            {
                var coll = GetCollection(client, isAcknowledged);
                coll.InsertOne(new BsonDocument());
            }
        }

        private void Update(MongoClient client, bool isAcknowledged, bool isLegacy)
        {
            if (isLegacy)
            {
                var coll = GetLegacyCollection(client, isAcknowledged);
                coll.Update(Query.NE("test", 1), new UpdateBuilder().Inc("a", 1));
            }
            else
            {
                var updateDocument = Builders<BsonDocument>.Update.Inc("a", 1);
                var coll = GetCollection(client, isAcknowledged);
                coll.UpdateOne("{}", updateDocument);
            }
        }

        private void Delete(MongoClient client, bool isAcknowledged, bool isLegacy)
        {
            if (isLegacy)
            {
                var coll = GetLegacyCollection(client, isAcknowledged);
                coll.Remove(Query.NE("test", 1));
            }
            else
            {
                var coll = GetCollection(client, isAcknowledged);
                coll.DeleteOne("{}");
            }
        }

        private void Read(MongoClient client, bool isAcknowledged, bool isLegacy, bool explain)
        {
            if (isLegacy)
            {
                var coll = GetLegacyCollection(client, isAcknowledged);
                var finder = coll.Find(Query.NE("test", 1));
                if (explain)
                {
                    _ = finder.Explain();
                }
                else
                {
                    _ = finder.ToList();
                }
            }
            else
            {
                var coll = GetCollection(client, isAcknowledged);
                var findOptions = new FindOptions();
                if (explain)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    findOptions.Modifiers = new BsonDocument("$explain", 1);
#pragma warning restore CS0618 // Type or member is obsolete
                }
                var finder = coll.Find("{}", findOptions);
                _ = finder.ToList();
            }
        }

        private MongoCollection<BsonDocument> GetLegacyCollection(MongoClient client, bool isAcknowledged)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return client.GetServer().GetDatabase("db").GetCollection<BsonDocument>("coll").WithWriteConcern(writeConcern: isAcknowledged ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private IMongoCollection<BsonDocument> GetCollection(MongoClient client, bool isAcknowledged)
        {
            return client.GetDatabase("db").GetCollection<BsonDocument>("coll").WithWriteConcern(writeConcern: isAcknowledged ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged);
        }
    }
}
