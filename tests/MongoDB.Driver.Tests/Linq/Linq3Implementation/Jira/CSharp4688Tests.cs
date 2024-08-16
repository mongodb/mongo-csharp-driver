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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4688Tests : Linq3IntegrationTest
    {
        [Theory]
        [ParameterAttributeData]
        public async Task IQueryable_Any_should_add_expected_stages(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable();

            var result = async ? await queryable.AnyAsync() : queryable.Any();

            AssertStages(
                queryable.GetLoggedStages(),
                "{ $limit : 1 }",
                "{ $project : { _id : 0, _v : null } }");
            result.Should().Be(true);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task IQueryable_First_should_add_expected_stages(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable();

            var result = async ? await queryable.FirstAsync() : queryable.First();

            AssertStages(queryable.GetLoggedStages(), "{ $limit : 1 }");
            result.Id.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task IQueryable_FirstOrDefault_should_add_expected_stages(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable();

            var result = async ? await queryable.FirstOrDefaultAsync() : queryable.FirstOrDefault();

            AssertStages(queryable.GetLoggedStages(), "{ $limit : 1 }");
            result.Id.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task IQueryable_Single_should_add_expected_stages(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            var result = async ? await queryable.SingleAsync() : queryable.Single();

            AssertStages(
                queryable.GetLoggedStages(),
                "{ $match : { X : 1 } }",
                "{ $limit : 2 }");
            result.Id.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task IQueryable_SingleOrDefault_should_add_expected_stages(
            [Values(false, true)] bool async)
        {
            var collection = GetCollection();
            var queryable = collection.AsQueryable().Where(x => x.X == 1);

            var result = async ? await queryable.SingleOrDefaultAsync() : queryable.SingleOrDefault();

            AssertStages(
                queryable.GetLoggedStages(),
                "{ $match : { X : 1 } }",
                "{ $limit : 2 }");
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
