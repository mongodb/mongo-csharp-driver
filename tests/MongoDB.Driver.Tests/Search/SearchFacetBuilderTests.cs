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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchFacetBuilderTests
    {
        [Fact]
        public void Date()
        {
            var subject = CreateSubject<BsonDocument>();
            var boundaries = new List<DateTime>()
            {
                DateTime.MinValue,
                DateTime.MaxValue
            };

            AssertRendered(
                subject.Date("date", "x", boundaries, "foo"),
                "{ type: 'date', path: 'x', boundaries: [{ $date: '0001-01-01T00:00:00Z' }, { $date: '9999-12-31T23:59:59.9999999Z' }], default: 'foo' }");
            AssertRendered(
                subject.Date("date", "x", boundaries),
                "{ type: 'date', path: 'x', boundaries: [{ $date: '0001-01-01T00:00:00Z' }, { $date: '9999-12-31T23:59:59.9999999Z' }] }");
        }

        [Fact]
        public void Date_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Date("date", x => x.Birthday, DateTime.MinValue, DateTime.MaxValue),
                "{ type: 'date', path: 'dob', boundaries: [{ $date: '0001-01-01T00:00:00Z' }, { $date: '9999-12-31T23:59:59.9999999Z' }] }");
            AssertRendered(
                subject.Date("date", "Birthday", DateTime.MinValue, DateTime.MaxValue),
                "{ type: 'date', path: 'dob', boundaries: [{ $date: '0001-01-01T00:00:00Z' }, { $date: '9999-12-31T23:59:59.9999999Z' }] }");
        }

        [Fact]
        public void Number()
        {
            var subject = CreateSubject<BsonDocument>();
            var boundaries = new List<BsonValue>()
            {
                0,
                50,
                100
            };

            AssertRendered(
                subject.Number("number", "x", boundaries, "foo"),
                "{ type: 'number', path: 'x', boundaries: [0, 50, 100], default: 'foo' }");
            AssertRendered(
                subject.Number("number", "x", boundaries),
                "{ type: 'number', path: 'x', boundaries: [0, 50, 100] }");
        }

        [Fact]
        public void Number_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Number("number", x => x.Age, 0, 18, 65, 120),
                "{ type: 'number', path: 'age', boundaries: [0, 18, 65, 120] }");
            AssertRendered(
                subject.Number("number", "Age", 0, 18, 65, 120),
                "{ type: 'number', path: 'age', boundaries: [0, 18, 65, 120] }");
        }

        [Fact]
        public void String()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.String("string", "x", 100),
                "{ type: 'string', path: 'x', numBuckets: 100 }");
        }

        [Fact]
        public void String_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.String("string", x => x.FirstName, 100),
                "{ type: 'string', path: 'fn', numBuckets: 100 }");
            AssertRendered(
                subject.String("string", "FirstName", 100),
                "{ type: 'string', path: 'fn', numBuckets: 100 }");
        }

        private void AssertRendered<TDocument>(SearchFacet<TDocument> facet, string expected) =>
            AssertRendered(facet, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SearchFacet<TDocument> facet, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedFacet = facet.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedFacet.Should().BeEquivalentTo(expected);
        }

        private SearchFacetBuilder<TDocument> CreateSubject<TDocument>() =>
            new SearchFacetBuilder<TDocument>();

        private class Person
        {
            [BsonElement("age")]
            public int Age { get; set; }

            [BsonElement("dob")]
            public DateTime Birthday { get; set; }

            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }
    }
}
