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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel;

public class ObjectIdTests
{
    [Theory]
    [InlineData(0x01020304, 0x0000000506070809, 0x0a0b0c, 0x01020304, 0x05060708, 0x090a0b0c)]
    [InlineData(0xf1f2f3f4, 0x000000f5f6f7f8f9, 0xfafbfc, 0xf1f2f3f4, 0xf5f6f7f8, 0xf9fafbfc)]
    public void Create_should_generate_expected_a_b_c(uint timestamp, long random, uint increment, uint expectedA, uint expectedB, uint expectedC)
    {
        var objectId = ObjectIdReflector.Create((int)timestamp, random, (int)increment);
        objectId._a().Should().Be((int)expectedA);
        objectId._b().Should().Be((int)expectedB);
        objectId._c().Should().Be((int)expectedC);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0xffffffffff, 0xffffff)]
    public void Create_should_not_throw_when_arguments_are_valid(long random, int increment)
    {
        var _ = ObjectIdReflector.Create(1, random, increment);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0x10000000000)]
    public void Create_should_throw_when_random_is_out_of_range(long random)
    {
        var exception = Record.Exception(() => ObjectIdReflector.Create(1, random, 1));
        var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
        e.ParamName.Should().Be("random");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0x1000000)]
    public void Create_should_throw_when_increment_is_out_of_range(int increment)
    {
        var exception = Record.Exception(() => ObjectIdReflector.Create(1, 1, increment));
        var e = exception.Should().BeOfType<ArgumentOutOfRangeException>().Subject;
        e.ParamName.Should().Be("increment");
    }

    [Theory]
    [InlineData(0x00000000, "1970-01-01T00:00:00Z")]
    [InlineData(0x7FFFFFFF, "2038-01-19T03:14:07Z")]
    [InlineData(0x80000000, "2038-01-19T03:14:08Z")]
    [InlineData(0xFFFFFFFF, "2106-02-07T06:28:15Z")]
    public void Ensure_that_timestamp_is_interpreted_as_unsigned_int(uint timestamp, string expectedDateString)
    {
        var objectId = ObjectId.GenerateNewId((int)timestamp);
        var expectedDate = DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        objectId.CreationTime.Should().Be(expectedDate);
    }

    [Fact]
    public void TestByteArrayConstructor()
    {
        byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var objectId = new ObjectId(bytes);
        Assert.Equal(0x01020304, objectId.Timestamp);
        Assert.Equal(0x01020304, objectId._a());
        Assert.Equal(0x05060708, objectId._b());
        Assert.Equal(0x090a0b0c, objectId._c());
        Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
        Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
        Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
        Span<byte> span = new byte[12];
        objectId.ToByteSpan(span);
        Assert.True(span.SequenceEqual(bytes));
    }

    [Fact]
    public void TestStringConstructor()
    {
        byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var objectId = new ObjectId("0102030405060708090a0b0c");
        Assert.Equal(0x01020304, objectId.Timestamp);
        Assert.Equal(0x01020304, objectId._a());
        Assert.Equal(0x05060708, objectId._b());
        Assert.Equal(0x090a0b0c, objectId._c());
        Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
        Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
        Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
        Span<char> span = new char[24];
        objectId.ToCharSpan(span);
        Assert.True(span.SequenceEqual("0102030405060708090a0b0c".AsSpan()));
    }

    [Fact]
    public void ReadOnlySpanByteConstructor_should_set_expected_fields()
    {
        ReadOnlySpan<byte> span = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        var objectId = new ObjectId(span);
        objectId._a().Should().Be(0x01020304);
        objectId._b().Should().Be(0x05060708);
        objectId._c().Should().Be(0x090a0b0c);
        objectId.ToString().Should().Be("0102030405060708090a0b0c");
    }

    [Fact]
    public void ReadOnlySpanByteConstructor_should_throw_when_span_is_wrong_length()
    {
        Assert.Throws<ArgumentException>(() => new ObjectId(new byte[] { 1, 2, 3 }.AsSpan()));
    }

    [Theory]
    [MemberData(nameof(ToByteSpan_TestCases))]
    public void ToByteSpan_should_return_expected_results(byte[] data, byte[] expectedBytes)
    {
        var objectId = new ObjectId(data);
        Span<byte> destination = new byte[12];
        objectId.ToByteSpan(destination);
        Assert.True(destination.SequenceEqual(expectedBytes));
    }

    public static IEnumerable<object[]> ToByteSpan_TestCases()
    {
        yield return [new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } ];
        yield return [new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } ];
        yield return [new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 } ];
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void ToByteSpan_should_throw_when_destination_is_too_short(int length)
    {
        var objectId = ObjectId.Empty;
        var exception = Record.Exception(() => objectId.ToByteSpan(new byte[length]));
        var e = exception.Should().BeOfType<ArgumentException>().Subject;
        e.ParamName.Should().Be("destination");
    }

    [Theory]
    [MemberData(nameof(ToCharSpan_TestCases))]
    public void ToCharSpan_should_return_expected_results(byte[] data, string expectedChars)
    {
        var objectId = new ObjectId(data);
        Span<char> destination = new char[24];
        objectId.ToCharSpan(destination);
        Assert.True(destination.SequenceEqual(expectedChars.AsSpan()));
        Assert.Equal(expectedChars, objectId.ToString());
    }

    public static IEnumerable<object[]> ToCharSpan_TestCases()
    {
        yield return [new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, "000000000000000000000000"];
        yield return [new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, "0102030405060708090a0b0c"];
        yield return [new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, "ffffffffffffffffffffffff"];
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    public void ToCharSpan_should_throw_when_destination_is_too_short(int length)
    {
        var objectId = ObjectId.Empty;
        var exception = Record.Exception(() => objectId.ToCharSpan(new char[length]));
        var e = exception.Should().BeOfType<ArgumentException>().Subject;
        e.ParamName.Should().Be("destination");
    }

    [Fact]
    public void TestGenerateNewId()
    {
        // compare against two timestamps in case seconds since epoch changes in middle of test
        var timestamp1 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
        var objectId = ObjectId.GenerateNewId();
        var timestamp2 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
        Assert.True(objectId.Timestamp == timestamp1 || objectId.Timestamp == timestamp2);
        Assert.NotEqual(0, objectId._a());
        Assert.NotEqual(0, objectId._b());
        Assert.NotEqual(0, objectId._c());
    }

    [Fact]
    public void TestGenerateNewIdWithDateTime()
    {
        var timestamp = new DateTime(2011, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var objectId = ObjectId.GenerateNewId(timestamp);
        Assert.True(objectId.CreationTime == timestamp);
        Assert.NotEqual(0, objectId._a());
        Assert.NotEqual(0, objectId._b());
        Assert.NotEqual(0, objectId._c());
    }

    [Fact]
    public void TestGenerateNewIdWithTimestamp()
    {
        var timestamp = 0x01020304;
        var objectId = ObjectId.GenerateNewId(timestamp);
        Assert.True(objectId.Timestamp == timestamp);
        Assert.NotEqual(0, objectId._a());
        Assert.NotEqual(0, objectId._b());
        Assert.NotEqual(0, objectId._c());
    }

    [Fact]
    public void TestIComparable()
    {
        var objectId1 = ObjectId.GenerateNewId();
        var objectId2 = ObjectId.GenerateNewId();
        Assert.Equal(0, objectId1.CompareTo(objectId1));
        Assert.Equal(-1, objectId1.CompareTo(objectId2));
        Assert.Equal(1, objectId2.CompareTo(objectId1));
        Assert.Equal(0, objectId2.CompareTo(objectId2));
    }

    [Fact]
    public void TestCompareEqualGeneratedIds()
    {
        var objectId1 = ObjectId.GenerateNewId();
        var objectId2 = objectId1;
        Assert.False(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.False(objectId1 != objectId2);
        Assert.True(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareSmallerTimestamp()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0c");
        var objectId2 = new ObjectId("0102030505060708090a0b0c");
        Assert.True(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.False(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareSmallerMachine()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0c");
        var objectId2 = new ObjectId("0102030405060808090a0b0c");
        Assert.True(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.False(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareSmallerPid()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0c");
        var objectId2 = new ObjectId("01020304050607080a0a0b0c");
        Assert.True(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.False(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareSmallerIncrement()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0c");
        var objectId2 = new ObjectId("0102030405060708090a0b0d");
        Assert.True(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.False(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareSmallerGeneratedId()
    {
        var objectId1 = ObjectId.GenerateNewId();
        var objectId2 = ObjectId.GenerateNewId();
        Assert.True(objectId1 < objectId2);
        Assert.True(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.False(objectId1 > objectId2);
        Assert.False(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareLargerTimestamp()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0c");
        var objectId2 = new ObjectId("0102030305060708090a0b0c");
        Assert.False(objectId1 < objectId2);
        Assert.False(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.True(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareLargerMachine()
    {
        var objectId1 = new ObjectId("0102030405060808090a0b0c");
        var objectId2 = new ObjectId("0102030405060708090a0b0c");
        Assert.False(objectId1 < objectId2);
        Assert.False(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.True(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareLargerPid()
    {
        var objectId1 = new ObjectId("01020304050607080a0a0b0c");
        var objectId2 = new ObjectId("0102030405060708090a0b0c");
        Assert.False(objectId1 < objectId2);
        Assert.False(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.True(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareLargerIncrement()
    {
        var objectId1 = new ObjectId("0102030405060708090a0b0d");
        var objectId2 = new ObjectId("0102030405060708090a0b0c");
        Assert.False(objectId1 < objectId2);
        Assert.False(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.True(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestCompareLargerGeneratedId()
    {
        var objectId2 = ObjectId.GenerateNewId(); // generate before objectId2
        var objectId1 = ObjectId.GenerateNewId();
        Assert.False(objectId1 < objectId2);
        Assert.False(objectId1 <= objectId2);
        Assert.True(objectId1 != objectId2);
        Assert.False(objectId1 == objectId2);
        Assert.True(objectId1 > objectId2);
        Assert.True(objectId1 >= objectId2);
    }

    [Fact]
    public void TestIConvertibleMethods()
    {
        var value = ObjectId.Empty;
        Assert.Equal(TypeCode.Object, ((IConvertible)value).GetTypeCode());
        Assert.Equal(value, ((IConvertible)value).ToType(typeof(object), null)); // not AreSame because of boxing
        Assert.Equal(value, ((IConvertible)value).ToType(typeof(ObjectId), null)); // not AreSame because of boxing
        Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
        Assert.Equal("000000000000000000000000", Convert.ToString(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
        Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));

        Assert.Equal(new BsonObjectId(value), ((IConvertible)value).ToType(typeof(BsonObjectId), null));
        Assert.Equal(new BsonString("000000000000000000000000"), ((IConvertible)value).ToType(typeof(BsonString), null));
        Assert.Equal("000000000000000000000000", ((IConvertible)value).ToType(typeof(string), null));
        Assert.Throws<InvalidCastException>(() => ((IConvertible)value).ToType(typeof(UInt64), null));
    }

    [Fact]
    public void TestParse()
    {
        var objectId1 = ObjectId.Parse("0102030405060708090a0b0c"); // lower case
        var objectId2 = ObjectId.Parse("0102030405060708090A0B0C"); // upper case
        Assert.True(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
        Assert.True(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
        Assert.True(objectId1.ToString() == objectId2.ToString());
        Assert.Throws<FormatException>(() => ObjectId.Parse("102030405060708090a0b0c")); // too short
        Assert.Throws<FormatException>(() => ObjectId.Parse("x102030405060708090a0b0c")); // invalid character
        Assert.Throws<FormatException>(() => ObjectId.Parse("00102030405060708090a0b0c")); // too long
    }

    [Fact]
    public void TestTryParse()
    {
        ObjectId objectId1, objectId2;
        Assert.True(ObjectId.TryParse("0102030405060708090a0b0c", out objectId1)); // lower case
        Assert.True(ObjectId.TryParse("0102030405060708090A0B0C", out objectId2)); // upper case
        Assert.True(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
        Assert.True(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
        Assert.True(objectId1.ToString() == objectId2.ToString());
        Assert.False(ObjectId.TryParse("102030405060708090a0b0c", out objectId1)); // too short
        Assert.False(ObjectId.TryParse("x102030405060708090a0b0c", out objectId1)); // invalid character
        Assert.False(ObjectId.TryParse("00102030405060708090a0b0c", out objectId1)); // too long
        Assert.False(ObjectId.TryParse(null, out objectId1)); // should return false not throw ArgumentNullException
    }

    [Fact]
    public void TestConvertObjectIdToObjectId()
    {
        var oid = ObjectId.GenerateNewId();

        var oidConverted = Convert.ChangeType(oid, typeof(ObjectId));

        Assert.Equal(oid, oidConverted);
    }
}

internal static class ObjectIdReflector
{
    public static int _a(this ObjectId obj)
    {
        return (int)Reflector.GetFieldValue(obj, nameof(_a));
    }

    public static int _b(this ObjectId obj)
    {
        return (int)Reflector.GetFieldValue(obj, nameof(_b));
    }

    public static int _c(this ObjectId obj)
    {
        return (int)Reflector.GetFieldValue(obj, nameof(_c));
    }

    public static ObjectId Create(int timestamp, long random, int increment)
    {
        return (ObjectId)Reflector.InvokeStatic(typeof(ObjectId), nameof(Create), timestamp, random, increment);
    }

    public static void __staticIncrement(int value)
    {
        Reflector.SetStaticFieldValue(typeof(ObjectId), nameof(__staticIncrement), value);
    }
}
