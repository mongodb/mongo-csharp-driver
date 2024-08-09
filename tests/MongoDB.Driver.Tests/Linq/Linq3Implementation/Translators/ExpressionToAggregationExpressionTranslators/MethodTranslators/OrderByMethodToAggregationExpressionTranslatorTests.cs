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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class OrderByMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Enumerable_OrderBy_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.OrderBy(x => x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : 1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.X).Should().Equal(1, 1, 2, 2);
        }

        [Fact]
        public void Enumerable_OrderByDescending_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.OrderByDescending(x => x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : -1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.X).Should().Equal(2, 2, 1, 1);
        }

        [Fact]
        public void Enumerable_ThenBy_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.OrderByDescending(x => x.X).ThenBy(x => x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : -1, Y : 1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Y).Should().Equal(3, 4, 1, 2);
        }

        [Fact]
        public void Enumerable_ThenByDescending_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.OrderBy(x => x.X).ThenByDescending(x => x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : 1, Y : -1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Y).Should().Equal(2, 1, 4, 3);
        }

        [Fact]
        public void Queryable_OrderBy_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().OrderBy(x => x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : 1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.X).Should().Equal(1, 1, 2, 2);
        }

        [Fact]
        public void Queryable_OrderByDescending_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().OrderByDescending(x => x.X));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : -1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.X).Should().Equal(2, 2, 1, 1);
        }

        [Fact]
        public void Queryable_ThenBy_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().OrderByDescending(x => x.X).ThenBy(x => x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : -1, Y : 1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Y).Should().Equal(3, 4, 1, 2);
        }

        [Fact]
        public void Queryable_ThenByDescending_should_work()
        {
            RequireServer.Check().Supports(Feature.SortArrayOperator);
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().OrderBy(x => x.X).ThenByDescending(x => x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $sortArray : { input : '$A', sortBy : { X : 1, Y : -1 } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Select(x => x.Y).Should().Equal(2, 1, 4, 3);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, A = new A[] { new A(1, 1), new A(1, 2), new A(2, 3), new A(2, 4)  } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public A[] A { get; set; }
        }

        public class A
        {
            public A(int x, int y) { X = x; Y = y; } 
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
