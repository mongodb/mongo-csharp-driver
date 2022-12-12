// Copyright 2010-present MongoDB Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchDefinitionBuilderTests
    {
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

        [Fact]
        public void Autocomplete()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Autocomplete("foo", "x"),
                "{ autocomplete: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Autocomplete("foo", new[] { "x", "y" }),
                "{ autocomplete: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Autocomplete(new[] { "foo", "bar" }, "x"),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Autocomplete(new[] { "foo", "bar" }, new[] { "x", "y" }),
                "{ autocomplete: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Autocomplete("foo", "x", AutocompleteTokenOrder.Any),
                "{ autocomplete: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Autocomplete("foo", "x", AutocompleteTokenOrder.Sequential),
                "{ autocomplete: { query: 'foo', path: 'x', tokenOrder: 'sequential' } }");

            AssertRendered(
                subject.Autocomplete("foo", "x", fuzzy: new FuzzyOptions()),
                "{ autocomplete: { query: 'foo', path: 'x', fuzzy: {} } }");
            AssertRendered(
                subject.Autocomplete("foo", "x", fuzzy: new FuzzyOptions()
                {
                    MaxEdits = 1,
                    PrefixLength = 5,
                    MaxExpansions = 25
                }),
                "{ autocomplete: { query: 'foo', path: 'x', fuzzy: { maxEdits: 1, prefixLength: 5, maxExpansions: 25 } } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Autocomplete("foo", "x", score: scoreBuilder.Constant(1)),
                "{ autocomplete: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Autocomplete_Typed()
        {
            var subject = CreateSubject<Person>();
            AssertRendered(
                subject.Autocomplete("foo", x => x.FirstName),
                "{ autocomplete: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Autocomplete("foo", "FirstName"),
                "{ autocomplete: { query: 'foo', path: 'fn' } }");

            AssertRendered(
                subject.Autocomplete(
                    "foo",
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ autocomplete: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Autocomplete("foo", new[] { "FirstName", "LastName" }),
                "{ autocomplete: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Autocomplete(new[] { "foo", "bar" }, x => x.FirstName),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Autocomplete(new[] { "foo", "bar" }, "FirstName"),
                "{ autocomplete: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Autocomplete(
                    new[] { "foo", "bar" },
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ autocomplete: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Autocomplete(new[] { "foo", "bar" }, new[] { "FirstName", "LastName" }),
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
        }

        [Fact]
        public void Equals()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Equals("x", true),
                "{ equals: { path: 'x', value: true } }");
            AssertRendered(
                subject.Equals("x", ObjectId.Empty),
                "{ equals: { path: 'x', value: { $oid: '000000000000000000000000' } } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Equals("x", true, scoreBuilder.Constant(1)),
                "{ equals: { path: 'x', value: true, score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Equals_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Equals(x => x.Retired, true),
                "{ equals: { path: 'ret', value: true } }");
            AssertRendered(
                subject.Equals("Retired", true),
                "{ equals: { path: 'ret', value: true } }");

            AssertRendered(
                subject.Equals(x => x.Id, ObjectId.Empty),
                "{ equals: { path: '_id', value: { $oid: '000000000000000000000000' } } }");
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
        public void Exists_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Exists(x => x.FirstName),
                "{ exists: { path: 'fn' } }");
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
                    subject.Phrase("foo", "x"),
                    facetBuilder.String("string", "y", 100)),
                "{ facet: { operator: { phrase: { query: 'foo', path: 'x' } }, facets: { string: { type: 'string', path: 'y', numBuckets: 100 } } } }");
        }

        [Fact]
        public void Facet_Typed()
        {
            var subject = CreateSubject<Person>();
            var facetBuilder = new SearchFacetBuilder<Person>();

            AssertRendered(
                subject.Facet(
                    subject.Phrase("foo", x => x.LastName),
                    facetBuilder.String("string", x => x.FirstName, 100)),
                "{ facet: { operator: { phrase: { query: 'foo', path: 'ln' } }, facets: { string: { type: 'string', path: 'fn', numBuckets: 100 } } } }");
            AssertRendered(
                subject.Facet(
                    subject.Phrase("foo", "LastName"),
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
        public void GeoShape()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.GeoShape(
                    __testPolygon,
                    "location",
                    GeoShapeRelation.Disjoint),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
        }

        [Fact]
        public void GeoShape_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.GeoShape(
                    __testPolygon,
                    x => x.Location,
                    GeoShapeRelation.Disjoint),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
            AssertRendered(
                subject.GeoShape(
                    __testPolygon,
                    "Location",
                    GeoShapeRelation.Disjoint),
                "{ geoShape: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location', relation: 'disjoint' } }");
        }

        [Fact]
        public void GeoWithin()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.GeoWithin(__testPolygon, "location"),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin(__testBox, "location"),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin(__testCircle, "location"),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
        }

        [Fact]
        public void GeoWithin_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.GeoWithin(__testPolygon, x => x.Location),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin(__testPolygon, "Location"),
                "{ geoWithin: { geometry: { type: 'Polygon', coordinates: [[[-161.323242, 22.512557], [-152.446289, 22.065278], [-156.09375, 17.811456], [-161.323242, 22.512557]]] }, path: 'location' } }");

            AssertRendered(
                subject.GeoWithin(__testBox, x => x.Location),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin(__testBox, "Location"),
                "{ geoWithin: { box: { bottomLeft: { type: 'Point', coordinates: [-161.323242, 22.065278] }, topRight: { type: 'Point', coordinates: [-152.446289, 22.512557] } }, path: 'location' } }");

            AssertRendered(
                subject.GeoWithin(__testCircle, x => x.Location),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
            AssertRendered(
                subject.GeoWithin(__testCircle, "Location"),
                "{ geoWithin: { circle: { center: { type: 'Point', coordinates: [-161.323242, 22.512557] }, radius: 7.5 }, path: 'location' } }");
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
        }

        [Fact]
        public void MoreLikeThis_Typed()
        {
            var subject = CreateSubject<SimplePerson>();

            AssertRendered(
                subject.MoreLikeThis(
                    new SimplePerson
                    {
                        FirstName = "John",
                        LastName = "Doe"
                    },
                    new SimplePerson
                    {
                        FirstName = "Jane",
                        LastName = "Doe"
                    }),
                "{ moreLikeThis: { like: [{ fn: 'John', ln: 'Doe' }, { fn: 'Jane', ln: 'Doe' }] } }");
        }

        [Fact]
        public void Must()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered<BsonDocument>(
                subject.Compound().Must(
                    subject.Exists("x"),
                    subject.Exists("y")),
                "{ compound: { must: [{ exists: { path: 'x' } }, { exists: { path: 'y' } }] } }");
        }

        [Fact]
        public void MustNot()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered<BsonDocument>(
                subject.Compound().MustNot(
                    subject.Exists("x"),
                    subject.Exists("y")),
                "{ compound: { mustNot: [{ exists: { path: 'x' } }, { exists: { path: 'y' } }] } }");
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

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Near("x", 5.0, 1.0, scoreBuilder.Constant(1)),
                "{ near: { path: 'x', origin: 5, pivot: 1, score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Near_Typed()
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
                subject.Phrase("foo", "x"),
                "{ phrase: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Phrase("foo", new[] { "x", "y" }),
                "{ phrase: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Phrase(new[] { "foo", "bar" }, "x"),
                "{ phrase: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Phrase(new[] { "foo", "bar" }, new[] { "x", "y" }),
                "{ phrase: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Phrase("foo", "x", 5),
                "{ phrase: { query: 'foo', path: 'x', slop: 5 } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Phrase("foo", "x", score: scoreBuilder.Constant(1)),
                "{ phrase: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Phrase_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Phrase("foo", x => x.FirstName),
                "{ phrase: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Phrase("foo", "FirstName"),
                "{ phrase: { query: 'foo', path: 'fn' } }");

            AssertRendered(
                subject.Phrase(
                    "foo",
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ phrase: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Phrase("foo", new[] { "FirstName", "LastName" }),
                "{ phrase: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Phrase(new[] { "foo", "bar" }, x => x.FirstName),
                "{ phrase: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Phrase(new[] { "foo", "bar" }, "FirstName"),
                "{ phrase: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Phrase(
                    new[] { "foo", "bar" },
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ phrase: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Phrase(new[] { "foo", "bar" }, new[] { "FirstName", "LastName" }),
                "{ phrase: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void QueryString()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.QueryString("x", "foo"),
                "{ queryString: { defaultPath: 'x', query: 'foo' } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.QueryString("x", "foo", scoreBuilder.Constant(1)),
                "{ queryString: { defaultPath: 'x', query: 'foo', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void QueryString_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.QueryString(x => x.FirstName, "foo"),
                "{ queryString: { defaultPath: 'fn', query: 'foo' } }");
            AssertRendered(
                subject.QueryString("FirstName", "foo"),
                "{ queryString: { defaultPath: 'fn', query: 'foo' } }");
        }

        [Fact]
        public void RangeDateTime()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Range(SearchRangeBuilder
                    .Gte(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .Lte(new DateTime(2009, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
                    "x"),
                "{ range: { path: 'x', gte: { $date: '2000-01-01T00:00:00Z' }, lte: { $date: '2009-12-31T00:00:00Z' } } }");
        }

        [Fact]
        public void RangeDouble()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Range(SearchRangeBuilder.Gt(1.5).Lt(2.5), "x"),
                "{ range: { path: 'x', gt: 1.5, lt: 2.5 } }");
        }

        [Fact]
        public void RangeInt32()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Range(SearchRangeBuilder.Gt(1).Lt(10), "x"),
                "{ range: { path: 'x', gt: 1, lt: 10 } }");
            AssertRendered(
                subject.Range(SearchRangeBuilder.Lt(10).Gt(1), "x"),
                "{ range: { path: 'x', gt: 1, lt: 10 } }");
            AssertRendered(
                subject.Range(SearchRangeBuilder.Gte(1).Lte(10), "x"),
                "{ range: { path: 'x', gte: 1, lte: 10 } }");
            AssertRendered(
                subject.Range(SearchRangeBuilder.Lte(10).Gte(1), "x"),
                "{ range: { path: 'x', gte: 1, lte: 10 } }");
        }

        [Fact]
        public void RangeInt32_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Range(SearchRangeBuilder.Gte(18).Lt(65), x => x.Age),
                "{ range: { path: 'age', gte: 18, lt: 65 } }");
            AssertRendered(
                subject.Range(SearchRangeBuilder.Gte(18).Lt(65), "Age"),
                "{ range: { path: 'age', gte: 18, lt: 65 } }");
        }

        [Fact]
        public void Regex()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Regex("foo", "x"),
                "{ regex: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Regex("foo", new[] { "x", "y" }),
                "{ regex: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Regex(new[] { "foo", "bar" }, "x"),
                "{ regex: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Regex(new[] { "foo", "bar" }, new[] { "x", "y" }),
                "{ regex: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Regex("foo", "x", false),
                "{ regex: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Regex("foo", "x", true),
                "{ regex: { query: 'foo', path: 'x', allowAnalyzedField: true } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Regex("foo", "x", score: scoreBuilder.Constant(1)),
                "{ regex: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Regex_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Regex("foo", x => x.FirstName),
                "{ regex: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Regex("foo", "FirstName"),
                "{ regex: { query: 'foo', path: 'fn' } }");

            AssertRendered(
                subject.Regex(
                    "foo",
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ regex: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Regex("foo", new[] { "FirstName", "LastName" }),
                "{ regex: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Regex(new[] { "foo", "bar" }, x => x.FirstName),
                "{ regex: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Regex(new[] { "foo", "bar" }, "FirstName"),
                "{ regex: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Regex(
                    new[] { "foo", "bar" },
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ regex: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Regex(new[] { "foo", "bar" }, new[] { "FirstName", "LastName" }),
                "{ regex: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void Should()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered<BsonDocument>(
                subject.Compound()
                    .Should(
                        subject.Exists("x"),
                        subject.Exists("y"))
                    .MinimumShouldMatch(2),
                "{ compound: { should: [{ exists: { path: 'x' } }, { exists: { path: 'y' } }], minimumShouldMatch: 2 } }");
        }

        [Fact]
        public void Span()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Span(Builders<BsonDocument>.Span
                        .First(Builders<BsonDocument>.Span.Term("foo", "x"), 5)),
                "{ span: { first: { operator: { term: { query: 'foo', path: 'x' } }, endPositionLte: 5 } } }");
        }

        [Fact]
        public void Text()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Text("foo", "x"),
                "{ text: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Text("foo", new[] { "x", "y" }),
                "{ text: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Text(new[] { "foo", "bar" }, "x"),
                "{ text: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Text(new[] { "foo", "bar" }, new[] { "x", "y" }),
                "{ text: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Text("foo", "x", new FuzzyOptions()),
                "{ text: { query: 'foo', path: 'x', fuzzy: {} } }");
            AssertRendered(
                subject.Text("foo", "x", new FuzzyOptions()
                {
                    MaxEdits = 1,
                    PrefixLength = 5,
                    MaxExpansions = 25
                }),
                "{ text: { query: 'foo', path: 'x', fuzzy: { maxEdits: 1, prefixLength: 5, maxExpansions: 25 } } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Text("foo", "x", score: scoreBuilder.Constant(1)),
                "{ text: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Text_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Text("foo", x => x.FirstName),
                "{ text: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Text("foo", "FirstName"),
                "{ text: { query: 'foo', path: 'fn' } }");

            AssertRendered(
                subject.Text(
                    "foo",
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ text: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Text("foo", new[] { "FirstName", "LastName" }),
                "{ text: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Text(new[] { "foo", "bar" }, x => x.FirstName),
                "{ text: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Text(new[] { "foo", "bar" }, "FirstName"),
                "{ text: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Text(
                    new[] { "foo", "bar" },
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ text: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Text(new[] { "foo", "bar" }, new[] { "FirstName", "LastName" }),
                "{ text: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        [Fact]
        public void Wildcard()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Wildcard("foo", "x"),
                "{ wildcard: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Wildcard("foo", new[] { "x", "y" }),
                "{ wildcard: { query: 'foo', path: ['x', 'y'] } }");
            AssertRendered(
                subject.Wildcard(new[] { "foo", "bar" }, "x"),
                "{ wildcard: { query: ['foo', 'bar'], path: 'x' } }");
            AssertRendered(
                subject.Wildcard(new[] { "foo", "bar" }, new[] { "x", "y" }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['x', 'y'] } }");

            AssertRendered(
                subject.Wildcard("foo", "x", false),
                "{ wildcard: { query: 'foo', path: 'x' } }");
            AssertRendered(
                subject.Wildcard("foo", "x", true),
                "{ wildcard: { query: 'foo', path: 'x', allowAnalyzedField: true } }");

            var scoreBuilder = new ScoreDefinitionBuilder<BsonDocument>();
            AssertRendered(
                subject.Wildcard("foo", "x", score: scoreBuilder.Constant(1)),
                "{ wildcard: { query: 'foo', path: 'x', score: { constant: { value: 1 } } } }");
        }

        [Fact]
        public void Wildcard_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Wildcard("foo", x => x.FirstName),
                "{ wildcard: { query: 'foo', path: 'fn' } }");
            AssertRendered(
                subject.Wildcard("foo", "FirstName"),
                "{ wildcard: { query: 'foo', path: 'fn' } }");

            AssertRendered(
                subject.Wildcard(
                    "foo",
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ wildcard: { query: 'foo', path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Wildcard("foo", new[] { "FirstName", "LastName" }),
                "{ wildcard: { query: 'foo', path: ['fn', 'ln'] } }");

            AssertRendered(
                subject.Wildcard(new[] { "foo", "bar" }, x => x.FirstName),
                "{ wildcard: { query: ['foo', 'bar'], path: 'fn' } }");
            AssertRendered(
                subject.Wildcard(new[] { "foo", "bar" }, "FirstName"),
                "{ wildcard: { query: ['foo', 'bar'], path: 'fn' } }");

            AssertRendered(
                subject.Wildcard(
                    new[] { "foo", "bar" },
                    new FieldDefinition<Person>[]
                    {
                        new ExpressionFieldDefinition<Person, string>(x => x.FirstName),
                        new ExpressionFieldDefinition<Person, string>(x => x.LastName)
                    }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
            AssertRendered(
                subject.Wildcard(new[] { "foo", "bar" }, new[] { "FirstName", "LastName" }),
                "{ wildcard: { query: ['foo', 'bar'], path: ['fn', 'ln'] } }");
        }

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, string expected) =>
            AssertRendered(query, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedQuery = query.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedQuery.Should().BeEquivalentTo(expected);
        }

        private SearchDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>new SearchDefinitionBuilder<TDocument>();

        private class SimplePerson
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }

        private class Person : SimplePerson
        {
            [BsonId]
            public ObjectId Id { get; set; }

            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("ret")]
            public bool Retired { get; set; }

            [BsonElement("dob")]
            public DateTime Birthday { get; set; }

            [BsonElement("location")]
            public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
        }
    }
}
