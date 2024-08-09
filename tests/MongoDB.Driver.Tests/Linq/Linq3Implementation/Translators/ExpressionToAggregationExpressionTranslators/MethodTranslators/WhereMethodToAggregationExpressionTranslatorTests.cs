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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class WhereMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Enumerable_Where_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.Where(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Equal();
            results[1].Should().Equal();
            results[2].Should().Equal(2);
            results[3].Should().Equal(2, 3);
        }

        [Fact]
        public void Enumerable_Where_Count_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.Where(x => x > 1).Count());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Be(0);
            results[1].Should().Be(0);
            results[2].Should().Be(1);
            results[3].Should().Be(2);
        }

        [Fact]
        public void Queryable_Where_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().Where(x => x > 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Equal();
            results[1].Should().Equal();
            results[2].Should().Equal(2);
            results[3].Should().Equal(2, 3);
        }

        [Fact]
        public void Queryable_Where_Count_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().Where(x => x > 1).Count());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $size : { $filter : { input : '$A', as : 'x', cond : { $gt : ['$$x', 1] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(4);
            results[0].Should().Be(0);
            results[1].Should().Be(0);
            results[2].Should().Be(1);
            results[3].Should().Be(2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 0, A = new int[0] },
                new C { Id = 1, A = new int[] { 1 } },
                new C { Id = 2, A = new int[] { 1, 2 } },
                new C { Id = 3, A = new int[] { 1, 2, 3 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
