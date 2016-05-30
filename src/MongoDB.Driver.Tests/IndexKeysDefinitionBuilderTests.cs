/* Copyright 2010-2015 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class IndexKeysDefinitionBuilderTests
    {
        [Fact]
        public void Ascending()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Ascending("a"), "{a: 1}");
        }

        [Fact]
        public void Ascending_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Ascending(x => x.FirstName), "{fn: 1}");
            Assert(subject.Ascending("FirstName"), "{fn: 1}");
        }

        [Fact]
        public void Combine()
        {
            var subject = CreateSubject<BsonDocument>();

            var result = subject.Combine(
                "{a: 1, b: -1}",
                subject.Descending("c"));

            Assert(result, "{a: 1, b: -1, c: -1}");
        }

        [Fact]
        public void Combine_with_repeated_fields()
        {
            var subject = CreateSubject<BsonDocument>();

            var combined = subject.Combine(
                "{a: 1, b: -1}",
                subject.Descending("a"));

            Action act = () => Render(combined);
            
            act.ShouldThrow<MongoException>();
        }

        [Fact]
        public void Combine_many_texts()
        {
            var subject = CreateSubject<BsonDocument>();

            var result = subject.Combine(
                subject.Text("a"),
                subject.Text("b"),
                subject.Text("c"));

            Assert(result, "{a: 'text', b: 'text', c: 'text'}");
        }

        [Fact]
        public void Combine_with_repeated_fields_using_extension_methods()
        {
            var subject = CreateSubject<BsonDocument>();

            var result = subject.Ascending("a")
                .Ascending("b")
                .Descending("c")
                .Descending("a");

            Action act = () => Render(result);

            act.ShouldThrow<MongoException>();
        }

        [Fact]
        public void Descending()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Descending("a"), "{a: -1}");
        }

        [Fact]
        public void Descending_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Descending(x => x.FirstName), "{fn: -1}");
            Assert(subject.Descending("FirstName"), "{fn: -1}");
        }

        [Fact]
        public void Geo2D()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Geo2D("a"), "{a: '2d'}");
        }

        [Fact]
        public void Geo2D_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Geo2D(x => x.FirstName), "{fn: '2d'}");
            Assert(subject.Geo2D("FirstName"), "{fn: '2d'}");
        }

        [Fact]
        public void GeoHaystack()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.GeoHaystack("a"), "{a: 'geoHaystack'}");
            Assert(subject.GeoHaystack("a", "b"), "{a: 'geoHaystack', b: 1 }");
        }

        [Fact]
        public void GeoHaystack_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.GeoHaystack(x => x.FirstName), "{fn: 'geoHaystack'}");
            Assert(subject.GeoHaystack(x => x.FirstName, x => x.LastName), "{fn: 'geoHaystack', ln: 1}");
            Assert(subject.GeoHaystack("FirstName", "LastName"), "{fn: 'geoHaystack', ln: 1}");
        }

        [Fact]
        public void Geo2DSphere()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Geo2DSphere("a"), "{a: '2dsphere'}");
        }

        [Fact]
        public void Geo2DSphere_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Geo2DSphere(x => x.FirstName), "{fn: '2dsphere'}");
            Assert(subject.Geo2DSphere("FirstName"), "{fn: '2dsphere'}");
        }

        [Fact]
        public void Text()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Text("a"), "{a: 'text'}");
            Assert(subject.Text("$**"), "{'$**': 'text'}");
        }

        [Fact]
        public void Text_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Text(x => x.FirstName), "{fn: 'text'}");
            Assert(subject.Text("FirstName"), "{fn: 'text'}");
            Assert(subject.Text("$**"), "{'$**': 'text'}");
        }

        private void Assert<TDocument>(IndexKeysDefinition<TDocument> keys, string expectedJson)
        {
            var renderedSort = Render<TDocument>(keys);

            renderedSort.Should().Be(expectedJson);
        }

        private BsonDocument Render<TDocument>(IndexKeysDefinition<TDocument> keys)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            return keys.Render(documentSerializer, BsonSerializer.SerializerRegistry);
        }

        private IndexKeysDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new IndexKeysDefinitionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }
    }
}
