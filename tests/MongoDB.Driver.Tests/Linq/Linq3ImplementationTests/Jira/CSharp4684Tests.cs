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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4684Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Multiple_result_query_executed_stages_can_be_logged_and_retrieved()
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable();
            var provider = (IMongoQueryProvider)queryable.Provider;
            BsonDocument[] loggedStages = null;
            provider.ExecutedStagesLogger = s => loggedStages = s;

            var results = queryable.Where(x => x.X == 1).ToList();

            loggedStages.Should().BeSameAs(provider.MostRecentlyExecutedStages);
            AssertStages(loggedStages, "{ $match : { X : 1 } }");

            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Single_result_query_executed_stages_can_be_logged_and_retrieved()
        {
            var collection = GetCollection();
            var queryable = (IQueryable<C>)collection.AsQueryable(); // cast to IQueryable so First below resolves to Queryable.First
            var provider = (IMongoQueryProvider)queryable.Provider;
            BsonDocument[] loggedStages = null;
            provider.ExecutedStagesLogger = s => loggedStages = s;

            var result = queryable.Where(x => x.X == 1).First();

            loggedStages.Should().BeSameAs(provider.MostRecentlyExecutedStages);
            AssertStages(
                loggedStages,
                "{ $match : { X : 1 } }",
                "{ $limit : 1 }");

            result.Id.Should().Be(1);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>();
            CreateCollection(
                collection,
                new C { Id = 1, X = 1 },
                new C { Id = 2, X = 2 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
