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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.TestHelpers;
using MongoDB.Driver.Tests;
using MongoDB.TestHelpers.XunitExtensions;
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
    public class ReplacementPipelineInjectionIntegrationTests : IntegrationTest<ReplacementPipelineInjectionIntegrationTests.ClassFixture>
    {
        public ReplacementPipelineInjectionIntegrationTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public async Task FindOneAndReplace_should_reject_array_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "findAndModify");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = new BsonArray { new BsonDocument("$set", new BsonDocument("role", "admin")) };

            using var client = DriverTestConfiguration.CreateMongoClient(eventCapturer);
            var collectionNamespace = Fixture.Collection.CollectionNamespace;
            var collection = client.GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName)
                .GetCollection<BsonValue>(collectionNamespace.CollectionName);
            var exception = async
                ? await Record.ExceptionAsync(() => collection.FindOneAndReplaceAsync(filter, replacement))
                : Record.Exception(() => collection.FindOneAndReplace(filter, replacement));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReplaceOne_should_reject_array_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = new BsonArray { new BsonDocument("$set", new BsonDocument("role", "admin")) };

            var exception = async
                ? await Record.ExceptionAsync(() => Fixture.Collection.ReplaceOneAsync(filter, replacement))
                : Record.Exception(() => Fixture.Collection.ReplaceOne(filter, replacement));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task FindOneAndReplace_should_reject_scalar_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "findAndModify");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = BsonValue.Create(42);

            var exception = async
                ? await Record.ExceptionAsync(() => Fixture.Collection.FindOneAndReplaceAsync(filter, replacement))
                : Record.Exception(() => Fixture.Collection.FindOneAndReplace(filter, replacement));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReplaceOne_should_reject_scalar_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = BsonValue.Create(42);

            var exception = async
                ? await Record.ExceptionAsync(() => Fixture.Collection.ReplaceOneAsync(filter, replacement))
                : Record.Exception(() => Fixture.Collection.ReplaceOne(filter, replacement));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task FindOneAndReplace_should_reject_operator_like_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "findAndModify");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = BsonDocument.Parse("{ $set : { role : 'admin' } }");

            var exception = async
                ? await Record.ExceptionAsync(() => Fixture.Collection.FindOneAndReplaceAsync(filter, replacement))
                : Record.Exception(() => Fixture.Collection.FindOneAndReplace(filter, replacement));

            exception.Should().BeOfType<ArgumentException>().Subject
                .ParamName.Should().Be("replacement");
            eventCapturer.Events.Should().BeEmpty();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task ReplaceOne_should_reject_operator_like_replacement([Values(true, false)] bool async)
        {
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "update");
            var filter = BsonDocument.Parse("{ _id : 1 }");
            var replacement = BsonDocument.Parse("{ $set : { role : 'admin' } }");

            var exception = async
                ? await Record.ExceptionAsync(() => Fixture.Collection.ReplaceOneAsync(filter, replacement))
                : Record.Exception(() => Fixture.Collection.ReplaceOne(filter, replacement));

            exception.Should().BeOfType<BsonSerializationException>().Subject
                .Message.Should().Be("Element name '$set' is not valid'.");
            eventCapturer.Events.Should().BeEmpty();
        }

        public sealed class ClassFixture : MongoCollectionFixture<BsonValue, BsonDocument>
        {
            protected override IEnumerable<BsonDocument> InitialData =>
                new[] { new BsonDocument { { "_id", 1 }, { "name", "alice" }, { "role", "user" } } };
        }
    }
}
