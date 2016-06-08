/* Copyright 2015 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonTimestampTests
    {
        [Fact]
        public void BsonType_get_should_return_expected_result()
        {
            var subject = new BsonTimestamp(0);

            var result = subject.BsonType;

            result.Should().Be(BsonType.Timestamp);
        }

        [Theory]
        [InlineData(0L, null, 1)]
        [InlineData(0L, 0L, 0)]
        [InlineData(0L, -1L, 1)]
        [InlineData(0L, 1L, -1)]
        [InlineData(-1L, 0L, -1)]
        [InlineData(1L, 0L, 1)]
        public void CompareTo_should_return_expected_result(long value1, long? value2, int expectedResult)
        {
            var subject = new BsonTimestamp(value1);
            var other = value2 == null ? null : new BsonTimestamp(value2.Value);

            var result1 = subject.CompareTo(other);
            var result2 = subject.CompareTo((BsonValue)other);

            result1.Should().Be(expectedResult);
            result2.Should().Be(expectedResult);
        }

        [Fact]
        public void CompareTo_should_return_minus_one_when_other_type_compares_higher()
        {
            var subject = new BsonTimestamp(0);

            var result = subject.CompareTo(BsonMaxKey.Value);

            result.Should().Be(-1);
        }

        [Fact]
        public void CompareTo_should_return_plus_one_when_other_type_compares_lower()
        {
            var subject = new BsonTimestamp(0);

            var result = subject.CompareTo(BsonMinKey.Value);

            result.Should().Be(1);
        }

        [Theory]
        [InlineData(0, 0, 0UL)]
        [InlineData(1, 2, 0x100000002UL)]
        [InlineData(-1, -2, 0xfffffffffffffffeUL)]
        [InlineData(int.MinValue, int.MinValue, 0x8000000080000000UL)]
        [InlineData(int.MaxValue, int.MaxValue, 0x7fffffff7fffffffUL)]
        [InlineData(int.MinValue, int.MaxValue, 0x800000007fffffffUL)]
        [InlineData(int.MaxValue, int.MinValue, 0x7fffffff80000000UL)]
        public void constructor_with_timestamp_increment_should_initialize_instance(int timestamp, int increment, ulong expectedValue)
        {
            var result = new BsonTimestamp(timestamp, increment);

            result.Value.Should().Be((long)expectedValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void constructor_with_value_should_initialize_instance(long value)
        {
            var result = new BsonTimestamp(value);

            result.Value.Should().Be(value);
        }

        [Theory]
        [InlineData(0L, 0L)]
        [InlineData(long.MinValue, long.MinValue)]
        [InlineData(long.MaxValue, long.MaxValue)]
        [InlineData(0UL, 0L)]
        [InlineData(ulong.MinValue, 0L)]
        [InlineData(ulong.MaxValue, -1L)]
        public void Create_should_return_expected_result(object value, long expectedValue)
        {
            var result = BsonTimestamp.Create(value);

            result.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void Create_should_throw_when_value_is_null()
        {
            Action action = () => { BsonTimestamp.Create(null); };

            action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_null()
        {
            var subject = new BsonTimestamp(0);
            BsonTimestamp other = null;

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);

            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_wrong_type()
        {
            var subject = new BsonTimestamp(0);
            var other = new object();

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);

            result1.Should().BeFalse();
            result2.Should().BeFalse();
        }

        [Theory]
        [InlineData(0L, 1L)]
        public void Equals_should_return_false_when_values_are_not_equal(long value1, long value2)
        {
            var subject = new BsonTimestamp(value1);
            var other = new BsonTimestamp(value2);

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);
            var subjectHashCode = subject.GetHashCode();
            var otherHashCode = other.GetHashCode();

            result1.Should().BeFalse();
            result2.Should().BeFalse();
            otherHashCode.Should().NotBe(subjectHashCode);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void Equals_should_return_true_when_values_are_equal(long value)
        {
            var subject = new BsonTimestamp(value);
            var other = new BsonTimestamp(value);
            other.Should().NotBeSameAs(subject);

            var result1 = subject.Equals(other);
            var result2 = subject.Equals((object)other);
            var subjectHashCode = subject.GetHashCode();
            var otherHashCode = other.GetHashCode();

            result1.Should().BeTrue();
            result2.Should().BeTrue();
            otherHashCode.Should().Be(subjectHashCode);
        }

        [Theory]
        [InlineData(0UL, 0)]
        [InlineData(0x100000002UL, 2)]
        [InlineData(0x7fffffff7fffffffUL, int.MaxValue)]
        [InlineData(0x7fffffff80000000UL, int.MinValue)]
        [InlineData(0x800000007fffffffUL, int.MaxValue)]
        [InlineData(0x8000000080000000UL, int.MinValue)]
        public void Increment_get_should_return_expected_result(ulong value, int expectedResult)
        {
            var subject = new BsonTimestamp((long)value);

            var result = subject.Increment;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0L, 1L)]
        [InlineData(null, 1L)]
        [InlineData(0L, null)]
        public void operator_equals_should_return_false_when_values_are_not_equal(long? value1, long? value2)
        {
            var lhs = value1 == null ? null : new BsonTimestamp(value1.Value);
            var rhs = value2 == null ? null : new BsonTimestamp(value2.Value);

            var result1 = lhs == rhs;
            var result2 = lhs != rhs;

            result1.Should().BeFalse();
            result2.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void operator_equals_should_return_true_when_values_are_equal(long? value)
        {
            var lhs = value == null ? null : new BsonTimestamp(value.Value);
            var rhs = value == null ? null : new BsonTimestamp(value.Value);
            if (value != null)
            {
                rhs.Should().NotBeSameAs(lhs);
            }

            var result1 = lhs == rhs;
            var result2 = lhs != rhs;

            result1.Should().BeTrue();
            result2.Should().BeFalse();
        }

        [Theory]
        [InlineData(0UL, 0)]
        [InlineData(0x100000002UL, 1)]
        [InlineData(0x7fffffff7fffffffUL, int.MaxValue)]
        [InlineData(0x800000007fffffffUL, int.MinValue)]
        [InlineData(0x7fffffff80000000UL, int.MaxValue)]
        [InlineData(0x8000000080000000UL, int.MinValue)]
        public void Timestamp_get_should_return_expected_result(ulong value, int expectedResult)
        {
            var subject = new BsonTimestamp((long)value);

            var result = subject.Timestamp;

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0L, "0")]
        [InlineData(1L, "1")]
        [InlineData(-1L, "-1")]
        public void ToString_should_return_expected_result(long value, string expectedResult)
        {
            var subject = new BsonTimestamp(value);

            var result = subject.ToString();

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        public void Value_get_should_return_expected_result(long value)
        {
            var subject = new BsonTimestamp(value);

            var result = subject.Value;

            result.Should().Be(value);
        }
    }
}
