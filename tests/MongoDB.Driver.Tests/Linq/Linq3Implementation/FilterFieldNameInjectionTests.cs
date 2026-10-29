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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation;

// A filter field path is emitted into the filter document as an element name, and the query language has no
// $literal with which to quote one. These tests cover the places where a path segment comes from application data
// (and therefore from a user). Only the names the server would reinterpret are rejected, and they are rejected with
// an exception that the translators do not catch to retry another way, because a retry would silently mean something
// different from what has been rendered for years:
// - a name containing "." is a path to a nested field, not an element with that name
// - a "$" prefixed name that is the first path component is read as an operator ($where runs JavaScript)
// - "$", "$[]" and "$[identifier]" are positional operators in an update path
// Every other name, including a nested "$" prefixed one, renders as it always has.
// This is the filter-side counterpart of FieldNameInjectionTests, which covers the aggregation expression side.
public class FilterFieldNameInjectionTests : LinqIntegrationTest<FilterFieldNameInjectionTests.ClassFixture>
{
    public FilterFieldNameInjectionTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Where_dictionary_item_with_a_dotted_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var queryable = collection.AsQueryable()
            .Where(x => x.D[key] == "v");

        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Fact]
    public void Where_BsonDocument_item_with_a_dotted_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var queryable = collection.AsQueryable()
            .Where(x => x.Doc[key] == "v");

        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Fact]
    public void Where_ContainsKey_with_a_dotted_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var queryable = collection.AsQueryable()
            .Where(x => x.D.ContainsKey(key));

        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Fact]
    public void Where_MqlField_with_a_dotted_field_name_should_throw()
    {
        var collection = Fixture.Collection;
        var fieldName = "Profile.Ssn";

        var queryable = collection.AsQueryable()
            .Where(x => Mql.Field(x, fieldName, new StringSerializer()) == "123-45-6789");

        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    // a regex translation of a retargeted clause would let the caller probe a value one prefix at a time
    [Fact]
    public void Where_MqlField_with_a_dotted_field_name_and_StartsWith_should_throw()
    {
        var collection = Fixture.Collection;
        var fieldName = "Profile.Ssn";

        var queryable = collection.AsQueryable()
            .Where(x => Mql.Field(x, fieldName, new StringSerializer()).StartsWith("1"));

        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Fact]
    public void Where_Any_dictionary_item_with_a_dotted_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var queryable = collection.AsQueryable()
            .Where(x => x.Items.Any(i => i.D[key] == "v"));

        // the fallback to $anyElementTrue/$map would fail the whole command on a document where Items is missing
        // or not an array, which $elemMatch simply skips
        AssertDottedKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Fact]
    public void Where_dictionary_item_with_a_null_key_should_throw()
    {
        var collection = Fixture.Collection;
        string key = null;

        var queryable = collection.AsQueryable()
            .Where(x => x.D[key] == "v");

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<ExpressionNotSupportedException>(); // a null key does not serialize as a string
    }

    [Fact]
    public void Where_BsonDocument_item_with_a_null_key_should_throw()
    {
        var collection = Fixture.Collection;
        string key = null;

        var queryable = collection.AsQueryable()
            .Where(x => x.Doc[key] == "v");

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
    }

    [Fact]
    public void Where_MqlField_with_a_null_field_name_should_throw()
    {
        var collection = Fixture.Collection;
        string fieldName = null;

        var queryable = collection.AsQueryable()
            .Where(x => Mql.Field(x, fieldName, new StringSerializer()) == "v");

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("the field name is null");
    }

    // an Mql.Field name on the root is the first element name of the filter document, which is where the server looks
    // for a top level operator. A find command accepts $where there, so a field name taken from application data
    // would otherwise be server side JavaScript.
    [Theory]
    [InlineData("$where")]
    [InlineData("$expr")]
    [InlineData("$or")]
    public void Find_MqlField_with_an_operator_name_should_throw(string fieldName)
    {
        var collection = Fixture.Collection;
        var serializer = new StringSerializer();

        var filter = Builders<C>.Filter.Where(x => Mql.Field(x, fieldName, serializer) == "this.Ssn.indexOf('123') == 0");

        var exception = Record.Exception(() => Render(collection, filter));

        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("would be read by the server as an operator");
    }

    // inside Any the lambda parameter is the current document of the $elemMatch, so a key on it is again the first
    // element name of a filter document
    [Fact]
    public void Where_Any_dictionary_element_with_an_operator_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "$where";

        var queryable = collection.AsQueryable()
            .Where(x => x.Dictionaries.Any(d => d[key] == "v"));

        var exception = Record.Exception(() => Translate(collection, queryable));

        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("would be read by the server as an operator");
    }

    [Theory]
    [InlineData("$")]
    [InlineData("$[]")]
    [InlineData("$[i]")]
    public void Update_dictionary_item_with_a_positional_key_should_throw(string key)
    {
        var update = Builders<C>.Update.Set(x => x.D[key], "v");

        var exception = Record.Exception(() => Render(update));

        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("positional operator");
    }

    [Fact]
    public void Update_dictionary_item_with_a_dotted_key_should_throw()
    {
        var key = "a.b";
        var update = Builders<C>.Update.Set(x => x.D[key], "v");

        AssertDottedKeyIsRejected(Record.Exception(() => Render(update)));
    }

    // the builders take the same field expressions, so they must reject the same names
    [Fact]
    public void Filter_builder_with_a_dotted_dictionary_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var filter = Builders<C>.Filter.Eq(x => x.D[key], "v");

        AssertDottedKeyIsRejected(Record.Exception(() => Render(collection, filter)));
    }

    [Fact]
    public void Filter_builder_with_a_dotted_BsonDocument_key_should_throw()
    {
        var collection = Fixture.Collection;
        var key = "a.b";

        var filter = Builders<C>.Filter.Eq(x => x.Doc[key], "v");

        AssertDottedKeyIsRejected(Record.Exception(() => Render(collection, filter)));
    }

    [Fact]
    public void Filter_builder_with_a_dotted_Mql_Field_name_should_throw()
    {
        var collection = Fixture.Collection;
        var fieldName = "Profile.Ssn";

        var filter = Builders<C>.Filter.Eq(x => Mql.Field(x, fieldName, new StringSerializer()), "123-45-6789");

        AssertDottedKeyIsRejected(Record.Exception(() => Render(collection, filter)));
    }

    [Fact]
    public void Filter_builder_with_a_safe_dictionary_key_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var key = "k";

        var filter = Builders<C>.Filter.Eq(x => x.D[key], "v");

        Render(collection, filter).Should().Be("{ 'D.k' : 'v' }");
        collection.Find(filter).ToList().Select(x => x.Id).Should().Equal(1);
    }

    // the server matches and writes a nested "$" prefixed name literally, so it is left alone
    [Fact]
    public void Where_dictionary_item_with_a_nested_dollar_prefixed_key_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var key = "$ne";

        var queryable = collection.AsQueryable()
            .Where(x => x.D[key] == "v");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'D.$ne' : 'v' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(5);
    }

    [Fact]
    public void Update_dictionary_item_with_a_nested_dollar_prefixed_key_should_use_the_field_path()
    {
        var key = "$ne";
        var update = Builders<C>.Update.Set(x => x.D[key], "v");

        Render(update).Should().Be("{ $set : { 'D.$ne' : 'v' } }");
    }

    [Fact]
    public void Where_Any_dictionary_item_with_a_nested_dollar_prefixed_key_should_use_elemMatch()
    {
        var collection = Fixture.Collection;
        var key = "$ne";

        var queryable = collection.AsQueryable()
            .Where(x => x.Items.Any(i => i.D[key] == "v"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Items : { $elemMatch : { 'D.$ne' : 'v' } } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(6);
    }

    [Fact]
    public void Where_dictionary_item_with_a_safe_key_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var key = "k";

        var queryable = collection.AsQueryable()
            .Where(x => x.D[key] == "v");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'D.k' : 'v' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_ContainsKey_with_a_safe_key_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var key = "k";

        var queryable = collection.AsQueryable()
            .Where(x => x.D.ContainsKey(key));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'D.k' : { $exists : true } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_BsonDocument_item_with_a_safe_key_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var key = "k";

        var queryable = collection.AsQueryable()
            .Where(x => x.Doc[key] == "v");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { 'Doc.k' : 'v' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_MqlField_with_a_safe_field_name_should_use_the_field_path()
    {
        var collection = Fixture.Collection;
        var fieldName = "Ssn";

        var queryable = collection.AsQueryable()
            .Where(x => Mql.Field(x, fieldName, new StringSerializer()) == "123-45-6789");

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Ssn : '123-45-6789' } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    // documents 2 to 5 have no Items array, which $elemMatch skips
    [Fact]
    public void Where_Any_dictionary_item_with_a_safe_key_should_work()
    {
        var collection = Fixture.Collection;
        var key = "k";

        var queryable = collection.AsQueryable()
            .Where(x => x.Items.Any(i => i.D[key] == "v"));

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Items : { $elemMatch : { 'D.k' : 'v' } } } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Theory]
    [InlineData(null)]      // would render as $[], silently widening the update to every element of the array
    [InlineData("")]
    [InlineData("a.b")]
    [InlineData("$ne")]
    [InlineData("i].Ssn")]  // would close the $[...] segment early and append a path segment of its own
    [InlineData("Elem")]    // the server requires an identifier to begin with a lowercase letter
    [InlineData("1elem")]
    [InlineData("el_em")]
    public void Update_AllMatchingElements_with_an_unsafe_identifier_should_throw(string identifier)
    {
        var update = Builders<C>.Update.Set(x => x.A.AllMatchingElements(identifier), "v");

        var exception = Record.Exception(() => Render(update));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    [Theory]
    [InlineData("elem")]
    [InlineData("e1")]
    [InlineData("elemA1")]
    public void Update_AllMatchingElements_with_a_safe_identifier_should_work(string identifier)
    {
        var update = Builders<C>.Update.Set(x => x.A.AllMatchingElements(identifier), "v");

        Render(update).Should().Be($"{{ $set : {{ 'A.$[{identifier}]' : 'v' }} }}");
    }

    // "@<current>" and "@<elem>" are the markers the filter AST uses for the implied document and element, not MQL. a
    // root key equal to one was read as the marker, so x[key]["Ssn"] silently targeted the root Ssn field
    [Theory]
    [InlineData("@<current>")]
    [InlineData("@<elem>")]
    public void Filter_BsonDocument_root_item_with_a_marker_key_should_throw(string key)
    {
        var filter = Builders<BsonDocument>.Filter.Where(x => x[key]["Ssn"] == "v");

        var exception = Record.Exception(() => filter.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)));

        AssertMarkerKeyIsRejected(exception);
    }

    [Theory]
    [InlineData("@<current>")]
    [InlineData("@<elem>")]
    public void Where_BsonDocument_root_item_with_a_marker_key_should_throw(string key)
    {
        var collection = GetBsonDocumentCollection();

        var queryable = collection.AsQueryable()
            .Where(x => x[key]["Ssn"] == "v");

        AssertMarkerKeyIsRejected(Record.Exception(() => Translate(collection, queryable)));
    }

    [Theory]
    [InlineData("@<current>")]
    [InlineData("@<elem>")]
    public void Update_BsonDocument_root_item_with_a_marker_key_should_throw(string key)
    {
        var update = Builders<BsonDocument>.Update.Set(x => x[key]["Ssn"], "v");

        var exception = Record.Exception(() => update.Render(new(BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry)));

        AssertMarkerKeyIsRejected(exception);
    }

    [Theory]
    [InlineData("@<current>")]
    [InlineData("@<elem>")]
    public void Find_MqlField_with_a_marker_name_should_throw(string fieldName)
    {
        var collection = Fixture.Collection;
        var serializer = new StringSerializer();

        var filter = Builders<C>.Filter.Where(x => Mql.Field(x, fieldName, serializer) == "v");

        AssertMarkerKeyIsRejected(Record.Exception(() => Render(collection, filter)));
    }

    // the key reaches AstSimplifier through the aggregation translators, which only require it to have no "$" prefix
    // and no ".", and AstSimplifier turns { $anyElementTrue : { $map : ... } } over a constant array into a filter on
    // the same path. "@<elem>.Ssn" would then be stripped to the root "Ssn" when that filter is rendered
    [Theory]
    [InlineData("@<current>")]
    [InlineData("@<elem>")]
    public void Where_BsonDocument_constant_Any_with_a_marker_key_should_not_become_a_filter(string key)
    {
        var collection = GetBsonDocumentCollection();
        var values = new BsonValue[] { "a", "b" };

        var queryable = collection.AsQueryable()
            .Where(x => values.Any(v => x[key]["Ssn"] == v));

        var stages = Translate(collection, queryable);

        AssertStages(stages, "{ $match : { $expr : { $anyElementTrue : { $map : { input : ['a', 'b'], as : 'v', in : { $eq : ['$" + key + ".Ssn', '$$v'] } } } } } }");
    }

    [Fact]
    public void Where_BsonDocument_constant_Any_with_a_safe_key_should_become_a_filter()
    {
        var collection = GetBsonDocumentCollection();
        var key = "k";
        var values = new BsonValue[] { "a", "b" };

        var queryable = collection.AsQueryable()
            .Where(x => values.Any(v => x[key]["Ssn"] == v));

        var stages = Translate(collection, queryable);

        AssertStages(stages, "{ $match : { 'k.Ssn' : { $in : ['a', 'b'] } } }");
    }

    private IMongoCollection<BsonDocument> GetBsonDocumentCollection() =>
        Fixture.Collection.Database.GetCollection<BsonDocument>(Fixture.Collection.CollectionNamespace.CollectionName);

    private static void AssertMarkerKeyIsRejected(Exception exception)
    {
        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("internal path marker");
    }

    private static void AssertDottedKeyIsRejected(Exception exception)
    {
        exception.Should().BeOfType<UnsafeFilterFieldNameException>();
        exception.Message.Should().Contain("contains \".\"");
        exception.Message.Should().Contain("x[\"a\"][\"b\"]");
    }

    private static BsonValue Render(IMongoCollection<C> collection, FilterDefinition<C> filter) =>
        filter.Render(new(collection.DocumentSerializer, BsonSerializer.SerializerRegistry));

    private static BsonValue Render(UpdateDefinition<C> update)
    {
        var documentSerializer = BsonSerializer.LookupSerializer<C>();
        return update.Render(new(documentSerializer, BsonSerializer.SerializerRegistry));
    }

    public class C
    {
        public int Id { get; set; }
        public string Ssn { get; set; }
        public Dictionary<string, string> D { get; set; }
        public BsonDocument Doc { get; set; }
        public string[] A { get; set; }
        public N[] Items { get; set; }
        public Dictionary<string, string>[] Dictionaries { get; set; }
    }

    public class N
    {
        public Dictionary<string, string> D { get; set; }
    }

    // the initial data is inserted as raw documents so that the collection can hold an element name that contains a
    // dot, and a nested field reached by that same path
    public sealed class ClassFixture : MongoCollectionFixture<C, BsonDocument>
    {
        protected override IEnumerable<BsonDocument> InitialData =>
        [
            new BsonDocument
            {
                { "_id", 1 },
                { "Ssn", "123-45-6789" },
                { "D", new BsonDocument("k", "v") },
                { "Doc", new BsonDocument("k", "v") },
                { "A", new BsonArray { "x" } },
                { "Items", new BsonArray { new BsonDocument("D", new BsonDocument("k", "v")) } }
            },
            new BsonDocument { { "_id", 2 }, { "Doc", new BsonDocument("a.b", "v") } },
            new BsonDocument { { "_id", 3 }, { "Doc", new BsonDocument("a", new BsonDocument("b", "v")) } },
            new BsonDocument { { "_id", 4 }, { "D", new BsonDocument("a.b", "v") } },
            new BsonDocument { { "_id", 5 }, { "D", new BsonDocument("$ne", "v") } },
            new BsonDocument { { "_id", 6 }, { "Items", new BsonArray { new BsonDocument("D", new BsonDocument("$ne", "v")) } } }
        ];
    }
}
