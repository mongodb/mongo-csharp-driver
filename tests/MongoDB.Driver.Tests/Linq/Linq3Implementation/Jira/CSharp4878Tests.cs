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

using System.Linq;
using FluentAssertions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4878Tests : Linq3IntegrationTest
    {
        [Fact]
        public void Repeat_with_constant_and_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Select(x => Enumerable.Repeat(2, 3));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : [2, 2, 2], _id : 0 } }");

            var result = queryable.First();
            result.Should().Equal(2, 2, 2);
        }

        [Fact]
        public void Repeat_with_constant_and_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Select(x => Enumerable.Repeat(2, x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $map : { input : { $range : [0, '$Y'] }, as : 'i', in : 2 } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Equal(2, 2, 2);
        }

        [Fact]
        public void Repeat_with_expression_and_constant_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Select(x => Enumerable.Repeat(x.X, 3));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $map : { input : { $range : [0, 3] }, as : 'i', in : '$X' } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Equal(2, 2, 2);
        }

        [Fact]
        public void Repeat_with_expression_and_expression_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable().Select(x => Enumerable.Repeat(x.X, x.Y));

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $project : { _v : { $map : { input : { $range : [0, '$Y'] }, as : 'i', in : '$X' } }, _id : 0 } }");

            var result = queryable.First();
            result.Should().Equal(2, 2, 2);
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1, X = 2, Y = 3 });
            return collection;
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
        }
    }
}
