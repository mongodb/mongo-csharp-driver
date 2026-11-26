// ABOUTME: Verifies that LINQ integral division (int / int, long / long) returns the same results as C#.
// ABOUTME: Regression tests for CSHARP-6314.
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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

// Regression test for CSHARP-6314.
public class SelectIntegralDivideTests : LinqIntegrationTest<SelectIntegralDivideTests.ClassFixture>
{
    public SelectIntegralDivideTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_int_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.Age / 10);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $sort : { _id : 1 } }",
            "{ $project : { _v : { $toInt : { $divide : ['$Age', 10] } }, _id : 0 } }");

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.Age / 10));
    }

    [Fact]
    public void Select_int_divided_by_field_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.Age / x.Divisor);

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.Age / x.Divisor));
    }

    [Fact]
    public void Select_long_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.Big / 10L);

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.Big / 10L));
    }

    [Fact]
    public void Select_nullable_int_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.NullableAge / 10);

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.NullableAge / 10));
    }

    [Fact]
    public void Select_int_divided_by_constant_then_multiplied_should_match_csharp()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.Age / 10 * 10);

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.Age / 10 * 10));
    }

    [Fact]
    public void GroupBy_int_divided_by_constant_should_group_by_truncated_key()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .GroupBy(x => x.Age / 10)
            .Select(g => new { Decade = g.Key, Count = g.Count() });

        var result = queryable.ToList().OrderBy(x => x.Decade).Select(x => (x.Decade, x.Count));

        var expected = TestData
            .GroupBy(x => x.Age / 10)
            .Select(g => (Decade: g.Key, Count: g.Count()))
            .OrderBy(x => x.Decade);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Where_int_divided_by_constant_should_compare_truncated_value()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Age / 10 == 2)
            .OrderBy(x => x.Id)
            .Select(x => x.Id);

        var result = queryable.ToList();

        result.Should().Equal(TestData.Where(x => x.Age / 10 == 2).OrderBy(x => x.Id).Select(x => x.Id));
    }

    [Fact]
    public void Select_int_divided_by_double_should_remain_fractional()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .OrderBy(x => x.Id)
            .Select(x => x.Age / 10.0);

        var result = queryable.ToList();

        result.Should().Equal(TestData.OrderBy(x => x.Id).Select(x => x.Age / 10.0));
    }

    public class C
    {
        public int Id { get; set; }
        public int Age { get; set; }
        public int Divisor { get; set; }
        public long Big { get; set; }
        public int? NullableAge { get; set; }
    }

    private static readonly C[] TestData =
    [
        new C { Id = 1, Age = 25, Divisor = 10, Big = 25, NullableAge = 25 },
        new C { Id = 2, Age = -25, Divisor = 10, Big = -25, NullableAge = -25 },
        new C { Id = 3, Age = 30, Divisor = 7, Big = 30, NullableAge = null },
        new C { Id = 4, Age = 0, Divisor = 3, Big = 0, NullableAge = 0 },
        new C { Id = 5, Age = 7, Divisor = -2, Big = 7, NullableAge = 7 },
        new C { Id = 6, Age = -7, Divisor = -2, Big = -7, NullableAge = -7 },
        new C { Id = 7, Age = 29, Divisor = 5, Big = 3_000_000_000_007, NullableAge = 29 }
    ];

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        protected override IEnumerable<C> InitialData => TestData;
    }
}
