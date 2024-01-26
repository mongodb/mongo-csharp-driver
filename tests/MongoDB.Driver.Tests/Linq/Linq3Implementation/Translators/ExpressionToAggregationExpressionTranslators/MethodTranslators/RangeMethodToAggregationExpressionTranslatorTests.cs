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
    public class RangeMethodToAggregationExpressionTranslatorTests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Range_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable = collection.AsQueryable().Select(x => Enumerable.Range(x.Start, x.Count));

            var stages = Translate(collection, queryable);
            if (linqProvider == LinqProvider.V2)
            {
                AssertStages(stages, "{ $project : { __fld0 : { $range : ['$Start', { $add : ['$Start', '$Count'] }] }, _id : 0 } }");
            }
            else
            {
                AssertStages(stages, "{ $project : { _v : { $range : ['$Start', { $add : ['$Start', '$Count'] }] }, _id : 0 } }");
            }

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].Should().Equal(1, 2);
            results[1].Should().Equal(3, 4, 5, 6);
        }

        private IMongoCollection<C> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, Start = 1, Count = 2 },
                new C { Id = 2, Start = 3, Count = 4 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int Start { get; set; }
            public int Count { get; set; }
        }
    }
}
