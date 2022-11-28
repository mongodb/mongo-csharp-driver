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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3144Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Where_with_Contains_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.Items.Select(e => e.GoodId).Contains(2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Items.GoodId' : 2 } }");

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        [Fact]
        public void Suggested_workaround_should_work()
        {
            var collection = CreateCollection();

            var queryable = collection
                .AsQueryable()
                .Where(x => x.Items.Select(e => e.GoodId).Any(e => e == 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { 'Items.GoodId' : 2 } }");

            var results = queryable.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        private IMongoCollection<Order> CreateCollection()
        {
            var collection = GetCollection<Order>();

            CreateCollection(
                collection,
                new Order
                {
                    Id = 1,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { GoodId = 1, Amount = 1 },
                        new OrderItem { GoodId = 2, Amount = 10 },
                        new OrderItem { GoodId = 3, Amount = 20 },
                        new OrderItem { GoodId = 4, Amount = 30 },
                        new OrderItem { GoodId = 5, Amount = 40 }
                    }
                },
                new Order
                {
                    Id = 2,
                    Items = new List<OrderItem>
                        {
                            new OrderItem { GoodId = 6, Amount = 1 },
                            new OrderItem { GoodId = 7, Amount = 10 },
                            new OrderItem { GoodId = 8, Amount = 20 },
                            new OrderItem { GoodId = 9, Amount = 30 },
                            new OrderItem { GoodId = 10, Amount = 40 }
                        }
                });

            return collection;
        }

        class Order
        {
            public virtual int Id { get; set; }
            public virtual List<OrderItem> Items { get; set; }

            public Order()
            {
                Items = new List<OrderItem>();
            }
        }

        class OrderItem
        {
            public virtual int Id { get; set; }
            public virtual int GoodId { get; set; }
            public virtual int Amount { get; set; }
        }
    }
}
