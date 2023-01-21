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
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4468Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Query1_should_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable =
                collection.AsQueryable()
                .SelectMany(i => i.Lines)
                .GroupBy(l => l.ItemId)
                .Select(g => new ItemSummary
                {
                    Id = g.Key,
                    TotalAmount = g.Sum(l => l.TotalAmount)
                });

            var stages = Translate(collection, queryable);
            string[] expectedStages;
            if (linqProvider == LinqProvider.V2)
            {
                expectedStages = new[]
                {
                    "{ $unwind : '$Lines' }",
                    "{ $project : { Lines : '$Lines', _id : 0 } }",
                    "{ $group : { _id : '$Lines.ItemId', __agg0 : { $sum : '$Lines.TotalAmount' } } }",
                    "{ $project : { Id : '$_id', TotalAmount : '$__agg0', _id : 0 } }"
                };
            }
            else
            {
                expectedStages = new[]
                {
                    "{ $project : { _v : '$Lines', _id : 0 } }",
                    "{ $unwind : '$_v' }",
                    "{ $group : { _id : '$_v.ItemId', __agg0 : { $sum : '$_v.TotalAmount' } } }",
                    "{ $project : { _id : '$_id', TotalAmount : '$__agg0' } }"
                };
            }
            AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void Query2_should_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = CreateCollection(linqProvider);

            var queryable =
                collection.AsQueryable()
                .GroupBy(l => l.Id)
                .Select(g => new ItemSummary
                {
                    Id = g.Key,
                    TotalAmount = g.Sum(l => l.TotalAmount)
                });

            var stages = Translate(collection, queryable);
            string[] expectedStages;
            if (linqProvider == LinqProvider.V2)
            {
                expectedStages = new[]
                {
                    "{ $group : { _id : '$_id', __agg0 : { $sum : '$TotalAmount' } } }",
                    "{ $project : { Id : '$_id', TotalAmount : '$__agg0', _id : 0 } }"
                };
            }
            else
            {
                expectedStages = new[]
                {
                    "{ $group : { _id : '$_id', __agg0 : { $sum : '$TotalAmount' } } }",
                    "{ $project : { _id : '$_id', TotalAmount : '$__agg0' } }" // only difference from LINQ2 is "_id" vs "Id"
                };
            }
            AssertStages(stages, expectedStages);
        }

        private IMongoCollection<OrderDao> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<OrderDao>(linqProvider: linqProvider);
            return collection;
        }

        public class OrderDao
        {
            public OrderLineDao[] Lines { get; set; }

            public decimal TotalAmount { get; set; }
            public Guid Id { get; set; }
        }

        public class OrderLineDao
        {
            public decimal TotalAmount { get; set; }
            public Guid ItemId { get; set; }
        }

        public class ItemSummary
        {
            public Guid Id { get; set; }
            public decimal TotalAmount { get; set; }
        }
    }
}
