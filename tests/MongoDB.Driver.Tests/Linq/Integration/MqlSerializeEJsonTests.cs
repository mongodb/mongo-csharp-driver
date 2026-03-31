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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class MqlSerializeEJsonTests : LinqIntegrationTest<MqlSerializeEJsonTests.ClassFixture>
{
    public MqlSerializeEJsonTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.SerializeEJsonOperator))
    {
    }

    [Fact]
    public void SerializeEJson_with_no_options_should_use_relaxed_format()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.SerializeEJson<int, BsonValue>(d.IntValue, null));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 1 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$IntValue' } }, '_id' : 0 } }");

        // Default is relaxed: int stays as int (no wrapper)
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create(42));
    }

    [Fact]
    public void SerializeEJson_with_relaxed_false_should_produce_canonical_format()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.SerializeEJson<int, BsonDocument>(d.IntValue, new SerializeEJsonOptions<BsonDocument> { Relaxed = false }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 1 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$IntValue', 'relaxed' : false } }, '_id' : 0 } }");

        // Canonical: int gets wrapped as { "$numberInt": "42" }
        // Can't use BsonDocument.Parse here because it interprets EJSON wrappers
        var result = queryable.Single();
        result.Should().Be(new BsonDocument("$numberInt", "42"));
    }

    [Fact]
    public void SerializeEJson_with_relaxed_true()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.SerializeEJson<int, BsonValue>(d.IntValue, new SerializeEJsonOptions<BsonValue> { Relaxed = true }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 1 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$IntValue', 'relaxed' : true } }, '_id' : 0 } }");

        // Relaxed: int stays as int
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create(42));
    }

    [Fact]
    public void SerializeEJson_with_document_input_canonical()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 2)
            .Select(d => Mql.SerializeEJson<BsonDocument, BsonDocument>(d.Document, new SerializeEJsonOptions<BsonDocument> { Relaxed = false }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 2 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$Document', 'relaxed' : false } }, '_id' : 0 } }");

        // Document { a: 1 } in canonical should become { a: { "$numberInt": "1" } }
        // Can't use BsonDocument.Parse here because it interprets EJSON wrappers
        var result = queryable.Single();
        result.Should().Be(new BsonDocument("a", new BsonDocument("$numberInt", "1")));
    }

    [Fact]
    public void SerializeEJson_with_long_value_canonical()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 3)
            .Select(d => Mql.SerializeEJson<long, BsonDocument>(d.LongValue, new SerializeEJsonOptions<BsonDocument> { Relaxed = false }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 3 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$LongValue', 'relaxed' : false } }, '_id' : 0 } }");

        // Can't use BsonDocument.Parse here because it interprets EJSON wrappers
        var result = queryable.Single();
        result.Should().Be(new BsonDocument("$numberLong", "100"));
    }

    // $serializeEJSON only errors on BSON depth/size limit violations, which are impractical to
    // trigger in a test. This test verifies the server accepts the onError option without errors.
    [Fact]
    public void SerializeEJson_with_onError_should_return_serialized_value_when_no_error()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.SerializeEJson<int, BsonValue>(d.IntValue, new SerializeEJsonOptions<BsonValue> { Relaxed = false, OnError = "fallback" }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 1 } }",
            "{ '$project' : { '_v' : { '$serializeEJSON' : { 'input' : '$IntValue', 'relaxed' : false, 'onError' : 'fallback' } }, '_id' : 0 } }");

        var result = queryable.Single();
        result.Should().Be(new BsonDocument("$numberInt", "42"));
    }

    public class C
    {
        public int Id { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public BsonDocument Document { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, IntValue = 42 },
            new() { Id = 2, Document = new BsonDocument("a", 1) },
            new() { Id = 3, LongValue = 100L },
        ];
    }
}
