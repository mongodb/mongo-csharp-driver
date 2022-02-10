/* Copyright 2021-present MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.sessions
{
    [Trait("Category", "Serverless")]
    public class SessionsProseTests
    {
        [SkippableFact]
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

        [SkippableFact]
        public async Task Ensure_server_session_are_allocated_only_on_connection_checkout()
        {
            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolCheckedOutConnectionEvent>()
               .Capture<CommandStartedEvent>();

            using var client = DriverTestConfiguration.CreateDisposableClient(
                (MongoClientSettings settings) =>
                {
                    settings.MaxConnectionPoolSize = 1;
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                },
                logger: null);

            var database = client.GetDatabase("test");

            var collection = database.GetCollection<BsonDocument>("inventory");
            database.DropCollection("inventory");

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
            await TasksUtils.WithTimeout(eventsTask, 1000);
        }
    }
}
