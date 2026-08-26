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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Tests;
using Xunit;

namespace MongoDB.Driver.Core.Operations
{
    // CSHARP-6158. A collection typed as IMongoCollection<BsonValue> let an attacker-controlled BsonArray be
    // passed where a replacement document was expected, because BsonArray is-a BsonValue and nothing on the
    // replace paths validated the shape. The server reads an array update as an aggregation pipeline, so the
    // stages ran as update operators.
    //
    // FindOneAndReplace was fully exploitable: FindOneAndReplaceOperation builds { "update", _replacement } into
    // a plain command document with no element name validator anywhere. ReplaceOne and the client bulkWrite
    // happened to be saved by the ReplacementElementNameValidator they push at their sink, but only by accident
    // of when BsonWriter swaps in a child validator, so all three are now guarded up front instead.
    [Trait("Category", "Integration")]
    public class ReplacementPipelineInjectionIntegrationTests
    {
        private const string CollectionName = "csharp6158_replacement_injection";

        private static BsonArray AttackerPipeline() =>
            new BsonArray { new BsonDocument("$set", new BsonDocument("role", "admin")) };

        [Fact]
        public void FindOneAndReplace_should_reject_an_array_replacement()
        {
            RequireServer.Check();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "findAndModify");
            using var client = DriverTestConfiguration.CreateMongoClient(eventCapturer);
            var collection = GetSeededCollection(client);

            Action action = () => collection.FindOneAndReplace(new BsonDocument("_id", 1), AttackerPipeline());

            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();

            // before the fix the pipeline ran: "role" became "admin" while "name" survived, which a genuine
            // replacement could never do. Assert the document is untouched.
            var stored = collection.Find(new BsonDocument("_id", 1)).Single().AsBsonDocument;
            stored["role"].AsString.Should().Be("user");
            stored["name"].AsString.Should().Be("alice");
        }

        [Fact]
        public async Task FindOneAndReplaceAsync_should_reject_an_array_replacement()
        {
            RequireServer.Check();

            using var client = DriverTestConfiguration.CreateMongoClient(new EventCapturer());
            var collection = GetSeededCollection(client);

            // the async overload shares CreateFindOneAndReplaceOperation, but assert it explicitly
            var exception = await Record.ExceptionAsync(
                () => collection.FindOneAndReplaceAsync(new BsonDocument("_id", 1), AttackerPipeline()));

            exception.Should().BeOfType<ArgumentException>()
                .Which.ParamName.Should().Be("replacement");
        }

        [Fact]
        public void ReplaceOne_should_reject_an_array_replacement()
        {
            RequireServer.Check();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update");
            using var client = DriverTestConfiguration.CreateMongoClient(eventCapturer);
            var collection = GetSeededCollection(client);

            Action action = () => collection.ReplaceOne(new BsonDocument("_id", 1), AttackerPipeline());

            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();

            collection.Find(new BsonDocument("_id", 1)).Single().AsBsonDocument["role"].AsString.Should().Be("user");
        }

        [Fact]
        public void ReplaceOne_should_reject_a_scalar_replacement()
        {
            RequireServer.Check();

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update");
            using var client = DriverTestConfiguration.CreateMongoClient(eventCapturer);
            var collection = GetSeededCollection(client);

            // previously this round-tripped and came back as "Update argument must be either an object or an array"
            Action action = () => collection.ReplaceOne(new BsonDocument("_id", 1), BsonValue.Create(42));

            action.ShouldThrow<ArgumentException>()
                .And.ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Fact]
        public void ReplaceOne_should_still_accept_a_document_replacement()
        {
            RequireServer.Check();

            using var client = DriverTestConfiguration.CreateMongoClient(new EventCapturer());
            var collection = GetSeededCollection(client);

            collection.ReplaceOne(new BsonDocument("_id", 1), new BsonDocument { { "_id", 1 }, { "name", "bob" } });

            var stored = collection.Find(new BsonDocument("_id", 1)).Single().AsBsonDocument;
            stored["name"].AsString.Should().Be("bob");
            stored.Contains("role").Should().BeFalse(); // a real replacement drops the other fields
        }

        private static IMongoCollection<BsonValue> GetSeededCollection(IMongoClient client)
        {
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            database.DropCollection(CollectionName);
            var collection = database.GetCollection<BsonValue>(CollectionName);
            collection.InsertOne(new BsonDocument { { "_id", 1 }, { "name", "alice" }, { "role", "user" } });
            return collection;
        }
    }
}
