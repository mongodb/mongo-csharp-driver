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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public class ReverseMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Fact]
        public void Enumerable_Reverse_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.Reverse());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reverseArray : '$A' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(3, 2, 1);
        }

        [Fact]
        public void Queryable_Reverse_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection.AsQueryable().Select(x => x.A.AsQueryable().Reverse());

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $reverseArray : '$A' }, _id : 0 } }");

            var result = queryable.Single();
            result.Should().Equal(3, 2, 1);
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
