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
    public class ElementAtMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void ElementAt_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().ElementAt(1)) :
                collection.AsQueryable().Select(x => x.A.ElementAt(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : ['$A', 1] }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 2);
        }

        [Theory]
        [ParameterAttributeData]
        public void ElementAtOrDefault_should_work(
          [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().ElementAtOrDefault(1)) :
                collection.AsQueryable().Select(x => x.A.ElementAtOrDefault(1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $cond : { if : { $gte : [1, { $size : '$A' }] }, then : 0, else : { $arrayElemAt : ['$A', 1] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(0, 0, 2, 2);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 0, A = new int[0] },
                new C { Id = 1, A = new int[] { 1 } },
                new C { Id = 2, A = new int[] { 1, 2 } },
                new C { Id = 3, A = new int[] { 1, 2, 2 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
