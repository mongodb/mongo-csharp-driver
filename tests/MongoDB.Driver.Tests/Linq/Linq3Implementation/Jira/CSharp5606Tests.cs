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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira;

public class CSharp5606Tests : LinqIntegrationTest<CSharp5606Tests.ClassFixture>
{
    public CSharp5606Tests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Theory]
    [ParameterAttributeData]
    public void Aggregage_Match_with_checked_or_unchecked_int_to_long_conversion_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<C, bool>> predicate;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                predicate = x => x.Id == 1L;
            }
        }
        else
        {
            predicate = x => x.Id == 1L;
        }

        var aggregate = collection.Aggregate()
            .Match(predicate);

        var stages = Translate(collection, aggregate);
        AssertStages(
            stages,
            """{ $match : { _id : 1 } }""");

        var results = aggregate.ToList();
        results.Select(x => x.Id).Should().Equal(1);
    }

    [Theory]
    [ParameterAttributeData]
    public void Aggregage_Group_with_checked_or_unchecked_int_to_long_conversion_should_work(
            [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<IGrouping<string, C>, MyResult>> groupLambdaExpression;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                groupLambdaExpression = x => new MyResult { Name = x.Key, Count = x.Count() };
            }
        }
        else
        {
            groupLambdaExpression = x => new MyResult { Name = x.Key, Count = x.Count() };
        }

        var aggregate = collection.Aggregate()
            .Group(
                x => x.MyField,
                groupLambdaExpression);

        var stages = Translate(collection, aggregate);
        AssertStages(
            stages,
            "{ $group : { _id : '$MyField', __agg0 : { $sum : 1 } } }",
            "{ $project : { Name : '$_id', Count : { $toLong : '$__agg0' }, _id : 0 } }");

        var results = aggregate.ToList().OrderBy(x => x.Name).ToList();
        results.Count.Should().Be(2);
        results[0].Name.Should().Be("a");
        results[0].Count.Should().Be(1L);
        results[1].Name.Should().Be("b");
        results[1].Count.Should().Be(2L);
    }

    [Theory]
    [ParameterAttributeData]
    public void Select_GroupBy_with_checked_or_unchecked_int_to_long_conversion_should_work(
        [Values(false, true)] bool @checked)
    {
        var collection = Fixture.Collection;

        Expression<Func<string, IEnumerable<C>, MyResult>> resultSelectorLambdaExpression;
        if (@checked)
        {
            checked // default compiler setting is not checked
            {
                resultSelectorLambdaExpression = (key, grouping) => new MyResult { Name = key, Count = grouping.Count() };
            }
        }
        else
        {
            resultSelectorLambdaExpression = (key, grouping) => new MyResult { Name = key, Count = grouping.Count() };
        }

        var queryable = collection.AsQueryable()
            .GroupBy(
                x => x.MyField,
                resultSelectorLambdaExpression);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $group : { _id : '$MyField', __agg0 : { $sum : 1 } } }",
            "{ $project : { Name : '$_id', Count : { $toLong : '$__agg0' }, _id : 0 } }");

        var results = queryable.ToList().OrderBy(x => x.Name).ToList();
        results.Count.Should().Be(2);
        results[0].Name.Should().Be("a");
        results[0].Count.Should().Be(1L);
        results[1].Name.Should().Be("b");
        results[1].Count.Should().Be(2L);
    }

    public class C
    {
        public int Id { get; set; }
        public string MyField { get; set; }
    }

    public class MyResult
    {
        public string Name { get; set; }
        public long Count { get; set; }
    }

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData =>
        [
            new C { Id = 1, MyField = "a" },
            new C { Id = 2, MyField = "b" },
            new C { Id = 3, MyField = "b" },
        ];
    }
}
