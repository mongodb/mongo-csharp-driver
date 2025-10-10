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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5730Tests : LinqIntegrationTest<CSharp5730Tests.ClassFixture>
{
    public CSharp5730Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Where_String_Compare_greater_than_zero_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(d => string.Compare(d.Key, "a4e48b55-0519-4ab3-b6b9-7c532fc65b56") > 0);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Key : { $gt : 'a4e48b55-0519-4ab3-b6b9-7c532fc65b56' } } }");

        var result = queryable.ToList();
        result.Select(x => x. Id).Should().Equal(4);
    }

    [Fact]
    public void Where_String_CompareTo_greater_than_zero_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(d => d.Key.CompareTo("a4e48b55-0519-4ab3-b6b9-7c532fc65b56") > 0);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { Key : { $gt : 'a4e48b55-0519-4ab3-b6b9-7c532fc65b56' } } }");

        var result = queryable.ToList();
        result.Select(x => x. Id).Should().Equal(4);
    }

    [Fact]
    public void Select_String_Compare_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Select(d => string.Compare(d.Key, "a4e48b55-0519-4ab3-b6b9-7c532fc65b56") > 0);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $project : { _v : { $gt : [{ $cmp : ['$Key', 'a4e48b55-0519-4ab3-b6b9-7c532fc65b56'] }, 0] }, _id : 0 } }");

        var results = queryable.ToList();
        results.Should().Equal(false, false, false, true);
    }

    public class C
    {
        public int Id { get; set; }
        public string Key { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Key = "1b2bc240-ec2a-4a17-8790-8407e3bbb847"},
            new C { Id = 2, Key = "a4e48b55-0519-4ab3-b6b9-7c532fc65b56"},
            new C { Id = 3, Key = "9ff72c5d-189e-4511-b7ad-3f83489e4ea4"},
            new C { Id = 4, Key = "d78ca958-abac-46cd-94a7-fbf7a2ba683d"}
        ];
    }
}
