/* Copyright 2010-2014 MongoDB Inc.
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
using NUnit.Framework;

namespace MongoDB.Driver.Tests
{
    [TestFixture]
    public class ProjectionBuilderTests
    {
        [Test]
        public void Combine()
        {
            var subject = CreateSubject<Person>();

            var projection = subject.Combine<BsonDocument>(
                subject.Include(x => x.FirstName),
                subject.Exclude("LastName"));

            Assert(projection, "{fn: 1, LastName: 0}");
        }

        [Test]
        public void Combine_with_redundant_fields()
        {
            var subject = CreateSubject<Person>();

            var projection = subject.Combine<BsonDocument>(
                subject.Include(x => x.FirstName),
                subject.Exclude("LastName"),
                subject.Include("fn"));

            Assert(projection, "{LastName: 0, fn: 1}");
        }

        [Test]
        public void Exclude()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Exclude("a"), "{a: 0}");
        }

        [Test]
        public void Exclude_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Exclude(x => x.FirstName), "{fn: 0}");
            Assert(subject.Exclude("FirstName"), "{FirstName: 0}");
        }

        [Test]
        public void Include()
        {
            var subject = CreateSubject<BsonDocument>();

            Assert(subject.Include("a"), "{a: 1}");
        }

        [Test]
        public void Include_Typed()
        {
            var subject = CreateSubject<Person>();

            Assert(subject.Include(x => x.FirstName), "{fn: 1}");
            Assert(subject.Include("FirstName"), "{FirstName: 1}");
        }

        private void Assert<TDocument, TResult>(Projection<TDocument, TResult> projection, string expectedJson)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedProjection = projection.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedProjection.Document.Should().Be(expectedJson);
        }

        private ProjectionBuilder<TDocument> CreateSubject<TDocument>()
        {
            return new ProjectionBuilder<TDocument>();
        }

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }
        }
    }
}
