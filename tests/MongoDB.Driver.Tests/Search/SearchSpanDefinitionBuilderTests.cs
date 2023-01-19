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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchSpanDefinitionBuilderTests
    {
        [Fact]
        public void First()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.First(subject.Term("x", "foo"), 5),
                "{ first: { operator: { term: { query: 'foo', path: 'x' } }, endPositionLte: 5 } }");
        }

        [Fact]
        public void First_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.First(subject.Term(x => x.Biography, "born"), 5),
                "{ first: { operator: { term: { query: 'born', path: 'bio' } }, endPositionLte: 5 } }");
            AssertRendered(
                subject.First(subject.Term("Biography", "born"), 5),
                "{ first: { operator: { term: { query: 'born', path: 'bio' } }, endPositionLte: 5 } }");
        }

        [Fact]
        public void Near()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Near(
                    new List<SearchSpanDefinition<BsonDocument>>()
                    {
                        subject.Term("x", "foo"),
                        subject.Term("x", "bar")
                    },
                    5,
                    inOrder: true),
                "{ near: { clauses: [{ term: { query: 'foo', path: 'x' } }, { term: { query: 'bar', path: 'x' } }], slop: 5, inOrder: true } }");
        }

        [Fact]
        public void Near_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Near(
                    new List<SearchSpanDefinition<Person>>()
                    {
                        subject.Term(x => x.Biography, "born"),
                        subject.Term(x => x.Biography, "school")
                    },
                    5,
                    inOrder: true),
                "{ near: { clauses: [{ term: { query: 'born', path: 'bio' } }, { term: { query: 'school', path: 'bio' } }], slop: 5, inOrder: true } }");
            AssertRendered(
                subject.Near(
                    new List<SearchSpanDefinition<Person>>()
                    {
                        subject.Term("Biography", "born"),
                        subject.Term("Biography", "school")
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
                    subject.Term("x", "foo"),
                    subject.Term("x", "bar")),
                "{ or: { clauses: [{ term: { query: 'foo', path: 'x' } }, { term: { query: 'bar', path: 'x' } }] } }");
        }

        [Fact]
        public void Or_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Or(
                    subject.Term(x => x.Biography, "engineer"),
                    subject.Term(x => x.Biography, "developer")),
                "{ or: { clauses: [{ term: { query: 'engineer', path: 'bio' } }, { term: { query: 'developer', path: 'bio' } }] } }");
            AssertRendered(
                subject.Or(
                    subject.Term("Biography", "engineer"),
                    subject.Term("Biography", "developer")),
                "{ or: { clauses: [{ term: { query: 'engineer', path: 'bio' } }, { term: { query: 'developer', path: 'bio' } }] } }");
        }

        [Fact]
        public void Subtract()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Subtract(
                    subject.Term("x", "foo"),
                    subject.Term("x", "bar")),
                "{ subtract: { include: { term: { query: 'foo', path: 'x' } }, exclude: { term: { query: 'bar', path: 'x' } } } }");
        }

        [Fact]
        public void Subtract_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Subtract(
                    subject.Term(x => x.Biography, "engineer"),
                    subject.Term(x => x.Biography, "train")),
                "{ subtract: { include: { term: { query: 'engineer', path: 'bio' } }, exclude: { term: { query: 'train', path: 'bio' } } } }");
            AssertRendered(
                subject.Subtract(
                    subject.Term("Biography", "engineer"),
                    subject.Term("Biography", "train")),
                "{ subtract: { include: { term: { query: 'engineer', path: 'bio' } }, exclude: { term: { query: 'train', path: 'bio' } } } }");
        }

        private void AssertRendered<TDocument>(SearchSpanDefinition<TDocument> span, string expected) =>
            AssertRendered(span, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SearchSpanDefinition<TDocument> span, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedSpan = span.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedSpan.Should().BeEquivalentTo(expected);
        }

        private SearchSpanDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>
            new SearchSpanDefinitionBuilder<TDocument>();

        private class Person
        {
            [BsonElement("bio")]
            public string Biography { get; set; }
        }
    }
}
