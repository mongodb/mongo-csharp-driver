﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace MongoDB.Driver.Tests.Samples
{
    public class AggregationSample
    {
        private IMongoCollection<ZipEntry> _collection;

        [SetUp]
        public void SetUp()
        {
            var client = Configuration.TestClient;
            var db = client.GetDatabase(Configuration.TestDatabase.Name);
            db.DropCollectionAsync(Configuration.TestCollection.Name);
            _collection = db.GetCollection<ZipEntry>(Configuration.TestCollection.Name);

            // This is a subset of the data from the mongodb docs zip code aggregation examples
            _collection.InsertManyAsync(new[] 
            {
                new ZipEntry { Zip = "01053", City = "LEEDS", State = "MA", Population = 1350 },
                new ZipEntry { Zip = "01054", City = "LEVERETT", State = "MA", Population = 1748 },
                new ZipEntry { Zip = "01056", City = "LUDLOW", State = "MA", Population = 18820 },
                new ZipEntry { Zip = "01057", City = "MONSON", State = "MA", Population = 8194 },
                new ZipEntry { Zip = "36779", City = "SPROTT", State = "AL", Population = 1191 },
                new ZipEntry { Zip = "36782", City = "SWEET WATER", State = "AL", Population = 2444 },
                new ZipEntry { Zip = "36783", City = "THOMASTON", State = "AL", Population = 1527 },
                new ZipEntry { Zip = "36784", City = "THOMASVILLE", State = "AL", Population = 6229 },
            }).GetAwaiter().GetResult();
        }

        [Test]
        public async Task States_with_pops_over_20000()
        {
            var result = await _collection.Aggregate()
                .Group(x => x.State, g => new { _id = g.Key, TotalPopulation = g.Sum(x => x.Population) })
                .Match(x => x.TotalPopulation > 20000)
                .ToListAsync();

            result.Count.Should().Be(1);
        }

        [Test]
        public async Task Average_city_population_by_state()
        {
            var result = await _collection.Aggregate()
                .Group(x => new { State = x.State, City = x.City }, g => new { _id = g.Key, Population = g.Sum(x => x.Population) })
                .Group(x => x._id.State, g => new { _id = g.Key, AverageCityPopulation = g.Average(x => x.Population) })
                .SortBy(x => x._id)
                .ToListAsync();

            result[0]._id.Should().Be("AL");
            result[0].AverageCityPopulation.Should().Be(2847.75);
            result[1].AverageCityPopulation.Should().Be(7528);
            result[1]._id.Should().Be("MA");
        }

        [Test]
        public async Task Largest_and_smallest_cities_by_state()
        {
            var result = await _collection.Aggregate()
                .Group(x => new { State = x.State, City = x.City }, g => new { _id = g.Key, Population = g.Sum(x => x.Population) })
                .SortBy(x => x.Population)
                .Group(x => x._id.State, g => new
                {
                    _id = g.Key,
                    BiggestCity = g.Last()._id.City,
                    BiggestPopulation = g.Last().Population,
                    SmallestCity = g.First()._id.City,
                    SmallestPopulation = g.First().Population
                })
                .Project(x => new
                {
                    State = x._id,
                    BiggestCity = new { Name = x.BiggestCity, Population = x.BiggestPopulation },
                    SmallestCity = new { Name = x.SmallestCity, Population = x.SmallestPopulation }
                })
                .SortBy(x => x.State)
                .ToListAsync();

            result[0].State.Should().Be("AL");
            result[0].BiggestCity.Name.Should().Be("THOMASVILLE");
            result[0].BiggestCity.Population.Should().Be(6229);
            result[0].SmallestCity.Name.Should().Be("SPROTT");
            result[0].SmallestCity.Population.Should().Be(1191);
            result[1].State.Should().Be("MA");
            result[1].BiggestCity.Name.Should().Be("LUDLOW");
            result[1].BiggestCity.Population.Should().Be(18820);
            result[1].SmallestCity.Name.Should().Be("LEEDS");
            result[1].SmallestCity.Population.Should().Be(1350);
        }

        [BsonIgnoreExtraElements]
        private class ZipEntry
        {
            [BsonId]
            public string Zip { get; set; }

            [BsonElement("city")]
            public string City { get; set; }

            [BsonElement("state")]
            public string State { get; set; }

            [BsonElement("pop")]
            public int Population { get; set; }
        }
    }
}
