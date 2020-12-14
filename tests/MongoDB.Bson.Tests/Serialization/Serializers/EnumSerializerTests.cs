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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public enum EnumWithUnderlyingTypeByte : byte
    {
        Zero = 0,
        One = 1,
        Max = byte.MaxValue
    }

    public enum EnumWithUnderlyingTypeSByte : sbyte
    {
        Zero = 0,
        One = 1,
        Max = sbyte.MaxValue
    }

    public enum EnumWithUnderlyingTypeInt16 : short
    {
        Zero = 0,
        One = 1,
        Max = short.MaxValue
    }

    public enum EnumWithUnderlyingTypeUInt16 : ushort
    {
        Zero = 0,
        One = 1,
        Max = ushort.MaxValue
    }

    public enum EnumWithUnderlyingTypeInt32 : int
    {
        Zero = 0,
        One = 1,
        Max = int.MaxValue
    }

    public enum EnumWithUnderlyingTypeUInt32 : uint
    {
        Zero = 0,
        One = 1,
        Max = uint.MaxValue
    }

    public enum EnumWithUnderlyingTypeInt64 : long
    {
        Zero = 0,
        One = 1,

        MaxInt = int.MaxValue,
        UIntOverflow = uint.MaxValue + 1L,
        Max = long.MaxValue
    }

    public enum EnumWithUnderlyingTypeUInt64 : ulong
    {
        Zero = 0,
        One = 1,

        MaxInt = int.MaxValue,
        UIntOverflow = uint.MaxValue + 1L,
        Max = ulong.MaxValue,
    }

    public class ClassWithEnumWithUnderlyingTypeByte
    {
        public EnumWithUnderlyingTypeByte E { get; set; }
    }

    public class ClassWithEnumWithUnderlyingTypeUInt16
    {
        public EnumWithUnderlyingTypeUInt16 E { get; set; }
    }

    public class ClassWithEnumWithUnderlyingTypeUInt32
    {
        public EnumWithUnderlyingTypeUInt32 E { get; set; }
    }

    public class ClassWithEnumWithUnderlyingTypeUInt64
    {
        public EnumWithUnderlyingTypeUInt64 E { get; set; }
    }

    public class ClassWithEnumAll
    {
        public EnumWithUnderlyingTypeByte Byte { get; set; }
        public EnumWithUnderlyingTypeSByte SByte { get; set; }
        public EnumWithUnderlyingTypeInt16 Int16 { get; set; }
        public EnumWithUnderlyingTypeUInt16 UInt16 { get; set; }
        public EnumWithUnderlyingTypeInt32 Int32 { get; set; }
        public EnumWithUnderlyingTypeUInt32 UInt32 { get; set; }

        [BsonRepresentation(BsonType.Int32)]
        public EnumWithUnderlyingTypeInt64 Int64AsInt32 { get; set; }

        [BsonRepresentation(BsonType.Int32)]
        public EnumWithUnderlyingTypeUInt64 UInt64AsInt32 { get; set; }
    }

    public class EnumSerializerTests
    {
        [Theory]
        [InlineData(EnumWithUnderlyingTypeByte.Zero, "{ $numberInt : '0' }")]
        [InlineData(EnumWithUnderlyingTypeByte.One, "{ $numberInt : '1' }")]
        [InlineData(EnumWithUnderlyingTypeByte.Max, "{ $numberInt : '255' }")]
        public void EnumWithUnderlyingTypeByte_should_roundtrip(
            EnumWithUnderlyingTypeByte value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeByte{ E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeByte>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeUInt16.Zero, "{ $numberInt : '0' }")]
        [InlineData(EnumWithUnderlyingTypeUInt16.One, "{ $numberInt : '1' }")]
        [InlineData(EnumWithUnderlyingTypeUInt16.Max, "{ $numberInt : '65535' }")]
        public void EnumWithUnderlyingTypeUInt16_should_roundtrip(
            EnumWithUnderlyingTypeUInt16 value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeUInt16 { E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeUInt16>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeUInt32.Zero, "{ $numberInt : '0' }")]
        [InlineData(EnumWithUnderlyingTypeUInt32.One, "{ $numberInt : '1' }")]
        [InlineData(EnumWithUnderlyingTypeUInt32.Max, "{ $numberInt : '-1' }")]
        public void EnumWithUnderlyingTypeUInt32_should_roundtrip(
            EnumWithUnderlyingTypeUInt32 value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeUInt32 { E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeUInt32>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeUInt64.Zero, "{ $numberLong : '0' }")]
        [InlineData(EnumWithUnderlyingTypeUInt64.One, "{ $numberLong : '1' }")]
        [InlineData(EnumWithUnderlyingTypeUInt64.Max, "{ $numberLong : '-1' }")]
        public void EnumWithUnderlyingTypeUInt64_should_roundtrip(
            EnumWithUnderlyingTypeUInt64 value,
            string expectedRepresentation)
        {
            var original = new ClassWithEnumWithUnderlyingTypeUInt64 { E = value };

            var bson = original.ToBson();
            var serialized = BsonSerializer.Deserialize<BsonDocument>(bson);
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumWithUnderlyingTypeUInt64>(bson);

            serialized["E"].Should().Be(expectedRepresentation);
            deserialized.E.Should().Be(original.E);
        }

        [Theory]
        [InlineData("{ SByte : -129 }")]
        [InlineData("{ SByte : 128 }")]
        [InlineData("{ Byte : 256 }")]
        [InlineData("{ Byte : -1 }")]
        [InlineData("{ Int16 : 32768 }")]
        [InlineData("{ Int16 : -32769.1 }")]
        [InlineData("{ Int32 : -2147483649 }")]
        [InlineData("{ Int32 : 2147483648 }")]
        [InlineData("{ UInt32 : NumberLong(-1) }")]
        [InlineData("{ UInt32 :  4294967296 }")]
        public void EnumDeserialization_should_overflow(string json)
        {
            var exception = Record.Exception(() => BsonSerializer.Deserialize<ClassWithEnumAll>(json));

            exception.Should().BeOfType<FormatException>();
            exception.InnerException.Should().BeOfType<OverflowException>();
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeInt64.Zero, EnumWithUnderlyingTypeUInt64.Max)]
        [InlineData(EnumWithUnderlyingTypeInt64.Zero, EnumWithUnderlyingTypeUInt64.UIntOverflow)]
        [InlineData(EnumWithUnderlyingTypeInt64.Max, EnumWithUnderlyingTypeUInt64.Zero)]
        [InlineData(EnumWithUnderlyingTypeInt64.UIntOverflow, EnumWithUnderlyingTypeUInt64.Zero)]
        public void EnumSerialization64As32_should_overflow(EnumWithUnderlyingTypeInt64 longAsInt, EnumWithUnderlyingTypeUInt64 ulongAsInt)
        {
            var obj = new ClassWithEnumAll()
            {
                Int64AsInt32 = longAsInt,
                UInt64AsInt32 = ulongAsInt
            };

            Record.Exception(() => obj.ToBson()).Should().BeOfType<OverflowException>();
        }

        [Theory]
        [InlineData(EnumWithUnderlyingTypeInt64.One, EnumWithUnderlyingTypeUInt64.One)]
        [InlineData(EnumWithUnderlyingTypeInt64.One, EnumWithUnderlyingTypeUInt64.MaxInt)]
        [InlineData(EnumWithUnderlyingTypeInt64.MaxInt, EnumWithUnderlyingTypeUInt64.One)]
        public void EnumSerialization64As32(EnumWithUnderlyingTypeInt64 longAsInt, EnumWithUnderlyingTypeUInt64 ulongAsInt)
        {
            var obj = new ClassWithEnumAll()
            {
                Int64AsInt32 = longAsInt,
                UInt64AsInt32 = ulongAsInt
            };

            var bson = obj.ToBson();
            var deserialized = BsonSerializer.Deserialize<ClassWithEnumAll>(bson);

            deserialized.Int64AsInt32.Should().Be(obj.Int64AsInt32);
            deserialized.UInt64AsInt32.Should().Be(obj.UInt64AsInt32);
        }
    }
}
