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
    [InlineData(101, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(102, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(103, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(104, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(105, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(106, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(107, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(108, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(109, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(110, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(111, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(112, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(201, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(202, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(203, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(204, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(205, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(206, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(207, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(208, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(209, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(210, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(211, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(212, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(301, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(302, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(303, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(304, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(305, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(306, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(307, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(308, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(309, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(310, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(311, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(312, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(401, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(402, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(403, "{ $project : { _v : { $eq : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(404, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(405, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(406, "{ $project : { _v : { $ne : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(407, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    [InlineData(408, "{ $project : { _v : { $gt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, false, false, false })]
    [InlineData(409, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, true, true, true })]
    [InlineData(410, "{ $project : { _v : { $lt : [{ $cmp : ['A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(411, "{ $project : { _v : { $lte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, true, true, true })]
    [InlineData(412, "{ $project : { _v : { $gte : [{ $cmp : ['A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, false, false, false })]
    public void Select_String_Compare_constant_to_field_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Select(x => String.Compare("A", x.B) == -1),
            102 => collection.AsQueryable().Select(x => String.Compare("A", x.B) == 0),
            103 => collection.AsQueryable().Select(x => String.Compare("A", x.B) == 1),
            104 => collection.AsQueryable().Select(x => String.Compare("A", x.B) != -1),
            105 => collection.AsQueryable().Select(x => String.Compare("A", x.B) != 0),
            106 => collection.AsQueryable().Select(x => String.Compare("A", x.B) != 1),
            107 => collection.AsQueryable().Select(x => String.Compare("A", x.B) > -1),
            108 => collection.AsQueryable().Select(x => String.Compare("A", x.B) > 0),
            109 => collection.AsQueryable().Select(x => String.Compare("A", x.B) < 0),
            110 => collection.AsQueryable().Select(x => String.Compare("A", x.B) < 1),
            111 => collection.AsQueryable().Select(x => String.Compare("A", x.B) <= 0),
            112 => collection.AsQueryable().Select(x => String.Compare("A", x.B) >= 0),

            201 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) == -1),
            202 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) == 0),
            203 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) == 1),
            204 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) != -1),
            205 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) != 0),
            206 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) != 1),
            207 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) > -1),
            208 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) > 0),
            209 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) < 0),
            210 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) < 1),
            211 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) <= 0),
            212 => collection.AsQueryable().Select(x => "A".CompareTo(x.B) >= 0),

            301 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) == -1),
            302 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) == 0),
            303 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) == 1),
            304 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) != -1),
            305 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) != 0),
            306 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) != 1),
            307 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) > -1),
            308 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) > 0),
            309 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) < 0),
            310 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) < 1),
            311 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) <= 0),
            312 => collection.AsQueryable().Select(x => ((IComparable)"A").CompareTo(x.B) >= 0),

            401 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) == -1),
            402 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) == 0),
            403 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) == 1),
            404 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) != -1),
            405 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) != 0),
            406 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) != 1),
            407 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) > -1),
            408 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) > 0),
            409 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) < 0),
            410 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) < 1),
            411 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) <= 0),
            412 => collection.AsQueryable().Select(x => ((IComparable<string>)"A").CompareTo(x.B) >= 0),
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
    [InlineData(101, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(102, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(103, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(104, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(105, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(106, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(107, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(108, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(109, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(110, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(111, "{ $project : { _v : { $lte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(112, "{ $project : { _v : { $gte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(201, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(202, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(203, "{ $project : { _v : { $eq : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(204, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(205, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(206, "{ $project : { _v : { $ne : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(207, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(208, "{ $project : { _v : { $gt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(209, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(210, "{ $project : { _v : { $lt : [{ $cmp : ['$A', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(211, "{ $project : { _v : { $lte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(212, "{ $project : { _v : { $gte : [{ $cmp : ['$A', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(301, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(302, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(303, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(304, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(305, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(306, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(307, "{ $project : { _v : { $gt : [{ $cmp : ['$ICA', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(308, "{ $project : { _v : { $gt : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(309, "{ $project : { _v : { $lt : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(310, "{ $project : { _v : { $lt : [{ $cmp : ['$ICA', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(311, "{ $project : { _v : { $lte : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(312, "{ $project : { _v : { $gte : [{ $cmp : ['$ICA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(401, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', 'B'] }, -1] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(402, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, false })]
    [InlineData(403, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', 'B'] }, 1] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(404, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(405, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, true })]
    [InlineData(406, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(407, "{ $project : { _v : { $gt : [{ $cmp : ['$ICSA', 'B'] }, -1] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    [InlineData(408, "{ $project : { _v : { $gt : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, false, true, true, true })]
    [InlineData(409, "{ $project : { _v : { $lt : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, false, false, false })]
    [InlineData(410, "{ $project : { _v : { $lt : [{ $cmp : ['$ICSA', 'B'] }, 1] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(411, "{ $project : { _v : { $lte : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { true, true, true, false, false, false })]
    [InlineData(412, "{ $project : { _v : { $gte : [{ $cmp : ['$ICSA', 'B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, true, true, true })]
    public void Select_String_Compare_field_to_constant_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") == -1),
            102 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") == 0),
            103 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") == 1),
            104 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") != -1),
            105 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") != 0),
            106 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") != 1),
            107 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") > -1),
            108 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") > 0),
            109 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") < 0),
            110 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") < 1),
            111 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") <= 0),
            112 => collection.AsQueryable().Select(x => String.Compare(x.A, "B") >= 0),

            201 => collection.AsQueryable().Select(x => x.A.CompareTo("B") == -1),
            202 => collection.AsQueryable().Select(x => x.A.CompareTo("B") == 0),
            203 => collection.AsQueryable().Select(x => x.A.CompareTo("B") == 1),
            204 => collection.AsQueryable().Select(x => x.A.CompareTo("B") != -1),
            205 => collection.AsQueryable().Select(x => x.A.CompareTo("B") != 0),
            206 => collection.AsQueryable().Select(x => x.A.CompareTo("B") != 1),
            207 => collection.AsQueryable().Select(x => x.A.CompareTo("B") > -1),
            208 => collection.AsQueryable().Select(x => x.A.CompareTo("B") > 0),
            209 => collection.AsQueryable().Select(x => x.A.CompareTo("B") < 0),
            210 => collection.AsQueryable().Select(x => x.A.CompareTo("B") < 1),
            211 => collection.AsQueryable().Select(x => x.A.CompareTo("B") <= 0),
            212 => collection.AsQueryable().Select(x => x.A.CompareTo("B") >= 0),

            301 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") == -1),
            302 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") == 0),
            303 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") == 1),
            304 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") != -1),
            305 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") != 0),
            306 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") != 1),
            307 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") > -1),
            308 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") > 0),
            309 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") < 0),
            310 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") < 1),
            311 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") <= 0),
            312 => collection.AsQueryable().Select(x => x.ICA.CompareTo("B") >= 0),

            401 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") == -1),
            402 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") == 0),
            403 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") == 1),
            404 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") != -1),
            405 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") != 0),
            406 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") != 1),
            407 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") > -1),
            408 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") > 0),
            409 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") < 0),
            410 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") < 1),
            411 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") <= 0),
            412 => collection.AsQueryable().Select(x => x.ICSA.CompareTo("B") >= 0),

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
    [InlineData(101, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(102, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(103, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(104, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(105, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(106, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(107, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(108, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(109, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(110, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(111, "{ $project : { _v : { $lte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(112, "{ $project : { _v : { $gte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(201, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(202, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(203, "{ $project : { _v : { $eq : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(204, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(205, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(206, "{ $project : { _v : { $ne : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(207, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(208, "{ $project : { _v : { $gt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(209, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(210, "{ $project : { _v : { $lt : [{ $cmp : ['$A', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(211, "{ $project : { _v : { $lte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(212, "{ $project : { _v : { $gte : [{ $cmp : ['$A', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(301, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(302, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(303, "{ $project : { _v : { $eq : [{ $cmp : ['$ICA', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(304, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(305, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(306, "{ $project : { _v : { $ne : [{ $cmp : ['$ICA', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(307, "{ $project : { _v : { $gt : [{ $cmp : ['$ICA', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(308, "{ $project : { _v : { $gt : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(309, "{ $project : { _v : { $lt : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(310, "{ $project : { _v : { $lt : [{ $cmp : ['$ICA', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(311, "{ $project : { _v : { $lte : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(312, "{ $project : { _v : { $gte : [{ $cmp : ['$ICA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(401, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', '$B'] }, -1] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(402, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, false, true, false, false })]
    [InlineData(403, "{ $project : { _v : { $eq : [{ $cmp : ['$ICSA', '$B'] }, 1] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(404, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(405, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, true, false, true, true })]
    [InlineData(406, "{ $project : { _v : { $ne : [{ $cmp : ['$ICSA', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(407, "{ $project : { _v : { $gt : [{ $cmp : ['$ICSA', '$B'] }, -1] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    [InlineData(408, "{ $project : { _v : { $gt : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, false, true, false, false, true })]
    [InlineData(409, "{ $project : { _v : { $lt : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { false, true, false, false, true, false })]
    [InlineData(410, "{ $project : { _v : { $lt : [{ $cmp : ['$ICSA', '$B'] }, 1] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(411, "{ $project : { _v : { $lte : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, true, false, true, true, false })]
    [InlineData(412, "{ $project : { _v : { $gte : [{ $cmp : ['$ICSA', '$B'] }, 0] }, _id : 0 } }", new bool[] { true, false, true, true, false, true })]
    public void Select_String_Compare_field_to_field_should_work(int scenario, string expectedStage, bool[] expectedResults)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) == -1),
            102 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) == 0),
            103 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) == 1),
            104 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) != -1),
            105 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) != 0),
            106 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) != 1),
            107 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) > -1),
            108 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) > 0),
            109 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) < 0),
            110 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) < 1),
            111 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) <= 0),
            112 => collection.AsQueryable().Select(x => String.Compare(x.A, x.B) >= 0),

            201 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) == -1),
            202 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) == 0),
            203 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) == 1),
            204 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) != -1),
            205 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) != 0),
            206 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) != 1),
            207 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) > -1),
            208 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) > 0),
            209 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) < 0),
            210 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) < 1),
            211 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) <= 0),
            212 => collection.AsQueryable().Select(x => x.A.CompareTo(x.B) >= 0),

            301 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) == -1),
            302 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) == 0),
            303 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) == 1),
            304 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) != -1),
            305 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) != 0),
            306 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) != 1),
            307 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) > -1),
            308 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) > 0),
            309 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) < 0),
            310 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) < 1),
            311 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) <= 0),
            312 => collection.AsQueryable().Select(x => x.ICA.CompareTo(x.B) >= 0),

            401 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) == -1),
            402 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) == 0),
            403 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) == 1),
            404 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) != -1),
            405 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) != 0),
            406 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) != 1),
            407 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) > -1),
            408 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) > 0),
            409 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) < 0),
            410 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) < 1),
            411 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) <= 0),
            412 => collection.AsQueryable().Select(x => x.ICSA.CompareTo(x.B) >= 0),

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
    [InlineData(101, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(102, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(103, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(104, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(105, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(106, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(107, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(108, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(109, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(110, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(111, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(112, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(201, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(202, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(203, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(204, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(205, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(206, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(207, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(208, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(209, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(210, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(211, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(212, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(301, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(302, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(303, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(304, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(305, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(306, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(307, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(308, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(309, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(310, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(311, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(312, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(401, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(402, "{ $match : { B : 'A' } }", new int[] { 1, 3 })]
    [InlineData(403, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(404, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(405, "{ $match : { B : { $ne : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(406, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(407, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    [InlineData(408, "{ $match : { B : { $lt : 'A' } } }", new int[] { })]
    [InlineData(409, "{ $match : { B : { $gt : 'A' } } }", new int[] { 2, 4, 5, 6 })]
    [InlineData(410, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(411, "{ $match : { B : { $gte : 'A' } } }", new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(412, "{ $match : { B : { $lte : 'A' } } }", new int[] { 1, 3 })]
    public void Where_String_Compare_constant_to_field_should_work(int scenario, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Where(x => String.Compare("A", x.B) == -1),
            102 => collection.AsQueryable().Where(x => String.Compare("A", x.B) == 0),
            103 => collection.AsQueryable().Where(x => String.Compare("A", x.B) == 1),
            104 => collection.AsQueryable().Where(x => String.Compare("A", x.B) != -1),
            105 => collection.AsQueryable().Where(x => String.Compare("A", x.B) != 0),
            106 => collection.AsQueryable().Where(x => String.Compare("A", x.B) != 1),
            107 => collection.AsQueryable().Where(x => String.Compare("A", x.B) > -1),
            108 => collection.AsQueryable().Where(x => String.Compare("A", x.B) > 0),
            109 => collection.AsQueryable().Where(x => String.Compare("A", x.B) < 0),
            110 => collection.AsQueryable().Where(x => String.Compare("A", x.B) < 1),
            111 => collection.AsQueryable().Where(x => String.Compare("A", x.B) <= 0),
            112 => collection.AsQueryable().Where(x => String.Compare("A", x.B) >= 0),

            201 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) == -1),
            202 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) == 0),
            203 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) == 1),
            204 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) != -1),
            205 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) != 0),
            206 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) != 1),
            207 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) > -1),
            208 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) > 0),
            209 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) < 0),
            210 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) < 1),
            211 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) <= 0),
            212 => collection.AsQueryable().Where(x => "A".CompareTo(x.B) >= 0),

            301 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) == -1),
            302 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) == 0),
            303 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) == 1),
            304 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) != -1),
            305 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) != 0),
            306 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) != 1),
            307 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) > -1),
            308 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) > 0),
            309 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) < 0),
            310 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) < 1),
            311 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) <= 0),
            312 => collection.AsQueryable().Where(x => ((IComparable)"A").CompareTo(x.B) >= 0),

            401 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) == -1),
            402 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) == 0),
            403 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) == 1),
            404 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) != -1),
            405 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) != 0),
            406 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) != 1),
            407 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) > -1),
            408 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) > 0),
            409 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) < 0),
            410 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) < 1),
            411 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) <= 0),
            412 => collection.AsQueryable().Where(x => ((IComparable<string>)"A").CompareTo(x.B) >= 0),

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
    [InlineData(101, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(102, "{ $match : { A : 'B' } }", new int[] { 3 })]
    [InlineData(103, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(104, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(105, "{ $match : { A : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(106, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(107, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(108, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(109, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(110, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(111, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(112, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(201, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(202, "{ $match : { A : 'B' } }", new int[] { 3 })]
    [InlineData(203, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(204, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(205, "{ $match : { A : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(206, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(207, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(208, "{ $match : { A : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(209, "{ $match : { A : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(210, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(211, "{ $match : { A : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(212, "{ $match : { A : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(301, "{ $match : { ICA : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(302, "{ $match : { ICA : 'B' } }", new int[] { 3 })]
    [InlineData(303, "{ $match : { ICA : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(304, "{ $match : { ICA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(305, "{ $match : { ICA : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(306, "{ $match : { ICA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(307, "{ $match : { ICA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(308, "{ $match : { ICA : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(309, "{ $match : { ICA : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(310, "{ $match : { ICA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(311, "{ $match : { ICA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(312, "{ $match : { ICA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(401, "{ $match : { ICSA : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(402, "{ $match : { ICSA : 'B' } }", new int[] { 3 })]
    [InlineData(403, "{ $match : { ICSA : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(404, "{ $match : { ICSA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(405, "{ $match : { ICSA : { $ne : 'B' } } }", new int[] { 1, 2, 4, 5, 6 })]
    [InlineData(406, "{ $match : { ICSA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(407, "{ $match : { ICSA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    [InlineData(408, "{ $match : { ICSA : { $gt : 'B' } } }", new int[] { 4, 5, 6 })]
    [InlineData(409, "{ $match : { ICSA : { $lt : 'B' } } }", new int[] { 1, 2 })]
    [InlineData(410, "{ $match : { ICSA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(411, "{ $match : { ICSA : { $lte : 'B' } } }", new int[] { 1, 2, 3 })]
    [InlineData(412, "{ $match : { ICSA : { $gte : 'B' } } }", new int[] { 3, 4, 5, 6 })]
    public void Where_String_Compare_field_to_constant_should_work(int scenario, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") == -1),
            102 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") == 0),
            103 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") == 1),
            104 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") != -1),
            105 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") != 0),
            106 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") != 1),
            107 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") > -1),
            108 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") > 0),
            109 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") < 0),
            110 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") < 1),
            111 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") <= 0),
            112 => collection.AsQueryable().Where(x => String.Compare(x.A, "B") >= 0),

            201 => collection.AsQueryable().Where(x => x.A.CompareTo("B") == -1),
            202 => collection.AsQueryable().Where(x => x.A.CompareTo("B") == 0),
            203 => collection.AsQueryable().Where(x => x.A.CompareTo("B") == 1),
            204 => collection.AsQueryable().Where(x => x.A.CompareTo("B") != -1),
            205 => collection.AsQueryable().Where(x => x.A.CompareTo("B") != 0),
            206 => collection.AsQueryable().Where(x => x.A.CompareTo("B") != 1),
            207 => collection.AsQueryable().Where(x => x.A.CompareTo("B") > -1),
            208 => collection.AsQueryable().Where(x => x.A.CompareTo("B") > 0),
            209 => collection.AsQueryable().Where(x => x.A.CompareTo("B") < 0),
            210 => collection.AsQueryable().Where(x => x.A.CompareTo("B") < 1),
            211 => collection.AsQueryable().Where(x => x.A.CompareTo("B") <= 0),
            212 => collection.AsQueryable().Where(x => x.A.CompareTo("B") >= 0),

            301 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") == -1),
            302 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") == 0),
            303 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") == 1),
            304 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") != -1),
            305 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") != 0),
            306 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") != 1),
            307 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") > -1),
            308 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") > 0),
            309 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") < 0),
            310 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") < 1),
            311 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") <= 0),
            312 => collection.AsQueryable().Where(x => x.ICA.CompareTo("B") >= 0),

            401 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") == -1),
            402 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") == 0),
            403 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") == 1),
            404 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") != -1),
            405 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") != 0),
            406 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") != 1),
            407 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") > -1),
            408 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") > 0),
            409 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") < 0),
            410 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") < 1),
            411 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") <= 0),
            412 => collection.AsQueryable().Where(x => x.ICSA.CompareTo("B") >= 0),

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
    [InlineData(101, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(102, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(103, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(104, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(105, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(106, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(107, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(108, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(109, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(110, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(111, "{ $match : { $expr : { $lte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(112, "{ $match : { $expr : { $gte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(201, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(202, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(203, "{ $match : { $expr : { $eq : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(204, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(205, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(206, "{ $match : { $expr : { $ne : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(207, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(208, "{ $match : { $expr : { $gt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(209, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(210, "{ $match : { $expr : { $lt : [{ $cmp : ['$A', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(211, "{ $match : { $expr : { $lte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(212, "{ $match : { $expr : { $gte : [{ $cmp : ['$A', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(301, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICA', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(302, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(303, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICA', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(304, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICA', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(305, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(306, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICA', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(307, "{ $match : { $expr : { $gt : [{ $cmp : ['$ICA', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(308, "{ $match : { $expr : { $gt : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(309, "{ $match : { $expr : { $lt : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(310, "{ $match : { $expr : { $lt : [{ $cmp : ['$ICA', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(311, "{ $match : { $expr : { $lte : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(312, "{ $match : { $expr : { $gte : [{ $cmp : ['$ICA', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(401, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICSA', '$B'] }, -1] } } }", new int[] { 2, 5 })]
    [InlineData(402, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 1, 4 })]
    [InlineData(403, "{ $match : { $expr : { $eq : [{ $cmp : ['$ICSA', '$B'] }, 1] } } }", new int[] { 3, 6 })]
    [InlineData(404, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICSA', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(405, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 2, 3, 5, 6 })]
    [InlineData(406, "{ $match : { $expr : { $ne : [{ $cmp : ['$ICSA', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(407, "{ $match : { $expr : { $gt : [{ $cmp : ['$ICSA', '$B'] }, -1] } } }", new int[] { 1, 3, 4, 6 })]
    [InlineData(408, "{ $match : { $expr : { $gt : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 3, 6 })]
    [InlineData(409, "{ $match : { $expr : { $lt : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 2, 5 })]
    [InlineData(410, "{ $match : { $expr : { $lt : [{ $cmp : ['$ICSA', '$B'] }, 1] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(411, "{ $match : { $expr : { $lte : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 1, 2, 4, 5 })]
    [InlineData(412, "{ $match : { $expr : { $gte : [{ $cmp : ['$ICSA', '$B'] }, 0] } } }", new int[] { 1, 3, 4, 6 })]
    public void Where_String_Compare_field_to_field_should_work(int scenario, string expectedStage, int[] expectedIds)
    {
        var collection = Fixture.Collection;

        var queryable = scenario switch
        {
            101 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) == -1),
            102 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) == 0),
            103 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) == 1),
            104 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) != -1),
            105 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) != 0),
            106 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) != 1),
            107 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) > -1),
            108 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) > 0),
            109 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) < 0),
            110 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) < 1),
            111 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) <= 0),
            112 => collection.AsQueryable().Where(x => String.Compare(x.A, x.B) >= 0),

            201 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) == -1),
            202 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) == 0),
            203 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) == 1),
            204 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) != -1),
            205 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) != 0),
            206 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) != 1),
            207 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) > -1),
            208 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) > 0),
            209 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) < 0),
            210 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) < 1),
            211 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) <= 0),
            212 => collection.AsQueryable().Where(x => x.A.CompareTo(x.B) >= 0),

            301 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) == -1),
            302 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) == 0),
            303 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) == 1),
            304 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) != -1),
            305 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) != 0),
            306 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) != 1),
            307 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) > -1),
            308 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) > 0),
            309 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) < 0),
            310 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) < 1),
            311 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) <= 0),
            312 => collection.AsQueryable().Where(x => x.ICA.CompareTo(x.B) >= 0),

            401 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) == -1),
            402 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) == 0),
            403 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) == 1),
            404 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) != -1),
            405 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) != 0),
            406 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) != 1),
            407 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) > -1),
            408 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) > 0),
            409 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) < 0),
            410 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) < 1),
            411 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) <= 0),
            412 => collection.AsQueryable().Where(x => x.ICSA.CompareTo(x.B) >= 0),

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
        public IComparable ICA { get; set; }
        public IComparable<string> ICSA { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, A = "A", B = "A", ICA = "A", ICSA = "A" },
            new C { Id = 2, A = "A", B = "B", ICA = "A", ICSA = "A" },
            new C { Id = 3, A = "B", B = "A", ICA = "B", ICSA = "B" },
            new C { Id = 4, A = "a", B = "a", ICA = "a", ICSA = "a" },
            new C { Id = 5, A = "a", B = "b", ICA = "a", ICSA = "a" },
            new C { Id = 6, A = "b", B = "a", ICA = "b", ICSA = "b" }
        ];
    }
}
