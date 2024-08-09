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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4457Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Filter_with_bool_field_should_work()
        {
            var collection = CreateCollection();
            var builder = Builders<C>.Filter;
            var filter = builder.Where(x => x.BoolField);

            var rendered = RenderFilter(filter);
            rendered.Should().Be("{ BoolField : true }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Filter_with_bool_property_should_work()
        {
            var collection = CreateCollection();
            var builder = Builders<C>.Filter;
            var filter = builder.Where(x => x.BoolProperty);

            var rendered = RenderFilter(filter);
            rendered.Should().Be("{ BoolProperty : true }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Filter_with_not_bool_field_should_work()
        {
            var collection = CreateCollection();
            var builder = Builders<C>.Filter;
            var filter = builder.Where(x => !x.BoolField);

            var rendered = RenderFilter(filter);
            rendered.Should().Be("{ BoolField : { $ne : true } }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Filter_with_not_bool_property_should_work()
        {
            var collection = CreateCollection();
            var builder = Builders<C>.Filter;
            var filter = builder.Where(x => !x.BoolProperty);

            var rendered = RenderFilter(filter);
            rendered.Should().Be("{ BoolProperty : { $ne : true } }");

            var results = collection.FindSync(filter).ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_bool_field_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Where(x => x.BoolField);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolField : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_with_bool_property_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Where(x => x.BoolProperty);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolProperty : true } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_with_not_bool_field_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Where(x => !x.BoolField);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolField : { $ne : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        [Fact]
        public void Where_with_not_bool_property_should_work()
        {
            var collection = CreateCollection();

            var queryable =
                collection.AsQueryable()
                .Where(x => !x.BoolProperty);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { BoolProperty : { $ne : true } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, BoolField = true, BoolProperty = true },
                new C { Id = 2, BoolField = false, BoolProperty = false });

            return collection;
        }

        private BsonDocument RenderFilter<TDocument>(FilterDefinition<TDocument> filter)
        {
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<TDocument>();
            return filter.Render(new(documentSerializer, serializerRegistry));
        }

        private class C
        {
            public bool BoolField;

            public int Id { get; set; }
            public bool BoolProperty { get; set; }
        }
    }
}
