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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4524Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Find_with_projection_using_LINQ2_should_work()
        {
            var collection = CreateCollection(LinqProvider.V2);
            var find = collection.Find("{}").Project(x => new SpawnData(x.StartDate, x.SpawnPeriod));

            var results = find.ToList();
            var projection = find.Options.Projection;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<MyData>();
            var renderedProjection = projection.Render(documentSerializer, serializerRegistry, LinqProvider.V2);
            renderedProjection.Document.Should().Be("{ SpawnPeriod : 1, StartDate : 1, _id : 0 }");

            results.Should().HaveCount(1);
            results[0].Date.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results[0].Period.Should().Be(SpawnPeriod.LIVE);
        }

        [Fact]
        public void Find_with_projection_using_LINQ3_should_work()
        {
            RequireServer.Check().Supports(Feature.FindProjectionExpressions);
            var collection = CreateCollection(LinqProvider.V3);
            var find = collection.Find("{}").Project(x => new SpawnData(x.StartDate, x.SpawnPeriod));

            var results = find.ToList();

            var projection = find.Options.Projection;
            var serializerRegistry = BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<MyData>();
            var renderedProjection = projection.Render(documentSerializer, serializerRegistry, LinqProvider.V3);
            renderedProjection.Document.Should().Be("{ Date : '$StartDate', Period : '$SpawnPeriod', _id : 0 }");

            results.Should().HaveCount(1);
            results[0].Date.Should().Be(new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc));
            results[0].Period.Should().Be(SpawnPeriod.LIVE);
        }

        private IMongoCollection<MyData> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<MyData>("data", linqProvider);

            CreateCollection(
                collection,
                new MyData { Id = 1, StartDate = new DateTime(2023, 1, 2, 3, 4, 5, DateTimeKind.Utc), SpawnPeriod = SpawnPeriod.LIVE });

            return collection;
        }

        public class MyData
        {
            public int Id { get; set; }
            public DateTime StartDate;
            public SpawnPeriod SpawnPeriod;
        }

        public enum SpawnPeriod { LIVE, MIDNIGHT, MORNING, EVENING }

        public struct SpawnData
        {
            public readonly DateTime Date;
            public readonly SpawnPeriod Period;

            public SpawnData(DateTime date, SpawnPeriod period)
            {
                // Normally there is more complex handling here, value-type semantics are important, there are custom comparison operators, etc. hence the point of this struct.
                Date = date;
                Period = period;
            }

            public bool Equals(SpawnData other) => Date == other.Date && Period == other.Period;
        }
    }
}
