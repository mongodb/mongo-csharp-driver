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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4651Tests : LinqIntegrationTest<CSharp4651Tests.ClassFixture>
    {
        public CSharp4651Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void First_custom_projection_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(c => c.VehicleType == VehicleType.Car)
                .Select(ToCustomProjection_Works(VehicleType.Car));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { VehicleType : 0 } }",
                "{ $project : { _id : '$_id', Description : '$Description' } }");

            var results = queryable.ToList();
            var result = results.Single();
            result.Id.Should().Be("5555XXX");
            result.Description.Should().Be($"Description for license: {result.Id}");
        }

        [Fact]
        public void Second_custom_projection_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Where(c => c.VehicleType == VehicleType.Truck)
                .Select(ToCustomProjection_Fails(VehicleType.Truck));

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { VehicleType : 1 } }",
                "{ $project : { _id : '$_id', Description : 'No description available for trucks' } }");

            var results = queryable.ToList();
            var result = results.Single();
            result.Id.Should().Be("6666YYY");
            result.Description.Should().Be("No description available for trucks");
        }

        private static Expression<Func<Car, CarDto>> ToCustomProjection_Works(VehicleType vehicleType)
        {
            var isTruck = vehicleType == VehicleType.Truck;
            return c => new CarDto
            {
                Id = c.Id,
                Description = isTruck ? "No description available for trucks" : c.Description
            };
        }

        private static Expression<Func<Car, CarDto>> ToCustomProjection_Fails(VehicleType vehicleType)
        {
            return c => new CarDto
            {
                Id = c.Id,
                Description = vehicleType == VehicleType.Truck ? "No description available for trucks" : c.Description
            };
        }

        public enum VehicleType
        {
            Car = 0,
            Truck = 1
        }

        public class Car
        {
            public string Id { get; set; }
            public string Description { get; set; }
            public VehicleType VehicleType { get; set; }
        }

        public class CarDto
        {
            public string Id { get; set; }
            public string Description { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<Car>
        {
            protected override IEnumerable<Car> InitialData =>
            [
                new Car
                {
                    Id = "5555XXX",
                    Description = $"Description for license: 5555XXX",
                    VehicleType = VehicleType.Car
                },
                new Car
                {
                    Id = "6666YYY",
                    Description = $"Description for license: 6666YYY",
                    VehicleType = VehicleType.Truck
                }
            ];
        }
    }
}
