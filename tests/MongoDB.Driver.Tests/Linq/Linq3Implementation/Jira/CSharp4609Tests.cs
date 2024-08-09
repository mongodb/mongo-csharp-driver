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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4609Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Project_dictionary_value_should_work()
        {
            var collection = CreateCollection();
            Expression<Func<MeasurementDetails, object>> field = x => x.CreationDate;
            var myValue = "2023-04-13T01:02:03Z";
            var filter = Builders<MeasurementDetails>.Filter.Lt(field, myValue);

            var find = collection.Find(filter);

            var translatedFilter = TranslateFilter(collection, filter);
            translatedFilter.Should().Be("{ CreationDate : { $lt : ISODate('2023-04-13T01:02:03Z') } }");

            var results = find.ToList();
            results.Select(r => r.Id).Should().Equal(1);
        }

        private IMongoCollection<MeasurementDetails> CreateCollection()
        {
            var collection = GetCollection<MeasurementDetails>("test");
            CreateCollection(
                collection,
                new MeasurementDetails { Id = 1, CreationDate = new DateTime(2023, 04, 13, 1, 2, 2, DateTimeKind.Utc) },
                new MeasurementDetails { Id = 2, CreationDate = new DateTime(2023, 04, 13, 1, 2, 3, DateTimeKind.Utc) });
            return collection;
        }

        private class MeasurementDetails
        {
            public int Id { get; set; }
            public DateTime CreationDate { get; set; }
        }
    }
}
