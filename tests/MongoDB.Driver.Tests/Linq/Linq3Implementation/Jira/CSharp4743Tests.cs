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
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Linq;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4743Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Where_using_DateTime_Date_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.DateOperatorsNewIn50);
            var collection = GetCollection(linqProvider);
            var memberId = 1;
            var startDateTime = new DateTime(2023, 08, 07, 1, 2, 3, DateTimeKind.Utc);

            var queryable = collection.AsQueryable()
                 .Where(
                    b => b.MemberId == memberId &&
                    b.InteractionDate.HasValue && b.InteractionDate.Value.Date >= startDateTime.Date);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { $and : [{ MemberId : 1 }, { InteractionDate : { $ne : null } }, { $expr : { $gte : [{ $dateTrunc : { date : '$InteractionDate', unit : 'day' } }, ISODate('2023-08-07')] } }] } }");

                var result = queryable.Single();
                result.Id.Should().Be(1);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_using_DateTime_TimeOfDay_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.DateOperatorsNewIn50);
            var collection = GetCollection(linqProvider);
            var memberId = 1;
            var startTimeOfDay = TimeSpan.FromHours(1);

            var queryable = collection.AsQueryable()
                 .Where(
                    b => b.MemberId == memberId &&
                    b.InteractionDate.HasValue && b.InteractionDate.Value.TimeOfDay >= startTimeOfDay);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { $and : [{ MemberId : 1 }, { InteractionDate : { $ne : null } }, { $expr : { $gte : [{ $dateDiff : { startDate : { $dateTrunc : { date : '$InteractionDate', unit : 'day' } }, endDate : '$InteractionDate', unit : 'millisecond' } }, { $numberLong : 3600000 }] } }] } }");

                var result = queryable.Single();
                result.Id.Should().Be(1);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_using_DateTime_Year_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            var collection = GetCollection(linqProvider);
            var memberId = 1;
            var startDateTime = new DateTime(2023, 08, 07, 0, 0, 0, DateTimeKind.Utc);

            var queryable = collection.AsQueryable()
                 .Where(
                    b => b.MemberId == memberId &&
                    b.InteractionDate.HasValue && b.InteractionDate.Value.Year >= startDateTime.Year);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<InvalidOperationException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $match : { $and : [{ MemberId : 1 }, { InteractionDate : { $ne : null } }, { $expr : { $gte : [{ $year : '$InteractionDate' }, 2023] } }] } }");

                var result = queryable.Single();
                result.Id.Should().Be(1);
            }
        }

        private IMongoCollection<C> GetCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, MemberId = 1, InteractionDate = new DateTime(2023, 08, 07, 1, 2, 3, DateTimeKind.Utc) });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int MemberId { get; set; }
            public DateTime? InteractionDate { get; set; }
        }
    }
}
