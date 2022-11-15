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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Jira
{
    public class CSharp3234Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Contains_should_work()
        {
            var collection = CreateCollection();
            var selectedIds = new[] { 1, 2, 3 };

            var queryable = collection
                .AsQueryable()
                .Where(x => selectedIds.Contains(x.Id));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $in : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.OrderBy(x => x.Id).Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        [Fact]
        public void Contains_equals_false_should_work()
        {
            var collection = CreateCollection();
            var selectedIds = new[] { 1, 2, 3 };

            var queryable = collection
                .AsQueryable()
                .Where(x => selectedIds.Contains(x.Id) == false);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $nin : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.OrderBy(x => x.Id).Select(x => x.Id).Should().Equal(4, 5);
        }

        [Fact]
        public void Contains_equals_true_should_work()
        {
            var collection = CreateCollection();
            var selectedIds = new[] { 1, 2, 3 };

            var queryable = collection
                .AsQueryable()
                .Where(x => selectedIds.Contains(x.Id) == true);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : { $in : [1, 2, 3] } } }");

            var results = queryable.ToList();
            results.OrderBy(x => x.Id).Select(x => x.Id).Should().Equal(1, 2, 3);
        }

        private IMongoCollection<C> CreateCollection()
        {
            var collection = GetCollection<C>("C");

            CreateCollection(
                collection,
                new C { Id = 1 },
                new C { Id = 2 },
                new C { Id = 3 },
                new C { Id = 4 },
                new C { Id = 5 });

            return collection;
        }

        private class C
        {
            public int Id { get; set; }
        }
    }
}
