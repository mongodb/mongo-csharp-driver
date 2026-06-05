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

#if NET10_0_OR_GREATER

using System;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    [Trait("Category", "Integration")]
    public class LeftJoinTests
    {
        [Fact]
        public void LeftJoin_returns_matched_and_unmatched_outer_elements()
        {
            var client = new MongoClient(InfrastructureUtilities.MongoUri);
            var database = client.GetDatabase("leftjoin_smoke_" + Guid.NewGuid().ToString("N"));

            try
            {
                var orders = database.GetCollection<Order>("orders");
                var customers = database.GetCollection<Customer>("customers");

                orders.InsertMany(new[]
                {
                    new Order { Id = 1, CustomerId = 10 },
                    new Order { Id = 2, CustomerId = 20 },
                    new Order { Id = 3, CustomerId = 99 }
                });

                customers.InsertMany(new[]
                {
                    new Customer { Id = 10, Name = "Alice" },
                    new Customer { Id = 20, Name = "Bob" }
                });

                var results = orders.AsQueryable()
                    .LeftJoin(
                        customers.AsQueryable(),
                        o => o.CustomerId,
                        c => c.Id,
                        (o, c) => new { Outer = o, Inner = c })
                    .OrderBy(r => r.Outer.Id)
                    .ToList();

                results.Should().HaveCount(3);
                results[0].Inner.Name.Should().Be("Alice");
                results[1].Inner.Name.Should().Be("Bob");
                results[2].Inner.Should().BeNull();
            }
            finally
            {
                client.DropDatabase(database.DatabaseNamespace.DatabaseName);
                ClusterRegistry.Instance.UnregisterAndDisposeCluster(client.Cluster);
            }
        }

        private class Order
        {
            public int Id { get; set; }
            public int CustomerId { get; set; }
        }

        private class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}

#endif
