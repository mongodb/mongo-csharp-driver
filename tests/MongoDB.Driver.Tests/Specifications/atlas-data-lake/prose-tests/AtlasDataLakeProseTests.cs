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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.atlas_data_lake.prose_tests
{
    [Trait("Category", "AtlasDataLake")]
    public class AtlasDataLakeProseTests
    {
        [SkippableFact]
        public void Driver_should_connect_to_AtlasDataLake_without_authentication()
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_DATA_LAKE_TESTS_ENABLED");

            using (var client = DriverTestConfiguration.CreateDisposableClient())
            {
                client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            }
        }

        [SkippableFact]
        public void Driver_should_connect_to_AtlasDataLake_with_SCRAM_SHA_1()
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_DATA_LAKE_TESTS_ENABLED");
            RequireServer.Check().Supports(Feature.ScramSha1Authentication);

            var connectionString = CoreTestConfiguration.ConnectionString;
            var username = connectionString.Username;
            var password = connectionString.Password;
            var source = connectionString.AuthSource;

            var settings = DriverTestConfiguration.Client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(mechanism: "SCRAM-SHA-1", source, username, password);

            using (var client = DriverTestConfiguration.CreateDisposableClient(settings))
            {
                client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            }
        }

        [SkippableFact]
        public void Driver_should_connect_to_AtlasDataLake_with_SCRAM_SHA_256()
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_DATA_LAKE_TESTS_ENABLED");
            RequireServer.Check().Supports(Feature.ScramSha256Authentication);

            var connectionString = CoreTestConfiguration.ConnectionString;
            var username = connectionString.Username;
            var password = connectionString.Password;
            var source = connectionString.AuthSource;

            var settings = DriverTestConfiguration.Client.Settings.Clone();
            settings.Credential = MongoCredential.FromComponents(mechanism: "SCRAM-SHA-256", source, username, password);

            using (var client = DriverTestConfiguration.CreateDisposableClient(settings))
            {
                client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
            }
        }

        [SkippableFact]
        public void KillCursors_should_return_expected_result()
        {
            RequireEnvironment.Check().EnvironmentVariable("ATLAS_DATA_LAKE_TESTS_ENABLED");
            RequireServer.Check().Supports(Feature.KillCursorsCommand);

            var databaseName = "test";
            var collectionName = "driverdata";

            var eventCapturer = new EventCapturer()
                .Capture<CommandStartedEvent>(x => "killCursors" == x.CommandName)
                .Capture<CommandSucceededEvent>(x => new[] { "killCursors", "find" }.Contains(x.CommandName));

            using (var client = DriverTestConfiguration.CreateDisposableClient(eventCapturer))
            {
                var cursor = client
                    .GetDatabase(databaseName)
                    .GetCollection<BsonDocument>(collectionName)
                    .Find(new BsonDocument(), new FindOptions { BatchSize = 2 })
                    .ToCursor();

                var findCommandSucceededEvent = eventCapturer.Events.OfType<CommandSucceededEvent>().First(x => x.CommandName == "find");
                var findCommandResult = findCommandSucceededEvent.Reply;
                var cursorId = findCommandResult["cursor"]["id"].AsInt64;
                var cursorNamespace = CollectionNamespace.FromFullName(findCommandResult["cursor"]["ns"].AsString);

                cursor.Dispose();

                var killCursorsCommandStartedEvent = eventCapturer.Events.OfType<CommandStartedEvent>().First(x => x.CommandName == "killCursors");
                var killCursorsCommandSucceededEvent = eventCapturer.Events.OfType<CommandSucceededEvent>().First(x => x.CommandName == "killCursors");
                var killCursorsStartedCommand = killCursorsCommandStartedEvent.Command;

                cursorNamespace.DatabaseNamespace.DatabaseName.Should().Be(killCursorsCommandStartedEvent.DatabaseNamespace.DatabaseName);
                cursorNamespace.CollectionName.Should().Be(killCursorsStartedCommand["killCursors"].AsString);
                cursorId.Should().Be(killCursorsStartedCommand["cursors"][0].AsInt64);
                cursorId.Should().Be(killCursorsCommandSucceededEvent.Reply["cursorsKilled"][0].AsInt64);
            }
        }
    }
}
