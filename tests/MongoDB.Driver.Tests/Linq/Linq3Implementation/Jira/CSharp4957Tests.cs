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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4957Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void New_array_with_zero_items_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (new int[] { }));

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : [], _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : [], _id : 0 } }");

            }

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal();
        }

        [Theory]
        [ParameterAttributeData]
        public void New_array_with_one_items_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X }));

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : ['$X'], _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : ['$X'], _id : 0 } }");

            }

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void New_array_with_two_items_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X, x.X + 1 }));

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : ['$X', { $add : ['$X', 1] }], _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : ['$X', { $add : ['$X', 1] }], _id : 0 } }");

            }

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void New_array_with_two_items_with_different_serializers_should_throw(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X, x.Y }));

            if (linqProvider == LinqProvider.V2)
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { __fld0 : ['$X', '$Y'], _id : 0 } }");

                var exception = Record.Exception(() => queryable.ToArray());
                exception.Should().BeOfType<FormatException>(); // LINQ2 doesn't fail until deserialization
            }
            else
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("all items in the array must be serialized using the same serializer");
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
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
