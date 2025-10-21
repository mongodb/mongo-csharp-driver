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

public class CSharp5730Tests : LinqIntegrationTest<CSharp5730Tests.ClassFixture>
{
    public CSharp5730Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [InlineData(1, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(2, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(3, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(4, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(5, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(6, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(7, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(8, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(9, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(10, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(11, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(12, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    public void Select_String_Compare_constant_to_field_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare("A", x.B) == -1),
            2 => collection.AsQueryable().Select(x => string.Compare("A", x.B) == 0),
            3 => collection.AsQueryable().Select(x => string.Compare("A", x.B) == 1),
            4 => collection.AsQueryable().Select(x => string.Compare("A", x.B) != -1),
            5 => collection.AsQueryable().Select(x => string.Compare("A", x.B) != 0),
            6 => collection.AsQueryable().Select(x => string.Compare("A", x.B) != 1),
            7 => collection.AsQueryable().Select(x => string.Compare("A", x.B) > -1),
            8 => collection.AsQueryable().Select(x => string.Compare("A", x.B) > 0),
            9 => collection.AsQueryable().Select(x => string.Compare("A", x.B) < 0),
            10 => collection.AsQueryable().Select(x => string.Compare("A", x.B) < 1),
            11 => collection.AsQueryable().Select(x => string.Compare("A", x.B) <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare("A", x.B) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, false, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(2, false, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(3, false, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(4, false, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(5, false, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(6, false, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(7, false, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(8, false, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(9, false, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(10, false, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(11, false, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(12, false, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(1, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(2, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(3, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(4, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(5, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(6, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(7, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(8, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(9, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(10, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(11, true, "{ $project : { _v : { $lte : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(12, true, "{ $project : { _v : { $gte : [{ $strcasecmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    public void Select_String_Compare_constant_to_field_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) == -1),
            2 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) == 0),
            3 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) == 1),
            4 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) != -1),
            5 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) != 0),
            6 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) != 1),
            7 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) > -1),
            8 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) > 0),
            9 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) < 0),
            10 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) < 1),
            11 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare("A", x.B, ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(2, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(3, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(4, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(5, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(6, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(7, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(8, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(9, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(10, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(11, "{ $project : { _v : { $lte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(12, "{ $project : { _v : { $gte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    public void Select_String_Compare_field_to_constant_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") == -1),
            2 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") == 0),
            3 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") == 1),
            4 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") != -1),
            5 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") != 0),
            6 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") != 1),
            7 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") > -1),
            8 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") > 0),
            9 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") < 0),
            10 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") < 1),
            11 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare(x.A, "B") >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(2, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(3, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(4, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(5, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(6, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(7, false, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(8, false, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(9, false, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(10, false, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(11, false, "{ $project : { _v : { $lte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(12, false, "{ $project : { _v : { $gte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(1, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(2, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(3, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(4, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(5, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(6, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(7, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(8, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(9, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(10, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(11, true, "{ $project : { _v : { $lte : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(12, true, "{ $project : { _v : { $gte : [{ $strcasecmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    public void Select_String_Compare_field_to_constant_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) == -1),
            2 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) == 0),
            3 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) == 1),
            4 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) != -1),
            5 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) != 0),
            6 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) != 1),
            7 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) > -1),
            8 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) > 0),
            9 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) < 0),
            10 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) < 1),
            11 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare(x.A, "B", ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(2, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(3, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(4, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(5, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(6, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(7, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(8, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(9, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(10, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(11, "{ $project : { _v : { $lte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(12, "{ $project : { _v : { $gte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    public void Select_String_Compare_field_to_field_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) == -1),
            2 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) == 0),
            3 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) == 1),
            4 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) != -1),
            5 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) != 0),
            6 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) != 1),
            7 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) > -1),
            8 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) > 0),
            9 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) < 0),
            10 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) < 1),
            11 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(2, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(3, false, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(4, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(5, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(6, false, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(7, false, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(8, false, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(9, false, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(10, false, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(11, false, "{ $project : { _v : { $lte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(12, false, "{ $project : { _v : { $gte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(1, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(2, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(3, true, "{ $project : { _v : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(4, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(5, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(6, true, "{ $project : { _v : { $ne : [{ $strcasecmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(7, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(8, true, "{ $project : { _v : { $gt : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(9, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(10, true, "{ $project : { _v : { $lt : [{ $strcasecmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(11, true, "{ $project : { _v : { $lte : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(12, true, "{ $project : { _v : { $gte : [{ $strcasecmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    public void Select_String_Compare_field_to_field_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) == -1),
            2 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) == 0),
            3 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) == 1),
            4 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) != -1),
            5 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) != 0),
            6 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) != 1),
            7 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) > -1),
            8 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) > 0),
            9 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) < 0),
            10 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) < 1),
            11 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) <= 0),
            12 => collection.AsQueryable().Select(x => string.Compare(x.A, x.B, ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        Assert(collection, queryable, expectedStage, expectedResults);
    }

    [Theory]
    [InlineData(1, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(2, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(3, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(4, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(5, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(6, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(7, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(8, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(9, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(10, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(11, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(12, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    public void Where_String_Compare_constant_to_field_should_work(int scenario, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare("A", x.B) == -1),
            2 => collection.AsQueryable().Where(x => string.Compare("A", x.B) == 0),
            3 => collection.AsQueryable().Where(x => string.Compare("A", x.B) == 1),
            4 => collection.AsQueryable().Where(x => string.Compare("A", x.B) != -1),
            5 => collection.AsQueryable().Where(x => string.Compare("A", x.B) != 0),
            6 => collection.AsQueryable().Where(x => string.Compare("A", x.B) != 1),
            7 => collection.AsQueryable().Where(x => string.Compare("A", x.B) > -1),
            8 => collection.AsQueryable().Where(x => string.Compare("A", x.B) > 0),
            9 => collection.AsQueryable().Where(x => string.Compare("A", x.B) < 0),
            10 => collection.AsQueryable().Where(x => string.Compare("A", x.B) < 1),
            11 => collection.AsQueryable().Where(x => string.Compare("A", x.B) <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare("A", x.B) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    [Theory]
    [InlineData(1, false, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(2, false, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(3, false, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(4, false, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(5, false, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(6, false, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(7, false, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(8, false, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(9, false, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(10, false, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(11, false, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(12, false, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(1, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(2, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(3, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['A', '$B'] }, 1] } } }", new int[] { })]
    [InlineData(4, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(5, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(6, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['A', '$B'] }, 1] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(7, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(8, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { })]
    [InlineData(9, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(10, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['A', '$B'] }, 1] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(11, true, "{ $match : { $expr : { $lte : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(12, true, "{ $match : { $expr : { $gte : [{ $strcasecmp : ['A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    public void Where_String_Compare_constant_to_field_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) == -1),
            2 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) == 0),
            3 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) == 1),
            4 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) != -1),
            5 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) != 0),
            6 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) != 1),
            7 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) > -1),
            8 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) > 0),
            9 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) < 0),
            10 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) < 1),
            11 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare("A", x.B, ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    [Theory]
    [InlineData(1, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(2, "{ $match : { A : 'B' } }", new int[] { 3 })]
    [InlineData(3, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(4, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(5, "{ $match : { A : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(6, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(7, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(8, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(9, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(10, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(11, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(12, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    public void Where_String_Compare_field_to_constant_should_work(int scenario, string expectedStage, int[] expectedIds)
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

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    [Theory]
    [InlineData(1, false, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(2, false, "{ $match : { A : 'B' } }", new int[] { 3 })]
    [InlineData(3, false, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(4, false, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(5, false, "{ $match : { A : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(6, false, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(7, false, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(8, false, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(9, false, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(10, false, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(11, false, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(12, false, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(1, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', 'B'] }, -1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(2, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(3, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', 'B'] }, 1] } } }", new int[] { })]
    [InlineData(4, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', 'B'] }, -1] } } }", new int[] { 3, 6 })]
    [InlineData(5, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { 1, 2, 4, 5  })]
    [InlineData(6, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', 'B'] }, 1] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(7, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['$A', 'B'] }, -1] } } }", new int[] { 3, 6 })]
    [InlineData(8, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { })]
    [InlineData(9, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(10, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['$A', 'B'] }, 1] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(11, true, "{ $match : { $expr : { $lte : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(12, true, "{ $match : { $expr : { $gte : [{ $strcasecmp : ['$A', 'B'] }, 0] } } }", new int[] { 3, 6 })]
    public void Where_String_Compare_field_to_constant_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) == -1),
            2 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) == 0),
            3 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) == 1),
            4 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) != -1),
            5 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) != 0),
            6 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) != 1),
            7 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) > -1),
            8 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) > 0),
            9 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) < 0),
            10 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) < 1),
            11 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare(x.A, "B", ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    [Theory]
    [InlineData(1, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(2, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(3, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(4, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(5, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(6, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(7, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(8, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(9, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(10, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(11, "{ $match : { $expr : { $lte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(12, "{ $match : { $expr : { $gte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    public void Where_String_Compare_field_to_field_should_work(int scenario, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) == -1),
            2 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) == 0),
            3 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) == 1),
            4 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) != -1),
            5 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) != 0),
            6 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) != 1),
            7 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) > -1),
            8 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) > 0),
            9 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) < 0),
            10 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) < 1),
            11 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    [Theory]
    [InlineData(1, false, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(2, false, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(3, false, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(4, false, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(5, false, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(6, false, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(7, false, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(8, false, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(9, false, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(10, false, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(11, false, "{ $match : { $expr : { $lte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(12, false, "{ $match : { $expr : { $gte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(1, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(2, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(3, true, "{ $match : { $expr : { $eq : [{ $strcasecmp : ['$A', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(4, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(5, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(6, true, "{ $match : { $expr : { $ne : [{ $strcasecmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(7, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(8, true, "{ $match : { $expr : { $gt : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(9, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(10, true, "{ $match : { $expr : { $lt : [{ $strcasecmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(11, true, "{ $match : { $expr : { $lte : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(12, true, "{ $match : { $expr : { $gte : [{ $strcasecmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    public void Where_String_Compare_field_to_field_with_ignoreCase_should_work(int scenario, bool ignoreCase, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            // Compare field to constant
            1 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) == -1),
            2 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) == 0),
            3 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) == 1),
            4 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) != -1),
            5 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) != 0),
            6 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) != 1),
            7 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) > -1),
            8 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) > 0),
            9 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) < 0),
            10 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) < 1),
            11 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) <= 0),
            12 => collection.AsQueryable().Where(x => string.Compare(x.A, x.B, ignoreCase) >= 0),
            _ => throw new ArgumentException($"Invalid scenario: {scenario}.")
        };

        AssertIds(collection, queryable, expectedStage, expectedIds);
    }

    private void Assert<TResult>(IMongoCollection<C> collection, IQueryable<TResult> queryable, string expectedStage, TResult[] expectedResults)
    {
        var stages = Translate(collection, queryable);
        AssertStages(stages, expectedStage);

        var results = queryable.ToList();
        results.Should().Equal(expectedResults);
    }

    private void AssertIds(IMongoCollection<C> collection, IQueryable<C> queryable, string expectedStage, int[] expectedIds)
    {
        var stages = Translate(collection, queryable);
        AssertStages(stages, expectedStage);

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(expectedIds);
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
