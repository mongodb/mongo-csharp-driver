/* Copyright 2020-present MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public enum EnumByte : byte
    {
        Zero = 0,
        One = 1,
        Min = byte.MinValue,
        Max = byte.MaxValue
    }

    public enum EnumSByte : sbyte
    {
        Zero = 0,
        One = 1,
        Min = sbyte.MinValue,
        Max = sbyte.MaxValue
    }

    public enum EnumInt16 : short
    {
        Zero = 0,
        One = 1,
        Min = short.MinValue,
        Max = short.MaxValue
    }

    public enum EnumUInt16 : ushort
    {
        Zero = 0,
        One = 1,
        Min = ushort.MinValue,
        Max = ushort.MaxValue
    }

    public enum EnumInt32 : int
    {
        Zero = 0,
        One = 1,
        Min = int.MinValue,
        Max = int.MaxValue
    }

    public enum EnumUInt32 : uint
    {
        Zero = 0,
        One = 1,
        Min = uint.MinValue,
        Max = uint.MaxValue
    }

    public enum EnumInt64 : long
    {
        Zero = 0,
        One = 1,
        Min = long.MinValue,
        Max = long.MaxValue
    }

    public enum EnumUInt64 : ulong
    {
        Zero = 0,
        One = 1,
        Min = ulong.MinValue,
        Max = ulong.MaxValue
    }

    public class EnumWrapper<T> where T : Enum
    {
        public T EnumValue { get; set; }
    }

    public class EnumWrapperAsInt32<T> where T : Enum
    {
        [BsonRepresentation(BsonType.Int32)]
        public T EnumValue { get; set; }
    }

    public class EnumWrapperAsInt64<T> where T : Enum
    {
        [BsonRepresentation(BsonType.Int64)]
        public T EnumValue { get; set; }
    }

    public class EnumWrapperAsString<T> where T : Enum
    {
        [BsonRepresentation(BsonType.String)]
        public T EnumValue { get; set; }
    }

    public class EnumSerializationTests
    {
        [Theory]
        [InlineData(EnumByte.Zero)]
        [InlineData(EnumSByte.Zero)]
        [InlineData(EnumInt16.Zero)]
        [InlineData(EnumUInt16.Zero)]
        [InlineData(EnumInt32.Zero)]
        [InlineData(EnumUInt32.Zero)]
        [InlineData(EnumInt64.Zero)]
        [InlineData(EnumUInt64.Zero)]
        public void EnumSerialization_should_roundtrip<T>(T enumValue) where T : struct, Enum
        {
            EnsureSerializer<T>();

            var enumValues = Enum.GetValues(typeof(T));

            foreach (var value in enumValues.Cast<T>())
            {
                // Test default serialization
                var original = new EnumWrapper<T> { EnumValue = value };

                var bson = original.ToBson();
                var deserialized = BsonSerializer.Deserialize<EnumWrapper<T>>(bson);

                deserialized.EnumValue.Should().Be(original.EnumValue);

                // Test serialization as string
                var originalAsString = new EnumWrapperAsString<T> { EnumValue = value };

                bson = original.ToBson();
                var deserializedAsString = BsonSerializer.Deserialize<EnumWrapperAsString<T>>(bson);

                deserializedAsString.EnumValue.Should().Be(originalAsString.EnumValue);
            }
        }

        [Theory]
        [InlineData(EnumByte.Max)]
        [InlineData(EnumSByte.Max)]
        [InlineData(EnumInt16.Max)]
        [InlineData(EnumUInt16.Max)]
        [InlineData(EnumInt32.Max)]
        [InlineData(EnumUInt32.Max)]
        [InlineData((EnumInt64)int.MaxValue)]
        [InlineData((EnumInt64)int.MinValue)]
        [InlineData((EnumUInt64)int.MaxValue)]
        [InlineData((EnumUInt64)uint.MinValue)]
        public void EnumSerializationAs32_should_roundtrip<T>(T enumValue) where T : struct, Enum
        {
            EnsureSerializer<T>();

            var original = new EnumWrapperAsInt32<T> { EnumValue = enumValue };
            var bson = original.ToBson();
            var deserialized = BsonSerializer.Deserialize<EnumWrapperAsInt32<T>>(bson);

            deserialized.EnumValue.Should().Be(original.EnumValue);
        }

        [Theory]
        [InlineData(EnumByte.Max)]
        [InlineData(EnumSByte.Max)]
        [InlineData(EnumInt16.Max)]
        [InlineData(EnumUInt16.Max)]
        [InlineData(EnumInt32.Max)]
        [InlineData(EnumUInt32.Max)]
        [InlineData(EnumInt64.Max)]
        [InlineData(EnumUInt64.Max)]
        public void EnumSerializationAs64_should_roundtrip<T>(T enumValue) where T : struct, Enum
        {
            EnsureSerializer<T>();

            var original = new EnumWrapperAsInt64<T> { EnumValue = enumValue };
            var bson = original.ToBson();
            var deserialized = BsonSerializer.Deserialize<EnumWrapperAsInt64<T>>(bson);

            deserialized.EnumValue.Should().Be(original.EnumValue);
        }

        [Theory]
        [InlineData(EnumInt64.Max)]
        [InlineData(EnumUInt64.Max)]
        [InlineData((EnumInt64)uint.MaxValue)]
        [InlineData((EnumUInt64)uint.MaxValue)]
        public void EnumSerializationAs32_should_overflow<T>(T enumValue) where T : struct, Enum
        {
            EnsureSerializer<T>();

            var obj = new EnumWrapperAsInt32<T> { EnumValue = enumValue };
            Record.Exception(() => obj.ToBson()).Should().BeOfType<OverflowException>();
        }

        [Theory]
        [InlineData("{ EnumValue : -1 }", EnumByte.Zero)]
        [InlineData("{ EnumValue : 256 }", EnumByte.Zero)]
        [InlineData("{ EnumValue : -129 }", EnumSByte.Zero)]
        [InlineData("{ EnumValue : 128 }", EnumSByte.Zero)]
        [InlineData("{ EnumValue : 32768 }", EnumInt16.Zero)]
        [InlineData("{ EnumValue : -32769.1 }", EnumInt16.Zero)]
        [InlineData("{ EnumValue : -2147483649 }", EnumInt32.Zero)]
        [InlineData("{ EnumValue : 2147483648 }", EnumInt32.Zero)]
        [InlineData("{ EnumValue : NumberLong(-1) }", EnumUInt32.Zero)]
        [InlineData("{ EnumValue :  4294967296 }", EnumUInt32.Zero)]
        public void EnumDeserialization_should_overflow<T>(string json, T enumValue) where T : struct, Enum
        {
            EnsureSerializer<T>();

            var exception = Record.Exception(() => BsonSerializer.Deserialize<EnumWrapper<T>>(json));

            exception.Should().BeOfType<FormatException>();
            exception.InnerException.Should().BeOfType<OverflowException>();
        }

        // private methods
        private void EnsureSerializer<T>() where T : struct, Enum
        {
            var serializer = BsonSerializer.LookupSerializer<T>();
            serializer.Should().BeOfType<EnumSerializer<T>>();
        }
    }
}
