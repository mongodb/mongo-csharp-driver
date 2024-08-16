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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Tests.Linq.Linq3Implementation;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp4684Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public async Task Multiple_result_query_logged_stages_can_be_retrieved_using_IQueryable_GetLoggedStages_method(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            var results = async ? await queryable.ToListAsync() : queryable.ToList();

            AssertStages(queryable.GetLoggedStages(), "{ $match : { X : 1 } }");
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Single_result_query_logged_stages_can_be_retrieved_using_IQueryable_GetLoggedStages_method(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            // cast to IQueryable so First below resolves to Queryable.First instead of IAsyncCursorSource.First
            var result = async ? await queryable.FirstAsync() : ((IQueryable<C>)queryable).First();

            AssertStages(
                queryable.GetLoggedStages(),
                "{ $match : { X : 1 } }",
                "{ $limit : 1 }");
            result.Id.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Multiple_result_query_logged_stages_can_be_retrieved_using_IMongoQueryProvider_LoggedStages_property(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            var results = async ? await queryable.ToListAsync() : queryable.ToList();

            AssertStages(((IMongoQueryProvider)queryable.Provider).LoggedStages, "{ $match : { X : 1 } }");
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Single_result_query_logged_stages_can_be_retrieved_using_IMongoQueryProvider_LoggedStages_property(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            // cast to IQueryable so First below resolves to Queryable.First instead of IAsyncCursorSource.First
            var result = async ? await queryable.FirstAsync() : ((IQueryable<C>)queryable).First();

            AssertStages(
                ((IMongoQueryProvider)queryable.Provider).LoggedStages,
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
