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
using MongoDB.Driver.TestHelpers;
using FluentAssertions;
using MongoDB.Driver.Tests.Specifications.connection_monitoring_and_pooling;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5572Tests : LinqIntegrationTest<CSharp5572Tests.ClassFixture>
{
    public CSharp5572Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Where_with_Substring_and_Trim_should_work()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(d => d.Str.Substring(2).Trim() == "cd");

        var stages = Translate(collection, queryable);
        AssertStages(stages, """{ $match : { $expr : { $eq : [{ $trim : { input : { $substrCP : ["$Str", 2, { $subtract : [{ $strLenCP : "$Str" }, 2] }] } } }, "cd"] } } }""");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Fact]
    public void Where_with_nullable_bool_comparison_should_work()
    {
        var collection = Fixture.Collection;

        var x = Expression.Parameter(typeof(C), "x");
        var body = Expression.Equal(
            Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Property(x, typeof(C).GetProperty("NullableBool")),
                Expression.Constant(true, typeof(bool?)),
                true,
                null
            ),
            Expression.Constant((bool?)true, typeof(bool?)));
        var parameters = new ParameterExpression[] { x };
        var selector = Expression.Lambda<Func<C, bool>>(body, parameters);

        var queryable = collection.AsQueryable()
            .Where(selector);

        var stages = Translate(collection, queryable);
        AssertStages(stages, "{ $match : { NullableBool : true } }");

        var results = queryable.ToList();
        results.Select(x => x.Id).Should().Equal(2);
    }

    public class C
    {
        public int Id  { get; set; }
        public string Str {  get; set; }
        public bool? NullableBool { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, Str = "abcd", NullableBool = false },
            new C { Id = 2, Str = "cdef", NullableBool = true },
            new C { Id = 3, Str = "efgh", NullableBool = null }
        ];
    }
}
