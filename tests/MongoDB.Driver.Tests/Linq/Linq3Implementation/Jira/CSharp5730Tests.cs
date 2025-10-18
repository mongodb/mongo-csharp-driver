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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

    [Theory]
    [InlineData( 1, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData( 2, "{ $match : { A : 'B' } }", new int[] { 3 })]
    [InlineData( 3, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData( 4, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData( 5, "{ $match : { A : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData( 6, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData( 7, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData( 8, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData( 9, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(10, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(11, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(12, "{ $match : { A : { $gte : 'B' } } }", new int[] { 1, 3, 4, 5, 6 })]
    public void Where_String_Compare_field_to_constant_should_work(int scenario, string expectedStage, int[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") == -1),
            2 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") == 0),
            3 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") == 1),
            4 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") != -1),
            5 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") != 0),
            6 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") != 1),
            7 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") > -1),
            8 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") > 0),
            9 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") < 0),
            10 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") < 1),
            11 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare(x.A, "B") >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    private void Assert(IMongoCollection<C> collection, IQueryable<C> queryable, string expectedStage, int[] expectedResults)
    {
        var stages = Translate(collection, queryable);
        AssertStages(stages, expectedStage);

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(expectedResults);
    }

    public class C
    {
        public int Id { get; set; }
        public string A { get; set; }
        public string B { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, A = "A", B = "A" },
            new C { Id = 2, A = "A", B = "B" },
            new C { Id = 3, A = "B", B = "A" },
            new C { Id = 4, A = "a", B = "a" },
            new C { Id = 5, A = "a", B = "b" },
            new C { Id = 6, A = "b", B = "a" }
        ];
    }
}
