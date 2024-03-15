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
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4985Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(1, "{ $project : { _v : '$X', _id : 0 } }", new[] { 1, 3 })]
        [InlineData(2, "{ $project : { _v : { $add : ['$X', 2] }, _id : 0 } }", new[] { 3, 5 })]
        [InlineData(3, "{ $project : { _v : { $add : ['$X', '$Y'] }, _id : 0 } }", new[] { 3, 7 })]
        [InlineData(4, "{ $project : { _v : { $add : ['$X', '$NY'] }, _id : 0 } }", null)]
        [InlineData(5, "{ $project : { _v : '$NX', _id : 0 } }", null)]
        [InlineData(6, "{ $project : { _v : { $add : ['$NX', 2] }, _id : 0 } }", null)]
        [InlineData(7, "{ $project : { _v : { $add : ['$NX', '$Y'] }, _id : 0 } }", null)]
        [InlineData(8, "{ $project : { _v : { $add : ['$NX', '$NY'] }, _id : 0 } }", null)]
        [InlineData(9, "{ $project : { _v : { $add : [1, '$Y'] }, _id : 0 } }", new[] { 3, 5 })]
        [InlineData(10, "{ $project : { _v : { $add : [1, '$NY'] }, _id : 0 } }", null)]
        public void Select_with_int_result_should_work(
             int test,
             string expectedStage,
             int[] expectedResults)
        {
            var collection = GetCollection();
            Expression<Func<C, int>> selector = test switch
            {
                1 => x => x.X,
                2 => x => x.X + 2,
                3 => x => x.X + x.Y,
                4 => x => x.X + x.NY.Value,
                5 => x => x.NX.Value,
                6 => x => x.NX.Value + 2,
                7 => x => x.NX.Value + x.Y,
                8 => x => x.NX.Value + x.NY.Value,
                9 => x => 1 + x.Y,
                10 => x => 1 + x.NY.Value,
                _ => throw new Exception()
            };
            var queryable = collection.AsQueryable().Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            List<int> results = null;
            var exception = Record.Exception(() => { results = queryable.ToList(); });
            if (expectedResults != null)
            {
                exception.Should().BeNull();
                results.Should().Equal(expectedResults);
            }
            else
            {
                exception.Should().BeOfType<FormatException>();
                exception.Message.Should().Contain("Cannot deserialize a 'Int32' from BsonType 'Null'");
            }
        }

        [Theory]
        [InlineData(1, "{ $project : { _v : '$X', _id : 0 } }", new object[] { 1, 3 })]
        [InlineData(2, "{ $project : { _v : { $add : ['$X', 2] }, _id : 0 } }", new object[] { 3, 5 })]
        [InlineData(3, "{ $project : { _v : { $add : ['$X', '$Y'] }, _id : 0 } }", new object[] { 3, 7 })]
        [InlineData(4, "{ $project : { _v : { $add : ['$X', '$NY'] }, _id : 0 } }", new object[] { 3, null })]
        [InlineData(5, "{ $project : { _v : '$NX', _id : 0 } }", new object[] { 1, null })]
        [InlineData(6, "{ $project : { _v : { $add : ['$NX', 2] }, _id : 0 } }", new object[] { 3, null })]
        [InlineData(7, "{ $project : { _v : { $add : ['$NX', '$Y'] }, _id : 0 } }", new object[] { 3, null })]
        [InlineData(8, "{ $project : { _v : { $add : ['$NX', '$NY'] }, _id : 0 } }", new object[] { 3, null })]
        [InlineData(9, "{ $project : { _v : { $add : [1, '$Y'] }, _id : 0 } }", new object[] { 3, 5 })]
        [InlineData(10, "{ $project : { _v : { $add : [1, '$NY'] }, _id : 0 } }", new object[] { 3, null })]
        public void Select_with_nullable_int_result_should_work(
            int test,
            string expectedStage,
            object[] expectedResults)
        {
            var collection = GetCollection();
            Expression<Func<C, int?>> selector = test switch
            {
                1 => x => x.X,
                2 => x => x.X + 2,
                3 => x => x.X + x.Y,
                4 => x => x.X + x.NY,
                5 => x => x.NX,
                6 => x => x.NX + 2,
                7 => x => x.NX + x.Y,
                8 => x => x.NX + x.NY,
                9 => x => 1 + x.Y,
                10 => x => 1 + x.NY,
                _ => throw new Exception()
            };
            var queryable = collection.AsQueryable().Select(selector);

            var stages = Translate(collection, queryable);
            AssertStages(stages, expectedStage);

            var results = queryable.ToList();
            results.Should().Equal(expectedResults.Select(r => r == null ? null : (int?)r));
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 1, Y = 2, NX = 1, NY = 2 },
                new C { Id = 2, X = 3, Y = 4, NX = null, NY = null });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int? NX { get; set; }
            public int? NY { get; set; }
        }
    }
}
