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

using System.Collections.Generic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SpanDefinitionBuilderTests
    {
        [Fact]
        public void First()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.First(subject.Term("foo", "x"), 5),
                "{ first: { operator: { term: { query: 'foo', path: 'x' } }, endPositionLte: 5 } }");
        }

        [Fact]
        public void First_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.First(subject.Term("born", x => x.Biography), 5),
                "{ first: { operator: { term: { query: 'born', path: 'bio' } }, endPositionLte: 5 } }");
            AssertRendered(
                subject.First(subject.Term("born", "Biography"), 5),
                "{ first: { operator: { term: { query: 'born', path: 'bio' } }, endPositionLte: 5 } }");
        }

        [Fact]
        public void Near()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Near(
                    new List<SpanDefinition<BsonDocument>>()
                    {
                        subject.Term("foo", "x"),
                        subject.Term("bar", "x")
                    },
                    5,
                    inOrder: true),
                "{ near: { clauses: [{ term: { query: 'foo', path: 'x' } }, { term: { query: 'bar', path: 'x' } }], slop: 5, inOrder: true } }");
        }

        [Fact]
        public void Near_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Near(
                    new List<SpanDefinition<Person>>()
                    {
                        subject.Term("born", x => x.Biography),
                        subject.Term("school", x => x.Biography)
                    },
                    5,
                    inOrder: true),
                "{ near: { clauses: [{ term: { query: 'born', path: 'bio' } }, { term: { query: 'school', path: 'bio' } }], slop: 5, inOrder: true } }");
            AssertRendered(
                subject.Near(
                    new List<SpanDefinition<Person>>()
                    {
                        subject.Term("born", "Biography"),
                        subject.Term("school", "Biography")
                    },
                    5,
                    inOrder: true),
                "{ near: { clauses: [{ term: { query: 'born', path: 'bio' } }, { term: { query: 'school', path: 'bio' } }], slop: 5, inOrder: true } }");
        }

        [Fact]
        public void Or()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Or(
                    subject.Term("foo", "x"),
                    subject.Term("bar", "x")),
                "{ or: { clauses: [{ term: { query: 'foo', path: 'x' } }, { term: { query: 'bar', path: 'x' } }] } }");
        }

        [Fact]
        public void Or_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Or(
                    subject.Term("engineer", x => x.Biography),
                    subject.Term("developer", x => x.Biography)),
                "{ or: { clauses: [{ term: { query: 'engineer', path: 'bio' } }, { term: { query: 'developer', path: 'bio' } }] } }");
            AssertRendered(
                subject.Or(
                    subject.Term("engineer", "Biography"),
                    subject.Term("developer", "Biography")),
                "{ or: { clauses: [{ term: { query: 'engineer', path: 'bio' } }, { term: { query: 'developer', path: 'bio' } }] } }");
        }

        [Fact]
        public void Subtract()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Subtract(
                    subject.Term("foo", "x"),
                    subject.Term("bar", "x")),
                "{ subtract: { include: { term: { query: 'foo', path: 'x' } }, exclude: { term: { query: 'bar', path: 'x' } } } }");
        }

        [Fact]
        public void Subtract_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Subtract(
                    subject.Term("engineer", x => x.Biography),
                    subject.Term("train", x => x.Biography)),
                "{ subtract: { include: { term: { query: 'engineer', path: 'bio' } }, exclude: { term: { query: 'train', path: 'bio' } } } }");
            AssertRendered(
                subject.Subtract(
                    subject.Term("engineer", "Biography"),
                    subject.Term("train", "Biography")),
                "{ subtract: { include: { term: { query: 'engineer', path: 'bio' } }, exclude: { term: { query: 'train', path: 'bio' } } } }");
        }

        private void AssertRendered<TDocument>(SpanDefinition<TDocument> span, string expected) =>
            AssertRendered(span, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SpanDefinition<TDocument> span, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedSpan = span.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedSpan.Should().BeEquivalentTo(expected);
        }

        private SpanDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>
            new SpanDefinitionBuilder<TDocument>();

        private class Person
        {
            [BsonElement("bio")]
            public string Biography { get; set; }
        }
    }
}
