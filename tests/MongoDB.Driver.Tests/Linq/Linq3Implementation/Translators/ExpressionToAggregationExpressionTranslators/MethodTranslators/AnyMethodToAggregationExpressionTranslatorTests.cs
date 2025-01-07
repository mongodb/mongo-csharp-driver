﻿/* Copyright 2010-present MongoDB Inc.
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
    public class AnyMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Any_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Any()) :
                collection.AsQueryable().Select(x => x.A.Any());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $gt : [{ $size : '$A' }, 0]  }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, true, true, true);
        }

        [Theory]
        [ParameterAttributeData]
        public void Any_with_predicate_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.A.AsQueryable().Any(x => x > 2)) :
                collection.AsQueryable().Select(x => x.A.Any(x => x > 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $anyElementTrue : { $map : { input : '$A', as : 'x', in : { $gt : ['$$x', 2] } } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(false, false, false, true);
        }

        [Fact]
        public void Any_on_constant_array_should_be_optimized()
        {
            var collection = CreateCollection();

            var obj = new[] { 1, 2, 3 };
            var queryable = collection.AsQueryable()
                .Where(x => obj.Any(y => x.Id == y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $in : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2, 3);
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
