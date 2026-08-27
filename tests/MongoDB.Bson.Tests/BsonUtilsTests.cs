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
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonUtilsTests
    {
        [Fact]
        public void TestMaxToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch);
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
            Assert.Equal(DateTime.MaxValue, actual);
        }

        [Fact]
        public void TestMinToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMinValueMillisecondsSinceEpoch);
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
            Assert.Equal(DateTime.MinValue, actual);
        }

        [Fact]
        public void TestZeroToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(0);
            Assert.Equal(DateTimeKind.Utc, actual.Kind);
            Assert.Equal(BsonConstants.UnixEpoch, actual);
        }

        [Fact]
        public void TestGreaterThanMaxToDateTimeConversion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                    BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1));
        }

        [Fact]
        public void TestLessThanMinToDateTimeConversion()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                    BsonConstants.DateTimeMinValueMillisecondsSinceEpoch - 1));
        }

        [Fact]
        public void TestMaxToMillisConversion()
        {
            var actual = BsonUtils.ToMillisecondsSinceEpoch(DateTime.MaxValue);
            Assert.Equal(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch, actual);
        }

        [Fact]
        public void TestMinToMillisConversion()
        {
            var actual = BsonUtils.ToMillisecondsSinceEpoch(DateTime.MinValue);
            Assert.Equal(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch, actual);
        }

        [Fact]
        public void TestToUniversalTimeUTCNow()
        {
            var expected = DateTime.UtcNow;
            var actual = BsonUtils.ToUniversalTime(expected.ToLocalTime());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestToUniversalTimeMax()
        {
            var expected = DateTime.MaxValue;
            var actual = BsonUtils.ToUniversalTime(expected);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestToUniversalTimeMin()
        {
            var expected = DateTime.MinValue;
            var actual = BsonUtils.ToUniversalTime(expected);
            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> ParseHexStringValidInput
        {
            get
            {
                yield return new object[] { "", new byte[0] };
                yield return new object[] { "1", new byte[] { 0x01 } };
                yield return new object[] { "12", new byte[] { 0x12 } };
                yield return new object[] { "123", new byte[] { 0x01, 0x23 } };
                yield return new object[] { "1234", new byte[] { 0x12, 0x34 } };
                yield return new object[] { "12345", new byte[] { 0x01, 0x23, 0x45 } };
                yield return new object[] { "123456", new byte[] { 0x12, 0x34, 0x56 } };
                yield return new object[] { "0123456789abcdefABCDEF", new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0xab, 0xcd, 0xef } };
            }
        }

        [Theory]
        [MemberData(nameof(ParseHexStringValidInput))]
        public void ParseHexString_should_return_expected_result(string s, byte[] expectedResult)
        {
            var result = BsonUtils.ParseHexString(s);

            result.Should().Equal(expectedResult);
        }

        [Theory]
        [MemberData(nameof(ParseHexStringValidInput))]
        public void ParseHexStringSpan_should_return_expected_result(string s, byte[] expectedResult)
        {
            Span<byte> result = stackalloc byte[(s.Length + 1) / 2];
            BsonUtils.ParseHexString(s.AsSpan(), result);

            result.ToArray().Should().Equal(expectedResult);
        }

        public static IEnumerable<object[]> ParseHexStringInvalidInput
        {
            get
            {
                yield return new object[] { "x" };
                yield return new object[] { "/" };  // character just before "0"
                yield return new object[] { ":" };  // character just after "9"
                yield return new object[] { "`" };  // character just before "a"
                yield return new object[] { "g" };  // character just after "f"
                yield return new object[] { "@" };  // character just before "A"
                yield return new object[] { "G" };  // character just after "F"
            }
        }

        [Theory]
        [MemberData(nameof(ParseHexStringInvalidInput))]
        public void ParseHexString_should_throw_when_string_is_invalid(string s)
        {
            var exception = Record.Exception(() => BsonUtils.ParseHexString(s));

            exception.Should().BeOfType<FormatException>();
        }

        [Theory]
        [MemberData(nameof(ParseHexStringInvalidInput))]
        public void ParseHexStringSpan_should_throw_when_string_is_invalid(string s)
        {
            var exception = Record.Exception(() => BsonUtils.ParseHexString(s.AsSpan(), new byte[(s.Length + 1) / 2]));

            exception.Should().BeOfType<FormatException>();
        }

        [Fact]
        public void ParseHexString_should_throw_when_string_is_null()
        {
            var exception = Record.Exception(() => BsonUtils.ParseHexString(null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("s");
        }

        [Theory]
        [InlineData(1, 0, true)]
        [InlineData(1, 1, false)]
        [InlineData(1, 2, true)]
        [InlineData(1, 3, true)]
        [InlineData(2, 0, true)]
        [InlineData(2, 1, false)]
        [InlineData(2, 2, true)]
        [InlineData(2, 3, true)]
        [InlineData(9, 1, true)]
        [InlineData(9, 4, true)]
        [InlineData(9, 5, false)]
        [InlineData(9, 6, true)]
        [InlineData(10, 1, true)]
        [InlineData(10, 4, true)]
        [InlineData(10, 5, false)]
        [InlineData(10, 6, true)]
        [InlineData(11, 1, true)]
        [InlineData(11, 4, true)]
        [InlineData(11, 5, true)]
        [InlineData(11, 6, false)]
        [InlineData(11, 7, true)]
        public void ParseHexStringSpan_should_throw_when_length_is_incorrect(int inputLength, int destinationLength, bool shouldThrow)
        {
            var exception = Record.Exception(() => BsonUtils.ParseHexString(new string('0', inputLength).AsSpan(), new byte[destinationLength]));

            if (shouldThrow)
            {
                var formatException = exception.Should().BeOfType<FormatException>().Subject;
                formatException.Message.Should().Be($"Target should be {(inputLength + 1) / 2} bytes long");
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(new byte[0], "")]
        [InlineData(new byte[] { 0x01 }, "01")]
        [InlineData(new byte[] { 0x01, 0x23 }, "0123")]
        [InlineData(new byte[] { 0x01, 0x23, 0x45 }, "012345")]
        [InlineData(new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef }, "0123456789abcdef")]
        public void ToHexString_should_return_expected_result(byte[] value, string expectedResult)
        {
            var result = BsonUtils.ToHexString(value);

            result.Should().Be(expectedResult);
        }

        [Fact]
        public void ToHexString_should_throw_when_bytes_is_null()
        {
            var exception = Record.Exception(() => BsonUtils.ToHexString(null));

            var argumentNullException = exception.Should().BeOfType<ArgumentNullException>().Subject;
            argumentNullException.ParamName.Should().Be("bytes");
        }

        [Theory]
        [MemberData(nameof(ParseHexStringValidInput))]
        public void TryParseHexString_should_return_expected_result(string s, byte[] expectedBytes)
        {
            byte[] bytes;
            var result = BsonUtils.TryParseHexString(s, out bytes);

            result.Should().BeTrue();
            bytes.Should().Equal(expectedBytes);
        }
        [Theory]
        [MemberData(nameof(ParseHexStringValidInput))]
        public void TryParseHexStringSpan_should_return_expected_result(string s, byte[] expectedBytes)
        {
            Span<byte> bytes = stackalloc byte[expectedBytes.Length];
            var result = BsonUtils.TryParseHexString(s.AsSpan(), bytes);

            result.Should().BeTrue();
            bytes.ToArray().Should().Equal(expectedBytes);
        }

        [Theory]
        [MemberData(nameof(ParseHexStringInvalidInput))]
        [InlineData(null)]
        public void TryParseHexString_should_return_expected_result_when_string_is_invalid(string s)
        {
            byte[] bytes;
            var result = BsonUtils.TryParseHexString(s, out bytes);

            result.Should().BeFalse();
            bytes.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(ParseHexStringInvalidInput))]
        public void TryParseHexStringSpan_should_return_expected_result_when_string_is_invalid(string s)
        {
            string input = "12345" + s;
            int length = (input.Length + 1) / 2;
            Span<byte> bytes = stackalloc byte[length];
            var result = BsonUtils.TryParseHexString(input.AsSpan(), bytes);

            result.Should().BeFalse();
            bytes.ToArray().Should().Equal(new byte[] { 0x12, 0x34 }.Concat(new byte[length - 2]));
        }
    }
}
