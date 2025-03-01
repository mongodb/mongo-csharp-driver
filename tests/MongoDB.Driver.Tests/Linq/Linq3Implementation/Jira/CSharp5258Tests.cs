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
    public class CSharp5258Tests : LinqIntegrationTest<CSharp5258Tests.ClassFixture>
    {
        public CSharp5258Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Fact]
        public void Select_First_with_predicate_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(_x => _x.List.First(_y => _y > 2));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $arrayElemAt : [{ $filter : { input : '$List', as : 'v__0', cond : { $gt : ['$$v__0', 2] } } }, 0]  }, _id : 0 } }");

            var results = queryable.ToList();
            results.Should().Equal(3, 4);
        }

        [Fact]
        public void Select_Where_should_work()
        {
            var collection = Fixture.Collection;

            var queryable = collection.AsQueryable()
                .Select(_x => _x.List.Where(_y => _y % 2 == 0));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $filter : { input : '$List', as : 'v__0', cond : { $eq : [{ $mod : ['$$v__0', 2] }, 0] } } }, _id : 0 } }");

            var results = queryable.ToList();
            results.Count.Should().Be(2);
            results[0].Should().Equal();
            results[1].Should().Equal(2, 4, 6);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] List { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, List = [1, 3, 5] },
                new C { Id = 2, List = [2, 4, 6] }
            ];
        }
    }
}
