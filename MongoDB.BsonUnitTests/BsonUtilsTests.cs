/* Copyright 2010-2013 10gen Inc.
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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonUtilsTests
    {
        [Test]
        public void TestMaxToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
            Assert.AreEqual(DateTime.MaxValue, actual);
        }

        [Test]
        public void TestMinToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMinValueMillisecondsSinceEpoch);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
            Assert.AreEqual(DateTime.MinValue, actual);
        }

        [Test]
        public void TestZeroToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(0);
            Assert.AreEqual(DateTimeKind.Utc, actual.Kind);
            Assert.AreEqual(BsonConstants.UnixEpoch, actual);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGreaterThanMaxToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1);
        }

        [Test]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestLessThanMinToDateTimeConversion()
        {
            var actual = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(
                BsonConstants.DateTimeMinValueMillisecondsSinceEpoch - 1);
        }

        [Test]
        public void TestMaxToMillisConversion()
        {
            var actual = BsonUtils.ToMillisecondsSinceEpoch(DateTime.MaxValue);
            Assert.AreEqual(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch, actual);
        }

        [Test]
        public void TestMinToMillisConversion()
        {
            var actual = BsonUtils.ToMillisecondsSinceEpoch(DateTime.MinValue);
            Assert.AreEqual(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch, actual);
        }

        [Test]
        public void TestToUniversalTimeUTCNow()
        {
            var expected = DateTime.UtcNow;
            var actual = BsonUtils.ToUniversalTime(expected.ToLocalTime());
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestToUniversalTimeMax()
        {
            var expected = DateTime.MaxValue;
            var actual = BsonUtils.ToUniversalTime(expected);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestToUniversalTimeMin()
        {
            var expected = DateTime.MinValue;
            var actual = BsonUtils.ToUniversalTime(expected);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestToHexString()
        {
            var value = new byte[] { 0, 1,2, 3, 4, 5 ,6 ,7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var expected = "000102030405060708090a0b0c0d0e0f10ff";
            var actual = BsonUtils.ToHexString(value);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestToHexStringNull()
        {
            var actual = BsonUtils.ToHexString(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestParseHexStringNull()
        {
            var actual = BsonUtils.ParseHexString(null);
        }

        [Test]
        public void TestParseHexStringEmpty()
        {
            byte[] expected = new byte[0];
            var actual = BsonUtils.ParseHexString(string.Empty);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestParseHexString()
        {
            var expected = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var value = "000102030405060708090a0b0c0d0e0f10ff";
            var actual = BsonUtils.ParseHexString(value);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestParseHexStringOdd()
        {
            var expected = new byte[] { 0, 15 };
            var value = "00f";
            var actual = BsonUtils.ParseHexString(value);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void TestParseHexStringInvalid()
        {
            var actual = BsonUtils.ParseHexString("1G");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void TestParseHexStringInvalid2()
        {
            var actual = BsonUtils.ParseHexString("00 1");
        }

        [Test]
        public void TestTryParseHexStringNull()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString(null, out actual);
            Assert.IsFalse(result);
            Assert.IsNull(actual);
        }

        [Test]
        public void TestTryParseHexStringEmpty()
        {
            byte[] expected = new byte[0];
            byte[] actual;
            var result = BsonUtils.TryParseHexString(string.Empty, out actual);
            Assert.IsTrue(result);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestTryParseHexString()
        {
            var expected = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 255 };
            var value = "000102030405060708090a0b0c0d0e0f10ff";
            byte[] actual;
            var result = BsonUtils.TryParseHexString(value, out actual);
            Assert.IsTrue(result);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestTryParseHexStringOdd()
        {
            var expected = new byte[] { 0, 15 };
            var value = "00f";
            byte[] actual;
            var result = BsonUtils.TryParseHexString(value, out actual);
            Assert.IsTrue(result);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestTryParseHexStringInvalid()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString("1G", out actual);
            Assert.IsFalse(result);
            Assert.IsNull(actual);
        }

        [Test]
        public void TestTryParseHexStringInvalid2()
        {
            byte[] actual;
            var result = BsonUtils.TryParseHexString("00 1", out actual);
            Assert.IsFalse(result);
            Assert.IsNull(actual);
        }

    }
}
