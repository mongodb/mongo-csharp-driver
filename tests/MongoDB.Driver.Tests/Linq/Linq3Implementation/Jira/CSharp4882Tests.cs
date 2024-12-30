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
using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4882Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2147483647] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : [2, 3, 4], _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        public void Skip_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Skip(1)),
                2 => collection.AsQueryable().Select(x => x.A.Skip(x.One)),
                3 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1)),
                4 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3, 4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2147483647] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : [4], _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2, 2147483647] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }, 2147483647] }, _id : 0 } }")]
        public void Skip_Skip_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Skip(1).Skip(2)),
                2 => collection.AsQueryable().Select(x => x.A.Skip(1).Skip(x.Two)),
                3 => collection.AsQueryable().Select(x => x.A.Skip(x.One).Skip(2)),
                4 => collection.AsQueryable().Select(x => x.A.Skip(x.One).Skip(x.Two)),
                5 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Skip(2)),
                6 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Skip(x.Two)),
                7 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Skip(2)),
                8 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Skip(x.Two)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : [2, 3], _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [[2, 3, 4], { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        public void Skip_Take_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Skip(1).Take(2)),
                2 => collection.AsQueryable().Select(x => x.A.Skip(1).Take(x.Two)),
                3 => collection.AsQueryable().Select(x => x.A.Skip(x.One).Take(2)),
                4 => collection.AsQueryable().Select(x => x.A.Skip(x.One).Take(x.Two)),
                5 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Take(2)),
                6 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(1).Take(x.Two)),
                7 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Take(2)),
                8 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.One).Take(x.Two)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 3, 2] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 2, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(9, "{ $project : { _v : [4], _id : 0 } }")]
        [InlineData(10, "{ $project : { _v : { $slice : [[3, 4], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 1, 2] }, _id : 0 } }")]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, 3] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }, 2147483647] }, { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        public void Skip_Take_Skip_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Skip(2).Take(3).Skip(1)),
                2 => collection.AsQueryable().Select(x => x.A.Skip(2).Take(3).Skip(x.One)),
                3 => collection.AsQueryable().Select(x => x.A.Skip(2).Take(x.Three).Skip(1)),
                4 => collection.AsQueryable().Select(x => x.A.Skip(2).Take(x.Three).Skip(x.One)),
                5 => collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(3).Skip(1)),
                6 => collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(3).Skip(x.One)),
                7 => collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(x.Three).Skip(1)),
                8 => collection.AsQueryable().Select(x => x.A.Skip(x.Two).Take(x.Three).Skip(x.One)),
                9 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(3).Skip(1)),
                10 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(3).Skip(x.One)),
                11 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(x.Three).Skip(1)),
                12 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(2).Take(x.Three).Skip(x.One)),
                13 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(3).Skip(1)),
                14 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(3).Skip(x.One)),
                15 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(x.Three).Skip(1)),
                16 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Skip(x.Two).Take(x.Three).Skip(x.One)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(4);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : ['$A', { $max : ['$One', 0] }] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : [1], _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [[1, 2, 3, 4], { $max : ['$One', 0] }] }, _id : 0 } }")]
        public void Take_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Take(1)),
                2 => collection.AsQueryable().Select(x => x.A.Take(x.One)),
                3 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(1)),
                4 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.One)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(1);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 1] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : [2], _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1, 2147483647] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, _id : 0 } }")]
        public void Take_Skip_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Take(2).Skip(1)),
                2 => collection.AsQueryable().Select(x => x.A.Take(2).Skip(x.One)),
                3 => collection.AsQueryable().Select(x => x.A.Take(x.Two).Skip(1)),
                4 => collection.AsQueryable().Select(x => x.A.Take(x.Two).Skip(x.One)),
                5 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Skip(1)),
                6 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Skip(x.One)),
                7 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Skip(1)),
                8 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Skip(x.One)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 2] }, { $max : ['$One', 0] }] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : [1], _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [[1, 2], { $max : ['$One', 0] }] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, 1] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Two', 0] }] }, { $max : ['$One', 0] }] }, _id : 0 } }")]
        public void Take_Take_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Take(2).Take(1)),
                2 => collection.AsQueryable().Select(x => x.A.Take(2).Take(x.One)),
                3 => collection.AsQueryable().Select(x => x.A.Take(x.Two).Take(1)),
                4 => collection.AsQueryable().Select(x => x.A.Take(x.Two).Take(x.One)),
                5 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Take(1)),
                6 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(2).Take(x.One)),
                7 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Take(1)),
                8 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Two).Take(x.One)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(1);
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : { $slice : ['$A', 1, 2] }, _id : 0 } }")]
        [InlineData(2, "{ $project : { _v : { $slice : [{ $slice : ['$A', 1, 2] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(3, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(4, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', 3] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(5, "{ $project : { _v : { $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }")]
        [InlineData(6, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(7, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(8, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : ['$A', { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(9, "{ $project : { _v : [2, 3], _id : 0 } }")]
        [InlineData(10, "{ $project : { _v : { $slice : [[2, 3], { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(11, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(12, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3], { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(13, "{ $project : { _v : { $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2] }, _id : 0 } }")]
        [InlineData(14, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, 1, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        [InlineData(15, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, 2] }, _id : 0 } }")]
        [InlineData(16, "{ $project : { _v : { $slice : [{ $slice : [{ $slice : [[1, 2, 3, 4], { $max : ['$Three', 0] }] }, { $max : ['$One', 0] }, 2147483647] }, { $max : ['$Two', 0] }] }, _id : 0 } }")]
        public void Take_Skip_Take_should_work(int scenario, string expectedStage)
        {
            var collection = GetCollection();

            var queryable = scenario switch
            {
                1 => collection.AsQueryable().Select(x => x.A.Take(3).Skip(1).Take(2)),
                2 => collection.AsQueryable().Select(x => x.A.Take(3).Skip(1).Take(x.Two)),
                3 => collection.AsQueryable().Select(x => x.A.Take(3).Skip(x.One).Take(2)),
                4 => collection.AsQueryable().Select(x => x.A.Take(3).Skip(x.One).Take(x.Two)),
                5 => collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(1).Take(2)),
                6 => collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(1).Take(x.Two)),
                7 => collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(x.One).Take(2)),
                8 => collection.AsQueryable().Select(x => x.A.Take(x.Three).Skip(x.One).Take(x.Two)),
                9 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(1).Take(2)),
                10 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(1).Take(x.Two)),
                11 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(x.One).Take(2)),
                12 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(3).Skip(x.One).Take(x.Two)),
                13 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(1).Take(2)),
                14 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(1).Take(x.Two)),
                15 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(x.One).Take(2)),
                16 => collection.AsQueryable().Select(x => new[] { 1, 2, 3, 4 }.Take(x.Three).Skip(x.One).Take(x.Two)),
                _ => throw new ArgumentException($"Invalid scenario: {scenario}", nameof(scenario))
            };

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var result = queryable.Single();
            result.Should().Equal(2, 3);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, A = [1, 2, 3, 4], One = 1, Two = 2, Three = 3 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int[] A { get; set; }
            public int One { get; set; }
            public int Two { get; set; }
            public int Three { get; set; }
        }
    }
}
