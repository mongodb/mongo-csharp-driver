/* Copyright 2016 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using System.Globalization;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonDecimal128Tests
    {
        [Fact]
        public void BsonType_should_return_expected_result()
        {
            var subject = new BsonDecimal128(Decimal128.Zero);

            var result = subject.BsonType;

            result.Should().Be(BsonType.Decimal128);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0)]
        [InlineData(-1.0, 0.0, -1)]
        [InlineData(1.0, 0.0, 1)]
        [InlineData(0.0, null, 1)]
        public void CompareTo_with_BsonDecimal128_should_return_expected_result(double doubleValue, double? otherDoubleValue, int expectedResult)
        {
            var subject = CreateSubject(doubleValue);
            var other = CreateSubject(otherDoubleValue);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CompareTo_with_BsonValue_of_MaxKey_should_return_expected_result()
        {
            var subject = new BsonDecimal128(Decimal128.Zero);
            var other = (BsonValue)BsonMaxKey.Value;

            var result = subject.CompareTo(other);

            result.Should().Be(-1);
        }

        [Fact]
        public void CompareTo_with_BsonValue_of_MinKey_should_return_expected_result()
        {
            var subject = new BsonDecimal128(Decimal128.Zero);
            var other = (BsonValue)BsonMinKey.Value;

            var result = subject.CompareTo(other);

            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_with_BsonValue_of_null_should_return_expected_result()
        {
            var subject = new BsonDecimal128(Decimal128.Zero);

            var result = subject.CompareTo((BsonValue)null);

            result.Should().Be(1);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0)]
        [InlineData(-1.0, 0.0, -1)]
        [InlineData(1.0, 0.0, 1)]
        public void CompareTo_with_BsonValue_of_type_Decimal128_should_return_expected_result(double doubleValue, double otherDoubleValue, int expectedResult)
        {
            var subject = CreateSubject(doubleValue);
            var other = (BsonValue)CreateSubject(otherDoubleValue);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0, 0.0, 0)]
        [InlineData(-1.0, 0.0, -1)]
        [InlineData(1.0, 0.0, 1)]
        public void CompareTo_with_BsonValue_of_type_double_should_return_expected_result(double doubleValue, double otherDoubleValue, int expectedResult)
        {
            var subject = CreateSubject(doubleValue);
            var other = (BsonValue)new BsonDouble(otherDoubleValue);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0, 0, 0)]
        [InlineData(-1.0, 0, -1)]
        [InlineData(1.0, 0, 1)]
        public void CompareTo_with_BsonValue_of_type_Int32_should_return_expected_result(double doubleValue, int otherInt32Value, int expectedResult)
        {
            var subject = new BsonDecimal128((Decimal128)(decimal)doubleValue);
            var other = (BsonValue)new BsonInt32(otherInt32Value);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0, 0L, 0)]
        [InlineData(-1.0, 0L, -1)]
        [InlineData(1.0, 0L, 1)]
        public void CompareTo_with_BsonValue_of_type_Int32_should_return_expected_result(double doubleValue, long otherInt64Value, int expectedResult)
        {
            var subject = new BsonDecimal128((Decimal128)(decimal)doubleValue);
            var other = (BsonValue)new BsonInt64(otherInt64Value);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void constructor_should_initialize_instance(double doubleValue)
        {
            var value = (Decimal128)(decimal)doubleValue;
            var subject = new BsonDecimal128(value);

            subject.BsonType.Should().Be(BsonType.Decimal128);
            subject.Value.Should().Be(value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(0L)]
        public void Create_should_return_expected_result(object value)
        {
            var result = BsonDecimal128.Create(value);

            result.Value.Should().Be(Decimal128.Zero);
        }

        [Theory]
        [InlineData(0.0, 0.0, true)]
        [InlineData(0.0, 1.0, false)]
        [InlineData(1.0, 0.0, false)]
        [InlineData(0.0, double.NaN, false)]
        [InlineData(double.NaN, 0.0, false)]
        [InlineData(double.NaN, double.NaN, true)]
        public void Equals_should_return_expected_result(double doubleValue, double? otherDoubleValue, bool expectedResult)
        {
            var subject = CreateSubject(doubleValue);
            var other = CreateSubject(otherDoubleValue);

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);
            var hashCode = subject.GetHashCode();
            var otherHashCode = other.GetHashCode();

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
            (hashCode == otherHashCode).Should().Be(expectedResult);
        }

        [Fact]
        public void IConvertible_GetTypeCode_should_return_expected_result()
        {
            var subject = (IConvertible)new BsonDecimal128(Decimal128.Zero);

            var result = subject.GetTypeCode();

            result.Should().Be(TypeCode.Object);
        }

        [Theory]
        [InlineData(0.0, false)]
        [InlineData(1.0, true)]
        [InlineData(double.NaN, false)]
        public void IConvertible_ToBoolean_should_return_expected_result(double doubleValue, bool expectedResult)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToBoolean(null);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToByte_should_return_expceted_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToByte(null);

            result.Should().Be((byte)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToDecimal_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToDecimal(null);

            result.Should().Be((decimal)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToDouble_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToDouble(null);

            result.Should().Be(doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToInt16_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToInt16(null);

            result.Should().Be((short)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToInt32_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToInt32(null);

            result.Should().Be((int)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToInt64_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToInt64(null);

            result.Should().Be((long)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToSByte_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToSByte(null);

            result.Should().Be((sbyte)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToSingle_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToSingle(null);

            result.Should().Be((float)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToString_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToString(null);

            result.Should().Be(doubleValue.ToString());
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToUInt16_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToUInt16(null);

            result.Should().Be((ushort)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToUInt32_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToUInt32(null);

            result.Should().Be((uint)doubleValue);
        }
        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void IConvertible_ToUInt64_should_return_expected_result(double doubleValue)
        {
            var subject = (IConvertible)CreateSubject(doubleValue);

            var result = subject.ToUInt64(null);

            result.Should().Be((ulong)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void implicit_conversion_from_Decimal128_should_return_expected_result(double doubleValue)
        {
            var decimal128Value = (Decimal128)(decimal)doubleValue;

            BsonDecimal128 subject = decimal128Value;

            subject.Value.Should().Be(decimal128Value);
        }

        [Theory]
        [InlineData(0.0, 0.0, true)]
        [InlineData(0.0, 1.0, false)]
        [InlineData(1.0, 0.0, false)]
        [InlineData(1.0, 1.0, true)]
        [InlineData(0.0, null, false)]
        [InlineData(null, 0.0, false)]
        [InlineData(null, null, true)]
        public void operator_equals_should_return_expected_result(double? lhsDoubleValue, double? rhsDoubleValue, bool expectedResult)
        {
            var lhs = CreateSubject(lhsDoubleValue);
            var rhs = CreateSubject(rhsDoubleValue);

            var result = lhs == rhs;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void RawValue_should_return_expected_result(double doubleValue)
        {
            var value = (Decimal128)(decimal)doubleValue;
            var subject = new BsonDecimal128(value);

#pragma warning disable 0618
            var result = subject.RawValue;
#pragma warning restore

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(0.0, false)]
        [InlineData(double.NaN, false)]
        [InlineData(1.0, true)]
        public void ToBoolean_should_return_expected_result(double doubleValue, bool expectedResult)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToBoolean();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToDecimal_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToDecimal();

            result.Should().Be((decimal)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToDecimal128_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToDecimal128();

            result.Should().Be(ToDecimal128(doubleValue));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToDouble_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToDouble();

            result.Should().Be(doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToInt32_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToInt32();

            result.Should().Be((int)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToInt64_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToInt64();

            result.Should().Be((long)doubleValue);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void ToString_should_return_expected_result(double doubleValue)
        {
            var subject = CreateSubject(doubleValue);

            var result = subject.ToString();

            result.Should().Be(doubleValue.ToString());
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void Value_should_return_expected_result(double doubleValue)
        {
            var value = (Decimal128)(decimal)doubleValue;
            var subject = new BsonDecimal128(value);

            var result = subject.Value;

            result.Should().Be(value);
        }

        // private methods
        private BsonDecimal128 CreateSubject(double doubleValue)
        {
            var decimal128Value = ToDecimal128(doubleValue); ;
            return new BsonDecimal128(decimal128Value);
        }

        private BsonDecimal128 CreateSubject(double? doubleValue)
        {
            return doubleValue == null ? null : CreateSubject(doubleValue.Value);
        }

        private Decimal128 ToDecimal128(double doubleValue)
        {
            if (double.IsNegativeInfinity(doubleValue))
            {
                return Decimal128.NegativeInfinity;
            }
            else if (double.IsPositiveInfinity(doubleValue))
            {
                return Decimal128.PositiveInfinity;
            }
            else if (double.IsNaN(doubleValue))
            {
                return Decimal128.QNaN;
            }
            else
            {
                return (Decimal128)(decimal)doubleValue;
            }
        }
    }
}