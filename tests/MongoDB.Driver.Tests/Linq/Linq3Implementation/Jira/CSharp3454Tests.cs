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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp3454Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Truncate_with_Day_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Day));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'day' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_Day_and_binSize_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Day, x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'day', binSize : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_Week_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Week));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_Week_and_binSize_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Week, x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week', binSize : '$S' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_WeekStartingMonday_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.WeekStartingMonday));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week', startOfWeek : 'monday' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_WeekStartingMonday_and_binSize_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.WeekStartingMonday, x.S));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week', binSize : '$S', startOfWeek : 'monday' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_Day_and_binSize_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Day, x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'day', binSize : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_Week_and_binSize_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.Week, x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week', binSize : '$S', timezone : '$TZ' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Truncate_with_WeekStartingMonday_and_binSize_and_timezone_should_work()
        {
            var collection = GetCollection<C>();

            var queryable = collection.AsQueryable()
                .Select(x => x.DateTime.Truncate(DateTimeUnit.WeekStartingMonday, x.S, x.TZ));

            var stages = Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $dateTrunc : { date : '$DateTime', unit : 'week', binSize : '$S', timezone : '$TZ', startOfWeek : 'monday' } }, _id : 0 } }"
            };
            AssertStages(stages, expectedStages);
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime DateTime { get; set; }
            public short S { get; set; }
            public string TZ { get; set; }
        }
    }
}
