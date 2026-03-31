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

public class MqlDeserializeEJsonTests : LinqIntegrationTest<MqlDeserializeEJsonTests.ClassFixture>
{
    public MqlDeserializeEJsonTests(ClassFixture fixture)
        : base(fixture, server => server.Supports(Feature.DeserializeEJsonOperator))
    {
    }

    [Fact]
    public void DeserializeEJson_should_convert_numberLong_wrapper_to_native_long()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 1)
            .Select(d => Mql.DeserializeEJson<BsonDocument, BsonValue>(d.EJsonValue, null));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 1 } }",
            "{ '$project' : { '_v' : { '$deserializeEJSON' : { 'input' : '$EJsonValue' } }, '_id' : 0 } }");

        // { "$numberLong": "123" } should become NumberLong(123)
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create(123L));
    }

    [Fact]
    public void DeserializeEJson_should_convert_numberInt_wrapper_to_native_int()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 2)
            .Select(d => Mql.DeserializeEJson<BsonValue, BsonValue>(d.Value, null));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 2 } }",
            "{ '$project' : { '_v' : { '$deserializeEJSON' : { 'input' : '$Value' } }, '_id' : 0 } }");

        // { "$numberInt": "42" } should become Int32(42)
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create(42));
    }

    [Fact]
    public void DeserializeEJson_should_pass_through_plain_values()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 3)
            .Select(d => Mql.DeserializeEJson<BsonValue, BsonValue>(d.Value, null));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 3 } }",
            "{ '$project' : { '_v' : { '$deserializeEJSON' : { 'input' : '$Value' } }, '_id' : 0 } }");

        // A plain string without EJSON wrappers should pass through unchanged
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create("hello"));
    }

    [Fact]
    public void DeserializeEJson_should_convert_document_with_wrapped_fields()
    {
        var collection = Fixture.Collection;
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 4)
            .Select(d => Mql.DeserializeEJson<BsonDocument, BsonDocument>(d.EJsonValue, null));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 4 } }",
            "{ '$project' : { '_v' : { '$deserializeEJSON' : { 'input' : '$EJsonValue' } }, '_id' : 0 } }");

        // { "a": { "$numberInt": "1" }, "b": { "$numberLong": "2" } }
        // should become { a: 1, b: NumberLong(2) }
        var result = queryable.Single();
        result["a"].Should().Be(BsonValue.Create(1));
        result["b"].Should().Be(BsonValue.Create(2L));
    }

    [Fact]
    public void DeserializeEJson_with_onError_should_return_fallback_on_invalid_input()
    {
        var collection = Fixture.Collection;
        // Id == 5 has invalid EJSON: { "$numberLong": "not_a_number" }
        var queryable = collection.AsQueryable()
            .Where(d => d.Id == 5)
            .Select(d => Mql.DeserializeEJson<BsonDocument, BsonValue>(d.EJsonValue, new DeserializeEJsonOptions<BsonValue> { OnError = "fallback" }));

        var renderedStages = Translate(collection, queryable);
        AssertStages(
            renderedStages,
            "{ '$match' : { '_id' : 5 } }",
            "{ '$project' : { '_v' : { '$deserializeEJSON' : { 'input' : '$EJsonValue', 'onError' : 'fallback' } }, '_id' : 0 } }");

        // Invalid EJSON should trigger onError and return "fallback"
        var result = queryable.Single();
        result.Should().Be(BsonValue.Create("fallback"));
    }

    public class C
    {
        public int Id { get; set; }
        public BsonDocument EJsonValue { get; set; }
        public BsonValue Value { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new() { Id = 1, EJsonValue = new BsonDocument("$numberLong", "123") },
            new() { Id = 2, Value = new BsonDocument("$numberInt", "42") },
            new() { Id = 3, Value = BsonValue.Create("hello") },
            new() { Id = 4, EJsonValue = new BsonDocument { { "a", new BsonDocument("$numberInt", "1") }, { "b", new BsonDocument("$numberLong", "2") } } },
            new() { Id = 5, EJsonValue = new BsonDocument("$numberLong", "not_a_number") },
        ];
    }
}
