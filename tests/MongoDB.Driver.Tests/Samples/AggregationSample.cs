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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using Xunit;

namespace MongoDB.Driver.Tests.Samples
{
    public class AggregationSample
    {
        private static IMongoCollection<ZipEntry> __collection;
        private static bool __oneTimeSetupHasRun = false;
        private static object __oneTimeSetupLock = new object();

        public AggregationSample()
        {
            lock (__oneTimeSetupLock)
            {
                __oneTimeSetupHasRun = __oneTimeSetupHasRun || OneTimeSetup();
            }
        }

        public bool OneTimeSetup()
        {
            var client = DriverTestConfiguration.Client;
            var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            db.DropCollection(DriverTestConfiguration.CollectionNamespace.CollectionName);
            __collection = db.GetCollection<ZipEntry>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            // This is a subset of the data from the mongodb docs zip code aggregation examples
            __collection.InsertMany(new[]
            {
                new ZipEntry { Zip = "01053", City = "LEEDS", State = "MA", Population = 1350 },
                new ZipEntry { Zip = "01054", City = "LEVERETT", State = "MA", Population = 1748 },
                new ZipEntry { Zip = "01056", City = "LUDLOW", State = "MA", Population = 18820 },
                new ZipEntry { Zip = "01057", City = "MONSON", State = "MA", Population = 8194 },
                new ZipEntry { Zip = "36779", City = "SPROTT", State = "AL", Population = 1191 },
                new ZipEntry { Zip = "36782", City = "SWEET WATER", State = "AL", Population = 2444 },
                new ZipEntry { Zip = "36783", City = "THOMASTON", State = "AL", Population = 1527 },
                new ZipEntry { Zip = "36784", City = "THOMASVILLE", State = "AL", Population = 6229 },
            });

            return true;
        }

        [Fact]
        public async Task States_with_pops_over_20000()
        {
            var collection = GetCollection();
            var pipeline = collection.Aggregate()
                .Group(x => x.State, g => new { State = g.Key, TotalPopulation = g.Sum(x => x.Population) })
                .Match(x => x.TotalPopulation > 20000);

            var pipelineTranslation = pipeline.ToString();
            var expectedTranslation =
                "aggregate([" +
                "{ \"$group\" : { \"_id\" : \"$state\", \"__agg0\" : { \"$sum\" : \"$pop\" } } }, " +
                "{ \"$project\" : { \"State\" : \"$_id\", \"TotalPopulation\" : \"$__agg0\", \"_id\" : 0 } }, " +
                "{ \"$match\" : { \"TotalPopulation\" : { \"$gt\" : 20000 } } }])";
            pipelineTranslation.Should().Be(expectedTranslation);

            var result = await pipeline.ToListAsync();

            result.Count.Should().Be(1);
        }

        [Fact]
        public async Task States_with_pops_over_20000_queryable_method()
        {
            var collection = GetCollection();
            var pipeline = collection.AsQueryable()
                .GroupBy(x => x.State, (k, s) => new { State = k, TotalPopulation = s.Sum(x => x.Population) })
                .Where(x => x.TotalPopulation > 20000);

            var result = await pipeline.ToListAsync();

            result.Count.Should().Be(1);
        }

#if !MONO
        [Fact]
        public void States_with_pops_over_20000_queryable_syntax()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(__collection.CollectionNamespace.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<ZipEntry>(__collection.CollectionNamespace.CollectionName);

            var queryable = from z in collection.AsQueryable()
                           group z by z.State into g
                           where g.Sum(x => x.Population) > 20000
                           select new { State = g.Key, TotalPopulation = g.Sum(x => x.Population) };

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = 
                new[]
                {
                    "{ $group : { _id : '$state', __agg0 : { $sum : '$pop' } } }",
                    "{ $match : { $expr : { $gt : ['$__agg0', 20000] } } }",
                    "{ $project : { State : '$_id', TotalPopulation : '$__agg0', _id : 0 } }"
                };
            Linq3TestHelpers.AssertStages(stages, expectedStages);

            var result = queryable.ToList();
            result.Count.Should().Be(1);
        }
#endif

        [Fact]
        public async Task Average_city_population_by_state()
        {
            var collection = GetCollection();
            var pipeline = collection.Aggregate()
                .Group(x => new { State = x.State, City = x.City }, g => new { StateAndCity = g.Key, Population = g.Sum(x => x.Population) })
                .Group(x => x.StateAndCity.State, g => new { State = g.Key, AverageCityPopulation = g.Average(x => x.Population) })
                .SortBy(x => x.State);

            var pipelineTranslation = pipeline.ToString();
            var expectedTranslation =
                "aggregate([" +
                "{ \"$group\" : { \"_id\" : { \"State\" : \"$state\", \"City\" : \"$city\" }, \"__agg0\" : { \"$sum\" : \"$pop\" } } }, " +
                "{ \"$project\" : { \"StateAndCity\" : \"$_id\", \"Population\" : \"$__agg0\", \"_id\" : 0 } }, " +
                "{ \"$group\" : { \"_id\" : \"$StateAndCity.State\", \"__agg0\" : { \"$avg\" : \"$Population\" } } }, " +
                "{ \"$project\" : { \"State\" : \"$_id\", \"AverageCityPopulation\" : \"$__agg0\", \"_id\" : 0 } }, " +
                "{ \"$sort\" : { \"State\" : 1 } }])";
            pipelineTranslation.Should().Be(expectedTranslation);

            var result = await pipeline.ToListAsync();

            result[0].State.Should().Be("AL");
            result[0].AverageCityPopulation.Should().Be(2847.75);
            result[1].AverageCityPopulation.Should().Be(7528);
            result[1].State.Should().Be("MA");
        }

        [Fact]
        public async Task Largest_and_smallest_cities_by_state()
        {
            var collection = GetCollection();
            var pipeline = collection.Aggregate()
                .Group(x => new { State = x.State, City = x.City }, g => new { StateAndCity = g.Key, Population = g.Sum(x => x.Population) })
                .SortBy(x => x.Population)
                .Group(x => x.StateAndCity.State, g => new
                {
                    State = g.Key,
                    BiggestCity = g.Last().StateAndCity.City,
                    BiggestPopulation = g.Last().Population,
                    SmallestCity = g.First().StateAndCity.City,
                    SmallestPopulation = g.First().Population
                })
                .Project(x => new
                {
                    x.State,
                    BiggestCity = new { Name = x.BiggestCity, Population = x.BiggestPopulation },
                    SmallestCity = new { Name = x.SmallestCity, Population = x.SmallestPopulation }
                })
                .SortBy(x => x.State);

            var pipelineTranslation = pipeline.ToString();
            var expectedTranslation = 
                "aggregate([" +
                "{ \"$group\" : { \"_id\" : { \"State\" : \"$state\", \"City\" : \"$city\" }, \"__agg0\" : { \"$sum\" : \"$pop\" } } }, " +
                "{ \"$project\" : { \"StateAndCity\" : \"$_id\", \"Population\" : \"$__agg0\", \"_id\" : 0 } }, " +
                "{ \"$sort\" : { \"Population\" : 1 } }, " +
                "{ \"$group\" : { \"_id\" : \"$StateAndCity.State\", \"__agg0\" : { \"$last\" : \"$$ROOT\" }, \"__agg1\" : { \"$first\" : \"$$ROOT\" } } }, " +
                "{ \"$project\" : { \"State\" : \"$_id\", \"BiggestCity\" : \"$__agg0.StateAndCity.City\", \"BiggestPopulation\" : \"$__agg0.Population\", \"SmallestCity\" : \"$__agg1.StateAndCity.City\", \"SmallestPopulation\" : \"$__agg1.Population\", \"_id\" : 0 } }, " +
                "{ \"$project\" : { \"State\" : \"$State\", \"BiggestCity\" : { \"Name\" : \"$BiggestCity\", \"Population\" : \"$BiggestPopulation\" }, \"SmallestCity\" : { \"Name\" : \"$SmallestCity\", \"Population\" : \"$SmallestPopulation\" }, \"_id\" : 0 } }, " +
                "{ \"$sort\" : { \"State\" : 1 } }])";
            pipelineTranslation.Should().Be(expectedTranslation);

            var result = await pipeline.ToListAsync();

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

#if !MONO
        [Fact]
        public async Task Largest_and_smallest_cities_by_state_queryable_syntax()
        {
            var pipeline = from o in
                               (
                                    from z in __collection.AsQueryable()
                                    group z by new { z.State, z.City } into g
                                    select new { StateAndCity = g.Key, Population = g.Sum(x => x.Population) }
                               )
                           orderby o.Population
                           group o by o.StateAndCity.State into g
                           orderby g.Key
                           select new
                           {
                               State = g.Key,
                               BiggestCity = new { Name = g.Last().StateAndCity.City, Population = g.Last().Population },
                               SmallestCity = new { Name = g.First().StateAndCity.City, Population = g.First().Population }
                           };

            var result = await pipeline.ToListAsync();

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
#endif

        private IMongoCollection<ZipEntry> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(__collection.Database.DatabaseNamespace.DatabaseName);
            return database.GetCollection<ZipEntry>(__collection.CollectionNamespace.CollectionName);
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
