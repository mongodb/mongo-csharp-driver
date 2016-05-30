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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class SortDefinitionBuilderTests
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

            var sort = subject.Combine(
                "{a: 1, b: -1}",
                subject.Descending("c"));

            Assert(sort, "{a: 1, b: -1, c: -1}");
        }

        [Fact]
        public void Combine_with_repeated_fields()
        {
            var subject = CreateSubject<BsonDocument>();

            var sort = subject.Combine(
                "{a: 1, b: -1}",
                subject.Descending("a"));

            Assert(sort, "{b: -1, a: -1}");
        }

        [Fact]
        public void Combine_with_repeated_fields_using_extension_methods()
        {
            var subject = CreateSubject<BsonDocument>();

            var sort = subject.Ascending("a")
                .Descending("b")
                .Descending("a");

            Assert(sort, "{b: -1, a: -1}");
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
        public void MetaTextScore()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.MetaTextScore("awesome"), "{awesome: {$meta: 'textScore'}}");
        }

        private void Assert<TDocument>(SortDefinition<TDocument> sort, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedSort = sort.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedSort.Should().Be(expectedJson);
        }

        private SortDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new SortDefinitionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }
        }
    }
}
