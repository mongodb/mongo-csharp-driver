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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5519Tests : LinqIntegrationTest<CSharp5519Tests.ClassFixture>
{
    public CSharp5519Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [ParameterAttributeData]
    public void Filter_Array_Any_item_equals_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };
        var find = withNestedAsQueryable ?
            collection.Find(t => array.AsQueryable().Any(item => item == t.Id)) :
            collection.Find(t => array.Any(item => item == t.Id));

        var filter = TranslateFindFilter(collection, find);

        filter.Should().Be("{ _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Filter_Array_Any_field_equals_item_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };
        var find = withNestedAsQueryable ?
            collection.Find(t => array.AsQueryable().Any(item => t.Id == item)) :
            collection.Find(t => array.Any(item => t.Id == item));

        var filter = TranslateFindFilter(collection, find);

        filter.Should().Be("{ _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Filter_Array_Contains_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };
        var find = withNestedAsQueryable ?
            collection.Find(t => array.AsQueryable().Contains(t.Id)) :
            collection.Find(t => array.Contains(t.Id));

        var filter = TranslateFindFilter(collection, find);

        filter.Should().Be("{ _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_Array_Any_item_equals_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Where(t => array.AsQueryable().Any(item => item == t.Id)) :
            collection.AsQueryable().Where(t => array.Any(item => item == t.Id));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_Array_Any_field_equals_item_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Where(t => array.AsQueryable().Any(item => t.Id == item)) :
            collection.AsQueryable().Where(t => array.Any(item => t.Id == item));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Where_Array_Contains_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Where(t => array.AsQueryable().Contains(t.Id)) :
            collection.AsQueryable().Where(t => array.Contains(t.Id));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { _id : { $in : [{ $oid : '0102030405060708090a0b0c' }] } } }");
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_Array_Any_item_equals_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Select(t => array.AsQueryable().Any(item => item == t.Id)) :
            collection.AsQueryable().Select(t => array.Any(item => item == t.Id));

        var exception = Record.Exception(() => Translate(collection, queryable)); // TODO: support?
        exception.Message.Should().Contain("Expression not supported: (id == t.Id) because the two arguments are serialized differently");
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_Array_Any_field_equals_item_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Select(t => array.AsQueryable().Any(item => t.Id == item)) :
            collection.AsQueryable().Select(t => array.Any(item => t.Id == item));

        var exception = Record.Exception(() => Translate(collection, queryable)); // TODO: support?
        exception.Message.Should().Contain("Expression not supported: (id == t.Id) because the two arguments are serialized differently");
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_Array_Contains_field_should_work(
        [Values(false, true)] bool withNestedAsQueryable)
    {
        var collection = Fixture.Collection;
        var array = new string[] { "0102030405060708090a0b0c" };

        var queryable = withNestedAsQueryable ?
            collection.AsQueryable().Select(t => array.AsQueryable().Contains(t.Id)) :
            collection.AsQueryable().Select(t => array.Contains(t.Id));

        var exception = Record.Exception(() => Translate(collection, queryable)); // TODO: support?
        exception.Message.Should().Contain("Expression not supported: value(System.String[]).Contains(t.Id) because the array items and the value are serialized differently");
    }

    public class Test
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<Test>
    {
        protected override IEnumerable<Test> InitialData => null;
    }
}
