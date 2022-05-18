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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3ImplementationTests;
using Xunit;

namespace MongoDB.Driver.Tests.Jira
{
    public class CSharp3915Tests : Linq3IntegrationTest
    {
        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Densify_time_series_data_example_using_aggregate_should_work(bool usingExpressions)
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateWeatherCollection();
            var subject = collection.Aggregate();

            var lowerBound = DateTime.Parse("2021-05-18T00:00:00.000Z");
            var upperBound = DateTime.Parse("2021-05-18T08:00:00.000Z");

            IAggregateFluent<WeatherInfo> aggregate;
            if (usingExpressions)
            {
                aggregate = subject.Densify(
                    field: x => x.Timestamp,
                    range: DensifyRange.DateTime(lowerBound, upperBound, step: 1, DensifyDateTimeUnit.Hours));
            }
            else
            {
                aggregate = subject.Densify(
                    field: "Timestamp",
                    range: DensifyRange.DateTime(lowerBound, upperBound, step: 1, DensifyDateTimeUnit.Hours));
            }

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $densify : { field : 'Timestamp', range : { step : 1, unit : 'hour', bounds : [ISODate('2021-05-18T00:00:00.000Z'), ISODate('2021-05-18T08:00:00.000Z')] } } }");

            var results = aggregate.ToList();
            var timestamps = results.Select(r => r.Timestamp).OrderBy(t => t).ToList();
            var expectedTimestamps = new[]
            {
                "2021-05-18T00:00:00.000Z",
                "2021-05-18T01:00:00.000Z",
                "2021-05-18T02:00:00.000Z",
                "2021-05-18T03:00:00.000Z",
                "2021-05-18T04:00:00.000Z",
                "2021-05-18T05:00:00.000Z",
                "2021-05-18T06:00:00.000Z",
                "2021-05-18T07:00:00.000Z",
                "2021-05-18T08:00:00.000Z",
                "2021-05-18T12:00:00.000Z"
            };
            timestamps.Should().Equal(expectedTimestamps.Select(t => JsonConvert.ToDateTime(t)));
        }

        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Fact]
        public void Densify_time_series_data_example_using_linq_should_work()
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateWeatherCollection();
            var subject = collection.AsQueryable();

            var lowerBound = DateTime.Parse("2021-05-18T00:00:00.000Z");
            var upperBound = DateTime.Parse("2021-05-18T08:00:00.000Z");

            var queryable = subject.Densify(
                field: x => x.Timestamp,
                range: DensifyRange.DateTime(lowerBound, upperBound, step: 1, DensifyDateTimeUnit.Hours));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $densify : { field : 'Timestamp', range : { step : 1, unit : 'hour', bounds : [ISODate('2021-05-18T00:00:00.000Z'), ISODate('2021-05-18T08:00:00.000Z')] } } }");

            var results = queryable.ToList();
            var timestamps = results.Select(r => r.Timestamp).OrderBy(t => t).ToList();
            var expectedTimestamps = new[]
            {
                "2021-05-18T00:00:00.000Z",
                "2021-05-18T01:00:00.000Z",
                "2021-05-18T02:00:00.000Z",
                "2021-05-18T03:00:00.000Z",
                "2021-05-18T04:00:00.000Z",
                "2021-05-18T05:00:00.000Z",
                "2021-05-18T06:00:00.000Z",
                "2021-05-18T07:00:00.000Z",
                "2021-05-18T08:00:00.000Z",
                "2021-05-18T12:00:00.000Z"
            };
            timestamps.Should().Equal(expectedTimestamps.Select(t => JsonConvert.ToDateTime(t)));
        }

        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Densify_the_full_range_of_values_example_using_aggregate_should_work(bool usingExpressions)
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateCoffeeCollection();
            var subject = collection.Aggregate();

            IAggregateFluent<CoffeeInfo> aggregate;
            if (usingExpressions)
            {
                aggregate = subject.Densify(
                    field: x => x.Altitude,
                    range: DensifyRange.Numeric(DensifyBounds.Full, step: 200),
                    partitionByFields: x => x.Variety);
            }
            else
            {
                aggregate = subject.Densify(
                    field: "Altitude",
                    range: DensifyRange.Numeric(DensifyBounds.Full, step: 200),
                    partitionByFields: "Variety");
            }

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $densify : { field : 'Altitude', partitionByFields : ['Variety'], range : { step : 200, bounds : 'full' } } }");

            var results = aggregate.ToList().Select(r => new { Variety = r.Variety, Altitude = r.Altitude }).OrderBy(r => r.Variety).ThenBy(r => r.Altitude).ToList();
            var expectedResults = new[]
            {
                new { Variety = "Arabica Typica", Altitude = 600 },
                new { Variety = "Arabica Typica", Altitude = 750 },
                new { Variety = "Arabica Typica", Altitude = 800 },
                new { Variety = "Arabica Typica", Altitude = 950 },
                new { Variety = "Arabica Typica", Altitude = 1000 },
                new { Variety = "Arabica Typica", Altitude = 1200 },
                new { Variety = "Arabica Typica", Altitude = 1400 },
                new { Variety = "Arabica Typica", Altitude = 1600 },
                new { Variety = "Gesha", Altitude = 600 },
                new { Variety = "Gesha", Altitude = 800 },
                new { Variety = "Gesha", Altitude = 1000 },
                new { Variety = "Gesha", Altitude = 1200 },
                new { Variety = "Gesha", Altitude = 1250 },
                new { Variety = "Gesha", Altitude = 1400 },
                new { Variety = "Gesha", Altitude = 1600 },
                new { Variety = "Gesha", Altitude = 1700 }
           };
            results.Should().Equal(expectedResults);
        }

        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Fact]
        public void Densify_the_full_range_of_values_example_using_linq_should_work()
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateCoffeeCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.Densify(
                field: x => x.Altitude,
                range: DensifyRange.Numeric(DensifyBounds.Full, step: 200),
                partitionByFields: x => x.Variety);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $densify : { field : 'Altitude', partitionByFields : ['Variety'], range : { step : 200, bounds : 'full' } } }");

            var results = queryable.ToList().Select(r => new { Variety = r.Variety, Altitude = r.Altitude }).OrderBy(r => r.Variety).ThenBy(r => r.Altitude).ToList();
            var expectedResults = new[]
            {
                new { Variety = "Arabica Typica", Altitude = 600 },
                new { Variety = "Arabica Typica", Altitude = 750 },
                new { Variety = "Arabica Typica", Altitude = 800 },
                new { Variety = "Arabica Typica", Altitude = 950 },
                new { Variety = "Arabica Typica", Altitude = 1000 },
                new { Variety = "Arabica Typica", Altitude = 1200 },
                new { Variety = "Arabica Typica", Altitude = 1400 },
                new { Variety = "Arabica Typica", Altitude = 1600 },
                new { Variety = "Gesha", Altitude = 600 },
                new { Variety = "Gesha", Altitude = 800 },
                new { Variety = "Gesha", Altitude = 1000 },
                new { Variety = "Gesha", Altitude = 1200 },
                new { Variety = "Gesha", Altitude = 1250 },
                new { Variety = "Gesha", Altitude = 1400 },
                new { Variety = "Gesha", Altitude = 1600 },
                new { Variety = "Gesha", Altitude = 1700 }
           };
            results.Should().Equal(expectedResults);
        }

        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Densify_values_within_each_partition_example_using_aggregate_should_work(bool usingExpressions)
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateCoffeeCollection();
            var subject = collection.Aggregate();

            IAggregateFluent<CoffeeInfo> aggregate;
            if (usingExpressions)
            {
                aggregate = subject.Densify(
                    field: x => x.Altitude,
                    range: DensifyRange.Numeric(DensifyBounds.Partition, step: 200),
                    partitionByFields: x => x.Variety);
            }
            else
            {
                aggregate = subject.Densify(
                    field: "Altitude",
                    range: DensifyRange.Numeric(DensifyBounds.Partition, step: 200),
                    partitionByFields: "Variety");
            }

            var stages = Translate(collection, aggregate);
            AssertStages(stages, "{ $densify : { field : 'Altitude', partitionByFields : ['Variety'], range : { step : 200, bounds : 'partition' } } }");

            var results = aggregate.ToList().Select(r => new { Variety = r.Variety, Altitude = r.Altitude }).OrderBy(r => r.Variety).ThenBy(r => r.Altitude).ToList();
            var expectedResults = new[]
            {
                new { Variety = "Arabica Typica", Altitude = 600 },
                new { Variety = "Arabica Typica", Altitude = 750 },
                new { Variety = "Arabica Typica", Altitude = 800 },
                new { Variety = "Arabica Typica", Altitude = 950 },
                new { Variety = "Gesha", Altitude = 1250 },
                new { Variety = "Gesha", Altitude = 1450 },
                new { Variety = "Gesha", Altitude = 1650 },
                new { Variety = "Gesha", Altitude = 1700 }
           };
            results.Should().Equal(expectedResults);
        }

        // this example is from: https://www.mongodb.com/docs/v5.2/reference/operator/aggregation/densify
        [Fact]
        public void Densify_values_within_each_partition_example_using_linq_should_work()
        {
            RequireServer.Check().Supports(Feature.DensifyStage);
            var collection = CreateCoffeeCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.Densify(
                field: x => x.Altitude,
                range: DensifyRange.Numeric(DensifyBounds.Partition, step: 200),
                partitionByFields: x => x.Variety);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $densify : { field : 'Altitude', partitionByFields : ['Variety'], range : { step : 200, bounds : 'partition' } } }");

            var results = queryable.ToList().Select(r => new { Variety = r.Variety, Altitude = r.Altitude }).OrderBy(r => r.Variety).ThenBy(r => r.Altitude).ToList();
            var expectedResults = new[]
            {
                new { Variety = "Arabica Typica", Altitude = 600 },
                new { Variety = "Arabica Typica", Altitude = 750 },
                new { Variety = "Arabica Typica", Altitude = 800 },
                new { Variety = "Arabica Typica", Altitude = 950 },
                new { Variety = "Gesha", Altitude = 1250 },
                new { Variety = "Gesha", Altitude = 1450 },
                new { Variety = "Gesha", Altitude = 1650 },
                new { Variety = "Gesha", Altitude = 1700 }
           };
            results.Should().Equal(expectedResults);
        }

        private IMongoCollection<WeatherInfo> CreateWeatherCollection()
        {
            var collection = GetCollection<WeatherInfo>();

            var documents = new[]
            {
                new WeatherInfo { Id = 1, Metadata = new MetadataInfo { SensorId = 5578, Type = "Temperature" }, Timestamp = JsonConvert.ToDateTime("2021-05-18T00:00:00.000Z"), Temp = 12 },
                new WeatherInfo { Id = 2, Metadata = new MetadataInfo { SensorId = 5578, Type = "Temperature" }, Timestamp = JsonConvert.ToDateTime("2021-05-18T04:00:00.000Z"), Temp = 11 },
                new WeatherInfo { Id = 3, Metadata = new MetadataInfo { SensorId = 5578, Type = "Temperature" }, Timestamp = JsonConvert.ToDateTime("2021-05-18T08:00:00.000Z"), Temp = 11 },
                new WeatherInfo { Id = 4, Metadata = new MetadataInfo { SensorId = 5578, Type = "Temperature" }, Timestamp = JsonConvert.ToDateTime("2021-05-18T12:00:00.000Z"), Temp = 12 }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        private IMongoCollection<CoffeeInfo> CreateCoffeeCollection()
        {
            var collection = GetCollection<CoffeeInfo>();

            var documents = new[]
            {
                new CoffeeInfo { Id = 1, Altitude = 600, Variety = "Arabica Typica", Score = 68.3 },
                new CoffeeInfo { Id = 2, Altitude = 750, Variety = "Arabica Typica", Score = 69.5 },
                new CoffeeInfo { Id = 3, Altitude = 950, Variety = "Arabica Typica", Score = 70.5 },
                new CoffeeInfo { Id = 4, Altitude = 1250, Variety = "Gesha", Score = 88.15 },
                new CoffeeInfo { Id = 5, Altitude = 1700, Variety = "Gesha", Score = 95.5, Price = 1029.00 }
            };
            CreateCollection(collection, documents);

            return collection;
        }

        public class WeatherInfo
        {
            public int Id { get; set; }
            public MetadataInfo Metadata { get; set; }
            public DateTime Timestamp { get; set; }
            public int Temp { get; set; }
        }

        public class MetadataInfo
        {
            public int SensorId { get; set; }
            public string Type { get; set; }
        }

        public class CoffeeInfo
        {
            public int Id { get; set; }
            public int Altitude { get; set; }
            public string Variety { get; set; }
            public double Score { get; set; }
            public double Price { get; set; }
        }
    }
}
