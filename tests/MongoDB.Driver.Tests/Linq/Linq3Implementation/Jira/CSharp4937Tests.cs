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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Driver.Linq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4937Tests : Linq3IntegrationTest
    {
        [Theory]
        [InlineData(2, "<" , 0, "{ _id : { $lt : 2 } }", new int[] { 1 })]
        [InlineData(2, "<=", 0, "{ _id : { $lte : 2 } }", new int[] { 1, 2 })]
        [InlineData(2, "==", 0, "{ _id : 2 }", new int[] { 2 })]
        [InlineData(2, "!=", 0, "{ _id : { $ne : 2 } }", new int[] { 1, 3 })]
        [InlineData(2, ">=", 0, "{ _id : { $gte : 2 } }", new int[] { 2, 3 })]
        [InlineData(2, ">" , 0, "{ _id : { $gt : 2 } }", new int[] { 3 })]
        public void CompareTo_filter_should_work(
            int comparand,
            string comparisonOperator,
            int compareToResult,
            string expectedFilter,
            int[] expectedIds)
        {
            var collection = GetCollection();

            Expression<Func<C, bool>> predicate = comparisonOperator switch
            {
                "<" => x => x.Id.CompareTo(comparand) < compareToResult,
                "<=" => x => x.Id.CompareTo(comparand) <= compareToResult,
                "==" => x => x.Id.CompareTo(comparand) == compareToResult,
                "!=" => x => x.Id.CompareTo(comparand) != compareToResult,
                ">=" => x => x.Id.CompareTo(comparand) >= compareToResult,
                ">" => x => x.Id.CompareTo(comparand) > compareToResult,
                _ => throw new InvalidOperationException()
            };

            Implementation(collection, comparand, predicate);

            void Implementation<TModel, TId>(IMongoCollection<TModel> collection, TId _, Expression<Func<TModel, bool>> predicate)
                where TModel : IIdentity<TId>
                where TId : IComparable<TId>
            {
                var queryable = collection.AsQueryable()
                    .Where(predicate);

                var stages = Translate(collection, queryable);
                var expectedStage = "{ $match : <filter> }".Replace("<filter>", expectedFilter);
                AssertStages(stages, expectedStage);

                var results = queryable.ToList();
                results.Select(x => x.Id).Should().Equal(expectedIds);
            }
        }

        [Theory]
        [InlineData(2, "<", 0, "_v : { $lt : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { true, false, false })]
        [InlineData(2, "<=", 0, "_v : { $lte : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { true, true, false })]
        [InlineData(2, "==", 0, "_v : { $eq : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { false, true, false })]
        [InlineData(2, "!=", 0, "_v : { $ne : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { true, false, true })]
        [InlineData(2, ">=", 0, "_v : { $gte : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { false, true, true })]
        [InlineData(2, ">", 0, "_v : { $gt : [{ $cmp : ['$_id', 2] }, 0] }", new bool[] { false, false, true })]
        public void CompareTo_expression_should_work(
            int comparand,
            string comparisonOperator,
            int compareToResult,
            string expectedProjection,
            bool[] expectedResults)
        {
            var collection = GetCollection();

            Expression<Func<C, bool>> selector = comparisonOperator switch
            {
                "<" => x => x.Id.CompareTo(comparand) < compareToResult,
                "<=" => x => x.Id.CompareTo(comparand) <= compareToResult,
                "==" => x => x.Id.CompareTo(comparand) == compareToResult,
                "!=" => x => x.Id.CompareTo(comparand) != compareToResult,
                ">=" => x => x.Id.CompareTo(comparand) >= compareToResult,
                ">" => x => x.Id.CompareTo(comparand) > compareToResult,
                _ => throw new InvalidOperationException()
            };

            Implementation(collection, comparand, selector);

            void Implementation<TModel, TId>(IMongoCollection<TModel> collection, TId _, Expression<Func<TModel, bool>> selector)
                where TModel : IIdentity<TId>
                where TId : IComparable<TId>
            {
                var queryable = collection.AsQueryable()
                    .Select(selector);

                var stages = Translate(collection, queryable);
                AssertStages(stages, "{ $project : { <projection>, _id : 0 } }".Replace("<projection>", expectedProjection));

                var results = queryable.ToList();
                results.Should().Equal(expectedResults);
            }
        }

        private IMongoCollection<C> GetCollection()
        {
            var collection = GetCollection<C>("test");
            CreateCollection(
                collection,
                new C { Id = 1 },
                new C { Id = 2 },
                new C { Id = 3 });
            return collection;
        }

        public interface IIdentity<TId>
        {
            public TId Id { get; set; }
        }

        private class C : IIdentity<int>
        {
            public int Id { get; set; }
        }
    }
}
