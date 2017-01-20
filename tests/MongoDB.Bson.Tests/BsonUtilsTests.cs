/* Copyright 2010-2014 MongoDB Inc.
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

        [Fact]
        public void TestToHexString()
        {
            var value = new byte[] { 0, 1,2, 3, 4, 5 ,6 ,7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var expected = "000102030405060708090a0b0c0d0e0f10ff";
            var actual = BsonUtils.ToHexString(value);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestToHexStringNull()
        {
            Assert.Throws<ArgumentNullException>(() => BsonUtils.ToHexString(null));
        }

        [Fact]
        public void TestParseHexStringNull()
        {
            Assert.Throws<ArgumentNullException>(() => BsonUtils.ParseHexString(null));
        }

        [Fact]
        public void TestParseHexStringEmpty()
        {
            byte[] expected = new byte[0];
            var actual = BsonUtils.ParseHexString(string.Empty);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestParseHexString()
        {
            var expected = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var value = "000102030405060708090a0b0c0d0e0f10ff";
            var actual = BsonUtils.ParseHexString(value);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestParseHexStringOdd()
        {
            var expected = new byte[] { 0, 15 };
            var value = "00f";
            var actual = BsonUtils.ParseHexString(value);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestParseHexStringInvalid()
        {
            Assert.Throws<FormatException>(() => BsonUtils.ParseHexString("1G"));
        }

        [Fact]
        public void TestParseHexStringInvalid2()
        {
            Assert.Throws<FormatException>(() => BsonUtils.ParseHexString("00 1"));
        }

        [Fact]
        public void TestTryParseHexStringNull()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString(null, out actual);
            Assert.False(result);
            Assert.Null(actual);
        }

        [Fact]
        public void TestTryParseHexStringEmpty()
        {
            byte[] expected = new byte[0];
            byte[] actual;
            var result = BsonUtils.TryParseHexString(string.Empty, out actual);
            Assert.True(result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryParseHexString()
        {
            var expected = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var value = "000102030405060708090a0b0c0d0e0f10ff";
            byte[] actual;
            var result = BsonUtils.TryParseHexString(value, out actual);
            Assert.True(result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryParseHexStringOdd()
        {
            var expected = new byte[] { 0, 15 };
            var value = "00f";
            byte[] actual;
            var result = BsonUtils.TryParseHexString(value, out actual);
            Assert.True(result);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTryParseHexStringInvalid()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString("1G", out actual);
            Assert.False(result);
            Assert.Null(actual);
        }

        [Fact]
        public void TestTryParseHexStringInvalid2()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString("00 1", out actual);
            Assert.False(result);
            Assert.Null(actual);
        }

    }
}
