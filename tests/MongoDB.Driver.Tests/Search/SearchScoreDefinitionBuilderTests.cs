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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Search;
using Xunit;

namespace MongoDB.Driver.Tests.Search
{
    public class SearchScoreDefinitionBuilderTests
    {
        [Fact]
        public void Boost()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Boost(1),
                "{ boost: { value: 1 } }");
            AssertRendered(
                subject.Boost("x"),
                "{ boost: { path: 'x' } }");
            AssertRendered(
                subject.Boost("x", 1),
                "{ boost: { path: 'x', undefined: 1 } }");
            AssertRendered(
                subject.Boost(p => p.Age, 1),
                "{ boost: { path: 'age', undefined: 1 } }");
        }

        [Fact]
        public void Boost_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Boost(x => x.Age),
                "{ boost: { path: 'age' } }");
            AssertRendered(
                subject.Boost("age"),
                "{ boost: { path: 'age' } }");
        }

        [Fact]
        public void Constant()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Constant(1),
                "{ constant: { value: 1 } }");
        }

        [Fact]
        public void Function()
        {
            var subject = CreateSubject<Person>();
            var functionBuilder = new SearchScoreFunctionBuilder<Person>();

            AssertRendered(
                subject.Function(functionBuilder.Path(p => p.Age)),
                "{ function: { path: 'age' } }");
            AssertRendered(
              subject.Function(functionBuilder.Path("age")),
              "{ function: { path: 'age' } }");
        }

        private void AssertRendered<TDocument>(SearchScoreDefinition<TDocument> score, string expected) =>
            AssertRendered(score, BsonDocument.Parse(expected));

        private void AssertRendered<TDocument>(SearchScoreDefinition<TDocument> score, BsonDocument expected)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedQuery = score.Render(documentSerializer, BsonSerializer.SerializerRegistry);

            renderedQuery.Should().BeEquivalentTo(expected);
        }

        private SearchScoreDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>
            new SearchScoreDefinitionBuilder<TDocument>();

        private class Person
        {
            [BsonElement("age")]
            public int Age { get; set; }
        }
    }
}
