﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2108Tests : LinqIntegrationTest<CSharp2108Tests.ClassFixture>
    {
        public CSharp2108Tests(ClassFixture fixture)
            : base(fixture, server => server.Supports(Feature.DateOperatorsNewIn50))
        {
        }

        [Fact]
        public void Aggregate_Project_should_work()
        {
            var collection = Fixture.Collection;
            var endDate = DateTime.Parse("2020-01-03Z", null, DateTimeStyles.AdjustToUniversal);

            var queryable = collection.Aggregate()
                .Sort(Builders<C>.Sort.Ascending(x => x.Id))
                .Project(x => new { Days = endDate.Subtract(x.StartDate, DateTimeUnit.Day) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { Days : { $dateDiff : { startDate : '$StartDate', endDate : ISODate('2020-01-03T00:00:00Z'), unit : 'day' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Days = 2 });
            results[1].ShouldBeEquivalentTo(new { Days = 1 });
        }

        [Fact]
        public void Queryable_Select_should_work()
        {
            var collection = Fixture.Collection;
            var endDate = DateTime.Parse("2020-01-03Z", null, DateTimeStyles.AdjustToUniversal);

            var queryable = collection.AsQueryable()
                .OrderBy(x => x.Id)
                .Select(x => new { Days = endDate.Subtract(x.StartDate, DateTimeUnit.Day) });

            var stages = Translate(collection, queryable);
            AssertStages(
                stages,
                "{ $sort : { _id : 1 } }",
                "{ $project : { Days : { $dateDiff : { startDate : '$StartDate', endDate : ISODate('2020-01-03T00:00:00Z'), unit : 'day' } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().HaveCount(2);
            results[0].ShouldBeEquivalentTo(new { Days = 2 });
            results[1].ShouldBeEquivalentTo(new { Days = 1 });
        }

        public class C
        {
            public int Id { get; set; }
            public DateTime StartDate { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, StartDate = DateTime.Parse("2020-01-01Z", null, DateTimeStyles.AdjustToUniversal) },
                new C { Id = 2, StartDate = DateTime.Parse("2020-01-02Z", null, DateTimeStyles.AdjustToUniversal) }
            ];
        }
    }
}
