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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3225Tests
    {
        // these examples are taken from: https://www.mongodb.com/docs/manual/reference/operator/aggregation/setWindowFields/#examples

        [Fact]
        public void Use_documents_window_to_obtain_cumulative_quantity_for_each_state_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new { CumulativeQuantityForState = p.Sum(x => x.Quantity, DocumentsWindow.Create(DocumentsWindow.Unbounded, DocumentsWindow.Current)) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : '$State',
                        sortBy : { OrderDate : 1 },
                        output : {
                            CumulativeQuantityForState : {
                                $sum : '$Quantity',
                                window : {
                                    documents : ['unbounded', 'current']
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, CumulativeQuantityForState : 162 }");
            results[1].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, CumulativeQuantityForState : 282 }");
            results[2].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, CumulativeQuantityForState : 427 }");
            results[3].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, CumulativeQuantityForState : 134 }");
            results[4].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, CumulativeQuantityForState : 238 }");
            results[5].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, CumulativeQuantityForState : 378 }");
        }

        [Fact]
        public void Use_documents_window_to_obtain_cumulative_quantity_for_each_year_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new { CumulativeQuantityForYear = p.Sum(x => x.Quantity, DocumentsWindow.Create(DocumentsWindow.Unbounded, DocumentsWindow.Current)) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : { $year : '$OrderDate' } ,
                        sortBy : { OrderDate : 1 },
                        output : {
                            CumulativeQuantityForYear : {
                                $sum : '$Quantity',
                                window : {
                                    documents : ['unbounded', 'current']
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, CumulativeQuantityForYear : 134 }");
            results[1].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, CumulativeQuantityForYear : 296 }");
            results[2].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, CumulativeQuantityForYear : 104 }");
            results[3].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, CumulativeQuantityForYear : 224 }");
            results[4].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, CumulativeQuantityForYear : 145 }");
            results[5].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, CumulativeQuantityForYear : 285 }");
        }

        [Fact]
        public void Use_documents_window_to_obtain_moving_average_quantity_for_each_year_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new { AverageQuantity = p.Average(x => x.Quantity, DocumentsWindow.Create(-1, 0)) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : { $year : '$OrderDate' } ,
                        sortBy : { OrderDate : 1 },
                        output : {
                            AverageQuantity : {
                                $avg : '$Quantity',
                                window : {
                                    documents : [-1, 0]
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, AverageQuantity : 134.0 }");
            results[1].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, AverageQuantity : 148.0 }");
            results[2].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, AverageQuantity : 104.0 }");
            results[3].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, AverageQuantity : 112.0 }");
            results[4].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, AverageQuantity : 145.0 }");
            results[5].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, AverageQuantity : 142.5 }");
        }

        [Fact]
        public void Use_documents_window_to_obtain_cumulative_and_maximum_quantity_for_each_year_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new
                    {
                        CumulativeQuantityForYear = p.Sum(x => x.Quantity, DocumentsWindow.Create(DocumentsWindow.Unbounded, DocumentsWindow.Current)),
                        MaximumQuantityForYear = p.Max(x => x.Quantity, DocumentsWindow.Create(DocumentsWindow.Unbounded, DocumentsWindow.Unbounded)),
                    });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : { $year : '$OrderDate' } ,
                        sortBy : { OrderDate : 1 },
                        output : {
                            CumulativeQuantityForYear : {
                                $sum : '$Quantity',
                                window : {
                                    documents : ['unbounded', 'current']
                                }
                            },
                            MaximumQuantityForYear : {
                                $max : '$Quantity',
                                window : {
                                    documents : ['unbounded', 'unbounded']
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().BeEquivalentTo("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, CumulativeQuantityForYear : 134, MaximumQuantityForYear : 162 }");
            results[1].Should().BeEquivalentTo("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, CumulativeQuantityForYear : 296, MaximumQuantityForYear : 162 }");
            results[2].Should().BeEquivalentTo("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, CumulativeQuantityForYear : 104, MaximumQuantityForYear : 120 }");
            results[3].Should().BeEquivalentTo("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, CumulativeQuantityForYear : 224, MaximumQuantityForYear : 120 }");
            results[4].Should().BeEquivalentTo("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, CumulativeQuantityForYear : 145, MaximumQuantityForYear : 145 }");
            results[5].Should().BeEquivalentTo("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, CumulativeQuantityForYear : 285, MaximumQuantityForYear : 145 }");
        }

        [Fact]
        public void Range_window_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.Price),
                    output: p => new { QuantityFromSimilarOrders = p.Sum(x => x.Quantity, RangeWindow.Create(-10, 10)) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : '$State',
                        sortBy : { Price : 1 },
                        output : {
                            QuantityFromSimilarOrders : {
                                $sum : '$Quantity',
                                window : {
                                    range : [-10.0, 10.0]
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, QuantityFromSimilarOrders : 265 }");
            results[1].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, QuantityFromSimilarOrders : 265 }");
            results[2].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, QuantityFromSimilarOrders : 162 }");
            results[3].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, QuantityFromSimilarOrders : 244 }");
            results[4].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, QuantityFromSimilarOrders : 244 }");
            results[5].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, QuantityFromSimilarOrders : 134 }");
        }

        [Fact]
        public void Use_a_time_range_window_with_a_positive_upper_bound_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new { RecentOrders = p.Push(x => x.OrderDate, RangeWindow.Create(RangeWindow.Unbounded, RangeWindow.Months(10))) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : '$State',
                        sortBy : { OrderDate : 1 },
                        output : {
                            RecentOrders : {
                                $push : '$OrderDate',
                                window : {
                                    range : ['unbounded', 10],
                                    unit : 'month'
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, RecentOrders : [ISODate('2019-05-18T16:09:01Z')] }");
            results[1].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, RecentOrders : [ISODate('2019-05-18T16:09:01Z'), ISODate('2020-05-18T14:10:30Z'), ISODate('2021-01-11T06:31:15Z')] }");
            results[2].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, RecentOrders : [ISODate('2019-05-18T16:09:01Z'), ISODate('2020-05-18T14:10:30Z'), ISODate('2021-01-11T06:31:15Z')] }");
            results[3].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, RecentOrders : [ISODate('2019-01-08T06:12:03Z')] }");
            results[4].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, RecentOrders : [ISODate('2019-01-08T06:12:03Z'), ISODate('2020-02-08T13:13:23Z')] }");
            results[5].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, RecentOrders : [ISODate('2019-01-08T06:12:03Z'), ISODate('2020-02-08T13:13:23Z'), ISODate('2021-03-20T11:30:05Z')] }");
        }

        [Fact]
        public void Use_a_time_range_window_with_a_negative_upper_bound_example_should_work()
        {
            RequireServer.Check().Supports(Feature.SetWindowFields);
            var collection = Setup();

            var aggregate = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: x => x.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(x => x.OrderDate),
                    output: p => new { RecentOrders = p.Push(x => x.OrderDate, RangeWindow.Create(RangeWindow.Unbounded, RangeWindow.Months(-10))) });

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                @"
                {
                    $setWindowFields : {
                        partitionBy : '$State',
                        sortBy : { OrderDate : 1 },
                        output : {
                            RecentOrders : {
                                $push : '$OrderDate',
                                window : {
                                    range : ['unbounded', -10],
                                    unit : 'month'
                                }
                            }
                        }
                    }
                }
                "
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var results = aggregate.ToList();
            results.Count.Should().Be(6);
            results[0].Should().Be("{ _id : 4, Type : 'strawberry', OrderDate : ISODate('2019-05-18T16:09:01Z'), State : 'CA', Price : 41.00, Quantity : 162, RecentOrders : [ ] }");
            results[1].Should().Be("{ _id : 0, Type : 'chocolate', OrderDate : ISODate('2020-05-18T14:10:30Z'), State : 'CA', Price : 13.00, Quantity : 120, RecentOrders : [ISODate('2019-05-18T16:09:01Z')] }");
            results[2].Should().Be("{ _id : 2, Type : 'vanilla', OrderDate : ISODate('2021-01-11T06:31:15Z'), State : 'CA', Price : 12.00, Quantity : 145, RecentOrders : [ISODate('2019-05-18T16:09:01Z')] }");
            results[3].Should().Be("{ _id : 5, Type : 'strawberry', OrderDate : ISODate('2019-01-08T06:12:03Z'), State : 'WA', Price : 43.00, Quantity : 134, RecentOrders : [ ] }");
            results[4].Should().Be("{ _id : 3, Type : 'vanilla', OrderDate : ISODate('2020-02-08T13:13:23Z'), State : 'WA', Price : 13.00, Quantity : 104, RecentOrders : [ISODate('2019-01-08T06:12:03Z')] }");
            results[5].Should().Be("{ _id : 1, Type : 'chocolate', OrderDate : ISODate('2021-03-20T11:30:05Z'), State : 'WA', Price : 14.00, Quantity : 140, RecentOrders : [ISODate('2019-01-08T06:12:03Z'), ISODate('2020-02-08T13:13:23Z')] }");
        }

        private IMongoCollection<CakeSales> Setup()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            var collection = database.GetCollection<CakeSales>("cakeSales");

            database.DropCollection("cakeSales");
            collection.InsertMany(new[]
            {
                new CakeSales { Id = 0, Type = "chocolate", OrderDate = DateTime.Parse("2020-05-18T14:10:30Z"), State = "CA", Price = 13.00, Quantity = 120 },
                new CakeSales { Id = 1, Type = "chocolate", OrderDate = DateTime.Parse("2021-03-20T11:30:05Z"), State = "WA", Price = 14.00, Quantity = 140 },
                new CakeSales { Id = 2, Type = "vanilla", OrderDate = DateTime.Parse("2021-01-11T06:31:15Z"), State = "CA", Price = 12.00, Quantity = 145 },
                new CakeSales { Id = 3, Type = "vanilla", OrderDate = DateTime.Parse("2020-02-08T13:13:23Z"), State = "WA", Price = 13.00, Quantity = 104 },
                new CakeSales { Id = 4, Type = "strawberry", OrderDate = DateTime.Parse("2019-05-18T16:09:01Z"), State = "CA", Price = 41.00, Quantity = 162 },
                new CakeSales { Id = 5, Type = "strawberry", OrderDate = DateTime.Parse("2019-01-08T06:12:03Z"), State = "WA", Price = 43.00, Quantity = 134 }
            });

            return collection;
        }

        public class CakeSales
        {
            public int Id { get; set; }
            public string Type { get; set; }
            public DateTime OrderDate { get; set; }
            public string State { get; set; }
            public double Price { get; set; }
            public int Quantity { get; set; }
        }
    }
}
