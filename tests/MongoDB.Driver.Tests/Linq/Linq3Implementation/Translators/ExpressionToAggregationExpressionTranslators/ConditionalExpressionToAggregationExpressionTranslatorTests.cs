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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    public class ConditionalExpressionToAggregationExpressionTranslatorTests : LinqIntegrationTest<ConditionalExpressionToAggregationExpressionTranslatorTests.ClassFixture>
    {
        public ConditionalExpressionToAggregationExpressionTranslatorTests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Conditional_with_null_array_branch_and_Select_on_non_null_branch_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(x => x.Items == null ? null : x.Items.Select(a => a.Name));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $project : { _v : { $cond : { if : { $eq : ['$Items', null] }, then : null, else : '$Items.Name' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(3);
            results[0].Should().BeNull();
            results[1].Should().BeEmpty();
            results[2].Should().Equal("a", "b");
        }

        public class C
        {
            public int Id { get; set; }
            public List<Item> Items { get; set; }
        }

        public class Item
        {
            public string Name { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, Items = null },
                new C { Id = 2, Items = [] },
                new C { Id = 3, Items = [new Item { Name = "a" }, new Item { Name = "b" }] }
            ];
        }
    }
}
