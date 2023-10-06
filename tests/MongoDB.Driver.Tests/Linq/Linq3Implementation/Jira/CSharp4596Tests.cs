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
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4596Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public void Subtract_DateTimes_should_work(
            [Values(LinqProvider.V2, LinqProvider.V3)] LinqProvider linqProvider)
        {
            RequireServer.Check().Supports(Feature.DateOperatorsNewIn50);
            var collection = CreateCollection(linqProvider);
            var startTime = new DateTime(2023, 04, 04, 0, 0, 0, DateTimeKind.Utc);

            var queryable = collection.AsQueryable()
                .Select(record => record.DateTimeUtc.Subtract(startTime, DateTimeUnit.Millisecond) / (double)5);

            if (linqProvider == LinqProvider.V2)
            {
                var exception = Record.Exception(() => Translate(collection, queryable));
                exception.Should().BeOfType<NotSupportedException>();
            }
            else
            {
                var stages = Translate(collection, queryable);
                AssertStages(
                    stages,
                    "{ $project : { _v : { $divide : [{ $dateDiff : { startDate : ISODate('2023-04-04T00:00:00Z'), endDate : '$DateTimeUtc', unit : 'millisecond' } }, 5.0] }, _id : 0 } }");

                var results = queryable.ToList();
                results.Should().Equal(200.0);
            }
        }

        private IMongoCollection<C> CreateCollection(LinqProvider linqProvider)
        {
            var collection = GetCollection<C>("test", linqProvider);
            CreateCollection(
                collection,
                new C { Id = 1, DateTimeUtc = new DateTime(2023, 04, 04, 0, 0, 1, DateTimeKind.Utc) });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime DateTimeUtc { get; set; }
        }
    }
}
