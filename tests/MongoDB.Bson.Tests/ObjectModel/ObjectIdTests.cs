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
using System.Globalization;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests
{
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
        [InlineData(0xFFFFFE, 0xFFFFFF)]
        [InlineData(0xFFFFFF, 0)]
        [InlineData(0x1000000, 1)]
        [InlineData(0x1FFFFFE, 0xFFFFFF)]
        [InlineData(0x1FFFFFF, 0)]
        [InlineData(0x2000000, 1)]
        public void Ensure_that_increment_wraps_around_after_max_value(int seedIncrement, int expectedIncrement)
        {
            ObjectIdReflector.__staticIncrement(seedIncrement);
            var objectId = ObjectId.GenerateNewId();
#pragma warning disable 618
            objectId.Increment.Should().Be(expectedIncrement);
#pragma warning restore 618
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
        }

        [Fact]
        public void TestIntIntShortIntConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
#pragma warning disable 618
            var objectId = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);
#pragma warning restore 618
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x01020304, objectId._a());
            Assert.Equal(0x05060708, objectId._b());
            Assert.Equal(0x090a0b0c, objectId._c());
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Fact]
        public void TestIntIntShortIntConstructorWithInvalidIncrement()
        {
#pragma warning disable 618
            var objectId = new ObjectId(0, 0, 0, 0x00ffffff);
            Assert.Equal(0x00ffffff, objectId.Increment);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(0, 0, 0, 0x01000000));
#pragma warning restore 618
        }

        [Fact]
        public void TestIntIntShortIntConstructorWithInvalidMachine()
        {
#pragma warning disable 618
            var objectId = new ObjectId(0, 0x00ffffff, 0, 0);
            Assert.Equal(0x00ffffff, objectId.Machine);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(0, 0x01000000, 0, 0));
#pragma warning restore 618
        }

        [Fact]
        public void TestPackWithInvalidIncrement()
        {
#pragma warning disable 618
            var objectId = new ObjectId(ObjectId.Pack(0, 0, 0, 0x00ffffff));
            Assert.Equal(0x00ffffff, objectId.Increment);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(ObjectId.Pack(0, 0, 0, 0x01000000)));
#pragma warning restore 618
        }

        [Fact]
        public void TestPackWithInvalidMachine()
        {
#pragma warning disable 618
            var objectId = new ObjectId(ObjectId.Pack(0, 0x00ffffff, 0, 0));
            Assert.Equal(0x00ffffff, objectId.Machine);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(ObjectId.Pack(0, 0x01000000, 0, 0)));
#pragma warning restore 618
        }

        [Fact]
        public void TestDateTimeConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var timestamp = BsonConstants.UnixEpoch.AddSeconds(0x01020304);
#pragma warning disable 618
            var objectId = new ObjectId(timestamp, 0x050607, 0x0809, 0x0a0b0c);
#pragma warning restore 618
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x01020304, objectId._a());
            Assert.Equal(0x05060708, objectId._b());
            Assert.Equal(0x090a0b0c, objectId._c());
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(uint.MaxValue)]
        public void TestDateTimeConstructorAtEdgeOfRange(uint secondsSinceEpoch)
        {
            var timestamp = BsonConstants.UnixEpoch.AddSeconds(secondsSinceEpoch);
#pragma warning disable 618
            var objectId = new ObjectId(timestamp, 0, 0, 0);
#pragma warning restore 618
            Assert.Equal(timestamp, objectId.CreationTime);
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData((long)uint.MaxValue + 1)]
        public void TestDateTimeConstructorArgumentOutOfRangeException(long secondsSinceEpoch)
        {
            var timestamp = BsonConstants.UnixEpoch.AddSeconds(secondsSinceEpoch);
#pragma warning disable 618
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectId(timestamp, 0, 0, 0));
#pragma warning restore 618
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
}
