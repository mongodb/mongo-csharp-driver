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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class ProjectionDefinitionBuilderTests
    {
        [Fact]
        public void Combine()
        {
            var subject = CreateSubject<Person>();

            var projection = subject.Combine(
                subject.Include(x => x.FirstName),
                subject.Exclude("LastName"));

            Assert(projection, "{fn: 1, LastName: 0}");
        }

        [Fact]
        public void Combine_with_redundant_fields()
        {
            var subject = CreateSubject<Person>();

            var projection = subject.Combine(
                subject.Include(x => x.FirstName),
                subject.Exclude("LastName"),
                subject.Include("fn"));

            Assert(projection, "{LastName: 0, fn: 1}");
        }

        [Fact]
        public void Combine_with_redundant_fields_using_extension_method()
        {
            var subject = CreateSubject<Person>();

            var projection = subject.Include(x => x.FirstName).Exclude("LastName").Include("fn");

            Assert(projection, "{LastName: 0, fn: 1}");
        }

        [Fact]
        public void ElemMatch()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.ElemMatch<BsonDocument>("a", "{b: 1}"), "{a: {$elemMatch: {b: 1}}}");
        }

        [Fact]
        public void ElemMatch_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.ElemMatch<Pet>("Pets", "{Name: 'Fluffy'}"), "{pets: {$elemMatch: {Name: 'Fluffy'}}}");
            Assert(subject.ElemMatch(x => x.Pets, "{Name: 'Fluffy'}"), "{pets: {$elemMatch: {Name: 'Fluffy'}}}");
            Assert(subject.ElemMatch(x => x.Pets, x => x.Name == "Fluffy"), "{pets: {$elemMatch: {name: 'Fluffy'}}}");
        }

        [Fact]
        public void ElemMatch_from_filter()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Include("a.$"), "{'a.$': 1}");
        }

        [Fact]
        public void ElemMatch_from_filter_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Include(x => x.Pets.ElementAt(-1)), "{'pets.$': 1}");
            Assert(subject.Include("Pets.$"), "{'pets.$': 1}");
        }

        [Fact]
        public void Exclude()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Exclude("a"), "{a: 0}");
        }

        [Fact]
        public void Exclude_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Exclude(x => x.FirstName), "{fn: 0}");
            Assert(subject.Exclude("FirstName"), "{fn: 0}");
        }

        [Fact]
        public void Include()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Include("a"), "{a: 1}");
        }

        [Fact]
        public void Include_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Include(x => x.FirstName), "{fn: 1}");
            Assert(subject.Include("FirstName"), "{fn: 1}");
        }

        [Fact]
        public void MetaTextScore()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.MetaTextScore("a"), "{a: {$meta: 'textScore'}}");
        }

        [Fact]
        public void Slice()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Slice("a", 10), "{a: {$slice: 10}}");
        }

        [Fact]
        public void Slice_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Slice(x => x.Pets, 10), "{pets: {$slice: 10}}");
            Assert(subject.Slice("Pets", 10), "{pets: {$slice: 10}}");
        }

        [Fact]
        public void Slice_with_limit()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Slice("a", 10, 20), "{a: {$slice: [10, 20]}}");
        }

        [Fact]
        public void Slice_Typed_with_limit()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Slice(x => x.Pets, 10, 20), "{pets: {$slice: [10, 20]}}");
            Assert(subject.Slice("Pets", 10, 20), "{pets: {$slice: [10, 20]}}");
        }

        private void Assert<TDocument>(ProjectionDefinition<TDocument> projection, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedProjection = projection.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedProjection.Should().Be(expectedJson);
        }

        private ProjectionDefinitionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new ProjectionDefinitionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }
        
            [BsonElement("pets")]
            public Pet[] Pets { get; set; }
        }

        private class Pet
        {
            [BsonElement("name")]
            public string Name { get; set; }
        }    
    }
}
