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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5529Tests : LinqIntegrationTest<CSharp5529Tests.ClassFixture>
{
    public CSharp5529Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [InlineData(1, 1, """{ $group: { _id : 1, __agg0 : { $first : "$X" } } }""", 1)]
    [InlineData(1, 2, """{ $group: { _id : 1, __agg0 : { $last : "$X" } } }""", 2)]
    [InlineData(2, 1, """{ $group: { _id : 1, __agg0 : { $first : "$D.Y" } } }""", 11)]
    [InlineData(2, 2, """{ $group: { _id : 1, __agg0 : { $last : "$D.Y" } } }""", 22)]
    [InlineData(3, 1, """{ $group: { _id : 1, __agg0 : { $first : "$D.E.Z" } } }""", 111)]
    [InlineData(3, 2, """{ $group: { _id : 1, __agg0 : { $last : "$D.E.Z" } } }""", 222)]
    public void First_or_Last_optimization_should_work(int level, int firstOrLast, string expectedGroupStage, int expectedResult)
    {
        var collection = Fixture.Collection;

        var queryable = (level, firstOrLast) switch
            {
                (1, 1) => collection.Aggregate().Group(x => 1, g => g.First().X),
                (1, 2) => collection.Aggregate().Group(x => 1, g => g.Last().X),
                (2, 1) => collection.Aggregate().Group(x => 1, g => g.First().D.Y),
                (2, 2) => collection.Aggregate().Group(x => 1, g => g.Last().D.Y),
                (3, 1) => collection.Aggregate().Group(x => 1, g => g.First().D.E.Z),
                (3, 2) => collection.Aggregate().Group(x => 1, g => g.Last().D.E.Z),
                _ => throw new ArgumentException()
            };

        var stages = Translate(collection,queryable);
        AssertStages(
            stages,
            expectedGroupStage,
            """{ $project : { _v : "$__agg0", _id : 0 } }""");

        var result = queryable.Single();
        result.Should().Be(expectedResult);
    }
    public class C
    {
        public int Id { get; set; }
        public int X { get; set; }

        public D D { get; set; }
    }

    public class D
    {
        public E E { get; set; }
        public int Y { get; set; }
    }

    public class E
    {
        public int Z { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, X = 1, D = new D { E = new E { Z = 111 }, Y = 11 } },
            new C { Id = 2, X = 2, D = new D { E = new E { Z = 222 }, Y = 22 } },
        ];
    }
}
