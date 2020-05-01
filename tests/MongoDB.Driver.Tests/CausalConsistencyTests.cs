/* Copyright 2017-present MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class CausalConsistencyTests
    {
        [SkippableFact]
        public void OperationTime_should_have_no_value_on_a_newly_created_ClientSession()
        {
            RequireServer.Check().SupportsCausalConsistency();

            using (var client = GetClient(new EventCapturer()))
            using (var session = client.StartSession())
            {
                session.OperationTime.Should().BeNull();
            }
        }

        [SkippableFact]
        public void AfterClusterTime_should_be_empty_on_the_first_operation()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer().Capture<CommandStartedEvent>(x => x.CommandName == "count");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command.GetValue("readConcern", null).Should().BeNull();
            }
        }

        [SkippableFact]
        public void Session_OperationTime_should_get_updated_after_an_operation()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count")
                .Capture<CommandSucceededEvent>(x => x.CommandName == "count");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command.GetValue("readConcern", null).Should().BeNull();

                var commandSucceededEvent = (CommandSucceededEvent)events.Next();
                session.OperationTime.Should().Be(commandSucceededEvent.Reply.GetValue("operationTime"));
            }
        }

        [SkippableFact]
        public void AfterClusterTime_should_be_sent_after_the_first_read_operation()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count")
                .Capture<CommandSucceededEvent>(x => x.CommandName == "find");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .FindSync(session, FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument, BsonDocument> { Limit = 1 });

                var commandSucceededEvent = (CommandSucceededEvent)events.Next();
                session.OperationTime.Should().Be(commandSucceededEvent.Reply.GetValue("operationTime"));
                var operationTime = session.OperationTime;

#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command["readConcern"]["afterClusterTime"].AsBsonTimestamp.Should().Be(operationTime);
            }
        }

        [SkippableFact]
        public void AfterClusterTime_should_be_sent_after_the_first_write_operation()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count")
                .Capture<CommandSucceededEvent>(x => x.CommandName == "insert");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .InsertOne(session, new BsonDocument("x", 1));

                var commandSucceededEvent = (CommandSucceededEvent)events.Next();
                session.OperationTime.Should().Be(commandSucceededEvent.Reply.GetValue("operationTime"));
                var operationTime = session.OperationTime;

#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command["readConcern"]["afterClusterTime"].AsBsonTimestamp.Should().Be(operationTime);
            }
        }

        [SkippableFact]
        public void AfterClusterTime_should_not_be_sent_when_the_session_is_not_causally_consistent()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count");
            using (var client = GetClient(events))
            using (var session = client.StartSession(new ClientSessionOptions { CausalConsistency = false }))
            {
#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command.Contains("readConcern").Should().BeFalse();
            }
        }

        [SkippableFact]
        public void ReadConcern_should_not_include_level_when_using_the_server_default()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .InsertOne(session, new BsonDocument("x", 1));

#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .WithReadConcern(ReadConcern.Default)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command["readConcern"].AsBsonDocument.Contains("level").Should().BeFalse();
            }
        }

        [SkippableFact]
        public void ReadConcern_should_include_level_when_not_using_the_server_default()
        {
            RequireServer.Check().SupportsCausalConsistency();

            var events = new EventCapturer()
                .Capture<CommandStartedEvent>(x => x.CommandName == "count");
            using (var client = GetClient(events))
            using (var session = client.StartSession())
            {
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .InsertOne(session, new BsonDocument("x", 1));

#pragma warning disable 618
                client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .WithReadConcern(ReadConcern.Majority)
                    .Count(session, FilterDefinition<BsonDocument>.Empty);
#pragma warning restore

                var commandStartedEvent = (CommandStartedEvent)events.Next();
                commandStartedEvent.Command["readConcern"].AsBsonDocument.Contains("level").Should().BeTrue();
                commandStartedEvent.Command["readConcern"].AsBsonDocument.Contains("afterClusterTime").Should().BeTrue();
            }
        }

        private DisposableMongoClient GetClient(EventCapturer capturer)
        {
            return DriverTestConfiguration.CreateDisposableClient(capturer);
        }
    }
}
