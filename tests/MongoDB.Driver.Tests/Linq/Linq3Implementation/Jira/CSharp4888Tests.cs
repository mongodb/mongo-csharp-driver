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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4888Tests : Linq3IntegrationTest
    {
        [Fact]
        public void GroupBy_Select_with_int_enum_representation_in_conditional_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => 0)
                .Select(g => new
                {
                    SportCarCount = g.Sum(c => c.CarType == CarType.Sport ? 1 : 0),
                    SuvCarCount = g.Sum(c => c.CarType == CarType.Suv ? 1 : 0)
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 0, __agg0 : { $sum : { $cond : { if : { $eq : ['$CarType', 1] }, then : 1, else : 0 } } }, __agg1 : { $sum : { $cond : { if : { $eq : ['$CarType', 2] }, then : 1, else : 0 } } } } }",
                "{ $project : { SportCarCount : '$__agg0', SuvCarCount : '$__agg1', _id : 0 } }");

            var result = queryable.Single();
            result.SportCarCount.Should().Be(1);
            result.SuvCarCount.Should().Be(0);
        }

        [Fact]
        public void GroupBy_Select_with_string_enum_representation_in_conditional_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .GroupBy(c => 0)
                .Select(g => new
                {
                    SportCarCount = g.Sum(c => c.CarTypeString == CarType.Sport ? 1 : 0),
                    SuvCarCount = g.Sum(c => c.CarTypeString == CarType.Suv ? 1 : 0)
                });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $group : { _id : 0, __agg0 : { $sum : { $cond : { if : { $eq : ['$CarTypeString', 'Sport'] }, then : 1, else : 0 } } }, __agg1 : { $sum : { $cond : { if : { $eq : ['$CarTypeString', 'Suv'] }, then : 1, else : 0 } } } } }",
                "{ $project : { SportCarCount : '$__agg0', SuvCarCount : '$__agg1', _id : 0 } }");

            var result = queryable.Single();
            result.SportCarCount.Should().Be(1);
            result.SuvCarCount.Should().Be(0);
        }

        private IMongoCollection<Car> GetCollection()
        {
            var collection = GetCollection<Car>("test");
            CreateCollection(
                collection,
                new Car { Id = 1, LicensePlate = "abcdef", CarType = CarType.Sport, CarTypeString = CarType.Sport });
            return collection;
        }

        private class Car
        {
            public int Id { get; set; }
            public string LicensePlate { get; set; }
            public CarType CarType { get; set; }
            [BsonRepresentation(BsonType.String)] public CarType CarTypeString { get; set; }
        }

        private enum CarType
        {
            Undefined = 0,
            Sport = 1,
            Suv = 2
        }
    }
}
