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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class ToListMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Array_ToList_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.Array.AsQueryable().ToList()) :
                collection.AsQueryable().Select(x => x.Array.ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$Array', _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void IEnumerable_ToList_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.IEnumerable.AsQueryable().ToList()) :
                collection.AsQueryable().Select(x => x.IEnumerable.ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$IEnumerable', _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2, 3);
        }

        [Theory]
        [ParameterAttributeData]
        public void List_ToList_should_work(
            [Values(false, true)] bool withNestedAsQueryable)
        {
            var collection = CreateCollection();

            var queryable = withNestedAsQueryable ?
                collection.AsQueryable().Select(x => x.List.AsQueryable().ToList()) :
                collection.AsQueryable().Select(x => x.List.ToList());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : '$List', _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(1, 2, 3);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C
                {
                    Id = 1,
                    Array = new int[] { 1, 2, 3 },
                    IEnumerable = new List<int> { 1, 2, 3 },
                    List = new List<int> { 1, 2, 3 }
                });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] Array { get; set; }
            public IEnumerable<int> IEnumerable { get; set; }
            public List<int> List { get; set; }
        }
    }
}
