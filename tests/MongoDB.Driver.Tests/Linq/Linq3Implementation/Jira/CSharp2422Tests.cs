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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp2422Tests
    {
        [Fact]
        public void Where_with_lambda_should_work()
        {
            var collection = GetCollection();

            var queryable = collection.AsQueryable()
                .Where(x => x.X > 0);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : {  X : { $gt : 0 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        [Theory]
        [ParameterAttributeData]
        public void Where_with_dynamically_created_predicate_should_work(
            [Values("x", null)] string parameterName)
        {
            var collection = GetCollection();
            var predicate = MakePredicate(parameterName);

            var queryable = collection.AsQueryable()
                .Where(predicate);

            var stages = Linq3TestHelpers.Translate(collection, queryable);
            var expectedStages = new[]
            {
                "{ $match : {  X : { $gt : 0 } } }"
            };
            Linq3TestHelpers.AssertStages(stages, expectedStages);
        }

        private IMongoCollection<C> GetCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase("test");
            return database.GetCollection<C>("test");
        }

        private Expression<Func<C, bool>> MakePredicate(string parameterName)
        {
            var parameter = Expression.Parameter(typeof(C), parameterName);
            var memberInfo = typeof(C).GetProperty("X");
            var body = Expression.MakeBinary(
                ExpressionType.GreaterThan,
                Expression.MakeMemberAccess(parameter, memberInfo),
                Expression.Constant(0));
            return Expression.Lambda<Func<C, bool>>(body, parameter);
        }

        private class C
        {
            public int Id { get; set; }
            public int X { get; set; }
        }
    }
}
