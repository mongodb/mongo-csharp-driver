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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4957Tests : Linq3IntegrationTest
    {
        [Fact]
        public void New_array_with_zero_items_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => (new int[] { }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : [], _id : 0 } }");

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal();
        }

        [Fact]
        public void New_array_with_one_items_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X'], _id : 0 } }");

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1);
        }

        [Fact]
        public void New_array_with_two_items_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X, x.X + 1 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X', { $add : ['$X', 1] }], _id : 0 } }");

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1, 2);
        }

        [Fact]
        public void New_array_with_two_items_with_different_serializers_should_throw()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X, x.Y }));

            var exception = Record.Exception(() => Translate(collection, queryable));
            exception.Should().BeOfType<ExpressionNotSupportedException>();
            exception.Message.Should().Contain("all items in the array must be serialized using the same serializer");
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1, Y = 2 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            [BsonSerializer(typeof(YSerializer))] public int Y { get; set; }
        }

        private class YSerializer : StructSerializerBase<int>
        {
            public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, int value)
            {
                var writer = context.Writer;
                writer.WriteString($"<{value}>"); // not parsable by int.Parse
            }
        }
    }
}
