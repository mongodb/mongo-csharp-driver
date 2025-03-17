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
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp5375Tests : LinqIntegrationTest<CSharp5375Tests.ClassFixture>
    {
        public CSharp5375Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : null, _id : 0 } }", new string[] { null, null })]
        [InlineData(2, "{ $project : { _v : 's2', _id : 0 } }", new string[] { "s2", "s2" })]
        [InlineData(3, "{ $project : { _v :  '$String2', _id : 0 } }", new string[] { "a2", "b2" })]
        [InlineData(4, "{ $project : { _v : 's1', _id : 0 } }", new string[] { "s1", "s1" })]
        [InlineData(5, "{ $project : { _v : 's1', _id : 0 } }", new string[] { "s1", "s1" })]
        [InlineData(6, "{ $project : { _v : 's1', _id : 0 } }", new string[] { "s1", "s1" })]
        [InlineData(7, "{ $project : { _v : '$String1', _id : 0 } }", new string[] { null, "b1" })]
        [InlineData(8, "{ $project : { _v : { $ifNull : ['$String1', 's2'] }, _id : 0 } }", new string[] { "s2", "b1" })]
        [InlineData(9, "{ $project : { _v : { $ifNull : ['$String1', '$String2'] }, _id : 0 } }", new string[] { "a2", "b1" })]
        public void Coalesce_with_reference_types_should_work(int scenario, string expectedStage, string[] expectedResults)
        {
            var collection = Fixture.Collection;
            string constant1 = "s1";
            string constant2 = "s2";
            string @null = null;

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => @null ?? null),
                2 => collection.AsQueryable().Select(x => @null ?? constant2),
                3 => collection.AsQueryable().Select(x => @null ?? x.String2),
                4 => collection.AsQueryable().Select(x => constant1 ?? null),
                5 => collection.AsQueryable().Select(x => constant1 ?? constant2),
                6 => collection.AsQueryable().Select(x => constant1 ?? x.String2),
                7 => collection.AsQueryable().Select(x => x.String1 ?? null),
                8 => collection.AsQueryable().Select(x => x.String1 ?? constant2),
                9 => collection.AsQueryable().Select(x => x.String1 ?? x.String2),
                _ => throw new Exception() // should not reach here
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : null, _id : 0 } }", new int[] { -1, -1 })] // -1 means null
        [InlineData(2, "{ $project : { _v : { $literal : 6 }, _id : 0 } }", new int[] { 6, 6 })]
        [InlineData(3, "{ $project : { _v :  '$NullableInt2', _id : 0 } }", new int[] { 2, 4 })]
        [InlineData(4, "{ $project : { _v : { $literal : 5 }, _id : 0 } }", new int[] { 5, 5 })]
        [InlineData(5, "{ $project : { _v : { $literal : 5 }, _id : 0 } }", new int[] { 5, 5 })]
        [InlineData(6, "{ $project : { _v : { $literal : 5 }, _id : 0 } }", new int[] { 5, 5 })]
        [InlineData(7, "{ $project : { _v : '$NullableInt1', _id : 0 } }", new int[] { -1, 3 })]
        [InlineData(8, "{ $project : { _v : { $ifNull : ['$NullableInt1', 6] }, _id : 0 } }", new int[] { 6, 3 })]
        [InlineData(9, "{ $project : { _v : { $ifNull : ['$NullableInt1', '$NullableInt2'] }, _id : 0 } }", new int[] { 2, 3 })]
        public void Coalesce_with_nullable_and_nullable_types_should_work(int scenario, string expectedStage, int[] expectedResults)
        {
            var collection = Fixture.Collection;
            int? constant1 = 5;
            int? constant2 = 6;
            int? @null = null;

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => @null ?? @null),
                2 => collection.AsQueryable().Select(x => @null ?? constant2),
                3 => collection.AsQueryable().Select(x => @null ?? x.NullableInt2),
                4 => collection.AsQueryable().Select(x => constant1 ?? @null),
                5 => collection.AsQueryable().Select(x => constant1 ?? x.NullableInt2),
                6 => collection.AsQueryable().Select(x => constant1 ?? @null),
                7 => collection.AsQueryable().Select(x => x.NullableInt1 ?? @null),
                8 => collection.AsQueryable().Select(x => x.NullableInt1 ?? constant2),
                9 => collection.AsQueryable().Select(x => x.NullableInt1 ?? x.NullableInt2),
                _ => throw new Exception() // should not reach here
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults.Select(x => x == -1 ? (int?)null : x));
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $literal : 6 }, _id : 0 } }", new int[] { 6, 6 })]
        [InlineData(2, "{ $project : { _v : '$Int2', _id : 0 } }", new int[] { 2, 4 })]
        [InlineData(3, "{ $project : { _v : { $literal : 5 }, _id : 0 } }", new int[] { 5, 5 })]
        [InlineData(4, "{ $project : { _v : { $literal : 5 }, _id : 0 } }", new int[] { 5, 5 })]
        [InlineData(5, "{ $project : { _v : { $ifNull : ['$NullableInt1', 6] }, _id : 0 } }", new int[] { 6, 3 })]
        [InlineData(6, "{ $project : { _v : { $ifNull : ['$NullableInt1', '$Int2'] }, _id : 0 } }", new int[] { 2, 3 })]
         public void Coalesce_with_nullable_and_non_nullable_types_should_work(int scenario, string expectedStage, int[] expectedResults)
        {
            var collection = Fixture.Collection;
            int? constant1 = 5;
            int constant2 = 6;
            int? @null = null;

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => @null ?? constant2),
                2 => collection.AsQueryable().Select(x => @null ?? x.Int2),
                3 => collection.AsQueryable().Select(x => constant1 ?? constant2),
                4 => collection.AsQueryable().Select(x => constant1 ?? x.Int2),
                5 => collection.AsQueryable().Select(x => x.NullableInt1 ?? constant2),
                6 => collection.AsQueryable().Select(x => x.NullableInt1 ?? x.Int2),
                _ => throw new Exception() // should not reach here
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults.Select(x => x == -1 ? (int?)null : x));
        }

        public class C
        {
            public int Id { get; set; }
            public int Int2 { get; set; }
            public int? NullableInt1 { get; set; }
            public int? NullableInt2 { get; set; }
            public string String1 { get; set; }
            public string String2 { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, NullableInt1 = null, NullableInt2 = 2, Int2 = 2, String1 = null, String2 = "a2" },
                new C { Id = 2, NullableInt1 = 3, NullableInt2 = 4, Int2 = 4, String1 = "b1", String2 = "b2"}
            ];
        }
    }
}
