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
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3343Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Add_with_unit_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.S, DateTimeUnit.Day));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        public void Add_with_unit_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.S, DateTimeUnit.Day, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_constant_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(TimeSpan.FromSeconds(1)));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : 1000.0 } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_constant_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(TimeSpan.FromSeconds(1), x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : 1000.0, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Ticks_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanTicks));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanTicks', 10000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Ticks_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanTicks, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanTicks', 10000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Nanoseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanNanoseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanNanoseconds', 1000000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Nanoseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanNanoseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanNanoseconds', 1000000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Microseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMicroseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanMicroseconds', 1000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Microseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMicroseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanMicroseconds', 1000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Milliseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMilliseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : '$TimeSpanMilliseconds' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Milliseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMilliseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : '$TimeSpanMilliseconds', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Seconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanSeconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'second', amount : '$TimeSpanSeconds' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Seconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanSeconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'second', amount : '$TimeSpanSeconds', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Minutes_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMinutes));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'minute', amount : '$TimeSpanMinutes' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Minutes_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanMinutes, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'minute', amount : '$TimeSpanMinutes', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Hours_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanHours));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'hour', amount : '$TimeSpanHours' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Hours_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanHours, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'hour', amount : '$TimeSpanHours', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Days_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanDays));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$TimeSpanDays' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Add_with_TimeSpan_expression_in_Days_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Add(x.TimeSpanDays, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$TimeSpanDays', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddDays_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddDays(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddDays_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddDays(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'day', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddHours_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddHours(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'hour', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddHours_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddHours(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'hour', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMilliseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMilliseconds(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMilliseconds_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMilliseconds(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMinutes_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMinutes(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'minute', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMinutes_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMinutes(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'minute', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMonths_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMonths(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'month', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddMonths_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddMonths(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'month', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddQuarters_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddQuarters(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'quarter', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddQuarters_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddQuarters(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'quarter', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddSeconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddSeconds(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'second', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddSeconds_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddSeconds(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'second', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddTicks_with_constant_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddTicks(10000)); // 10000 ticks is 1 millisecond

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : 1.0 } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddTicks_with_expression_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddTicks(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$S', 10000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddWeeks_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddWeeks(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'week', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddWeeks_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddWeeks(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'week', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddYears_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddYears(x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'year', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void AddYears_with_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.AddYears(x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateAdd : { startDate : '$DateTime', unit : 'year', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'millisecond' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'millisecond', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_Day_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.Day));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'day' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_Week_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.Week));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'week' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_WeekStartingMonday_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.WeekStartingMonday));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'week', startOfWeek : 'monday' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_Day_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.Day, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'day', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_Week_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.Week, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'week', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_DateTime_and_WeekStartingMonday_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime1.Subtract(x.DateTime2, DateTimeUnit.WeekStartingMonday, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateDiff : { startDate : '$DateTime2', endDate : '$DateTime1', unit : 'week', timezone : '$TZ', startOfWeek : 'monday' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        public void Subtract_with_TimeSpan_constant_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(TimeSpan.FromSeconds(1)));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : 1000.0 } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_constant_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(TimeSpan.FromSeconds(1), x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : 1000.0, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Ticks_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanTicks));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanTicks', 10000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Ticks_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanTicks, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanTicks', 10000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Nanoseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanNanoseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanNanoseconds', 1000000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Nanoseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanNanoseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanNanoseconds', 1000000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Microseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMicroseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanMicroseconds', 1000.0] } } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Microseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMicroseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : { $divide : ['$TimeSpanMicroseconds', 1000.0] }, timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Milliseconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMilliseconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : '$TimeSpanMilliseconds' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Milliseconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMilliseconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'millisecond', amount : '$TimeSpanMilliseconds', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Seconds_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanSeconds));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'second', amount : '$TimeSpanSeconds' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Seconds_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanSeconds, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'second', amount : '$TimeSpanSeconds', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Minutes_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMinutes));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'minute', amount : '$TimeSpanMinutes' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Minutes_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanMinutes, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'minute', amount : '$TimeSpanMinutes', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Hours_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanHours));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'hour', amount : '$TimeSpanHours' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Hours_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanHours, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'hour', amount : '$TimeSpanHours', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Days_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanDays));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'day', amount : '$TimeSpanDays' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_TimeSpan_expression_in_Days_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.TimeSpanDays, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'day', amount : '$TimeSpanDays', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Subtract_with_unit_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.S, DateTimeUnit.Day));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'day', amount : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        public void Subtractwith_unit_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Subtract(x.S, DateTimeUnit.Day, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateSubtract : { startDate : '$DateTime', unit : 'day', amount : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public short S { get; set; }
            public DateTime DateTime { get; set; }
            public DateTime DateTime1 { get; set; }
            public DateTime DateTime2 { get; set; }
            public string TZ { get; set; }

            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Ticks)]
            public TimeSpan TimeSpanTicks { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Nanoseconds)]
            public TimeSpan TimeSpanNanoseconds { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Microseconds)]
            public TimeSpan TimeSpanMicroseconds { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Milliseconds)]
            public TimeSpan TimeSpanMilliseconds { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Seconds)]
            public TimeSpan TimeSpanSeconds { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Minutes)]
            public TimeSpan TimeSpanMinutes { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Hours)]
            public TimeSpan TimeSpanHours { get; set; }
            [BsonTimeSpanOptions(BsonType.Int32, TimeSpanUnits.Days)]
            public TimeSpan TimeSpanDays { get; set; }
        }
    }
}
