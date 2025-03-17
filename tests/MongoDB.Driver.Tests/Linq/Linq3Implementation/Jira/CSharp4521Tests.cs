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
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
    public class CSharp4512Tests : LinqIntegrationTest<CSharp4512Tests.ClassFixture>
    {
        public CSharp4512Tests(ClassFixture fixture)
            : base(fixture)
        {
        }

        [Theory]
        [InlineData("x")]
        [InlineData("")]
        [InlineData(null)]
        public void Where_should_work(string paramName)
        {
            var collection = Fixture.Collection;
            var param = Expression.Parameter(typeof(C), paramName);
            var body = Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Property(param, "Id"),
                Expression.Constant(1));
            var predicate = Expression.Lambda<Func<C, bool>>(body, param);

            var queryable = collection.AsQueryable().Where(predicate);

            var stages = Translate(collection, queryable);
            AssertStages(stages, "{ $match : { _id : 1 } }");

            var results = queryable.ToList();
            results.Select(x => x.Id).Should().Equal(1);
        }

        public class C
        {
            public int Id { get; set; }
        }

        public sealed class ClassFixture : MongoCollectionFixture<C>
        {
            protected override IEnumerable<C> InitialData =>
            [
                new C { Id = 1 },
                new C { Id = 2 }
            ];
        }
    }
}
