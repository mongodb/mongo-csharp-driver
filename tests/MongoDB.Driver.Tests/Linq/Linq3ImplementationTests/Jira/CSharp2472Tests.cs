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
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp2472Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Numeric_casts_should_work()
        {
            RequireServer.Check().Supports(Feature.ToConversionOperators);
            var collection = CreateCollection();
            var equipmentId = 1;
            var startDate = new DateTime(2022, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2022, 01, 02, 0, 0, 0, DateTimeKind.Utc);

            var queryable = collection
                .AsQueryable()
                .Where(
                    q => q.equipment_id == equipmentId
                    && q.timestamp >= startDate
                    && q.timestamp <= endDate
                )
                .GroupBy(g => g.timestamp)
                .Select(p => new MyDTO
                {
                    timestamp = p.Key,
                    sqrt_calc = (decimal)Math.Sqrt(
                        p.Sum(x => (double)x.my_decimal_value)
                    )
                })
                .OrderBy(q => q.timestamp);

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $match : { equipment_id : 1, timestamp : { $gte : ISODate('2022-01-01T00:00:00Z'), $lte : ISODate('2022-01-02T00:00:00Z') } } }",
                "{ $group : { _id : '$timestamp', __agg0 : { $sum : { $toDouble : '$my_decimal_value' } } } }",
                "{ $project : { timestamp : '$_id', sqrt_calc : { $toDecimal : { $sqrt : '$__agg0' } }, _id : 0 } }",
                "{ $sort : { timestamp : 1 } }");

            var result = queryable.Single();
            result.timestamp.Should().Be(new DateTime(2022, 01, 01, 12, 0, 0, DateTimeKind.Utc));
            result.sqrt_calc.Should().Be(2M);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1, equipment_id = 1, timestamp = new DateTime(2022, 01, 01, 12, 0, 0, DateTimeKind.Utc), my_decimal_value = 4M });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int equipment_id { get; set; }
            public DateTime timestamp { get; set; }
            public decimal my_decimal_value { get; set; }
        }

        private class MyDTO
        {
            public DateTime timestamp { get; set; }
            public decimal sqrt_calc { get; set; }
        }
    }
}
