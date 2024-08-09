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
using System.Linq.Expressions;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4803Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Find_filter_with_nonnullable_date_fields_should_work()
        {
            var collection = GetCollection();
            var fromDate = new DateTime(2023, 9, 27, 0, 0, 0, DateTimeKind.Utc);
            Expression<Func<C, bool>> filter = ft => ft.Date >= fromDate;

            var find = collection.Find(filter);

            var renderedFilter = TranslateFindFilter(collection, find);
            renderedFilter.Should().Be("{ Date : { $gte : ISODate('2023-09-27T00:00:00Z') } }");
        }

        [Fact]
        public void Find_filter_with_nullable_date_fields_should_work()
        {
            var collection = GetCollection();
            var fromDate = new DateTime(2023, 9, 27, 0, 0, 0, DateTimeKind.Utc);
            Expression<Func<C, bool>> filter = ft => ft.NullableDate >= fromDate;

            var find = collection.Find(filter);

            var renderedFilter = TranslateFindFilter(collection, find);
            renderedFilter.Should().Be("{ NullableDate : { $gte : ISODate('2023-09-27T00:00:00Z') } }");
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public DateTime? NullableDate { get; set; }
        }
    }
}
