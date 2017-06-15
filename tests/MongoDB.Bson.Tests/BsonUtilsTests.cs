/* Copyright 2010-2017 MongoDB Inc.
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

        [Theory]
        [InlineData("", new byte[0])]
        [InlineData("1", new byte[] { 0x01 })]
        [InlineData("12", new byte[] { 0x12 })]
        [InlineData("123", new byte[] { 0x01, 0x23 })]
        [InlineData("1234", new byte[] { 0x12, 0x34 })]
        [InlineData("12345", new byte[] { 0x01, 0x23, 0x45 })]
        [InlineData("123456", new byte[] { 0x12, 0x34, 0x56 })]
        [InlineData("0123456789abcdefABCDEF", new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0xab, 0xcd, 0xef })]
        public void ParseHexString_should_return_expected_result(string s, byte[] expectedResult)
        {
            var result = BsonUtils.ParseHexString(s);

            result.Should().Equal(expectedResult);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("/")] // character just before "0"
        [InlineData(":")] // character just after "9"
        [InlineData("`")] // character just before "a"
        [InlineData("g")] // character just after "f"
        [InlineData("@")] // character just before "A"
        [InlineData("G")] // character just after "F"
        public void ParseHexString_should_throw_when_string_is_invalid(string s)
        {
            var exception = Record.Exception(() => BsonUtils.ParseHexString(s));

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
        [InlineData("", new byte[0])]
        [InlineData("1", new byte[] { 0x01 })]
        [InlineData("12", new byte[] { 0x12 })]
        [InlineData("123", new byte[] { 0x01, 0x23 })]
        [InlineData("1234", new byte[] { 0x12, 0x34 })]
        [InlineData("12345", new byte[] { 0x01, 0x23, 0x45 })]
        [InlineData("123456", new byte[] { 0x12, 0x34, 0x56 })]
        [InlineData("0123456789abcdefABCDEF", new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef, 0xab, 0xcd, 0xef })]
        public void TryParseHexString_should_return_expected_result(string s, byte[] expectedBytes)
        {
            byte[] bytes;
            var result = BsonUtils.TryParseHexString(s, out bytes);

            result.Should().BeTrue();
            bytes.Should().Equal(expectedBytes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("x")]
        [InlineData("/")] // character just before "0"
        [InlineData(":")] // character just after "9"
        [InlineData("`")] // character just before "a"
        [InlineData("g")] // character just after "f"
        [InlineData("@")] // character just before "A"
        [InlineData("G")] // character just after "F"
        public void TryParseHexString_should_return_expected_result_when_string_is_invalid(string s)
        {
            byte[] bytes;
            var result = BsonUtils.TryParseHexString(s, out bytes);

            result.Should().BeFalse();
            bytes.Should().BeNull();
        }
    }
}
