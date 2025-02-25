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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4888Tests : LinqIntegrationTest<CSharp4888Tests.ClassFixture>
    {
        public CSharp4888Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void GroupBy_Select_with_int_enum_representation_in_conditional_should_work()
        {
            var collection = Fixture.Collection;

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
            var collection = Fixture.Collection;

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

        public class Car
        {
            public int Id { get; set; }
            public string LicensePlate { get; set; }
            public CarType CarType { get; set; }
            [BsonRepresentation(BsonType.String)] public CarType CarTypeString { get; set; }
        }

        public enum CarType
        {
            Undefined = 0,
            Sport = 1,
            Suv = 2
        }

        public sealed class ClassFixture : MongoCollectionFixture<Car>
        {
            protected override IEnumerable<Car> InitialData =>
            [
                new Car { Id = 1, LicensePlate = "abcdef", CarType = CarType.Sport, CarTypeString = CarType.Sport }
            ];
        }
    }
}
