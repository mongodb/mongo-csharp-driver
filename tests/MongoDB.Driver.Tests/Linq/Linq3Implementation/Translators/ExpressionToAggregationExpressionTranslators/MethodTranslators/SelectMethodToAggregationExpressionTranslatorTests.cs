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
    public class SelectMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Enumerable_Select_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.Select(x => x + 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $map : { input : '$A', as : 'x', in : { $add : ['$$x', 1] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(2, 3, 4);
        }

        [Fact]
        public void Queryable_Select_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().Select(x => x + 1));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $map : { input : '$A', as : 'x', in : { $add : ['$$x', 1] } } }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(2, 3, 4);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, A = new int[] { 1, 2, 3 } });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
        }
    }
}
