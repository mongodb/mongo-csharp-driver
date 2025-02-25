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
    public class CSharp4882Tests : LinqIntegrationTest<CSharp4882Tests.ClassFixture>
    {
        public CSharp4882Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : [2, 3, 4], _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : [2, 3, 4], _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        public void Skip_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(1).ToArray()),
                2 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.One).ToArray()),
                3 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).ToArray()),
                4 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3, 4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2147483647] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2147483647] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : [4], _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : [4], _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }", true)]
        public void Skip_Skip_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(1).Skip(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(1).Skip(2).ToArray()),
                2 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(1).Skip(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(1).Skip(x.Two).ToArray()),
                3 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.One).Skip(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.One).Skip(2).ToArray()),
                4 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.One).Skip(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.One).Skip(x.Two).ToArray()),
                5 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(1).Skip(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Skip(2).ToArray()),
                6 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(1).Skip(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Skip(x.Two).ToArray()),
                7 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.One).Skip(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Skip(2).ToArray()),
                8 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.One).Skip(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Skip(x.Two).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : [2, 3], _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : [2, 3], _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        public void Skip_Take_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(1).Take(2).ToArray()),
                2 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(1).Take(x.Two).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.One).Take(2).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.One).Take(x.Two).ToArray()),
                5 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Take(2).ToArray()),
                6 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Take(x.Two).ToArray()),
                7 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Take(2).ToArray()),
                8 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Take(x.Two).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(9, "{ $project : { _v : [4], _id : 0 } }", false)]
        [InlineData(9, "{ $project : { _v : [4], _id : 0 } }", true)]
        [InlineData(10, "{ $project : { _v : { $slice : [[3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(10, "{ $project : { _v : { $slice : [[3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }", false)]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }", true)]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        public void Skip_Take_Skip_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(2).Take(3).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(2).Take(3).Skip(1).ToArray()),
                2 =>  withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(2).Take(3).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(2).Take(3).Skip(x.One).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(2).Take(x.Three).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(2).Take(x.Three).Skip(1).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(2).Take(x.Three).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(2).Take(x.Three).Skip(x.One).ToArray()),
                5 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.Two).Take(3).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(3).Skip(1).ToArray()),
                6 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.Two).Take(3).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(3).Skip(x.One).ToArray()),
                7 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.Two).Take(x.Three).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(x.Three).Skip(1).ToArray()),
                8 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Skip(x.Two).Take(x.Three).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(x.Three).Skip(x.One).ToArray()),
                9 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(2).Take(3).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(3).Skip(1).ToArray()),
                10 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(2).Take(3).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(3).Skip(x.One).ToArray()),
                11 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(2).Take(x.Three).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(x.Three).Skip(1).ToArray()),
                12 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(2).Take(x.Three).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(x.Three).Skip(x.One).ToArray()),
                13 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.Two).Take(3).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(3).Skip(1).ToArray()),
                14 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.Two).Take(3).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(3).Skip(x.One).ToArray()),
                15 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.Two).Take(x.Three).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(x.Three).Skip(1).ToArray()),
                16 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Skip(x.Two).Take(x.Three).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(x.Three).Skip(x.One).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : [1], _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : [1], _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        public void Take_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(1).ToArray()),
                2 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.One).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(1).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.One).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(1);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 1] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 1] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : [2], _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : [2], _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }", true)]
        public void Take_Skip_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(2).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(2).Skip(1).ToArray()),
                2 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(2).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(2).Skip(x.One).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Two).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Two).Skip(1).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Two).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Two).Skip(x.One).ToArray()),
                5 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(2).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Skip(1).ToArray()),
                6 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(2).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Skip(x.One).ToArray()),
                7 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Two).Skip(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Skip(1).ToArray()),
                8 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Two).Skip(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Skip(x.One).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : [1], _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : [1], _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }", true)]
        public void Take_Take_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(2).Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(2).Take(1).ToArray()),
                2 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(2).Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(2).Take(x.One).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Two).Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Two).Take(1).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Two).Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Two).Take(x.One).ToArray()),
                5 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(2).Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Take(1).ToArray()),
                6 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(2).Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Take(x.One).ToArray()),
                7 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Two).Take(1).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Take(1).ToArray()),
                8 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Two).Take(x.One).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Take(x.One).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(1);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }", false)]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }", true)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }", false)]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }", true)]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(9, "{ $project : { _v : [2, 3], _id : 0 } }", false)]
        [InlineData(9, "{ $project : { _v : [2, 3], _id : 0 } }", true)]
        [InlineData(10, "{ $project : { _v : { $slice : [[2, 3], { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(10, "{ $project : { _v : { $slice : [[2, 3], { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }", false)]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }", true)]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", false)]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }", true)]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", false)]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }", true)]
        public void Take_Skip_Take_should_work(int scenario, string expectedStage, bool withNestedAsQueryable)
        {
            var collection = Fixture.Collection;

            var queryable = scenario switch
            {
                1 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(3).Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(3).Skip(1).Take(2).ToArray()),
                2 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(3).Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(3).Skip(1).Take(x.Two).ToArray()),
                3 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(3).Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(3).Skip(x.One).Take(2).ToArray()),
                4 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(3).Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(3).Skip(x.One).Take(x.Two).ToArray()),
                5 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Three).Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(1).Take(2).ToArray()),
                6 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Three).Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(1).Take(x.Two).ToArray()),
                7 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Three).Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(x.One).Take(2).ToArray()),
                8 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => x.A.AsQueryable().Take(x.Three).Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(x.One).Take(x.Two).ToArray()),
                9 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(3).Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(1).Take(2).ToArray()),
                10 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(3).Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(1).Take(x.Two).ToArray()),
                11 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(3).Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(x.One).Take(2).ToArray()),
                12 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(3).Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(x.One).Take(x.Two).ToArray()),
                13 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Three).Skip(1).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(1).Take(2).ToArray()),
                14 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Three).Skip(1).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(1).Take(x.Two).ToArray()),
                15 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Three).Skip(x.One).Take(2).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(x.One).Take(2).ToArray()),
                16 => withNestedAsQueryable ?
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.AsQueryable().Take(x.Three).Skip(x.One).Take(x.Two).ToArray()) :
                    collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(x.One).Take(x.Two).ToArray()),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3);
        }

        public class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int One { get; set; }
            public int Two { get; set; }
            public int Three { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1, A = [1, 2, 3, 4], One = 1, Two = 2, Three = 3 }
            ];
        }
    }
}
