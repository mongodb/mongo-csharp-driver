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

public class CSharp5628Tests : LinqIntegrationTest<CSharp5628Tests.ClassFixture>
{
    public CSharp5628Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [InlineData(1, "{ $project : { _v : { $eq : ['$P', true] }, _id : 0 } }", new bool[] { false, false, true, true })]
    [InlineData(2, "{ $project : { _v : { $ne : ['$P', false] }, _id : 0 } }", new bool[] { false, false, true, true })]
    [InlineData(3, "{ $project : { _v : { $eq : ['$P', false] }, _id : 0 } }", new bool[] { true, true, false, false })]
    [InlineData(4, "{ $project : { _v : { $ne : ['$P', true] }, _id : 0 } }", new bool[] { true, true, false, false })]
    [InlineData(5, "{ $project : { _v : { $or : ['$P', '$Q'] }, _id : 0 } }", new bool[] { false, true, true, true })]
    [InlineData(6, "{ $project : { _v : { $and : ['$P', '$Q'] }, _id : 0 } }", new bool[] { false, false, false, true })]
    [InlineData(7, "{ $project : { _v : { $or : [{ $not : '$P' }, '$Q'] }, _id : 0 } }", new bool[] { true, true, false, true })]
    [InlineData(8, "{ $project : { _v : { $and : [{ $not : '$P' }, '$Q'] }, _id : 0 } }", new bool[] { false, true, false, false })]
    [InlineData(9, "{ $project : { _v : '$P', _id : 0 } }", new bool[] { false, false, true, true })]
    [InlineData(10, "{ $project : { _v : { $not : '$P' }, _id : 0 } }", new bool[] { true, true, false, false })]
    [InlineData(11, "{ $project : { _v : '$P', _id : 0 } }", new bool[] { false, false, true, true })]
    [InlineData(12, "{ $project : { _v : '$P', _id : 0 } }", new bool[] { false, false, true, true })]
    [InlineData(13, "{ $project : { _v : '$P', _id : 0 } }", new bool[] { false, false, true, true })]

    [InlineData(14, "{ $project : { _v : { $literal : false }, _id : 0 } }", new bool[] { false, false, false, false })]
    [InlineData(15, "{ $project : { _v : { $literal : true }, _id : 0 } }", new bool[] { true, true, true, true })]
    [InlineData(16, "{ $project : { _v : { $literal : true }, _id : 0 } }", new bool[] { true, true, true, true })]
    [InlineData(17, "{ $project : { _v : { $literal : false }, _id : 0 } }", new bool[] { false, false, false, false })]

    [InlineData(18, "{ $project : { _v : { $ne : ['$X', '$Y'] }, _id : 0 } }", new bool[] { false, true, true, false })]
    [InlineData(19, "{ $project : { _v : { $eq : ['$X', '$Y'] }, _id : 0 } }", new bool[] { true, false, false, true })]
    [InlineData(20, "{ $project : { _v : { $not : { $lt : ['$X', '$Y'] } }, _id : 0 } }", new bool[] { true, false, true, true })]
    [InlineData(21, "{ $project : { _v : { $not : { $gt : ['$X', '$Y'] } }, _id : 0 } }", new bool[] { true, true, false, true })]
    [InlineData(22, "{ $project : { _v : { $not : { $lte : ['$X', '$Y'] } }, _id : 0 } }", new bool[] { false, false, true, false })]
    [InlineData(23, "{ $project : { _v : { $not : { $gte : ['$X', '$Y'] } }, _id : 0 } }", new bool[] { false, true, false, false })]
    public void Select_simplifications_should_work(int testCase, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        // see: https://codeql.github.com/codeql-query-help/csharp/cs-simplifiable-boolean-expression/#recommendation
        // not all simplifications listed there are safe for a database (because of possibly missing fields or tri-valued logic)
        var queryable = testCase switch
        {
            1 => collection.AsQueryable().Select(x => x.P == true), // not safe
            2 => collection.AsQueryable().Select(x => x.P != false), // not safe
            3 => collection.AsQueryable().Select(x => x.P == false), // not safe
            4 => collection.AsQueryable().Select(x => x.P != true), // not safe
            5 => collection.AsQueryable().Select(x => x.P ? true : x.Q),
            6 => collection.AsQueryable().Select(x => x.P ? x.Q : false),
            7 => collection.AsQueryable().Select(x => x.P ? x.Q : true),
            8 => collection.AsQueryable().Select(x => x.P ? false : x.Q),
            9 => collection.AsQueryable().Select(x => x.P ? true : false),
            10 => collection.AsQueryable().Select(x => x.P ? false : true),
            11 => collection.AsQueryable().Select(x => !!x.P),
            12 => collection.AsQueryable().Select(x => x.P && true),
            13 => collection.AsQueryable().Select(x => x.P || false),

            14 => collection.AsQueryable().Select(x => x.P && false),
            15 => collection.AsQueryable().Select(x => x.P || true),
            16 => collection.AsQueryable().Select(x => x.P ? true : true),
            17 => collection.AsQueryable().Select(x => x.P ? false : false),

            18 => collection.AsQueryable().Select(x => !(x.X == x.Y)),
            19 => collection.AsQueryable().Select(x => !(x.X != x.Y)),
            20 => collection.AsQueryable().Select(x => !(x.X < x.Y)), // not safe
            21 => collection.AsQueryable().Select(x => !(x.X > x.Y)), // not safe
            22 => collection.AsQueryable().Select(x => !(x.X <= x.Y)), // not safe
            23 => collection.AsQueryable().Select(x => !(x.X >= x.Y)), // not safe
            _ => throw new ArgumentException($"Invalid test case: {testCase}")
        };

        var stages = Translate(collection, queryable);
        AssertStages(stages, expectedStage);

        var results = queryable.ToList();
        results.Should().Equal(expectedResults);
    }

    [Theory]
    [InlineData(1, "{ $match : { P : true } }", new int[] { 3, 4 })]
    [InlineData(2, "{ $match : { P : { $ne : false } } }", new int[] { 3, 4 })]
    [InlineData(3, "{ $match : { P : false } }", new int[] { 1, 2, })]
    [InlineData(4, "{ $match : { P : { $ne : true } } }", new int[] { 1, 2 })]
    [InlineData(5, "{ $match : { $or : [{ P : true }, { Q : true }] } }", new int[] { 2, 3, 4 })]
    [InlineData(6, "{ $match : { P : true, Q : true } }", new int[] { 4 })]
    [InlineData(7, "{ $match : { $or : [{ P : { $ne : true } }, { Q : true }] } }", new int[] { 1, 2, 4 })]
    [InlineData(8, "{ $match : { P : { $ne : true }, Q : true } }", new int[] { 2 })]
    [InlineData(9, "{ $match : { P : true } }", new int[] { 3, 4 })]
    [InlineData(10, "{ $match : { P : { $ne : true } } }", new int[] { 1, 2 })]
    [InlineData(11, "{ $match : { P : true } }", new int[] { 3, 4 })]
    [InlineData(12, "{ $match : { P : true } }", new int[] { 3, 4 })]
    [InlineData(13, "{ $match : { P : true } }", new int[] { 3, 4 })]

    [InlineData(14, "{ $match : { _id : { $type : -1 } } }", new int[] { })]
    [InlineData(15, null, new int[] { 1, 2, 3, 4 })]
    [InlineData(16, null, new int[] { 1, 2, 3, 4 })]
    [InlineData(17, "{ $match : { _id : { $type : -1 } } }", new int[] { })]

    [InlineData(18, "{ $match : { $nor : [{ $expr : { $eq : ['$X', '$Y'] } }] } }", new int[] { 2, 3 })]
    [InlineData(19, "{ $match : { $nor : [{ $expr : { $ne : ['$X', '$Y'] } }] } }", new int[] { 1, 4 })]
    [InlineData(20, "{ $match : { $nor : [{ $expr : { $lt : ['$X', '$Y'] } }] } }", new int[] { 1, 3, 4 })]
    [InlineData(21, "{ $match : { $nor : [{ $expr : { $gt : ['$X', '$Y'] } }] } }", new int[] { 1, 2, 4 })]
    [InlineData(22, "{ $match : { $nor : [{ $expr : { $lte : ['$X', '$Y'] } }] } }", new int[] { 3 })]
    [InlineData(23, "{ $match : { $nor : [{ $expr : { $gte : ['$X', '$Y'] } }] } }", new int[] { 2 })]
    public void Where_simplifications_should_work(int testCase, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        // see: https://codeql.github.com/codeql-query-help/csharp/cs-simplifiable-boolean-expression/#recommendation
        // not all simplifications listed there are safe for a database (because of possibly missing fields or tri-valued logic)
        var queryable = testCase switch
        {
            1 => collection.AsQueryable().Where(x => x.P == true), // not safe
            2 => collection.AsQueryable().Where(x => x.P != false), // not safe
            3 => collection.AsQueryable().Where(x => x.P == false), // not safe
            4 => collection.AsQueryable().Where(x => x.P != true), // not safe
            5 => collection.AsQueryable().Where(x => x.P ? true : x.Q),
            6 => collection.AsQueryable().Where(x => x.P ? x.Q : false),
            7 => collection.AsQueryable().Where(x => x.P ? x.Q : true),
            8 => collection.AsQueryable().Where(x => x.P ? false : x.Q),
            9 => collection.AsQueryable().Where(x => x.P ? true : false),
            10 => collection.AsQueryable().Where(x => x.P ? false : true),
            11 => collection.AsQueryable().Where(x => !!x.P),
            12 => collection.AsQueryable().Where(x => x.P && true),
            13 => collection.AsQueryable().Where(x => x.P || false),

            14 => collection.AsQueryable().Where(x => x.P && false),
            15 => collection.AsQueryable().Where(x => x.P || true),
            16 => collection.AsQueryable().Where(x => x.P ? true : true),
            17 => collection.AsQueryable().Where(x => x.P ? false : false),

            18 => collection.AsQueryable().Where(x => !(x.X == x.Y)),
            19 => collection.AsQueryable().Where(x => !(x.X != x.Y)),
            20 => collection.AsQueryable().Where(x => !(x.X < x.Y)), // not safe
            21 => collection.AsQueryable().Where(x => !(x.X > x.Y)), // not safe
            22 => collection.AsQueryable().Where(x => !(x.X <= x.Y)), // not safe
            23 => collection.AsQueryable().Where(x => !(x.X >= x.Y)), // not safe
            _ => throw new ArgumentException($"Invalid test case: {testCase}")
        };

        var stages = Translate(collection, queryable);
        string[] expectedStages = expectedStage == null ? [] : [expectedStage];
        AssertStages(stages, expectedStages);

        var results = queryable.ToList();
        results.Select(r => r.Id).Should().Equal(expectedIds);
    }

    public class C
    {
        public int Id { get; set; }
        public bool P  { get; set; }
        public bool Q  { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, P = false, Q = false, X = 1, Y = 1 },
            new C { Id = 2, P = false, Q = true, X = 1, Y = 2 },
            new C { Id = 3, P = true, Q = false, X = 2, Y = 1 },
            new C { Id = 4, P = true, Q = true, X = 2, Y = 2 }
        ];
    }
}
