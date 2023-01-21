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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class EnumSerializerTests
    {
        public enum CaseSensitiveEnum
        {
            AnEnumValue,
            anenumvalue
        }

        public enum EnumByte : byte
        {
            Min = byte.MinValue,
            One = 1,
            Max = byte.MaxValue
        }

        public enum EnumInt16 : short
        {
            Min = short.MinValue,
            MinusOne = -1,
            Zero = 0,
            One = 1,
            Max = short.MaxValue
        }

        public enum EnumInt32 : int
        {
            Min = int.MinValue,
            MinusOne = -1,
            Zero = 0,
            One = 1,
            Max = int.MaxValue
        }

        public enum EnumInt64 : long
        {
            Min = long.MinValue,
            MinInt32MinusOne = (long)int.MinValue - 1,
            MinInt32 = (long)int.MinValue,
            MinusOne = -1,
            Zero = 0,
            One = 1,
            MaxInt32 = int.MaxValue,
            MaxInt32PlusOne = (long)int.MaxValue + 1,
            Max = long.MaxValue
        }

        public enum EnumSByte : sbyte
        {
            Min = sbyte.MinValue,
            MinusOne = -1,
            Zero = 0,
            One = 1,
            Max = sbyte.MaxValue
        }

        public enum EnumUInt16 : ushort
        {
            Min = ushort.MinValue,
            One = 1,
            Max = ushort.MaxValue
        }

        public enum EnumUInt32 : uint
        {
            Min = uint.MinValue,
            One = 1,
            Max = uint.MaxValue
        }

        public enum EnumUInt64 : ulong
        {
            Min = ulong.MinValue,
            One = 1,
            UMaxInt32 = uint.MaxValue,
            UMaxInt32PlusOne = (long)uint.MaxValue + 1,
            Max = ulong.MaxValue,
        }

        public class EnumPrototypes : IValueGenerator
        {
            public object[] GenerateValues()
            {
                return new object[] { EnumByte.Min, EnumInt16.Min, EnumInt32.Min, EnumInt64.Min, EnumSByte.Min, EnumUInt16.Min, EnumUInt32.Min, EnumUInt64.Min };
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_no_arguments_should_return_expected_result<TEnum>(
            [ClassValues(typeof(EnumPrototypes))] TEnum _)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>();

            var expectedRepresentation = GetExpectedRepresentation<TEnum>(0);
            subject.Representation.Should().Be((BsonType)expectedRepresentation);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_representation_should_return_expected_result<TEnum>(
            [ClassValues(typeof(EnumPrototypes))]
            TEnum _,
            [Values((BsonType)0, BsonType.Int32, BsonType.Int64, BsonType.String)]
            BsonType representation)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>(representation);

            var expectedRepresentation = GetExpectedRepresentation<TEnum>(representation);
            subject.Representation.Should().Be(expectedRepresentation);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_representation_should_throw_when_representation_is_invalid<TEnum>(
            [ClassValues(typeof(EnumPrototypes))]
            TEnum _,
            [Values(-1, 1, 3, 9, 11, 13)] // these are the values adjacent to the valid values
            BsonType representation)
            where TEnum : struct, Enum
        {
            var exception = Record.Exception(() => new EnumSerializer<TEnum>(representation));

            var e = exception.Should().BeOfType<ArgumentException>().Subject;
            e.ParamName.Should().Be("representation");
        }

        [Theory]
        [ParameterAttributeData]
        public void Deserialize_should_throw_when_bson_type_is_invalid<TEnum>(
            [ClassValues(typeof(EnumPrototypes))]
            TEnum _,
            [Values("{ x : false }")]
            string json)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>();
            var bson = ToBson(json);

            var exception = Record.Exception(() => Deserialize(subject, bson));

            exception.Should().BeOfType<FormatException>();
        }

        [Theory]
        // EnumByte test cases
        [InlineData(EnumByte.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumByte.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumByte.Min, "{ x : { $numberDouble : '-1.0' } }")] // (double)EnumByte.Min - 1
        [InlineData(EnumByte.Min, "{ x : { $numberDouble : '256.0' } }")] // (double)EnumByte.Max + 1
        [InlineData(EnumByte.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumByte.Min, "{ x : { $numberInt : '-2147483648' } }")] // int.MinValue
        [InlineData(EnumByte.Min, "{ x : { $numberInt : '-1' } }")] // (int)EnumByte.Min - 1
        [InlineData(EnumByte.Min, "{ x : { $numberInt : '256' } }")] // (int)EnumByte.Max + 1
        [InlineData(EnumByte.Min, "{ x : { $numberInt : '2147483647' } }")] // int.MaxValue
        [InlineData(EnumByte.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumByte.Min, "{ x : { $numberLong : '-1' } }")] // (long)EnumByte.Min - 1
        [InlineData(EnumByte.Min, "{ x : { $numberLong : '256' } }")] // (long)EnumByte.Min + 1
        [InlineData(EnumByte.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumInt16 test cases
        [InlineData(EnumInt16.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumInt16.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumInt16.Min, "{ x : { $numberDouble : '-32769.0' } }")] // (double)EnumInt16.Min - 1
        [InlineData(EnumInt16.Min, "{ x : { $numberDouble : '32768.0' } }")] // (double)EnumInt16.Max + 1
        [InlineData(EnumInt16.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumInt16.Min, "{ x : { $numberInt : '-2147483648' } }")] // int.MinValue
        [InlineData(EnumInt16.Min, "{ x : { $numberInt : '-32769' } }")] // (int)EnumInt16.Min - 1
        [InlineData(EnumInt16.Min, "{ x : { $numberInt : '32768' } }")] // (int)EnumInt16.Max + 1
        [InlineData(EnumInt16.Min, "{ x : { $numberInt : '2147483647' } }")] // int.MaxValue
        [InlineData(EnumInt16.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumInt16.Min, "{ x : { $numberLong : '-32769' } }")] // (long)EnumInt16.Min - 1
        [InlineData(EnumInt16.Min, "{ x : { $numberLong : '32768' } }")] // (long)EnumInt16.Min + 1
        [InlineData(EnumInt16.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumInt32 test cases
        [InlineData(EnumInt32.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumInt32.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumInt32.Min, "{ x : { $numberDouble : '-2147483649.0' } }")] // (double)EnumInt32.Min - 1
        [InlineData(EnumInt32.Min, "{ x : { $numberDouble : '2147483648.0' } }")] // (double)EnumInt32.Max + 1
        [InlineData(EnumInt32.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumInt32.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumInt32.Min, "{ x : { $numberLong : '-2147483649' } }")] // (long)EnumInt32.Min - 1
        [InlineData(EnumInt32.Min, "{ x : { $numberLong : '2147483648' } }")] // (long)EnumInt32.Min + 1
        [InlineData(EnumInt32.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumInt64 test cases
        [InlineData(EnumInt64.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumInt64.Min, "{ x : { $numberDouble : '-9223372036854776840.0' } }")] // next smallest integral double < long.MinValue
        [InlineData(EnumInt64.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)EnumInt64.Max + 1
        // EnumSByte test cases
        [InlineData(EnumSByte.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumSByte.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumSByte.Min, "{ x : { $numberDouble : '-129.0' } }")] // (double)EnumByte.Min - 1
        [InlineData(EnumSByte.Min, "{ x : { $numberDouble : '128.0' } }")] // (double)EnumByte.Max + 1
        [InlineData(EnumSByte.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumSByte.Min, "{ x : { $numberInt : '-2147483648' } }")] // int.MinValue
        [InlineData(EnumSByte.Min, "{ x : { $numberInt : '-129' } }")] // (int)EnumByte.Min - 1
        [InlineData(EnumSByte.Min, "{ x : { $numberInt : '128' } }")] // (int)EnumByte.Max + 1
        [InlineData(EnumSByte.Min, "{ x : { $numberInt : '2147483647' } }")] // int.MaxValue
        [InlineData(EnumSByte.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumSByte.Min, "{ x : { $numberLong : '-129' } }")] // (long)EnumByte.Min - 1
        [InlineData(EnumSByte.Min, "{ x : { $numberLong : '128' } }")] // (long)EnumByte.Min + 1
        [InlineData(EnumSByte.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumUInt16 test cases
        [InlineData(EnumUInt16.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumUInt16.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberDouble : '-1.0' } }")] // (double)EnumInt16.Min - 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberDouble : '65536.0' } }")] // (double)EnumInt16.Max + 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberInt : '-2147483648' } }")] // int.MinValue
        [InlineData(EnumUInt16.Min, "{ x : { $numberInt : '-1' } }")] // (int)EnumInt16.Min - 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberInt : '65536' } }")] // (int)EnumInt16.Max + 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberInt : '2147483647' } }")] // int.MaxValue
        [InlineData(EnumUInt16.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumUInt16.Min, "{ x : { $numberLong : '-1' } }")] // (long)EnumInt16.Min - 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberLong : '65536' } }")] // (long)EnumInt16.Min + 1
        [InlineData(EnumUInt16.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumUInt32 test cases
        [InlineData(EnumUInt32.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumUInt32.Min, "{ x : { $numberDouble : '-9223372036854775809.0' } }")] // (double)long.MinValue - 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberDouble : '-1.0' } }")] // (double)EnumInt32.Min - 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberDouble : '4294967296.0' } }")] // (double)EnumInt32.Max + 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.MaxValue + 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberLong : '-9223372036854775808' } }")] // long.MinValue
        [InlineData(EnumUInt32.Min, "{ x : { $numberLong : '-1' } }")] // (long)EnumInt32.Min - 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberLong : '4294967296' } }")] // (long)EnumInt32.Min + 1
        [InlineData(EnumUInt32.Min, "{ x : { $numberLong : '9223372036854775807' } }")] // long.MaxValue
        // EnumUInt64 test cases
        [InlineData(EnumUInt64.Min, "{ x : { $numberDouble : '0.5' } }")] // fractional
        [InlineData(EnumUInt64.Min, "{ x : { $numberDouble : '-9223372036854776840.0' } }")] // next smallest integral double < long.MinValue
        [InlineData(EnumUInt64.Min, "{ x : { $numberDouble : '9223372036854775808.0' } }")] // (double)long.Max + 1
        public void Deserialize_should_throw_on_overflow<TEnum>(TEnum _, string json)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>();

            var exception = Record.Exception(() => Deserialize(subject, ToBson(json)));

            exception.Should().BeOfType<OverflowException>();
        }

        [Theory]
        [InlineData("{ x : 'AnEnumValue' }", CaseSensitiveEnum.AnEnumValue)]
        [InlineData("{ x : 'anenumvalue' }", CaseSensitiveEnum.anenumvalue)]
        [InlineData("{ x : 'ANENUMVALUE' }", CaseSensitiveEnum.AnEnumValue)]
        public void Deserialize_string_should_be_caseinsensitive(string json, CaseSensitiveEnum result)
        {
            var subject = new EnumSerializer<CaseSensitiveEnum>(BsonType.String);

            var deserialized = Deserialize(subject, ToBson(json));

            deserialized.Should().Be(result);
        }

        [Fact]
        public void Deserialize_string_should_throw_when_enum_field_is_not_found()
        {
            var subject = new EnumSerializer<CaseSensitiveEnum>(BsonType.String);

            var json = "{ x : 'NotEnumField' }";
            var exception = Record.Exception(() => Deserialize(subject, ToBson(json)));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Theory]
        // EnumByte test cases
        [InlineData(EnumByte.Min, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumByte.Min, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumByte.Min, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumByte.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumByte.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumByte.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumByte.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumByte.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumByte.Max, (BsonType).0, "{ x : { $numberInt : '255' } }")]
        [InlineData(EnumByte.Max, BsonType.Int32, "{ x : { $numberInt : '255' } }")]
        [InlineData(EnumByte.Max, BsonType.Int64, "{ x : { $numberLong : '255' } }")]
        [InlineData(EnumByte.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumInt16 test cases
        [InlineData(EnumInt16.Min, (BsonType).0, "{ x : { $numberInt : '-32768' } }")]
        [InlineData(EnumInt16.Min, BsonType.Int32, "{ x : { $numberInt : '-32768' } }")]
        [InlineData(EnumInt16.Min, BsonType.Int64, "{ x : { $numberLong : '-32768' } }")]
        [InlineData(EnumInt16.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumInt16.MinusOne, (BsonType).0, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumInt16.MinusOne, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumInt16.MinusOne, BsonType.Int64, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumInt16.MinusOne, BsonType.String, "{ x : 'MinusOne' }")]
        [InlineData(EnumInt16.Zero, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumInt16.Zero, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumInt16.Zero, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumInt16.Zero, BsonType.String, "{ x : 'Zero' }")]
        [InlineData(EnumInt16.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumInt16.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumInt16.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumInt16.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumInt16.Max, (BsonType).0, "{ x : { $numberInt : '32767' } }")]
        [InlineData(EnumInt16.Max, BsonType.Int32, "{ x : { $numberInt : '32767' } }")]
        [InlineData(EnumInt16.Max, BsonType.Int64, "{ x : { $numberLong : '32767' } }")]
        [InlineData(EnumInt16.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumInt32 test cases
        [InlineData(EnumInt32.Min, (BsonType).0, "{ x : { $numberInt : '-2147483648' } }")]
        [InlineData(EnumInt32.Min, BsonType.Int32, "{ x : { $numberInt : '-2147483648' } }")]
        [InlineData(EnumInt32.Min, BsonType.Int64, "{ x : { $numberLong : '-2147483648' } }")]
        [InlineData(EnumInt32.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumInt32.MinusOne, (BsonType).0, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumInt32.MinusOne, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumInt32.MinusOne, BsonType.Int64, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumInt32.MinusOne, BsonType.String, "{ x : 'MinusOne' }")]
        [InlineData(EnumInt32.Zero, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumInt32.Zero, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumInt32.Zero, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumInt32.Zero, BsonType.String, "{ x : 'Zero' }")]
        [InlineData(EnumInt32.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumInt32.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumInt32.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumInt32.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumInt32.Max, (BsonType).0, "{ x : { $numberInt : '2147483647' } }")]
        [InlineData(EnumInt32.Max, BsonType.Int32, "{ x : { $numberInt : '2147483647' } }")]
        [InlineData(EnumInt32.Max, BsonType.Int64, "{ x : { $numberLong : '2147483647' } }")]
        [InlineData(EnumInt32.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumInt64 test cases
        [InlineData(EnumInt64.Min, (BsonType).0, "{ x : { $numberLong : '-9223372036854775808' } }")]
        [InlineData(EnumInt64.Min, BsonType.Int32, "overflow")]
        [InlineData(EnumInt64.Min, BsonType.Int64, "{ x : { $numberLong : '-9223372036854775808' } }")]
        [InlineData(EnumInt64.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumInt64.MinInt32MinusOne, (BsonType).0, "{ x : { $numberLong : '-2147483649' } }")]
        [InlineData(EnumInt64.MinInt32MinusOne, BsonType.Int32, "overflow")]
        [InlineData(EnumInt64.MinInt32MinusOne, BsonType.Int64, "{ x : { $numberLong : '-2147483649' } }")]
        [InlineData(EnumInt64.MinInt32MinusOne, BsonType.String, "{ x : 'MinInt32MinusOne' }")]
        [InlineData(EnumInt64.MinInt32, (BsonType).0, "{ x : { $numberLong : '-2147483648' } }")]
        [InlineData(EnumInt64.MinInt32, BsonType.Int32, "{ x : { $numberInt : '-2147483648' } }")]
        [InlineData(EnumInt64.MinInt32, BsonType.Int64, "{ x : { $numberLong : '-2147483648' } }")]
        [InlineData(EnumInt64.MinInt32, BsonType.String, "{ x : 'MinInt32' }")]
        [InlineData(EnumInt64.MinusOne, (BsonType).0, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumInt64.MinusOne, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumInt64.MinusOne, BsonType.Int64, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumInt64.MinusOne, BsonType.String, "{ x : 'MinusOne' }")]
        [InlineData(EnumInt64.Zero, (BsonType).0, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumInt64.Zero, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumInt64.Zero, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumInt64.Zero, BsonType.String, "{ x : 'Zero' }")]
        [InlineData(EnumInt64.One, (BsonType).0, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumInt64.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumInt64.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumInt64.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumInt64.MaxInt32, (BsonType).0, "{ x : { $numberLong : '2147483647' } }")]
        [InlineData(EnumInt64.MaxInt32, BsonType.Int32, "{ x : { $numberInt : '2147483647' } }")]
        [InlineData(EnumInt64.MaxInt32, BsonType.Int64, "{ x : { $numberLong : '2147483647' } }")]
        [InlineData(EnumInt64.MaxInt32, BsonType.String, "{ x : 'MaxInt32' }")]
        [InlineData(EnumInt64.MaxInt32PlusOne, (BsonType).0, "{ x : { $numberLong : '2147483648' } }")]
        [InlineData(EnumInt64.MaxInt32PlusOne, BsonType.Int32, "overflow")]
        [InlineData(EnumInt64.MaxInt32PlusOne, BsonType.Int64, "{ x : { $numberLong : '2147483648' } }")]
        [InlineData(EnumInt64.MaxInt32PlusOne, BsonType.String, "{ x : 'MaxInt32PlusOne' }")]
        [InlineData(EnumInt64.Max, (BsonType).0, "{ x : { $numberLong : '9223372036854775807' } }")]
        [InlineData(EnumInt64.Max, BsonType.Int32, "overflow")]
        [InlineData(EnumInt64.Max, BsonType.Int64, "{ x : { $numberLong : '9223372036854775807' } }")]
        [InlineData(EnumInt64.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumSByte test cases
        [InlineData(EnumSByte.Min, (BsonType).0, "{ x : { $numberInt : '-128' } }")]
        [InlineData(EnumSByte.Min, BsonType.Int32, "{ x : { $numberInt : '-128' } }")]
        [InlineData(EnumSByte.Min, BsonType.Int64, "{ x : { $numberLong : '-128' } }")]
        [InlineData(EnumSByte.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumSByte.MinusOne, (BsonType).0, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumSByte.MinusOne, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumSByte.MinusOne, BsonType.Int64, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumSByte.MinusOne, BsonType.String, "{ x : 'MinusOne' }")]
        [InlineData(EnumSByte.Zero, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumSByte.Zero, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumSByte.Zero, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumSByte.Zero, BsonType.String, "{ x : 'Zero' }")]
        [InlineData(EnumSByte.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumSByte.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumSByte.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumSByte.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumSByte.Max, (BsonType).0, "{ x : { $numberInt : '127' } }")]
        [InlineData(EnumSByte.Max, BsonType.Int32, "{ x : { $numberInt : '127' } }")]
        [InlineData(EnumSByte.Max, BsonType.Int64, "{ x : { $numberLong : '127' } }")]
        [InlineData(EnumSByte.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumUInt16 test cases
        [InlineData(EnumUInt16.Min, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumUInt16.Min, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumUInt16.Min, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumUInt16.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumUInt16.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumUInt16.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumUInt16.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumUInt16.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumUInt16.Max, (BsonType).0, "{ x : { $numberInt : '65535' } }")]
        [InlineData(EnumUInt16.Max, BsonType.Int32, "{ x : { $numberInt : '65535' } }")]
        [InlineData(EnumUInt16.Max, BsonType.Int64, "{ x : { $numberLong : '65535' } }")]
        [InlineData(EnumUInt16.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumUInt32 test cases
        [InlineData(EnumUInt32.Min, (BsonType).0, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumUInt32.Min, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumUInt32.Min, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumUInt32.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumUInt32.One, (BsonType).0, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumUInt32.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumUInt32.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumUInt32.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumUInt32.Max, (BsonType).0, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumUInt32.Max, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumUInt32.Max, BsonType.Int64, "{ x : { $numberLong : '4294967295' } }")]
        [InlineData(EnumUInt32.Max, BsonType.String, "{ x : 'Max' }")]
        // EnumUInt64 test cases
        [InlineData(EnumUInt64.Min, (BsonType).0, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumUInt64.Min, BsonType.Int32, "{ x : { $numberInt : '0' } }")]
        [InlineData(EnumUInt64.Min, BsonType.Int64, "{ x : { $numberLong : '0' } }")]
        [InlineData(EnumUInt64.Min, BsonType.String, "{ x : 'Min' }")]
        [InlineData(EnumUInt64.One, (BsonType).0, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumUInt64.One, BsonType.Int32, "{ x : { $numberInt : '1' } }")]
        [InlineData(EnumUInt64.One, BsonType.Int64, "{ x : { $numberLong : '1' } }")]
        [InlineData(EnumUInt64.One, BsonType.String, "{ x : 'One' }")]
        [InlineData(EnumUInt64.UMaxInt32, (BsonType).0, "{ x : { $numberLong : '4294967295' } }")]
        [InlineData(EnumUInt64.UMaxInt32, BsonType.Int32, "{ x : { $numberInt : '-1' } }")]
        [InlineData(EnumUInt64.UMaxInt32, BsonType.Int64, "{ x : { $numberLong : '4294967295' } }")]
        [InlineData(EnumUInt64.UMaxInt32, BsonType.String, "{ x : 'UMaxInt32' }")]
        [InlineData(EnumUInt64.UMaxInt32PlusOne, (BsonType).0, "{ x : { $numberLong : '4294967296' } }")]
        [InlineData(EnumUInt64.UMaxInt32PlusOne, BsonType.Int32, "overflow")]
        [InlineData(EnumUInt64.UMaxInt32PlusOne, BsonType.Int64, "{ x : { $numberLong : '4294967296' } }")]
        [InlineData(EnumUInt64.UMaxInt32PlusOne, BsonType.String, "{ x : 'UMaxInt32PlusOne' }")]
        [InlineData(EnumUInt64.Max, (BsonType).0, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumUInt64.Max, BsonType.Int32, "overflow")]
        [InlineData(EnumUInt64.Max, BsonType.Int64, "{ x : { $numberLong : '-1' } }")]
        [InlineData(EnumUInt64.Max, BsonType.String, "{ x : 'Max' }")]
        public void Serialize_and_Deserialize_should_return_the_expected_result<TEnum>(TEnum value, BsonType representation, string expectedJson)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>(representation);

            if (expectedJson == "overflow")
            {
                var exception = Record.Exception(() => Serialize(subject, value));

                exception.Should().BeOfType<OverflowException>();
            }
            else
            {
                var bson = Serialize(subject, value);
                var deserialized = Deserialize(subject, bson);

                if (!bson.SequenceEqual(ToBson(expectedJson)))
                {
                    var message = $"Expected: {expectedJson} but found: {ToJson(bson)}.";
                    throw new AssertionException(message);
                }
                deserialized.Should().Be(value);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void WithRepresentation_should_return_expected_result<TEnum>(
            [ClassValues(typeof(EnumPrototypes))]
            TEnum _,
            [Values((BsonType)0, BsonType.Int32, BsonType.Int64, BsonType.String)]
            BsonType originalRepresentation,
            [Values((BsonType)0, BsonType.Int32, BsonType.Int64, BsonType.String)]
            BsonType newRepresentation)
            where TEnum : struct, Enum
        {
            var subject = new EnumSerializer<TEnum>(originalRepresentation);

            var result = subject.WithRepresentation(newRepresentation);

            var effectiveOriginalRepresentation = GetExpectedRepresentation<TEnum>(originalRepresentation);
            var expectedRepresentation = GetExpectedRepresentation<TEnum>(newRepresentation);

            if (expectedRepresentation == effectiveOriginalRepresentation)
            {
                result.Should().BeSameAs(subject);
            }
            else
            {
                result.Should().NotBeSameAs(subject);
                result.Representation.Should().Be(expectedRepresentation);
            }
        }

        // private methods
        private TEnum Deserialize<TEnum>(IBsonSerializer<TEnum> serializer, byte[] bson)
        {
            using (var stream = new MemoryStream(bson))
            using (var reader = new BsonBinaryReader(stream))
            {
                var context = BsonDeserializationContext.CreateRoot(reader);
                reader.ReadStartDocument();
                reader.ReadName("x");
                var value = serializer.Deserialize(context);
                reader.ReadEndDocument();
                return value;
            }
        }

        private BsonType GetExpectedRepresentation<TEnum>(BsonType representation)
        {
            if (representation == 0)
            {
                var underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
                return (underlyingType == typeof(long) || underlyingType == typeof(ulong)) ? BsonType.Int64 : BsonType.Int32;
            }
            else
            {
                return representation;
            }
        }

        private byte[] Serialize<TEnum>(IBsonSerializer<TEnum> serializer, TEnum value)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                var context = BsonSerializationContext.CreateRoot(writer);
                writer.WriteStartDocument();
                writer.WriteName("x");
                serializer.Serialize(context, value);
                writer.WriteEndDocument();
                return stream.ToArray();
            }
        }

        private byte[] ToBson(string json)
        {
            return BsonSerializer.Deserialize<BsonDocument>(json).ToBson();
        }

        private string ToJson(byte[] bson)
        {
            var document = BsonSerializer.Deserialize<BsonDocument>(bson);
            var writerSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson };
            return document.ToJson(writerSettings);
        }
    }
}
