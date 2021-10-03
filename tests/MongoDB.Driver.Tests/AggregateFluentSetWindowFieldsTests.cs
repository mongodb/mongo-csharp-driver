/* Copyright 2021-present MongoDB Inc.
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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests
{
    public class AggregateFluentSetWindowFieldsTests
    {
        [SkippableFact]
        public void SetWindowFields_with_plain_mql_and_empty_partitionBy_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields<CakeSales>(
                output: @$"
 {{
    CumulativeQuantity:
    {{
        $sum: ""$Quantity"",
    }}
}}",
                sortBy: "{ OrderDate: 1 }",
                outputWindowOptions: new AggregateOutputWindowOptions<CakeSales>("CumulativeQuantity")
                {
                    Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(134);
            result[1].CumulativeQuantity.Should().Be(296);
            result[2].CumulativeQuantity.Should().Be(400);
            result[3].CumulativeQuantity.Should().Be(520);
            result[4].CumulativeQuantity.Should().Be(665);
            result[5].CumulativeQuantity.Should().Be(805);
        }

        [SkippableFact]
        public void SetWindowFields_with_plain_mql_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields<BsonValue, CakeSales>(
                partitionBy: "{ partitionBy : '$State' }",
                sortBy: "{ OrderDate: 1 }",
                output: @$"
 {{
    CumulativeQuantity:
    {{
        $sum: ""$Quantity"",
    }}
}}"             ,
                outputWindowOptions: new AggregateOutputWindowOptions<CakeSales>("CumulativeQuantity")
                {
                    Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(162);
            result[1].CumulativeQuantity.Should().Be(282);
            result[2].CumulativeQuantity.Should().Be(427);
            result[3].CumulativeQuantity.Should().Be(134);
            result[4].CumulativeQuantity.Should().Be(238);
            result[5].CumulativeQuantity.Should().Be(378);
        }

        [SkippableFact]
        public void SetWindowFields_with_sum_and_window_documents_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    output: sw => new CakeSales { CumulativeQuantity = sw.Sum(c => c.Quantity) },
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, int>(owo => owo.CumulativeQuantity)
                        {
                            Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                        })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(162);
            result[1].CumulativeQuantity.Should().Be(282);
            result[2].CumulativeQuantity.Should().Be(427);
            result[3].CumulativeQuantity.Should().Be(134);
            result[4].CumulativeQuantity.Should().Be(238);
            result[5].CumulativeQuantity.Should().Be(378);
        }

        [SkippableFact]
        public void SetWindowFields_with_sum_and_window_documents_and_nested_output_field_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    output:
                        sw =>
                            new CakeSales
                            {
                                NestedNode1 = new NestedNode
                                {
                                    ChildNested2 = new NestedNode { CumulativeQuantity = sw.Sum(c => c.Quantity) }
                                }
                            },
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, int>(owo => owo.NestedNode1.ChildNested2.CumulativeQuantity)
                        {
                            Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                        })
                .ToList();

            result[0].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(162);
            result[1].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(282);
            result[2].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(427);
            result[3].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(134);
            result[4].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(238);
            result[5].NestedNode1.ChildNested2.CumulativeQuantity.Should().Be(378);
        }

        [SkippableFact]
        public void SetWindowFields_with_partition_by_year_and_sum_and_window_documents_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    output: sw => new CakeSales { CumulativeQuantity = sw.Sum(c => c.Quantity) },
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, int>(owo => owo.CumulativeQuantity)
                        {
                            Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                        })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(134);
            result[1].CumulativeQuantity.Should().Be(296);
            result[2].CumulativeQuantity.Should().Be(104);
            result[3].CumulativeQuantity.Should().Be(224);
            result[4].CumulativeQuantity.Should().Be(145);
            result[5].CumulativeQuantity.Should().Be(285);
        }

        [SkippableFact]
        public void SetWindowFields_with_partition_by_year_and_average_and_window_documents_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    output: sw => new CakeSales { AverageQuantity = sw.Average(c => c.Quantity) },
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, double>(owo => owo.AverageQuantity)
                        {
                            Documents = WindowRange.Create(WindowBound.CreatePosition(-1), WindowBound.CreatePosition(0))
                        })
                .ToList();

            result[0].AverageQuantity.Should().Be(134);
            result[1].AverageQuantity.Should().Be(148);
            result[2].AverageQuantity.Should().Be(104);
            result[3].AverageQuantity.Should().Be(112);
            result[4].AverageQuantity.Should().Be(145);
            result[5].AverageQuantity.Should().Be(142.5);
        }

        [SkippableFact]
        public void SetWindowFields_with_partition_by_year_andfew_output_fields_and_window_documents_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.OrderDate.Year,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    output: sw => new CakeSales { CumulativeQuantity = sw.Sum(c => c.Quantity), MaximumQuantityForYear = sw.Max(c => c.Quantity) },
                    new AggregateOutputWindowOptions<CakeSales, int>(owo => owo.CumulativeQuantity)
                    {
                        Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Current)
                    },
                    new AggregateOutputWindowOptions<CakeSales, double>(owo => owo.MaximumQuantityForYear)
                    {
                        Documents = WindowRange.Create(WindowBound.Unbounded, WindowBound.Unbounded)
                    })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(134);
            result[0].MaximumQuantityForYear.Should().Be(162);

            result[1].CumulativeQuantity.Should().Be(296);
            result[1].MaximumQuantityForYear.Should().Be(162);

            result[2].CumulativeQuantity.Should().Be(104);
            result[2].MaximumQuantityForYear.Should().Be(120);

            result[3].CumulativeQuantity.Should().Be(224);
            result[3].MaximumQuantityForYear.Should().Be(120);

            result[4].CumulativeQuantity.Should().Be(145);
            result[4].MaximumQuantityForYear.Should().Be(145);

            result[5].CumulativeQuantity.Should().Be(285);
            result[5].MaximumQuantityForYear.Should().Be(145);
        }

        [SkippableFact]
        public void SetWindowFields_with_sum_and_window_range_should_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.State,
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.Price),
                    output: sw => new CakeSales { CumulativeQuantity = sw.Sum(c => c.Quantity) },
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, int>(owo => owo.CumulativeQuantity)
                        {
                            Range = WindowRange.Create(WindowBound.CreatePosition(-10), WindowBound.CreatePosition(10))
                        })
                .ToList();

            result[0].CumulativeQuantity.Should().Be(265);
            result[1].CumulativeQuantity.Should().Be(265);
            result[2].CumulativeQuantity.Should().Be(162);
            result[3].CumulativeQuantity.Should().Be(244);
            result[4].CumulativeQuantity.Should().Be(244);
            result[5].CumulativeQuantity.Should().Be(134);
        }

        [SkippableFact]
        public void SetWindowFields_with_push_and_window_range_and_unit_shoud_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.State,
                    output: sw => new CakeSales { RecentOrders = sw.Select(c => c.OrderDate) },
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, IEnumerable<DateTime>>(owo => owo.RecentOrders)
                        {
                            Range = WindowRange.Create(WindowBound.Unbounded, WindowBound.CreatePosition(10)),
                            Unit = WindowTimeUnit.Month
                        })
                .ToList();

            result[0].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-05-18T16:09:01Z") });
            result[1].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-05-18T16:09:01Z"), CreateUtcDateTime("2020-05-18T14:10:30Z"), CreateUtcDateTime("2021-01-11T06:31:15Z") });
            result[2].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-05-18T16:09:01Z"), CreateUtcDateTime("2020-05-18T14:10:30Z"), CreateUtcDateTime("2021-01-11T06:31:15Z") });
            result[3].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-01-08T06:12:03Z") });
            result[4].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-01-08T06:12:03Z"), CreateUtcDateTime("2020-02-08T13:13:23Z") });
            result[5].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-01-08T06:12:03Z"), CreateUtcDateTime("2020-02-08T13:13:23Z"), CreateUtcDateTime("2021-03-20T11:30:05Z") });
        }

        [SkippableFact]
        public void SetWindowFields_with_push_and_window_negative_range_and_unit_shoud_return_expected_result()
        {
            RequireServer.Check().Supports(Feature.AggregateSetWindowFields);

            var collection = SetupCollection();

            var result = collection
                .Aggregate()
                .SetWindowFields(
                    partitionBy: c => c.State,
                    output: sw => new CakeSales { RecentOrders = sw.Select(c => c.OrderDate) },
                    sortBy: Builders<CakeSales>.Sort.Ascending(s => s.OrderDate),
                    outputWindowOptions:
                        new AggregateOutputWindowOptions<CakeSales, IEnumerable<DateTime>>(owo => owo.RecentOrders)
                        {
                            Range = WindowRange.Create(WindowBound.Unbounded, WindowBound.CreatePosition(-10)),
                            Unit = WindowTimeUnit.Month
                        })
                .ToList();

            result[0].RecentOrders.Should().BeEmpty();
            result[1].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-05-18T16:09:01Z") });
            result[2].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-05-18T16:09:01Z") });
            result[3].RecentOrders.Should().BeEmpty();
            result[4].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-01-08T06:12:03Z") });
            result[5].RecentOrders.Should().BeEquivalentTo(new[] { CreateUtcDateTime("2019-01-08T06:12:03Z"), CreateUtcDateTime("2020-02-08T13:13:23Z") });
        }

        // private methods
        private DateTime CreateUtcDateTime(string date) => DateTime.Parse(date).ToUniversalTime();


        private IMongoCollection<CakeSales> SetupCollection()
        {
            var collectionNamespace = CollectionNamespace.FromFullName("db.cakeSales11");

            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(collectionNamespace.DatabaseNamespace.DatabaseName);
            database.DropCollection(collectionNamespace.CollectionName);
            var collection = database.GetCollection<CakeSales>(collectionNamespace.CollectionName);
            collection.InsertMany
            (
                new[]
                {
                    new CakeSales{ Type = "chocolate", OrderDate = DateTime.Parse("2020-05-18T14:10:30Z"), State  = State.CA, Price = 13, Quantity = 120 },
                    new CakeSales{ Type = "chocolate", OrderDate = DateTime.Parse("2021-03-20T11:30:05Z"), State  = State.WA, Price = 14, Quantity = 140 },
                    new CakeSales{ Type = "vanilla", OrderDate = DateTime.Parse("2021-01-11T06:31:15Z"), State  = State.CA, Price = 12, Quantity = 145 },
                    new CakeSales{ Type = "vanilla", OrderDate = DateTime.Parse("2020-02-08T13:13:23Z"), State  = State.WA, Price = 13, Quantity = 104 },
                    new CakeSales{ Type = "strawberry", OrderDate = DateTime.Parse("2019-05-18T16:09:01Z"), State  = State.CA, Price = 41, Quantity = 162 },
                    new CakeSales{ Type = "strawberry", OrderDate = DateTime.Parse("2019-01-08T06:12:03Z"), State  = State.WA, Price = 43, Quantity = 134 },
                }
            );
            return collection;
        }

        // nested types
        public class CakeSales
        {
            public ObjectId Id { get; set; }
            public string Type { get; set; }
            public DateTime OrderDate { get; set; }
            public State State { get; set; }
            public int Price { get; set; }
            public int Quantity { get; set; }
            public int CumulativeQuantity { get; set; }
            public double AverageQuantity { get; set; }
            public int MaximumQuantityForYear { get; set; }
            public IEnumerable<DateTime> RecentOrders { get; set; }
            public NestedNode NestedNode1 { get; set; }
            public NestedNode NestedNode2 { get; set; }
        }

        public class NestedNode
        {
            public int CumulativeQuantity { get; set; }
            public NestedNode ChildNested1 { get; set; }
            public NestedNode ChildNested2 { get; set; }
        }

        public enum State
        {
            CA,
            WA
        }
    }
}
