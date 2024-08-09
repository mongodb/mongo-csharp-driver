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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class AggregateMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_func_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Aggregate((x, y) => x * y)) :
                collection.AsQueryable().Select(x => x.A.Aggregate((x, y) => x * y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $let : { vars : { seed : { $arrayElemAt : ['$A', 0] }, rest : { $slice : ['$A', 1, 2147483647] } }, in : { $cond : { if : { $eq : [{ $size : '$$rest' }, 0] }, then : '$$seed', else : { $reduce : { input : '$$rest', initialValue : '$$seed', in : { $multiply : ['$$value', '$$this'] } } } } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 1, 2, 6); // C# throws exception on empty sequence but MQL returns 0
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_seed_and_func_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Aggregate(2, (x, y) => x * y)) :
                collection.AsQueryable().Select(x => x.A.Aggregate(2, (x, y) => x * y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reduce : { input : '$A', initialValue : 2, in : { $multiply : ['$$value', '$$this'] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(2, 2, 4, 12);
        }

        [Theory]
        [ParameterAttributeData]
        public void Aggregate_with_seed_func_and_result_selector_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Aggregate(2, (x, y) => x * y, x => x * 3)) :
                collection.AsQueryable().Select(x => x.A.Aggregate(2, (x, y) => x * y, x => x * 3));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v     : { $let : { vars : { x : { $reduce : { input : '$A', initialValue : 2, in : { $multiply : ['$$value', '$$this'] } } } }, in : { $multiply : ['$$x', 3] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(6, 6, 12, 36);
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
