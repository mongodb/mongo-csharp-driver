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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class ScoreDefinitionBuilderTests
    {
        [Fact]
        public void Boost()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Boost(1),
                "{ boost: { value: 1 } }");
            AssertRendered(
                subject.Boost("x"),
                "{ boost: { path: 'x' } }");
            AssertRendered(
                subject.Boost("x", 1),
                "{ boost: { path: 'x', undefined: 1 } }");
        }

        [Fact]
        public void Boost_Typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Boost(x => x.Age),
                "{ boost: { path: 'age' } }");
            AssertRendered(
                subject.Boost("Age"),
                "{ boost: { path: 'age' } }");
        }

        [Fact]
        public void Constant()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Constant(1),
                "{ constant: { value: 1 } }");
        }

        [Fact]
        public void Function()
        {
            var subject = CreateSubject<BsonDocument>();

            var functionBuilder = new ScoreFunctionBuilder<BsonDocument>();
            AssertRendered(
                subject.Function(functionBuilder.Path("x")),
                "{ function: { path: 'x' } }");
        }

        private void AssertRendered<TDocument>(ScoreDefinition<TDocument> score, string expected) =>
            AssertRendered(score, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(ScoreDefinition<TDocument> score, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedQuery = score.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedQuery.Should().BeEquivalentTo(expected);
        }

        private ScoreDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>
            new ScoreDefinitionBuilder<TDocument>();

        private class Person
        {
            [BsonElement("age")]
            public int Age { get; set; }
        }
    }
}
