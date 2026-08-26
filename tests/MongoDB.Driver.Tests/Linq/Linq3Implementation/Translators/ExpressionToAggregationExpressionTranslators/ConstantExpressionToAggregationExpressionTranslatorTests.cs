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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

public class ConstantExpressionToAggregationExpressionTranslatorTests : LinqIntegrationTest<ConstantExpressionToAggregationExpressionTranslatorTests.ClassFixture>
{
    public ConstantExpressionToAggregationExpressionTranslatorTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_string_that_looks_like_a_field_path_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = "$Ssn";

        var queryable = collection.AsQueryable()
            .GroupBy(x => key)
            .Select(g => g.Key);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { $literal : '$Ssn' } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Be("$Ssn");
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_document_containing_a_field_path_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = "$Ssn";

        var queryable = collection.AsQueryable()
            .GroupBy(x => new { A = key })
            .Select(g => g.Key);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { $literal : { A : '$Ssn' } } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.A.Should().Be("$Ssn");
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_document_containing_an_operator_name_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = new Dictionary<string, string> { { "$toUpper", "$Ssn" } };

        var queryable = collection.AsQueryable()
            .GroupBy(x => key)
            .Select(g => g.Key);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { $literal : { '$toUpper' : '$Ssn' } } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(key);
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_array_containing_a_field_path_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = new[] { "$Ssn" };

        var queryable = collection.AsQueryable()
            .GroupBy(x => key)
            .Select(g => g.Key);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { $literal : ['$Ssn'] } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("$Ssn");
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_document_containing_no_field_paths_should_not_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = "abc";

        var queryable = collection.AsQueryable()
            .GroupBy(x => new { A = key })
            .Select(g => g.Key);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { A : 'abc' } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.A.Should().Be("abc");
    }

    [Fact]
    public void GroupBy_element_that_is_a_constant_array_containing_a_field_path_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var element = new[] { "$Ssn" };

        var queryable = collection.AsQueryable()
            .GroupBy(x => 1, x => element)
            .Select(g => g.First());

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : 1, __agg0 : { $first : { $literal : ['$Ssn'] } } } }",
            "{ $project : { _v : '$__agg0', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("$Ssn");
    }

    [Fact]
    public void Select_a_constant_array_containing_a_field_path_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var value = new[] { "$Ssn" };

        var queryable = collection.AsQueryable()
            .Select(x => value);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { _v : { $literal : ['$Ssn'] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal("$Ssn");
    }

    [Fact]
    public void Select_a_computed_document_with_a_constant_document_containing_an_operator_name_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var value = new Dictionary<string, string> { { "$toUpper", "$Ssn" } };

        var queryable = collection.AsQueryable()
            .Select(x => new { X = x.X, V = value });

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { X : '$X', V : { $literal : { '$toUpper' : '$Ssn' } }, _id : 0 } }");

        var result = queryable.Single();
        result.X.Should().Be(42);
        result.V.Should().Equal(value);
    }

    [Fact]
    public void Where_with_a_constant_array_containing_a_field_path_should_not_be_quoted_in_a_match_filter()
    {
        var collection = Fixture.Collection;
        var value = new[] { "$Ssn" };

        var queryable = collection.AsQueryable()
            .Where(x => value.Contains(x.Ssn));

        // a $match filter is written in the query language, not the expression language, so the elements of $in are
        // already values and must not be quoted (there is no $literal in the query language anyway)
        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { Ssn : { $in : ['$Ssn'] } } }");

        var results = queryable.ToList();
        results.Should().BeEmpty();
    }

    [Fact]
    public void GroupBy_key_that_is_a_constant_document_containing_a_dotted_element_name_should_be_quoted()
    {
        var collection = Fixture.Collection;
        var key = new Dictionary<string, string> { { "a.b", "c" } };

        var queryable = collection.AsQueryable()
            .GroupBy(x => key)
            .Select(g => g.Key);

        // unquoted the server rejects this with "FieldPath field names may not contain '.'"
        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : { $literal : { 'a.b' : 'c' } } } }",
            "{ $project : { _v : '$_id', _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(key);
    }

    public class C
    {
        public int Id { get; set; }
        public int X { get; set; }
        public string Ssn { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, X = 42, Ssn = "123-45-6789" }
        ];
    }
}
