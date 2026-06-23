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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    // Regression test for CSHARP-4957.
    public class SelectNewArrayProjectionTests : LinqIntegrationTest<SelectNewArrayProjectionTests.ClassFixture>
    {
        public SelectNewArrayProjectionTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void New_array_with_zero_items_should_work()
        {
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => (new[] { x.X, x.X + 1 }));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : ['$X', { $add : ['$X', 1] }], _id : 0 } }");

            var results = queryable.ToArray();
            results.Should().HaveCount(1);
            results[0].Should().Equal(1, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void New_array_with_two_items_with_different_serializers_should_work(
            [Values(false, true)] bool enableClientSideProjections)
        {
            var collection = Fixture.Collection;
            var translationOptions = new ExpressionTranslationOptions { EnableClientSideProjections = enableClientSideProjections };

            var queryable = collection.AsQueryable(translationOptions)
                .Select(x => new[] { x.X, x.Y });

            var stages = Translate(collection, queryable, out var outputSerializer);
            AssertStages(stages, "{ $project : { _v : ['$X', '$Y'], _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2);
        }

        public class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            [BsonRepresentation(BsonType.String)] public int Y { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, X = 1, Y = 2 }
            ];
        }
    }
}
