﻿/* Copyright 2015-present MongoDB Inc.
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
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonInt32Tests
    {
        [Theory]
        [InlineData(0, 1.0, -1)]
        [InlineData(0, 0.0, 0)]
        [InlineData(1, 0.0, 1)]
        public void CompareTo_BsonDecimal128_should_return_expected_result(int int32Value, double otherDoubleValue, int expectedResult)
        {
            var subject = new BsonInt32(int32Value);
            var other = new BsonDecimal128((Decimal128)(decimal)otherDoubleValue);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, 1.0, -1)]
        [InlineData(0, 0.0, 0)]
        [InlineData(1, 0.0, 1)]
        public void CompareTo_BsonDouble_should_return_expected_result(int int32Value, double otherDoubleValue, int expectedResult)
        {
            var subject = new BsonInt32(int32Value);
            var other = new BsonDouble(otherDoubleValue);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, 1, -1)]
        [InlineData(0, 0, 0)]
        [InlineData(1, 0, 1)]
        public void CompareTo_BsonInt32_should_return_expected_result(int int32Value, int otherInt32Value, int expectedResult)
        {
            var subject = new BsonInt32(int32Value);
            var other = new BsonInt32(otherInt32Value);

            var result1 = subject.CompareTo((BsonInt32)other);
            var result2 = subject.CompareTo((BsonValue)other);

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0, 1L, -1)]
        [InlineData(0, 0L, 0)]
        [InlineData(1, 0L, 1)]
        public void CompareTo_BsonInt64_should_return_expected_result(int int32Value, long otherInt64Value, int expectedResult)
        {
            var subject = new BsonInt32(int32Value);
            var other = new BsonInt64(otherInt64Value);

            var result = subject.CompareTo(other);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void CompareTo_null_should_return_expected_result()
        {
            var subject = new BsonInt32(0);

            var result1 = subject.CompareTo((BsonInt32)null);
            var result2 = subject.CompareTo((BsonValue)null);

            result1.Should().Be(1);
            result2.Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_int_should_return_new_instance(
            [Values(-101, 101)]
            int value)
        {
            var result1 = (BsonInt32)value;
            var result2 = (BsonInt32)value;

            result2.Should().NotBeSameAs(result1);
        }

        [Theory]
        [ParameterAttributeData]
        public void implicit_conversion_from_int_should_return_precreated_instance(
            [Range(-100, 100)]
            int value)
        {
            var result1 = (BsonInt32)value;
            var result2 = (BsonInt32)value;

            result2.Should().BeSameAs(result1);
        }

        [Theory]
        [InlineData(1, 1.0, true)]
        [InlineData(1, 2.0, false)]
        [InlineData(2, 1.0, false)]
        public void operator_equals_with_BsonDecimal128_should_return_expected_result(int lhsInt32Value, double rhsDoubleValue, bool expectedResult)
        {
            var lhs = new BsonInt32(lhsInt32Value);
            var rhs = new BsonDecimal128((Decimal128)(decimal)rhsDoubleValue);

            var result = lhs == rhs;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1, 1.0, true)]
        [InlineData(1, 2.0, false)]
        [InlineData(2, 1.0, false)]
        public void operator_equals_with_BsonDouble_should_return_expected_result(int lhsInt32Value, double rhsDoubleValue, bool expectedResult)
        {
            var lhs = new BsonInt32(lhsInt32Value);
            var rhs = new BsonDouble(rhsDoubleValue);

            var result = lhs == rhs;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(1, 2, false)]
        [InlineData(2, 1, false)]
        public void operator_equals_with_BsonInt32_should_return_expected_result(int lhsInt32Value, int rhsInt32Value, bool expectedResult)
        {
            var lhs = new BsonInt32(lhsInt32Value);
            var rhs = new BsonInt32(rhsInt32Value);

            var result = lhs == rhs;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1, 1L, true)]
        [InlineData(1, 2L, false)]
        [InlineData(2, 1L, false)]
        public void operator_equals_with_BsonInt64_should_return_expected_result(int lhsInt32Value, long rhsInt64Value, bool expectedResult)
        {
            var lhs = new BsonInt32(lhsInt32Value);
            var rhs = new BsonInt64(rhsInt64Value);

            var result = lhs == rhs;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void operator_equals_with_nulls_should_return_expected_result(bool isLhsNull, bool isRhsNull)
        {
            var lhs = isLhsNull ? null : new BsonInt32(1);
            var rhs = isRhsNull ? null : new BsonInt32(1);

            var result = lhs == rhs;

            result.Should().Be(isLhsNull == isRhsNull);
        }

        [Theory]
        [ParameterAttributeData]
        public void precreated_instances_should_have_the_expected_value(
            [Range(-100, 100)]
            int value)
        {
            var result = (BsonInt32)value;

            result.Value.Should().Be(value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void ToDecimal_should_return_expected_result(int int32Value)
        {
            var subject = new BsonInt32(int32Value);

            var result = subject.ToDecimal();

            result.Should().Be((decimal)int32Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void ToDecimal128_should_return_expected_result(int int32Value)
        {
            var subject = new BsonInt32(int32Value);

            var result = subject.ToDecimal128();

            result.Should().Be((Decimal128)int32Value);
        }
    }
}
