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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5877Tests : LinqIntegrationTest<CSharp5877Tests.ClassFixture>
{
    public CSharp5877Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void GroupBy_with_First_field_access_should_use_correct_path_when_source_is_wrapped()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .GroupBy(g => g.Timestamp, v => v)
            .Select(g => g.OrderBy(o => o.DataTimestamp).Last())
            .OrderBy(o => o.Timestamp)
            .GroupBy(g => g.Timestamp, v => v)
            .Select(g => new AggregatedResult
            {
                Timestamp = g.First().Timestamp,
                Price = g.Average(i => i.Price)
            })
            .OrderBy(o => o.Timestamp);

        var stages = Translate<PriceData, AggregatedResult>(queryable);

        AssertStages(
            stages,
            """{ $group : { _id : "$t", _elements : { $push : "$$ROOT" } } }""",
            """{ $project : { _v : { $arrayElemAt : [{ $sortArray : { input : "$_elements", sortBy : { o : 1 } } }, -1] }, _id : 0 } }""",
            """{ $sort : { "_v.t" : 1 } }""",
            """{ $group : { _id : "$_v.t", __agg0 : { $first : "$_v.t" }, __agg1 : { $avg : "$_v.p" } } }""",
            """{ $project : { Timestamp : "$__agg0", Price : "$__agg1", _id : 0 } }""",
            """{ $sort : { Timestamp : 1 } }""");

        var results = queryable.ToList();

        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Timestamp != default);
    }

    public class PriceData
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("t")]
        public DateTime Timestamp { get; set; }

        [BsonElement("o")]
        public DateTime DataTimestamp { get; set; }

        [BsonElement("p")]
        public decimal Price { get; set; }
    }

    public class AggregatedResult
    {
        public DateTime Timestamp { get; set; }
        public decimal Price { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<PriceData>
    {
        protected override IEnumerable<PriceData> InitialData
        {
            get
            {
                var baseTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
                return
                [
                    new PriceData { Id = ObjectId.GenerateNewId(), Timestamp = baseTime, DataTimestamp = baseTime.AddSeconds(1), Price = 100.0m },
                    new PriceData { Id = ObjectId.GenerateNewId(), Timestamp = baseTime, DataTimestamp = baseTime.AddSeconds(2), Price = 101.0m },
                    new PriceData { Id = ObjectId.GenerateNewId(), Timestamp = baseTime.AddMinutes(1), DataTimestamp = baseTime.AddMinutes(1).AddSeconds(1), Price = 102.0m },
                    new PriceData { Id = ObjectId.GenerateNewId(), Timestamp = baseTime.AddMinutes(1), DataTimestamp = baseTime.AddMinutes(1).AddSeconds(2), Price = 103.0m },
                    new PriceData { Id = ObjectId.GenerateNewId(), Timestamp = baseTime.AddMinutes(2), DataTimestamp = baseTime.AddMinutes(2).AddSeconds(1), Price = 104.0m },
                ];
            }
        }
    }
}
