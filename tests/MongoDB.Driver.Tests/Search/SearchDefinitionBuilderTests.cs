// Copyright 2021-present MongoDB Inc.
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
        [Fact]
        public void Text()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(subject.Text("foo", "x"),
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

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, string expected)
        {
            AssertRendered(query, BsonDocument.Parse(expected));
        }

        private void AssertRendered<TDocument>(SearchDefinition<TDocument> query, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedQuery = query.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedQuery.Should().Be(expected);
        }

        private SearchDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new SearchDefinitionBuilder<TDocument>();
        }

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
