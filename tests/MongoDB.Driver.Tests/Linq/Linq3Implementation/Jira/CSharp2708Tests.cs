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
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2708Tests
    {
        [Fact]
        public void IsNullOrEmpty_in_aggregate_match_should_work()
        {
            var collection = GetCollection();
            var subject = collection.Aggregate();

            var aggregate = subject.Match(x => string.IsNullOrEmpty(x.S));

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $match : { S : { $in : [null, ''] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void IsNullOrEmpty_with_null_coalescing_operator_in_aggregate_match_should_work()
        {
            var collection = GetCollection();
            var subject = collection.Aggregate();

            var aggregate = subject.Match(x => string.IsNullOrEmpty(x.S ?? ""));

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $in : [{ $ifNull : ['$S', ''] }, [null, '']] } } }" // requires use of $expr
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Null_coalescing_operator_in_aggregate_project_should_work()
        {
            var collection = GetCollection();
            var subject = collection.Aggregate();

            var aggregate = subject.Project(x => x.S ?? "");

            var stages = Linq3TestHelpers.Translate(collection, aggregate);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $ifNull : ['$S', ''] }, _id : 0  } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void Null_coalescing_operator_in_queryable_select_should_work()
        {
            var collection = GetCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.Select(x => x.S ?? "");

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $project : { _v : { $ifNull : ['$S', ''] }, _id : 0  } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void IsNullOrEmpty_in_queryable_where_should_work()
        {
            var collection = GetCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.Where(x => string.IsNullOrEmpty(x.S));

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { S : { $in : [null, ''] } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Fact]
        public void IsNullOrEmpty_with_null_coalescing_operator_in_queryable_where_should_work()
        {
            var collection = GetCollection();
            var subject = collection.AsQueryable();

            var queryable = subject.Where(x => string.IsNullOrEmpty(x.S ?? ""));

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : { $expr : { $in : [{ $ifNull : ['$S', ''] }, [null, '']] } } }" // requires use of $expr
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<C> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("foo");
            return database.GetCollection<C>("foo");
        }

        private class C
        {
            public string S { get; set; }
        }
    }
}
