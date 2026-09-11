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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Integration;

public class DollarPrefixedValuesInConstantsTests : LinqIntegrationTest<DollarPrefixedValuesInConstantsTests.ClassFixture>
{
    public DollarPrefixedValuesInConstantsTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Projected_array_constant_with_dollar_prefixed_string_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new[] { "$Secret" };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : { $literal : ['$Secret'] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values.Single()).Should().Equal("$Secret", "$Secret");
    }

    [Fact]
    public void Projected_nested_array_constant_with_dollar_prefixed_string_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new List<List<string>> { new() { "$Secret" } };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : { $literal : [['$Secret']] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values.Single().Single()).Should().Equal("$Secret", "$Secret");
    }

    [Fact]
    public void Projected_dictionary_constant_with_dollar_prefixed_key_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new Dictionary<string, string> { { "$concat", "abc" } };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : { $literal : { $concat : 'abc' } } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values["$concat"]).Should().Equal("abc", "abc");
    }

    [Fact]
    public void Projected_dictionary_constant_with_dollar_prefixed_value_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new Dictionary<string, string> { { "a", "$Secret" } };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : { $literal : { a : '$Secret' } } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values["a"]).Should().Equal("$Secret", "$Secret");
    }

    [Fact]
    public void Dictionary_constant_represented_as_array_of_documents_with_dollar_prefixed_value_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new Dictionary<string, string> { { "a", "$Secret" } };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Match = x.Values == values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Match : { $eq : ['$Values', { $literal : [{ k : 'a', v : '$Secret' }] }] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Match).Should().Equal(true, false);
    }

    [Fact]
    public void Projected_array_constant_of_documents_with_dollar_prefixed_value_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new[] { new Dictionary<string, string> { { "a", "$Secret" } } };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : { $literal : [{ a : '$Secret' }] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values.Single()["a"]).Should().Equal("$Secret", "$Secret");
    }

    [Fact]
    public void Contains_with_dollar_prefixed_string_in_array_constant_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new[] { "$Secret" };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Match = values.Contains(x.Name) });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Match : { $in : ['$Name', { $literal : ['$Secret'] }] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Match).Should().Equal(false, false);
    }

    [Fact]
    public void Where_using_expr_with_dollar_prefixed_string_in_array_constant_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new[] { "$Secret" };

        var queryable = collection.AsQueryable().Where(x => values.Contains(x.Name) == values.Contains(x.Secret));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { $expr : { $eq : [{ $in : ['$Name', { $literal : ['$Secret'] }] }, { $in : ['$Secret', { $literal : ['$Secret'] }] }] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Constants_without_dollar_prefixed_values_should_not_be_quoted()
    {
        var collection = Fixture.Collection;
        var values = new[] { "abc" };

        var queryable = collection.AsQueryable().Select(x => new { x.Id, Values = values, Match = values.Contains(x.Name) });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _id : '$_id', Values : ['abc'], Match : { $in : ['$Name', ['abc']] } } }");

        var results = queryable.ToList();
        results.Select(x => x.Values.Single()).Should().Equal("abc", "abc");
    }

    public class C
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Secret { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<string, string> Values { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Name = "a", Secret = "secret1", Values = new() { { "a", "$Secret" } } },
            new C { Id = 2, Name = "b", Secret = "secret2", Values = new() { { "a", "secret2" } } }
        ];
    }
}
