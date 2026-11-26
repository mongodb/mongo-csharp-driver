// ABOUTME: Verifies LINQ integral division edge cases (overflow, unsigned and large operands) against C# semantics.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

// Regression test for CSHARP-6314.
public class SelectIntegralDivideEdgeCaseTests : LinqIntegrationTest<SelectIntegralDivideEdgeCaseTests.ClassFixture>
{
    public SelectIntegralDivideEdgeCaseTests(ClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public void Select_int_min_value_divided_by_minus_one_should_fail_instead_of_returning_wrong_value()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 1)
            .Select(x => x.Int / x.Divisor);

        var exception = Record.Exception(() => queryable.ToList());

        exception.Should().BeOfType<MongoCommandException>()
            .Which.Message.Should().Contain("Conversion would overflow target type");
    }

    [Fact]
    public void Select_long_min_value_divided_by_minus_one_should_fail_instead_of_returning_wrong_value()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 51)
            .Select(x => x.Big / x.LongDivisor);

        var exception = Record.Exception(() => queryable.ToList());

        exception.Should().BeOfType<MongoCommandException>()
            .Which.Message.Should().Contain("Conversion would overflow target type");
    }

    [Fact]
    public void Select_uint_divided_by_constant_should_truncate()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 2)
            .Select(x => x.UInt / 7u);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { _id : 2 } }",
            "{ $project : { _v : { $toLong : { $divide : ['$UInt', NumberLong('7')] } }, _id : 0 } }");

        var result = queryable.ToList();

        result.Should().Equal(4_000_000_000u / 7u);
    }

    [Fact]
    public void Select_ulong_divided_by_constant_should_truncate()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 2)
            .Select(x => x.ULong / 7ul);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { _id : 2 } }",
            "{ $project : { _v : { $trunc : { $divide : [{ $toDecimal : '$ULong' }, { $toDecimal : 7 }] } }, _id : 0 } }");

        var result = queryable.ToList();

        result.Should().Equal(18_000_000_000_000_000_000ul / 7ul);
    }

    [Fact]
    public void Select_ulong_quotient_above_long_max_value_should_not_overflow()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 3)
            .Select(x => x.ULong / 1ul);

        var result = queryable.ToList();

        result.Should().Equal(18_000_000_000_000_000_000ul);
    }

    [Fact]
    public void Select_long_divided_by_long_should_be_exact_above_2_pow_53()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 10 && x.Id < 20)
            .OrderBy(x => x.Id)
            .Select(x => x.Big / x.LongDivisor);

        var stages = Translate(collection, queryable);
        AssertStages(
            stages,
            "{ $match : { _id : { $gt : 10, $lt : 20 } } }",
            "{ $sort : { _id : 1 } }",
            "{ $project : { _v : { $toLong : { $divide : [{ $toDecimal : '$Big' }, { $toDecimal : '$LongDivisor' }] } }, _id : 0 } }");

        var result = queryable.ToList();

        var expected = Fixture.TestData.Where(x => x.Id > 10 && x.Id < 20).OrderBy(x => x.Id).Select(x => x.Big / x.LongDivisor);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_int_divided_by_long_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 20 && x.Id < 30)
            .OrderBy(x => x.Id)
            .Select(x => x.Int / x.LongDivisor);

        var result = queryable.ToList();

        var expected = TestData.Where(x => x.Id > 20 && x.Id < 30).OrderBy(x => x.Id).Select(x => x.Int / x.LongDivisor);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_long_divided_by_int_should_be_exact_above_2_pow_53()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 20 && x.Id < 30)
            .OrderBy(x => x.Id)
            .Select(x => x.Big / x.Divisor);

        var result = queryable.ToList();

        var expected = TestData.Where(x => x.Id > 20 && x.Id < 30).OrderBy(x => x.Id).Select(x => x.Big / x.Divisor);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_nullable_int_divided_by_nullable_int_should_propagate_null()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 30 && x.Id < 40)
            .OrderBy(x => x.Id)
            .Select(x => x.NullableInt / x.NullableInt2);

        var result = queryable.ToList();

        var expected = TestData.Where(x => x.Id > 30 && x.Id < 40).OrderBy(x => x.Id).Select(x => x.NullableInt / x.NullableInt2);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_nullable_long_divided_by_nullable_long_should_propagate_null()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 30 && x.Id < 40)
            .OrderBy(x => x.Id)
            .Select(x => x.NullableLong / x.NullableLong2);

        var result = queryable.ToList();

        var expected = TestData.Where(x => x.Id > 30 && x.Id < 40).OrderBy(x => x.Id).Select(x => x.NullableLong / x.NullableLong2);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_nullable_ulong_divided_by_nullable_ulong_should_propagate_null()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id > 30 && x.Id < 40)
            .OrderBy(x => x.Id)
            .Select(x => x.NullableULong / x.NullableULong2);

        var result = queryable.ToList();

        var expected = TestData.Where(x => x.Id > 30 && x.Id < 40).OrderBy(x => x.Id).Select(x => x.NullableULong / x.NullableULong2);
        result.Should().Equal(expected);
    }

    [Fact]
    public void Select_short_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 41)
            .Select(x => x.Short / 7);

        var result = queryable.ToList();

        result.Should().Equal(short.MinValue / 7);
    }

    [Fact]
    public void Select_byte_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 41)
            .Select(x => x.Byte / 7);

        var result = queryable.ToList();

        result.Should().Equal(byte.MaxValue / 7);
    }

    [Fact]
    public void Select_sbyte_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 41)
            .Select(x => x.SByte / 7);

        var result = queryable.ToList();

        result.Should().Equal(sbyte.MinValue / 7);
    }

    [Fact]
    public void Select_ushort_divided_by_constant_should_truncate_toward_zero()
    {
        var collection = Fixture.Collection;

        var queryable = collection.AsQueryable()
            .Where(x => x.Id == 41)
            .Select(x => x.UShort / 7);

        var result = queryable.ToList();

        result.Should().Equal(ushort.MaxValue / 7);
    }

    public class C
    {
        public int Id { get; set; }
        public int Int { get; set; }
        public int Divisor { get; set; }
        public short Short { get; set; }
        public byte Byte { get; set; }
        public sbyte SByte { get; set; }
        public ushort UShort { get; set; }
        public int? NullableInt { get; set; }
        public int? NullableInt2 { get; set; }
        public long? NullableLong { get; set; }
        public long? NullableLong2 { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public ulong? NullableULong { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public ulong? NullableULong2 { get; set; }
        public long Big { get; set; }
        public long LongDivisor { get; set; }
        [BsonRepresentation(BsonType.Int64)] public uint UInt { get; set; }
        [BsonRepresentation(BsonType.Decimal128)] public ulong ULong { get; set; }
    }

    private static readonly C[] TestData =
    [
        new C { Id = 11, Big = 638_000_000_000_000_001L, LongDivisor = 10_000_000L },
        new C { Id = 12, Big = 1_700_000_000_123_456_789L, LongDivisor = 1_000_000_000L },
        new C { Id = 13, Big = long.MaxValue, LongDivisor = 1L },
        new C { Id = 14, Big = long.MaxValue, LongDivisor = 3L },
        new C { Id = 15, Big = -long.MaxValue, LongDivisor = 7L },
        new C { Id = 16, Big = 9_007_199_254_740_993L, LongDivisor = 1L },
        new C { Id = 17, Big = 4_611_686_018_427_387_903L, LongDivisor = 4_194_304L },
        new C { Id = 18, Big = -638_000_000_000_000_001L, LongDivisor = 10_000_000L },
        new C { Id = 21, Int = 2_000_000_000, Divisor = 7, Big = long.MaxValue, LongDivisor = 3 },
        new C { Id = 22, Int = -17, Divisor = -5, Big = -9_007_199_254_740_993L, LongDivisor = 2 },
        new C { Id = 23, Int = 5, Divisor = 2, Big = 9_223_372_036_854_775_806L, LongDivisor = 1 },
        new C { Id = 31, NullableInt = 25, NullableInt2 = 10, NullableLong = long.MaxValue, NullableLong2 = 3, NullableULong = ulong.MaxValue, NullableULong2 = 3 },
        new C { Id = 32, NullableInt = null, NullableInt2 = 10, NullableLong = null, NullableLong2 = 3, NullableULong = null, NullableULong2 = 3 },
        new C { Id = 33, NullableInt = 25, NullableInt2 = null, NullableLong = long.MaxValue, NullableLong2 = null, NullableULong = ulong.MaxValue, NullableULong2 = null },
        new C { Id = 41, Short = short.MinValue, Byte = byte.MaxValue, SByte = sbyte.MinValue, UShort = ushort.MaxValue },
        new C { Id = 51, Big = long.MinValue, LongDivisor = -1L }
    ];

    public sealed class ClassFixture : MongoCollectionFixture<C>
    {
        public IEnumerable<C> TestData => SelectIntegralDivideEdgeCaseTests.TestData;

        protected override IEnumerable<C> InitialData =>
        [
            ..SelectIntegralDivideEdgeCaseTests.TestData,
            new C { Id = 1, Int = int.MinValue, Divisor = -1 },
            new C { Id = 3, Int = 0, Divisor = 1, ULong = 18_000_000_000_000_000_000ul },
            new C { Id = 2, Int = 0, Divisor = 1, UInt = 4_000_000_000u, ULong = 18_000_000_000_000_000_000ul }
        ];
    }
}
