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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "Integration")]
    public class ReadConcernIntegrationTests : LoggableTestClass
    {
        public ReadConcernIntegrationTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Find_with_snapshot_session_and_non_default_collection_readConcern_should_not_produce_duplicate_readConcern()
        {
            RequireServer.Check()
                .Supports(Feature.SnapshotReads)
                .ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(e => e.CommandName == "find");

            using var client = DriverTestConfiguration.CreateMongoClient(
                settings =>
                {
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.LoggingSettings = LoggingSettings;
                });

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database
                .GetCollection<BsonDocument>("snapshot_readconcern_test")
                .WithReadConcern(ReadConcern.Local);

            database.DropCollection("snapshot_readconcern_test");
            collection.WithReadConcern(ReadConcern.Default).InsertOne(new BsonDocument("x", 1));

            var sessionOptions = new ClientSessionOptions { Snapshot = true };
            using var session = client.StartSession(sessionOptions);

            var result = collection.Find(session, FilterDefinition<BsonDocument>.Empty).ToList();

            result.Should().HaveCount(1);

            var findEvent = eventCapturer.Events.OfType<CommandStartedEvent>().First();
            findEvent.Command["readConcern"]["level"].AsString.Should().Be("snapshot");
        }
    }
}
