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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4500Tests : LinqIntegrationTest<CSharp4500Tests.ClassFixture>
    {
        public CSharp4500Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Where_should_work()
        {
            var collection = Fixture.Collection;
            var input = "B";

            var queryable =
                collection.AsQueryable()
                .Where(x => x.List.Any(xx => xx.ToLower().Contains(input.ToLower())));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { List : /b/is } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        [Fact]
        public void Where_with_inner_not_should_work()
        {
            var collection = Fixture.Collection;
            var input = "B";

            var queryable =
                collection.AsQueryable()
                .Where(x => x.List.Any(xx => !xx.ToLower().Contains(input.ToLower())));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { List : { $elemMatch : { $not : /b/is } } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1, 2);
        }

        [Fact]
        public void Where_with_outer_not_should_work()
        {
            var collection = Fixture.Collection;
            var input = "B";

            var queryable =
                collection.AsQueryable()
                .Where(x => !x.List.Any(xx => xx.ToLower().Contains(input.ToLower())));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { List : { $not : /b/is } } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(2);
        }

        public class C
        {
            public int Id { get; set; }
            public List<string> List { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, List = new List<string> { "abc", "def" } },
                new C { Id = 2, List = new List<string> { "ghi", "jkl" } }
            ];
        }
    }
}
