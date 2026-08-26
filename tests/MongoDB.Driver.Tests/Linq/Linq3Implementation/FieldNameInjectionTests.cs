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
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation;

// A field name is emitted into the pipeline as is, so unlike a value it cannot be quoted with $literal. These tests
// cover the places where an element name can come from application data (and therefore from a user) and would
// otherwise be reinterpreted by the server as an operator or as a path to a nested field.
public class FieldNameInjectionTests : LinqIntegrationTest<FieldNameInjectionTests.ClassFixture>
{
    public FieldNameInjectionTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [InlineData("$toUpper")]
    [InlineData("a.b")]
    [InlineData("")]
    public void Select_new_BsonDocument_with_an_unsafe_element_name_should_throw(string fieldName)
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(x => new BsonDocument(fieldName, x.Ssn));

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain($"field name \"{fieldName}\" is not valid");
    }

    [Fact]
    public void Select_new_BsonDocument_with_a_safe_element_name_should_work()
    {
        var collection = Fixture.Collection;
        var fieldName = "A";

        var queryable = collection.AsQueryable()
            .Select(x => new { V = new BsonDocument(fieldName, x.Ssn) });

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { V : { A : '$Ssn' }, _id : 0 } }");

        var result = queryable.Single();
        result.V.Should().Be(new BsonDocument("A", "123-45-6789"));
    }

    // the Dictionary(IEnumerable<KeyValuePair<TKey, TValue>>) constructor these tests rely on does not exist
    // on .NET Framework, so the expression cannot be written there at all
#if !NETFRAMEWORK
    [Fact]
    public void NewDictionary_with_an_unsafe_key_should_not_be_promoted_to_an_element_name()
    {
        var collection = Fixture.Collection;
        var key = "$toUpper";

        var queryable = collection.AsQueryable()
            .Select(x => new Dictionary<string, string>(new[] { new KeyValuePair<string, string>(key, x.Ssn) }));

        // $arrayToObject must be kept: promoting the key to an element name would turn it into an operator and
        // { $project : { _v : { $toUpper : '$Ssn' } } } would return the uppercased Ssn instead of a dictionary
        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $project : { _v : { $arrayToObject : [[{ k : { $literal : '$toUpper' }, v : '$Ssn' }]] }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(new Dictionary<string, string> { { "$toUpper", "123-45-6789" } });
    }

    [Fact]
    public void NewDictionary_with_a_safe_key_should_be_promoted_to_an_element_name()
    {
        var collection = Fixture.Collection;
        var key = "A";

        var queryable = collection.AsQueryable()
            .Select(x => new Dictionary<string, string>(new[] { new KeyValuePair<string, string>(key, x.Ssn) }));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { A : '$Ssn' }, _id : 0 } }");

        var result = queryable.Single();
        result.Should().Equal(new Dictionary<string, string> { { "A", "123-45-6789" } });
    }
#endif

    [Fact]
    public void Where_equals_a_document_that_looks_like_query_operators_should_keep_the_eq()
    {
        var collection = Fixture.Collection;
        var value = new Dictionary<string, string> { { "$ne", "" } };

        var queryable = collection.AsQueryable()
            .Where(x => x.D == value);

        // collapsing this to { D : { $ne : '' } } would turn the comparison into a query operator that matches
        // every document instead of comparing D to the value
        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { D : { $eq : { $ne : '' } } } }");

        var results = queryable.ToList();
        results.Should().BeEmpty();
    }

    [Fact]
    public void Where_equals_an_ordinary_document_should_still_collapse_the_eq()
    {
        var collection = Fixture.Collection;
        var value = new Dictionary<string, string> { { "k", "v" } };

        var queryable = collection.AsQueryable()
            .Where(x => x.D == value);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { D : { k : 'v' } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_equals_a_scalar_should_still_collapse_the_eq()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Ssn == "123-45-6789");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Ssn : '123-45-6789' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    public class C
    {
        public int Id { get; set; }
        public string Ssn { get; set; }
        public Dictionary<string, string> D { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Ssn = "123-45-6789", D = new Dictionary<string, string> { { "k", "v" } } }
        ];
    }
}
