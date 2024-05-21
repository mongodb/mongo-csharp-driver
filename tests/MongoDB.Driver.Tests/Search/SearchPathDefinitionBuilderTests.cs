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
    public class SearchPathDefinitionBuilderTests
    {
        [Fact]
        public void Analyzer()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Analyzer("x", "english"),
                "{ value: 'x', multi: 'english' }");
            AssertRendered(
                subject.Analyzer("x", "english"),
                "{ value: 'basepath.x', multi: 'english' }",
                "basepath");
        }

        [Fact]
        public void Analyzer_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Analyzer(x => x.FirstName, "english"),
                "{ value: 'fn', multi: 'english' }");
            AssertRendered(
                subject.Analyzer(x => x.FirstName, "english"),
                "{ value: 'basepath.fn', multi: 'english' }",
                "basepath");
            AssertRendered(
                subject.Analyzer("FirstName", "english"),
                "{ value: 'fn', multi: 'english' }");
            AssertRendered(
                subject.Analyzer("FirstName", "english"),
                "{ value: 'basepath.fn', multi: 'english' }",
                "basepath");
        }

        [Fact]
        public void Multi()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(
                subject.Multi("x", "y"),
                new BsonArray()
                {
                    new BsonString("x"),
                    new BsonString("y")
                });
            AssertRendered(
                subject.Multi("x", "y"),
                new BsonArray()
                {
                    new BsonString("basepath.x"),
                    new BsonString("basepath.y")
                },
                "basepath");
            AssertRendered(
                subject.Multi(
                    new List<FieldDefinition<BsonDocument>>()
                    {
                        "x",
                        "y"
                    }),
                new BsonArray()
                {
                    new BsonString("x"),
                    new BsonString("y")
                });
            AssertRendered(
                subject.Multi(
                    new List<FieldDefinition<BsonDocument>>()
                    {
                        "x",
                        "y"
                    }),
                new BsonArray()
                {
                    new BsonString("basepath.x"),
                    new BsonString("basepath.y")
                },
                "basepath");
        }

        [Fact]
        public void Multi_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Multi(x => x.FirstName, x => x.LastName),
                new BsonArray()
                {
                    new BsonString("fn"),
                    new BsonString("ln")
                });
            AssertRendered(
                subject.Multi(x => x.FirstName, x => x.LastName),
                new BsonArray()
                {
                    new BsonString("basepath.fn"),
                    new BsonString("basepath.ln")
                },
                "basepath");
            AssertRendered(
                subject.Multi("FirstName", "LastName"),
                new BsonArray()
                {
                    new BsonString("fn"),
                    new BsonString("ln")
                });
            AssertRendered(
                subject.Multi("FirstName", "LastName"),
                new BsonArray()
                {
                    new BsonString("basepath.fn"),
                    new BsonString("basepath.ln")
                },
                "basepath");
        }

        [Fact]
        public void NestedPath()
        {
            var subject = CreateSubject<BsonDocument>();
            AssertRendered(subject.Single("x"), new BsonString("a.b.x"), "a.b");
            AssertRendered(subject.Single("x"), new BsonString("a.b.c.x"), "a.b.c");
        }

        [Fact]
        public void Single()
        {
            var subject = CreateSubject<BsonDocument>();

            AssertRendered(subject.Single("x"), new BsonString("x"));
            AssertRendered(subject.Single("x"), new BsonString("basepath.x"), "basepath");
        }

        [Fact]
        public void Single_typed()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(subject.Single(x => x.FirstName), new BsonString("fn"));
            AssertRendered(subject.Single(x => x.FirstName), new BsonString("basepath.fn"), "basepath");
            AssertRendered(subject.Single("FirstName"), new BsonString("fn"));
            AssertRendered(subject.Single("FirstName"), new BsonString("basepath.fn"), "basepath");
        }

        [Fact]
        public void Wildcard()
        {
            var subject = CreateSubject<Person>();

            AssertRendered(
                subject.Wildcard("*"),
                "{ wildcard: '*' }");
            AssertRendered(
                subject.Wildcard("*"),
                "{ wildcard: '*' }",
                "basepath");
        }

        private void AssertRendered<TDocument>(SearchPathDefinition<TDocument> path, string expected, string pathPrefix = null) =>
            AssertRendered(path, BsonDocument.Parse(expected), pathPrefix);

        private void AssertRendered<TDocument>(SearchPathDefinition<TDocument> path, BsonValue expected, string pathPrefix = null)
        {
            var documentSerializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
            var renderedPath = path.Render(new RenderArgs<TDocument>(documentSerializer, BsonSerializer.SerializerRegistry, pathRenderArgs: new(PathPrefix: pathPrefix)));

            renderedPath.Should().Be(expected);
        }

        private SearchPathDefinitionBuilder<TDocument> CreateSubject<TDocument>() =>
            new SearchPathDefinitionBuilder<TDocument>();

        private class Person
        {
            [BsonElement("fn")]
            public string FirstName { get; set; }

            [BsonElement("ln")]
            public string LastName { get; set; }
        }
    }
}
