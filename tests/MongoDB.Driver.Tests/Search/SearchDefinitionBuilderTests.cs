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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchDefinitionBuilderTests
    {
        private static readonly GeoWithinBox<GeoJson2DGeographicCoordinates> __testBox =
            new GeoWithinBox<GeoJson2DGeographicCoordinates>(
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(-161.323242, 22.065278)),
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(-152.446289, 22.512557)));

        private static readonly GeoWithinCircle<GeoJson2DGeographicCoordinates> __testCircle =
            new GeoWithinCircle<GeoJson2DGeographicCoordinates>(
                new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
                    new GeoJson2DGeographicCoordinates(-161.323242, 22.512557)),
                7.5);

        private static readonly GeoJsonPolygon<GeoJson2DGeographicCoordinates> __testPolygon =
            new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(
                new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(
                    new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>(
                        new List<GeoJson2DGeographicCoordinates>()
                        {
                            new GeoJson2DGeographicCoordinates(-161.323242, 22.512557),
                            new GeoJson2DGeographicCoordinates(-152.446289, 22.065278),
                            new GeoJson2DGeographicCoordinates(-156.09375, 17.811456),
                            new GeoJson2DGeographicCoordinates(-161.323242, 22.512557)
                        })));

        [Fact]
        public void Autocomplete()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Autocomplete("x", "foo"),
                "{ autocomplete: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Autocomplete(new[] { "x", "y" }, "foo"),
                "{ autocomplete: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Autocomplete("x", new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Autocomplete(new[] { "x", "y" }, new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Autocomplete("x", "foo", SearchAutocompleteTokenOrder.Any),
                "{ autocomplete: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Autocomplete("x", "foo", SearchAutocompleteTokenOrder.Sequential),
                "{ autocomplete: { query: 'foo', path: 'x', tokenOrder: 'sequential' } }");

            AssertRendered(
                subject.Autocomplete("x", "foo", fuzzy: new SearchFuzzyOptions()),
                "{ autocomplete: { query: 'foo', path: 'x', fuzzy: {} } }");
            AssertRendered(
                subject.Autocomplete("x", "foo", fuzzy: new SearchFuzzyOptions()
                {
                    MaxEdits = 1,
                    PrefixLength = 5,
                    MaxExpansions = 25
                }),
                "{ autocomplete: { query: 'foo', path: 'x', fuzzy: { maxEdits: 1, prefixLength: 5, maxExpansions: 25 } } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Autocomplete("x", "foo", score: scoreBuilder.Constant(1)),
                "{ autocomplete: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Autocomplete_typed()
        {
            var subject = CreateSubject<Person>();
            AssertRendered(
                subject.Autocomplete(x => x.FirstName, "foo"),
                "{ autocomplete: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Autocomplete("FirstName", "foo"),
                "{ autocomplete: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Autocomplete(x => x.Hobbies, "foo"),
                "{ autocomplete: { query: 'foo', path: 'hobbies' } }");

            AssertRendered(
                subject.Autocomplete(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    "foo"),
                "{ autocomplete: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Autocomplete(new[] { "FirstName", "LastName" }, "foo"),
                "{ autocomplete: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Autocomplete(x => x.FirstName, new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Autocomplete("FirstName", new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Autocomplete(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Autocomplete(new[] { "FirstName", "LastName" }, new[] { "foo", "bar" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void Compound()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered<BsonDocument>(
                subject.Compound()
                    .Must(
                        subject.Exists("x"),
                        subject.Exists("y"))
                    .MustNot(
                        subject.Exists("foo"),
                        subject.Exists("bar"))
                    .Must(
                        subject.Exists("z")),
                "{ compound: { must: [{ exists: { path: 'x' } }, { exists: { path: 'y' } }, { exists: { path: 'z' } }], mustNot: [{ exists: { path: 'foo' } }, { exists: { path: 'bar' } }] } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered<BsonDocument>(
                    subject.Compound(scoreBuilder.Constant(123)).Must(subject.Exists("x"), subject.Exists("y")),
                    "{ compound: { must: [{ exists: { path: 'x' } }, { exists: { path: 'y' } } ], score: { constant: { value: 123 } }  } }");
        }

        [Fact]
        public void Compound_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered<Person>(
                subject.Compound()
                    .Must(
                        subject.Exists(p => p.Age),
                        subject.Exists(p => p.FirstName))
                    .MustNot(
                        subject.Exists(p => p.Retired),
                        subject.Exists(p => p.Birthday))
                    .Must(
                        subject.Exists(p => p.LastName)),
                "{ compound: { must: [{ exists: { path: 'age' } }, { exists: { path: 'fn' } }, { exists: { path: 'ln' } }], mustNot: [{ exists: { path: 'ret' } }, { exists: { path: 'dob' } }] } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<Person>();
            AssertRendered<Person>(
                    subject.Compound(scoreBuilder.Constant(123)).Must(subject.Exists(p => p.Age)),
                    "{ compound: { must: [{ exists: { path: 'age' } } ], score: { constant: { value: 123 } }  } }");
        }

        [Fact]
        public void EmbeddedDocument()
        {
            var subject = CreateSubject<BsonDocument>();

            // Compound
            var compound = subject
                .Compound()
                .Must(subject.Text("y", "1"))
                .Should(subject.Exists("z"));

            AssertRendered(
                subject.EmbeddedDocument<BsonDocument>("x", compound),
                "{ embeddedDocument: { path : 'x', operator : { compound : { must : [{ text : { query : '1', path : 'x.y' } }], should : [{ exists : { path : 'x.z' } }] } } } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.EmbeddedDocument<BsonDocument>("x", compound, scoreBuilder.Constant(123)),
                "{ embeddedDocument: { path : 'x', operator : { compound : { must : [{ text : { query : '1', path : 'x.y' } }], should : [{ exists : { path : 'x.z' } }] } }, score: { constant: { value: 123 } } } }");

            // Multipath
            AssertRendered(
                subject.EmbeddedDocument("x", subject.Text(new[] { "y", "z", "w" }, "berg")),
                "{ embeddedDocument: { path : 'x', operator : { text : { query : 'berg', path : ['x.y', 'x.z', 'x.w'] } } } }");

            // Nested
            AssertRendered(
                subject.EmbeddedDocument("x", subject.EmbeddedDocument("y", subject.QueryString("z", "berg"))),
                "{ embeddedDocument: { path : 'x', operator : { embeddedDocument: { path : 'x.y', operator : {  'queryString' : { defaultPath : 'x.y.z', query : 'berg' } } } } } }");

            // Query
            AssertRendered(
                subject.EmbeddedDocument("x", subject.QueryString("y", "berg")),
                "{ embeddedDocument: { path : 'x', operator : { 'queryString' : { defaultPath : 'x.y', query : 'berg' } } } }");
        }

        [Fact]
        public void EmbeddedDocument_typed()
        {
            var subjectFamily = CreateSubject<Family>();
            var subjectPerson = CreateSubject<SimplePerson>();

            // Compound
            var compound = subjectPerson
                .Compound()
                .Must(subjectPerson.Text(p => p.FirstName, "John"))
                .Should(subjectPerson.Text(p => p.LastName, "Smith"));

            AssertRendered(
                subjectFamily.EmbeddedDocument(p => p.Children, compound),
                "{ embeddedDocument: { path : 'Children', operator : { compound : { must : [{ text : { query : 'John', path : 'Children.fn' } }], should : [{ text : { query : 'Smith', path : 'Children.ln' } }] } } } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<Family>();
            AssertRendered(
                subjectFamily.EmbeddedDocument(p => p.Children, compound, scoreBuilder.Constant(123)),
                "{ embeddedDocument: { path : 'Children', operator : { compound : { must : [{ text : { query : 'John', path : 'Children.fn' } }], should : [{ text : { query : 'Smith', path : 'Children.ln' } }] } }, score: { constant: { value: 123 } } } }");

            // Nested
            AssertRendered(
                subjectFamily.EmbeddedDocument(p => p.Relatives,
                    subjectFamily.EmbeddedDocument(p => p.Children, subjectPerson.Text(p => p.FirstName, "Alice"))),
                "{ embeddedDocument: { path : 'Relatives', operator : { embeddedDocument: { path : 'Relatives.Children', operator : {  'text' : { path: 'Relatives.Children.fn', query : 'Alice' } } } } } }");

            // Multipath
            var pathBuilder = new SearchPathDefinitionBuilder<SimplePerson>();
            var multiPath = pathBuilder.Multi(p => p.FirstName, p => p.LastName);
            AssertRendered(
                subjectFamily.EmbeddedDocument(p => p.Children, subjectPerson.Text(multiPath, "berg")),
                "{ embeddedDocument: { path : 'Children', operator : { text : { query : 'berg', path : ['Children.fn', 'Children.ln'] } } } }");

            // Query
            AssertRendered(
                subjectFamily.EmbeddedDocument(p => p.Children, subjectPerson.QueryString(p => p.LastName, "berg")),
                "{ embeddedDocument: { path : 'Children', operator : { 'queryString' : { defaultPath : 'Children.ln', query : 'berg' } } } }");
        }
        
        [Fact]
        public void Equals_with_array_should_render_supported_type()
        {
            var subjectTyped = CreateSubject<Person>();
            
            AssertRendered(
                subjectTyped.Equals(p => p.Hobbies, "soccer"),
                "{ equals: { path: 'hobbies', value: 'soccer' } }");
        }

        [Theory]
        [MemberData(nameof(EqualsSupportedTypesTestData))]
        public void Equals_should_render_supported_type<T>(
            T value,
            string valueRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldRendered)
        {
            var subject = CreateSubject<BsonDocument>();
            var subjectTyped = CreateSubject<Person>();

            //When using an untyped query, the default GuidSerializer is used, and that will throw an exception
            //because the GuidRepresentation is Unspecified.
            if (typeof(T) != typeof(Guid))
            {
                AssertRendered(
                    subject.Equals("x", value),
                    $"{{ equals: {{ path: 'x', value: {valueRendered} }} }}");

                var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
                AssertRendered(
                    subject.Equals("x", value, scoreBuilder.Constant(1)),
                    $"{{ equals: {{ path: 'x', value: {valueRendered}, score: {{ constant: {{ value: 1 }} }} }} }}");
            }

            AssertRendered(
                subjectTyped.Equals(fieldExpression, value),
                $"{{ equals: {{ path: '{fieldRendered}', value: {valueRendered} }} }}");
        }

        [Theory]
        [MemberData(nameof(EqualsWithConfiguredSerializersSetToFalseSupportedTypesTestData))]
        public void Equals_with_configured_serializers_false_should_render_supported_type<T>(
            T value,
            string valueRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldRendered)
        {
            var subject = CreateSubject<BsonDocument>();
            var subjectTyped = CreateSubject<Person>();

            //When using an untyped query, the default GuidSerializer is used, and that will throw an exception
            //because the GuidRepresentation is Unspecified.
            AssertRendered(
                subject.Equals("x", value).UseConfiguredSerializers(false),
                $"{{ equals: {{ path: 'x', value: {valueRendered} }} }}");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Equals("x", value, scoreBuilder.Constant(1)).UseConfiguredSerializers(false),
                $"{{ equals: {{ path: 'x', value: {valueRendered}, score: {{ constant: {{ value: 1 }} }} }} }}");

            AssertRendered(
                subjectTyped.Equals(fieldExpression, value).UseConfiguredSerializers(false),
                $"{{ equals: {{ path: '{fieldRendered}', value: {valueRendered} }} }}");
        }

        public static object[][] EqualsSupportedTypesTestData => new[]
        {
            new object[] { true, "true", Exp(p => p.Retired), "ret" },
            new object[] { (sbyte)1, "1", Exp(p => p.Int8), nameof(Person.Int8), },
            new object[] { (byte)1, "1", Exp(p => p.UInt8), nameof(Person.UInt8), },
            new object[] { (short)1, "1", Exp(p => p.Int16), nameof(Person.Int16) },
            new object[] { (ushort)1, "1", Exp(p => p.UInt16), nameof(Person.UInt16) },
            new object[] { (int)1, "1", Exp(p => p.Int32), nameof(Person.Int32) },
            new object[] { (uint)1, "1", Exp(p => p.UInt32), nameof(Person.UInt32) },
            new object[] { long.MaxValue, "NumberLong(\"9223372036854775807\")", Exp(p => p.Int64), nameof(Person.Int64) },
            new object[] { (float)1, "1", Exp(p => p.Float), nameof(Person.Float) },
            new object[] { (double)1, "1", Exp(p => p.Double), nameof(Person.Double) },
            new object[] { DateTime.MinValue, "ISODate(\"0001-01-01T00:00:00Z\")", Exp(p => p.Birthday), "dob" },
            new object[] { DateTimeOffset.MaxValue, """{ "DateTime" : { "$date" : "9999-12-31T23:59:59.999Z" }, "Ticks" : 3155378975999999999, "Offset" : 0 }""", Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset) },
            new object[] { ObjectId.Empty, "{ $oid: '000000000000000000000000' }", Exp(p => p.Id), "_id" },
            new object[] { Guid.Empty, """{ "$binary" : { "base64" : "AAAAAAAAAAAAAAAAAAAAAA==", "subType" : "04" } }""", Exp(p => p.Guid), nameof(Person.Guid) },
            new object[] { null, "null", Exp(p => p.Name), nameof(Person.Name) },
            new object[] { "Jim", "\"Jim\"", Exp(p => p.FirstName), "fn" }
        };

        public static object[][] EqualsWithConfiguredSerializersSetToFalseSupportedTypesTestData => new[]
        {
            new object[] { true, "true", Exp(p => p.Retired), "ret" },
            new object[] { (sbyte)1, "1", Exp(p => p.Int8), nameof(Person.Int8), },
            new object[] { (byte)1, "1", Exp(p => p.UInt8), nameof(Person.UInt8), },
            new object[] { (short)1, "1", Exp(p => p.Int16), nameof(Person.Int16) },
            new object[] { (ushort)1, "1", Exp(p => p.UInt16), nameof(Person.UInt16) },
            new object[] { (int)1, "1", Exp(p => p.Int32), nameof(Person.Int32) },
            new object[] { (uint)1, "1", Exp(p => p.UInt32), nameof(Person.UInt32) },
            new object[] { long.MaxValue, "NumberLong(\"9223372036854775807\")", Exp(p => p.Int64), nameof(Person.Int64) },
            new object[] { (float)1, "1", Exp(p => p.Float), nameof(Person.Float) },
            new object[] { (double)1, "1", Exp(p => p.Double), nameof(Person.Double) },
            new object[] { DateTime.MinValue, "ISODate(\"0001-01-01T00:00:00Z\")", Exp(p => p.Birthday), "dob" },
            new object[] { DateTimeOffset.MaxValue, "ISODate(\"9999-12-31T23:59:59.999Z\")", Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset) },
            new object[] { ObjectId.Empty, "{ $oid: '000000000000000000000000' }", Exp(p => p.Id), "_id" },
            new object[] { Guid.Empty, """{ "$binary" : { "base64" : "AAAAAAAAAAAAAAAAAAAAAA==", "subType" : "04" } }""", Exp(p => p.Guid), nameof(Person.Guid) },
            new object[] { null, "null", Exp(p => p.Name), nameof(Person.Name) },
            new object[] { "Jim", "\"Jim\"", Exp(p => p.FirstName), "fn" }
        };

        [Fact]
        public void Equals_should_use_correct_serializers_when_using_attributes_and_expression_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Equals(t => t.DefaultGuid, testGuid),
                """{ "equals" : { "value" : { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, "path" : "DefaultGuid" } } """);

            AssertRendered(
                subjectTyped.Equals(t => t.StringGuid, testGuid),
                """{ "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "StringGuid" } }""");
        }

        [Fact]
        public void Equals_should_use_correct_serializers_when_using_attributes_and_string_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Equals("DefaultGuid", testGuid),
                """{ "equals" : { "value" : { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, "path" : "DefaultGuid" } } """);

            AssertRendered(
                subjectTyped.Equals("StringGuid", testGuid),
                """{ "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "StringGuid" } }""");
        }

        [Fact(Skip = "This should only be run manually due to the use of BsonSerializer.RegisterSerializer")]
        public void Equals_should_use_correct_serializers_when_using_serializer_registry()
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Equals(t => t.UndefinedRepresentationGuid, testGuid).UseConfiguredSerializers(false),
                """{ "equals" : { "value" : "01020304-0506-0708-090a-0b0c0d0e0f10", "path" : "UndefinedRepresentationGuid" } }""");
        }

        [Fact]
        public void Exists()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Exists("x"),
                "{ exists: { path: 'x' } }");
        }

        [Fact]
        public void Exists_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Exists(x => x.FirstName),
                "{ exists: { path: 'fn' } }");
            AssertRendered(
                subject.Exists(x => x.Hobbies),
                "{ exists: { path: 'hobbies' } }");
            AssertRendered(
                subject.Exists("FirstName"),
                "{ exists: { path: 'fn' } }");
        }

        [Fact]
        public void Facet()
        {
            var subject = CreateSubject<BsonDocument>();
            var facetBuilder = new SearchFacetBuilder<BsonDocument>();

            AssertRendered(
                subject.Facet(
                    subject.Phrase("x", "foo"),
                    facetBuilder.String("string", "y", 100)),
                "{ facet: { operator: { phrase: { query: 'foo', path: 'x' } }, facets: { string: { type: 'string', path: 'y', numBuckets: 100 } } } }");
        }

        [Fact]
        public void Facet_typed()
        {
            var subject = CreateSubject<Person>();
            var facetBuilder = new SearchFacetBuilder<Person>();

            AssertRendered(
                subject.Facet(
                    subject.Phrase(x => x.LastName, "foo"),
                    facetBuilder.String("string", x => x.FirstName, 100)),
                "{ facet: { operator: { phrase: { query: 'foo', path: 'ln' } }, facets: { string: { type: 'string', path: 'fn', numBuckets: 100 } } } }");
            AssertRendered(
                subject.Facet(
                    subject.Phrase("LastName", "foo"),
                    facetBuilder.String("string", "FirstName", 100)),
                "{ facet: { operator: { phrase: { query: 'foo', path: 'ln' } }, facets: { string: { type: 'string', path: 'fn', numBuckets: 100 } } } }");
        }

        [Fact]
        public void Filter()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered<BsonDocument>(
                subject.Compound().Filter(
                    subject.Exists("x"),
                    subject.Exists("y")),
                "{ compound: { filter: [{ exists: { path: 'x' } }, { exists: { path: 'y' } }] } }");
        }

        [Fact]
        public void Filter_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered<Person>(
                subject.Compound().Filter(
                    subject.Exists(p => p.Age),
                    subject.Exists(p => p.Birthday)),
                "{ compound: { filter: [{ exists: { path: 'age' } }, { exists: { path: 'dob' } }] } }");
        }

        [Fact]
        public void GeoShape()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.GeoShape(
                    "location",
                    GeoShapeRelation.Disjoint,
                    __testPolygon),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
        }

        [Fact]
        public void GeoShape_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.GeoShape(
                    x => x.Location,
                    GeoShapeRelation.Disjoint,
                    __testPolygon),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
            AssertRendered(
                subject.GeoShape(
                    "Location",
                    GeoShapeRelation.Disjoint,
                    __testPolygon),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
        }

        [Fact]
        public void GeoWithin()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.GeoWithin("location", __testPolygon),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin("location", __testBox),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin("location", __testCircle),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
        }

        [Fact]
        public void GeoWithin_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.GeoWithin(x => x.Location, __testPolygon),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin("Location", __testPolygon),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");

            AssertRendered(
                subject.GeoWithin(x => x.Location, __testBox),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin("Location", __testBox),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");

            AssertRendered(
                subject.GeoWithin(x => x.Location, __testCircle),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin("Location", __testCircle),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
        }

        [Theory]
        [MemberData(nameof(InTestData))]
        public void In<T>(T[] fieldValues, string[] fieldsRendered)
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.In("x", fieldValues),
                $"{{ in: {{ path: 'x', value: [{string.Join(",", fieldsRendered)}] }} }}");
        }

        [Theory]
        [MemberData(nameof(InWithConfiguredSerializersSetToFalseTestData))]
        public void InWithConfiguredSerializersSetToFalse<T>(T[] fieldValues, string[] fieldsRendered)
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.In("x", fieldValues).UseConfiguredSerializers(false),
                $"{{ in: {{ path: 'x', value: [{string.Join(",", fieldsRendered)}] }} }}");
        }

        public static readonly object[][] InWithConfiguredSerializersSetToFalseTestData =
        {
            new object[] { new bool[] { true, false }, new[] { "true", "false" } },
            new object[] { new byte[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new sbyte[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new short[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new ushort[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new int[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new uint[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new long[] { long.MaxValue, long.MinValue }, new[] { "NumberLong(\"9223372036854775807\")", "NumberLong(\"-9223372036854775808\")" } },
            new object[] { new float[] { 1.5f, 2.5f }, new[] { "1.5", "2.5" } },
            new object[] { new double[] { 1.5, 2.5 }, new[] { "1.5", "2.5" } },
            new object[] { new decimal[] { 1.5m, 2.5m }, new[] { "NumberDecimal(\"1.5\")", "NumberDecimal(\"2.5\")" } },
            new object[] { new[] { "str1", "str2" }, new[] { "'str1'", "'str2'" } },
            new object[] { new[] { DateTime.MinValue, DateTime.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" } },
            new object[] { new[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" } },
            new object[] { new[] { ObjectId.Empty, ObjectId.Parse("4d0ce088e447ad08b4721a37") }, new[] { "{ $oid: '000000000000000000000000' }", "{ $oid: '4d0ce088e447ad08b4721a37' }" } },
            new object[] { new object[] { (byte)1, (short)2, (int)3 }, new[] { "1", "2", "3" } }
        };

        public static readonly object[][] InTestData =
        {
            new object[] { new bool[] { true, false }, new[] { "true", "false" } },
            new object[] { new byte[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new sbyte[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new short[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new ushort[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new int[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new uint[] { 1, 2 }, new[] { "1", "2" } },
            new object[] { new long[] { long.MaxValue, long.MinValue }, new[] { "NumberLong(\"9223372036854775807\")", "NumberLong(\"-9223372036854775808\")" } },
            new object[] { new float[] { 1.5f, 2.5f }, new[] { "1.5", "2.5" } },
            new object[] { new double[] { 1.5, 2.5 }, new[] { "1.5", "2.5" } },
            new object[] { new decimal[] { 1.5m, 2.5m }, new[] { "NumberDecimal(\"1.5\")", "NumberDecimal(\"2.5\")" } },
            new object[] { new[] { "str1", "str2" }, new[] { "'str1'", "'str2'" } },
            new object[] { new[] { DateTime.MinValue, DateTime.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" } },
            new object[] { new[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue }, new[] { """{ "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : 0, "Offset" : 0 } """, """ { "DateTime" : { "$date" : "9999-12-31T23:59:59.999Z" }, "Ticks" : 3155378975999999999, "Offset" : 0 } """ } },
            new object[] { new[] { ObjectId.Empty, ObjectId.Parse("4d0ce088e447ad08b4721a37") }, new[] { "{ $oid: '000000000000000000000000' }", "{ $oid: '4d0ce088e447ad08b4721a37' }" } },
        };

        [Theory]
        [MemberData(nameof(InTypedTestData))]
        public void In_typed<T>(
            T[] fieldValues,
            string[] fieldValuesRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldNameRendered)
        {
            var subjectTyped = CreateSubject<Person>();
            var fieldValuesArray = $"[{string.Join(",", fieldValuesRendered)}]";

            //There is no property called "x" where to pick up a properly configured GuidSerializer for the tests
            if (typeof(T) != typeof(Guid))
            {
                AssertRendered(
                    subjectTyped.In("x", fieldValues),
                    $"{{ in: {{ path: 'x', value: {fieldValuesArray} }} }}");
            }

            AssertRendered(
                subjectTyped.In(fieldExpression, fieldValues),
                $"{{ in: {{path: '{fieldNameRendered}', value: {fieldValuesArray} }} }}");
        }

        [Theory]
        [MemberData(nameof(InTypedWithConfiguredSerializersSetToFalseTestData))]
        public void InWithConfiguredSerializersSetToFalse_typed<T>(
            T[] fieldValues,
            string[] fieldValuesRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldNameRendered)
        {
            var subject = CreateSubject<Person>();
            var fieldValuesArray = $"[{string.Join(",", fieldValuesRendered)}]";

            AssertRendered(
                subject.In("x", fieldValues).UseConfiguredSerializers(false),
                $"{{ in: {{ path: 'x', value: {fieldValuesArray} }} }}");

            AssertRendered(
                subject.In(fieldExpression, fieldValues).UseConfiguredSerializers(false),
                $"{{ in: {{path: '{fieldNameRendered}', value: {fieldValuesArray} }} }}");
        }

        public static readonly object[][] InTypedTestData =
        {
             new object[] { new bool[] { true, false }, new[] { "true", "false" }, Exp(p => p.Retired), "ret" },
             new object[] { new byte[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt8), nameof(Person.UInt8) },
             new object[] { new sbyte[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int8), nameof(Person.Int8) },
             new object[] { new short[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int16), nameof(Person.Int16) },
             new object[] { new ushort[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt16), nameof(Person.UInt16) },
             new object[] { new int[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int32), nameof(Person.Int32) },
             new object[] { new uint[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt32), nameof(Person.UInt32) },
             new object[] { new long[] { long.MaxValue, long.MinValue }, new[] { "NumberLong(\"9223372036854775807\")", "NumberLong(\"-9223372036854775808\")" }, Exp(p => p.Int64), nameof(Person.Int64) },
             new object[] { new float[] { 1.5f, 2.5f }, new[] { "1.5", "2.5" }, Exp(p => p.Float), nameof(Person.Float) },
             new object[] { new double[] { 1.5, 2.5 }, new[] { "1.5", "2.5" }, Exp(p => p.Double), nameof(Person.Double) },
             new object[] { new decimal[] { 1.5m, 2.5m }, new[] { "NumberDecimal(\"1.5\")", "NumberDecimal(\"2.5\")" }, Exp(p => p.Decimal), nameof(Person.Decimal) },
             new object[] { new[] { "str1", "str2" }, new[] { "'str1'", "'str2'" }, Exp(p => p.FirstName), "fn" },
             new object[] { new[] { DateTime.MinValue, DateTime.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" }, Exp(p => p.Birthday), "dob" },
             new object[] { new[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue }, new[] { """{ "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : 0, "Offset" : 0 } """, """ { "DateTime" : { "$date" : "9999-12-31T23:59:59.999Z" }, "Ticks" : 3155378975999999999, "Offset" : 0 } """ }, Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset)},
             new object[] { new[] { ObjectId.Empty, ObjectId.Parse("4d0ce088e447ad08b4721a37") }, new[] { "{ $oid: '000000000000000000000000' }", "{ $oid: '4d0ce088e447ad08b4721a37' }" }, Exp(p => p.Id), "_id" },
             new object[] { new[] { Guid.Empty, Guid.Parse("b52af144-bc97-454f-a578-418a64fa95bf") }, new[] { """{ "$binary" : { "base64" : "AAAAAAAAAAAAAAAAAAAAAA==", "subType" : "04" } }""", """{ "$binary" : { "base64" : "tSrxRLyXRU+leEGKZPqVvw==", "subType" : "04" } }""" }, Exp(p => p.Guid), nameof(Person.Guid) },
        };

        public static readonly object[][] InTypedWithConfiguredSerializersSetToFalseTestData =
        {
             new object[] { new bool[] { true, false }, new[] { "true", "false" }, Exp(p => p.Retired), "ret" },
             new object[] { new byte[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt8), nameof(Person.UInt8) },
             new object[] { new sbyte[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int8), nameof(Person.Int8) },
             new object[] { new short[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int16), nameof(Person.Int16) },
             new object[] { new ushort[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt16), nameof(Person.UInt16) },
             new object[] { new int[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.Int32), nameof(Person.Int32) },
             new object[] { new uint[] { 1, 2 }, new[] { "1", "2" }, Exp(p => p.UInt32), nameof(Person.UInt32) },
             new object[] { new long[] { long.MaxValue, long.MinValue }, new[] { "NumberLong(\"9223372036854775807\")", "NumberLong(\"-9223372036854775808\")" }, Exp(p => p.Int64), nameof(Person.Int64) },
             new object[] { new float[] { 1.5f, 2.5f }, new[] { "1.5", "2.5" }, Exp(p => p.Float), nameof(Person.Float) },
             new object[] { new double[] { 1.5, 2.5 }, new[] { "1.5", "2.5" }, Exp(p => p.Double), nameof(Person.Double) },
             new object[] { new decimal[] { 1.5m, 2.5m }, new[] { "NumberDecimal(\"1.5\")", "NumberDecimal(\"2.5\")" }, Exp(p => p.Decimal), nameof(Person.Decimal) },
             new object[] { new[] { "str1", "str2" }, new[] { "'str1'", "'str2'" }, Exp(p => p.FirstName), "fn" },
             new object[] { new[] { DateTime.MinValue, DateTime.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" }, Exp(p => p.Birthday), "dob" },
             new object[] { new[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue }, new[] { "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")" }, Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset)},
             new object[] { new[] { ObjectId.Empty, ObjectId.Parse("4d0ce088e447ad08b4721a37") }, new[] { "{ $oid: '000000000000000000000000' }", "{ $oid: '4d0ce088e447ad08b4721a37' }" }, Exp(p => p.Id), "_id" },
             new object[] { new[] { Guid.Empty, Guid.Parse("b52af144-bc97-454f-a578-418a64fa95bf") }, new[] { """{ "$binary" : { "base64" : "AAAAAAAAAAAAAAAAAAAAAA==", "subType" : "04" } }""", """{ "$binary" : { "base64" : "tSrxRLyXRU+leEGKZPqVvw==", "subType" : "04" } }""" }, Exp(p => p.Guid), nameof(Person.Guid) },
             new object[] { new object[] { (byte)1, (short)2, (int)3 }, new[] { "1", "2", "3" }, Exp(p => p.Object), nameof(Person.Object) }
        };

        [Fact]
        public void In_should_throw_when_values_are_invalid()
        {
            var subject = CreateSubject<BsonDocument>();
            Record.Exception(() => subject.In("x", new int[] { })).Should().BeOfType<ArgumentException>();
            Record.Exception(() => subject.In<int>("x", null)).Should().BeOfType<ArgumentNullException>();

            var subjectTyped = CreateSubject<Person>();
            Record.Exception(() => subjectTyped.In(p => p.Age, new int[] { })).Should().BeOfType<ArgumentException>();
            Record.Exception(() => subjectTyped.In(p => p.Age, null)).Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void In_should_use_correct_serializers_when_using_attributes_and_expression_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.In(t => t.DefaultGuid, [testGuid, testGuid]),
                """{ "in" : { "value" : [{ "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }], "path" : "DefaultGuid" } } """);

            AssertRendered(
                subjectTyped.In(t => t.StringGuid, [testGuid, testGuid]),
                """{ "in" : { "value" : ["01020304-0506-0708-090a-0b0c0d0e0f10", "01020304-0506-0708-090a-0b0c0d0e0f10"], "path" : "StringGuid" } }""");
        }

        [Fact]
        public void In_should_use_correct_serializers_when_using_attributes_and_string_path()
        {
            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.In("DefaultGuid", [testGuid, testGuid]),
                """{ "in" : { "value" : [{ "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }, { "$binary" : { "base64" : "AQIDBAUGBwgJCgsMDQ4PEA==", "subType" : "04" } }], "path" : "DefaultGuid" } } """);

            AssertRendered(
                subjectTyped.In("StringGuid", [testGuid, testGuid]),
                """{ "in" : { "value" : ["01020304-0506-0708-090a-0b0c0d0e0f10", "01020304-0506-0708-090a-0b0c0d0e0f10"], "path" : "StringGuid" } }""");
        }

        [Fact(Skip = "This should only be run manually due to the use of BsonSerializer.RegisterSerializer")]
        public void In_should_use_correct_serializers_when_using_serializer_registry()
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

            var testGuid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.In(t => t.UndefinedRepresentationGuid, [testGuid, testGuid]),
                """{ "in" : { "value" : ["01020304-0506-0708-090a-0b0c0d0e0f10", "01020304-0506-0708-090a-0b0c0d0e0f10"], "path" : "UndefinedRepresentationGuid" } }""");
        }

        [Fact]
        public void In_with_array_field_should_render_correctly()
        {
            var subjectTyped = CreateSubject<Person>();

            AssertRendered(
                subjectTyped.In(p => p.Hobbies, ["dance", "ski"]),
                "{ in: { path: 'hobbies', value: ['dance', 'ski'] } }");
        }

        [Fact]
        public void In_with_wildcard_path_should_render_correctly()
        {
            var subjectTyped = CreateSubject<Person>();

            var path = new SearchPathDefinitionBuilder<Person>();
            AssertRendered(
                subjectTyped.In(path.Wildcard("*"), ["dance"]),
                "{ in: { path: { wildcard: '*'}, value: ['dance'] } }");
        }

        [Fact]
        public void MoreLikeThis()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.MoreLikeThis(
                    new BsonDocument("x", "foo"),
                    new BsonDocument("x", "bar")),
                "{ moreLikeThis: { like: [{ x: 'foo' }, { x: 'bar' }] } }");

            AssertRendered(
              subject.MoreLikeThis(
                  new SimplePerson { FirstName = "John", LastName = "Doe" },
                  new SimplePerson { FirstName = "Jane", LastName = "Doe" }),
             "{ moreLikeThis: { like: [{ fn: 'John', ln: 'Doe' }, { fn: 'Jane', ln: 'Doe' }] } }");
        }

        [Fact]
        public void MoreLikeThis_typed()
        {
            var subject = CreateSubject<SimplePerson>();

            AssertRendered(
                subject.MoreLikeThis(
                    new SimplestPerson { FirstName = "John" },
                    new SimplestPerson { FirstName = "Jane" }),
                "{ moreLikeThis: { like: [{ fn: 'John' }, { fn: 'Jane' }] } }");

            AssertRendered(
                subject.MoreLikeThis(
                    new SimplePerson { FirstName = "John", LastName = "Doe" },
                    new SimplePerson { FirstName = "Jane", LastName = "Doe" }),
                "{ moreLikeThis: { like: [{ fn: 'John', ln: 'Doe' }, { fn: 'Jane', ln: 'Doe' }] } }");

            AssertRendered(
               subject.MoreLikeThis(
                   new BsonDocument
                   {
                        { "fn", "John" },
                        { "ln", "Doe" },
                   },
                   new BsonDocument
                   {
                        { "fn", "Jane" },
                        { "ln", "Doe" },
                   }),
               "{ moreLikeThis: { like: [{ fn: 'John', ln: 'Doe' }, { fn: 'Jane', ln: 'Doe' }] } }");
        }

        [Fact]
        public void Must()
        {
            var subject = CreateSubject<Person>();

            AssertRendered<Person>(
                subject.Compound().Must(
                    subject.Exists(p => p.Age),
                    subject.Exists(p => p.Birthday)),
                "{ compound: { must: [{ exists: { path: 'age' } }, { exists: { path: 'dob' } }] } }");
        }

        [Fact]
        public void MustNot()
        {
            var subject = CreateSubject<Person>();

            AssertRendered<Person>(
                subject.Compound().MustNot(
                    subject.Exists(p => p.Age),
                    subject.Exists(p => p.Birthday)),
                "{ compound: { mustNot: [{ exists: { path: 'age' } }, { exists: { path: 'dob' } }] } }");
        }

        [Fact]
        public void Near()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Near("x", 5.0, 1.0),
                "{ near: { path: 'x', origin: 5.0, pivot: 1.0 } }");
            AssertRendered(
                subject.Near("x", 5, 1),
                "{ near: { path: 'x', origin: 5, pivot: 1 } }");
            AssertRendered(
                subject.Near("x", 5L, 1L),
                "{ near: { path: 'x', origin: { $numberLong: '5' }, pivot: { $numberLong: '1' } } }");
            AssertRendered(
                subject.Near("x", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1000L),
                "{ near: { path: 'x', origin: { $date: '2000-01-01T00:00:00Z' }, pivot: { $numberLong: '1000' } } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Near("x", 5.0, 1.0, scoreBuilder.Constant(1)),
                "{ near: { path: 'x', origin: 5, pivot: 1, score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Near_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Near(x => x.Age, 35.0, 5.0),
                "{ near: { path: 'age', origin: 35.0, pivot: 5.0 } }");
            AssertRendered(
                subject.Near("Age", 35.0, 5.0),
                "{ near: { path: 'age', origin: 35.0, pivot: 5.0 } }");

            AssertRendered(
                subject.Near(x => x.Age, 35, 5),
                "{ near: { path: 'age', origin: 35, pivot: 5 } }");
            AssertRendered(
                subject.Near("Age", 35, 5),
                "{ near: { path: 'age', origin: 35, pivot: 5 } }");

            AssertRendered(
                subject.Near(x => x.Age, 35L, 5L),
                "{ near: { path: 'age', origin: { $numberLong: '35' }, pivot: { $numberLong: '5' } } }");
            AssertRendered(
                subject.Near("Age", 35L, 5L),
                "{ near: { path: 'age', origin: { $numberLong: '35' }, pivot: { $numberLong: '5' } } }");

            AssertRendered(
                subject.Near(x => x.Birthday, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1000L),
                "{ near: { path: 'dob', origin: { $date: '2000-01-01T00:00:00Z' }, pivot: { $numberLong: '1000' } } }");
            AssertRendered(
                subject.Near("Birthday", new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), 1000L),
                "{ near: { path: 'dob', origin: { $date: '2000-01-01T00:00:00Z' }, pivot: { $numberLong: '1000' } } }");
        }

        [Fact]
        public void Phrase()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Phrase("x", "foo"),
                "{ phrase: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Phrase(new[] { "x", "y" }, "foo"),
                "{ phrase: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Phrase("x", new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Phrase(new[] { "x", "y" }, new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Phrase("x", "foo", 5),
                "{ phrase: { query: 'foo', path: 'x', slop: 5 } }");

            AssertRendered(
                subject.Phrase("x", "foo", new SearchPhraseOptions<BsonDocument> { Synonyms = "testSynonyms" }),
                "{ phrase: { query: 'foo', path: 'x', synonyms: 'testSynonyms' } }");

            AssertRendered(
                subject.Phrase("x", "foo", 5),
                "{ phrase: { query: 'foo', path: 'x', slop: 5 } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Phrase("x", "foo", score: scoreBuilder.Constant(1)),
                "{ phrase: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");

            AssertRendered(
                subject.Phrase("x", "foo", new SearchPhraseOptions<BsonDocument> { Score = scoreBuilder.Constant(1), Slop = 5}),
                "{ phrase: { query: 'foo', slop: 5, path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Phrase_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Phrase(x => x.FirstName, "foo"),
                "{ phrase: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Phrase("FirstName", "foo"),
                "{ phrase: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Phrase(x => x.Hobbies, "foo"),
                "{ phrase: { query: 'foo', path: 'hobbies' } }");

            AssertRendered(
                subject.Phrase(x => x.FirstName, "foo", new SearchPhraseOptions<Person> { Synonyms = "testSynonyms" }),
                "{ phrase: { query: 'foo', synonyms: 'testSynonyms', path: 'fn' } }");

            AssertRendered(
                subject.Phrase(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    "foo"),
                "{ phrase: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Phrase(new[] { "FirstName", "LastName" }, "foo"),
                "{ phrase: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Phrase(x => x.FirstName, new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Phrase("FirstName", new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Phrase(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Phrase(new[] { "FirstName", "LastName" }, new[] { "foo", "bar" }),
                "{ phrase: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void QueryString()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.QueryString("x", "foo"),
                "{ queryString: { defaultPath: 'x', query: 'foo' } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.QueryString("x", "foo", scoreBuilder.Constant(1)),
                "{ queryString: { defaultPath: 'x', query: 'foo', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void QueryString_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.QueryString(x => x.FirstName, "foo"),
                "{ queryString: { defaultPath: 'fn', query: 'foo' } }");
            AssertRendered(
                subject.QueryString("FirstName", "foo"),
                "{ queryString: { defaultPath: 'fn', query: 'foo' } }");
            AssertRendered(
                subject.QueryString(x => x.Hobbies, "foo"),
                "{ queryString: { defaultPath: 'hobbies', query: 'foo' } }");
        }

        [Theory]
        [InlineData(1, null, false, false, "gt: 1")]
        [InlineData(1, null, true, false, "gte: 1")]
        [InlineData(null, 1, false, false, "lt: 1")]
        [InlineData(null, 1, false, true, "lte: 1")]
        [InlineData(1, 10, false, false, "gt: 1, lt: 10")]
        [InlineData(1, 10, true, false, "gte: 1, lt: 10")]
        [InlineData(1, 10, false, true, "gt: 1, lte: 10")]
        [InlineData(1, 10, true, true, "gte: 1, lte: 10")]
        public void Range_should_render_correct_operator(int? min, int? max, bool minInclusive, bool maxInclusive, string rangeRendered)
        {
            var subject = CreateSubject<BsonDocument>();
            AssertRendered(
                    subject.Range("x", new SearchRange<int>(min, max, minInclusive, maxInclusive)),
                    $"{{ range: {{ path: 'x', {rangeRendered} }} }}");
        }

        [Theory]
        [MemberData(nameof(RangeSupportedTypesTestData))]
        public void Range_should_render_supported_types<T>(
            T min,
            T max,
            string minRendered,
            string maxRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldRendered)
            where T : struct, IComparable<T>
        {
            var subject = CreateSubject<BsonDocument>();
            var subjectTyped = CreateSubject<Person>();

            AssertRendered(
                subject.Range("age", SearchRangeBuilder.Gte(min).Lt(max)),
                $"{{ range: {{ path: 'age', gte: {minRendered}, lt: {maxRendered} }} }}");

            AssertRendered(
                subjectTyped.Range(fieldExpression, SearchRangeBuilder.Gte(min).Lt(max)),
                $"{{ range: {{ path: '{fieldRendered}', gte: {minRendered}, lt: {maxRendered} }} }}");
        }

        [Theory]
        [MemberData(nameof(RangeWithConfiguredSerializersSetToFalseSupportedTypesTestData))]
        public void Range_with_configured_serializers_should_render_supported_types<T>(
            T min,
            T max,
            string minRendered,
            string maxRendered,
            Expression<Func<Person, T>> fieldExpression,
            string fieldRendered)
            where T : struct, IComparable<T>
        {
            var subject = CreateSubject<BsonDocument>();
            var subjectTyped = CreateSubject<Person>();

            AssertRendered(
                subject.Range("age", SearchRangeBuilder.Gte(min).Lt(max)).UseConfiguredSerializers(false),
                $"{{ range: {{ path: 'age', gte: {minRendered}, lt: {maxRendered} }} }}");

            AssertRendered(
                subjectTyped.Range(fieldExpression, SearchRangeBuilder.Gte(min).Lt(max)).UseConfiguredSerializers(false),
                $"{{ range: {{ path: '{fieldRendered}', gte: {minRendered}, lt: {maxRendered} }} }}");
        }

        public static object[][] RangeSupportedTypesTestData => new[]
        {
            new object[] { (sbyte)1, (sbyte)2, "1", "2", Exp(p => p.Int8), nameof(Person.Int8) },
            new object[] { (byte)1, (byte)2, "1", "2", Exp(p => p.UInt8), nameof(Person.UInt8) },
            new object[] { (short)1, (short)2, "1", "2", Exp(p => p.Int16), nameof(Person.Int16) },
            new object[] { (ushort)1, (ushort)2, "1", "2", Exp(p => p.UInt16), nameof(Person.UInt16) },
            new object[] { (int)1, (int)2, "1", "2", Exp(p => p.Int32), nameof(Person.Int32) },
            new object[] { (uint)1, (uint)2, "1", "2", Exp(p => p.UInt32), nameof(Person.UInt32) },
            new object[] { long.MinValue, long.MaxValue, "NumberLong(\"-9223372036854775808\")", "NumberLong(\"9223372036854775807\")", Exp(p => p.Int64), nameof(Person.Int64) },
            new object[] { (float)1, (float)2, "1", "2", Exp(p => p.Float), nameof(Person.Float) },
            new object[] { (double)1, (double)2, "1", "2", Exp(p => p.Double), nameof(Person.Double) },
            new object[] { DateTime.MinValue, DateTime.MaxValue, "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")", Exp(p => p.Birthday), "dob" },
            new object[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue,"""{ "DateTime" : { "$date" : { "$numberLong" : "-62135596800000" } }, "Ticks" : 0, "Offset" : 0 }""", """{ "DateTime" : { "$date" : "9999-12-31T23:59:59.999Z" }, "Ticks" : 3155378975999999999, "Offset" : 0 }""", Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset) }
        };

        public static object[][] RangeWithConfiguredSerializersSetToFalseSupportedTypesTestData => new[]
        {
            new object[] { (sbyte)1, (sbyte)2, "1", "2", Exp(p => p.Int8), nameof(Person.Int8) },
            new object[] { (byte)1, (byte)2, "1", "2", Exp(p => p.UInt8), nameof(Person.UInt8) },
            new object[] { (short)1, (short)2, "1", "2", Exp(p => p.Int16), nameof(Person.Int16) },
            new object[] { (ushort)1, (ushort)2, "1", "2", Exp(p => p.UInt16), nameof(Person.UInt16) },
            new object[] { (int)1, (int)2, "1", "2", Exp(p => p.Int32), nameof(Person.Int32) },
            new object[] { (uint)1, (uint)2, "1", "2", Exp(p => p.UInt32), nameof(Person.UInt32) },
            new object[] { long.MinValue, long.MaxValue, "NumberLong(\"-9223372036854775808\")", "NumberLong(\"9223372036854775807\")", Exp(p => p.Int64), nameof(Person.Int64) },
            new object[] { (float)1, (float)2, "1", "2", Exp(p => p.Float), nameof(Person.Float) },
            new object[] { (double)1, (double)2, "1", "2", Exp(p => p.Double), nameof(Person.Double) },
            new object[] { DateTime.MinValue, DateTime.MaxValue, "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")", Exp(p => p.Birthday), "dob" },
            new object[] { DateTimeOffset.MinValue, DateTimeOffset.MaxValue, "ISODate(\"0001-01-01T00:00:00Z\")", "ISODate(\"9999-12-31T23:59:59.999Z\")", Exp(p => p.DateTimeOffset), nameof(Person.DateTimeOffset) }
        };

        [Fact]
        public void Range_should_use_correct_serializers_when_using_attributes_and_expression_path()
        {
            var testLong = 23;
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Range(t => t.DefaultLong, new SearchRange<long>(testLong, null, false, false )),
                """{"range" :{ "gt" : 23, "path" : "DefaultLong" }}""");

            AssertRendered(
                subjectTyped.Range(t => t.StringLong, new SearchRange<long>(testLong, null, false, false )),
                """{"range":{ "gt" : "23", "path" : "StringLong" }}""");
        }

        [Fact]
        public void Range_should_use_correct_serializers_when_using_attributes_and_string_path()
        {
            var testLong = 23;
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Range("DefaultLong", new SearchRange<long>(testLong, null, false, false )),
                """{"range" :{ "gt" : 23, "path" : "DefaultLong" }}""");

            AssertRendered(
                subjectTyped.Range("StringLong", new SearchRange<long>(testLong, null, false, false )),
                """{"range":{ "gt" : "23", "path" : "StringLong" }}""");
        }

        [Fact(Skip = "This should only be run manually due to the use of BsonSerializer.RegisterSerializer")]
        public void Range_should_use_correct_serializers_when_using_serializer_registry()
        {
            BsonSerializer.RegisterSerializer(new Int64Serializer(BsonType.String));

            var testLong = 23;
            var subjectTyped = CreateSubject<AttributesTestClass>();

            AssertRendered(
                subjectTyped.Range(t => t.DefaultLong, new SearchRange<long>(testLong, null, false, false )),
                """{"range":{ "gt" : "23", "path" : "DefaultLong" }}""");
        }

        [Fact]
        public void Range_with_array_field_should_render_correctly()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Range(x => x.SalaryHistory, SearchRangeBuilder.Gte(1000).Lt(2000)),
                "{ range: { path: 'salaries', gte: 1000, lt: 2000 } }");
        }

        [Fact]
        public void Regex()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Regex("x", "foo"),
                "{ regex: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Regex(new[] { "x", "y" }, "foo"),
                "{ regex: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Regex("x", new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Regex(new[] { "x", "y" }, new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Regex("x", "foo", false),
                "{ regex: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Regex("x", "foo", true),
                "{ regex: { query: 'foo', path: 'x', allowAnalyzedField: true } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Regex("x", "foo", score: scoreBuilder.Constant(1)),
                "{ regex: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Regex_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Regex(x => x.FirstName, "foo"),
                "{ regex: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Regex("FirstName", "foo"),
                "{ regex: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Regex(x => x.Hobbies, "foo"),
                "{ regex: { query: 'foo', path: 'hobbies' } }");

            AssertRendered(
                subject.Regex(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    "foo"),
                "{ regex: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Regex(new[] { "FirstName", "LastName" }, "foo"),
                "{ regex: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Regex(x => x.FirstName, new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Regex("FirstName", new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Regex(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Regex(new[] { "FirstName", "LastName" }, new[] { "foo", "bar" }),
                "{ regex: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void Should()
        {
            var subject = CreateSubject<Person>();

            AssertRendered<Person>(
                subject.Compound()
                    .Should(
                        subject.Exists(p => p.Age),
                        subject.Exists(p => p.Birthday))
                    .MinimumShouldMatch(2),
                "{ compound: { should: [{ exists: { path: 'age' } }, { exists: { path: 'dob' } }], minimumShouldMatch: 2 } }");
        }

        [Fact]
        public void Span()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Span(Builders<Person>.SearchSpan
                        .First(Builders<Person>.SearchSpan.Term(p => p.Age, "foo"), 5)),
                "{ span: { first: { operator: { term: { query: 'foo', path: 'age' } }, endPositionLte: 5 } } }");
        }

        [Fact]
        public void Text()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Text("x", "foo"),
                "{ text: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Text(new[] { "x", "y" }, "foo"),
                "{ text: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Text("x", new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Text(new[] { "x", "y" }, new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Text(new[] { "x", "y" }, new[] { "foo", "bar" }, "testSynonyms"),
                "{ text: { query: ['foo', 'bar'], synonyms: 'testSynonyms', path: ['x', 'y'] } }");

            AssertRendered(
                subject.Text(new[] { "x", "y" }, new[] { "foo", "bar" }, new SearchTextOptions<BsonDocument>{ MatchCriteria = MatchCriteria.Any }),
                "{ text: { query: ['foo', 'bar'], matchCriteria: 'any', path: ['x', 'y'] } }");

            AssertRendered(
                subject.Text("x", "foo", new SearchFuzzyOptions()),
                "{ text: { query: 'foo', path: 'x', fuzzy: {} } }");
            AssertRendered(
                subject.Text("x", "foo", new SearchFuzzyOptions()
                {
                    MaxEdits = 1,
                    PrefixLength = 5,
                    MaxExpansions = 25
                }),
                "{ text: { query: 'foo', path: 'x', fuzzy: { maxEdits: 1, prefixLength: 5, maxExpansions: 25 } } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Text("x", "foo", score: scoreBuilder.Constant(1)),
                "{ text: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
            AssertRendered(
                subject.Text("x", "foo", "testSynonyms", scoreBuilder.Constant(1)),
                "{ text: { query: 'foo', synonyms: 'testSynonyms', path: 'x', score: { constant: { value: 1 } } } }");

            AssertRendered(
                subject.Text("x", "foo", new SearchTextOptions<BsonDocument> {Score = scoreBuilder.Constant(1), MatchCriteria = MatchCriteria.All}),
                "{ text: { query: 'foo', matchCriteria: 'all', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Text_should_throw_with_invalid_options()
        {
            var subject = CreateSubject<BsonDocument>();

            Action act = () =>
                subject.Text("x", "foo", new SearchTextOptions<BsonDocument> { MatchCriteria = (MatchCriteria)3 });

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Text_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Text(x => x.FirstName, "foo"),
                "{ text: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Text(x => x.FirstName, "foo", new SearchTextOptions<Person> { MatchCriteria = MatchCriteria.All}),
                "{ text: { query: 'foo', matchCriteria: 'all', path: 'fn' } }");
            AssertRendered(
                subject.Text("FirstName", "foo"),
                "{ text: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Text(x => x.Hobbies, "foo"),
                "{ text: { query: 'foo', path: 'hobbies' } }");

            AssertRendered(
                subject.Text(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    "foo"),
                "{ text: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Text(new[] { "FirstName", "LastName" }, "foo"),
                "{ text: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Text(x => x.FirstName, new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Text(x => x.FirstName, new[] { "foo", "bar" }, "testSynonyms"),
                "{ text: { query: ['foo', 'bar'], synonyms: 'testSynonyms', path: 'fn' } }");
            AssertRendered(
                subject.Text("FirstName", new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Text(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Text(new[] { "FirstName", "LastName" }, new[] { "foo", "bar" }),
                "{ text: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void Wildcard()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Wildcard("x", "foo"),
                "{ wildcard: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Wildcard(new[] { "x", "y" }, "foo"),
                "{ wildcard: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Wildcard("x", new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Wildcard(new[] { "x", "y" }, new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Wildcard("x", "foo", false),
                "{ wildcard: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Wildcard("x", "foo", true),
                "{ wildcard: { query: 'foo', path: 'x', allowAnalyzedField: true } }");

            var scoreBuilder = new SearchScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Wildcard("x", "foo", score: scoreBuilder.Constant(1)),
                "{ wildcard: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Wildcard_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Wildcard(x => x.FirstName, "foo"),
                "{ wildcard: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Wildcard("FirstName", "foo"),
                "{ wildcard: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Wildcard(x => x.Hobbies, "foo"),
                "{ wildcard: { query: 'foo', path: 'hobbies' } }");

            AssertRendered(
                subject.Wildcard(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    "foo"),
                "{ wildcard: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Wildcard(new[] { "FirstName", "LastName" }, "foo"),
                "{ wildcard: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Wildcard(x => x.FirstName, new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Wildcard("FirstName", new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Wildcard(
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    },
                    new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Wildcard(new[] { "FirstName", "LastName" }, new[] { "foo", "bar" }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, string expected) =>
            AssertRendered(query, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedQuery = query.Render(new RenderArgs<TDocument>(documentSerializer, BsonSerializer.SerializerRegistry));

            renderedQuery.Should().BeEquivalentTo(expected);
        }

        private SearchDefinitionBuilder<TDocument> CreateSubject<TDocument>() => new SearchDefinitionBuilder<TDocument>();

        private static Expression<Func<Person, T>> Exp<T>(Expression<Func<Person, T>> expression) => expression;

        public class Person : SimplePerson
        {
            public byte UInt8 { get; set; }
            public sbyte Int8 { get; set; }
            public short Int16 { get; set; }
            public ushort UInt16 { get; set; }
            public int Int32 { get; set; }
            public uint UInt32 { get; set; }
            public long Int64 { get; set; }
            public ulong UInt64 { get; set; }
            public float Float { get; set; }
            public double Double { get; set; }
            public decimal Decimal { get; set; }

            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid Guid { get; set; }
            public DateTimeOffset DateTimeOffset { get; set; }
            public TimeSpan TimeSpan { get; set; }

            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("dob")]
            public DateTime Birthday { get; set; }

            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("location")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }

            [BsonElement("ret")]
            public bool Retired { get; set; }
            
            [BsonElement("hobbies")]
            public string[] Hobbies { get; set; }

            [BsonElement("salaries")]
            public int[] SalaryHistory { get; set; }
            
            public object Object { get; set; }

            public string Name { get; set; }
        }

        public class Family
        {
            public SimplePerson Parent1 { get; set; }
            public SimplePerson Parent2 { get; set; }

            public SimplePerson[] Children { get; set; }

            public Family[] Relatives { get; set; }
        }

        public class SimplePerson
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }

        public class SimplestPerson
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }
        }

        public class AttributesTestClass
        {
            [BsonGuidRepresentation(GuidRepresentation.Standard)]
            public Guid DefaultGuid { get; set; }

            [BsonRepresentation(BsonType.String)]
            public Guid StringGuid { get; set; }

            public Guid UndefinedRepresentationGuid { get; set; }

            public long DefaultLong { get; set; }

            [BsonRepresentation(BsonType.String)]
            public long StringLong { get; set; }
        }
    }
}
