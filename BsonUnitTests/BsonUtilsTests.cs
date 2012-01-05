/* Copyright 2010-2011 10gen Inc.
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
using NUnit.Framework;

using MongoDB.Bson;

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
            var actual = BsonUtils.ToUniversalTime(expected);
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

    }
}
