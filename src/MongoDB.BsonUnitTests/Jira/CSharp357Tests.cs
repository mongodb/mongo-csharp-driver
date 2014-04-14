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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.Jira
{
    [TestFixture]
    public class CSharp357Tests
    {
        [Test]
        public void TestAsLocalTime()
        {
            var now = DateTime.Now;
            var nowTruncated = now.AddTicks(-(now.Ticks % 10000));
            var bsonDateTime = new BsonDateTime(now);
            var localDateTime = bsonDateTime.ToLocalTime();
            Assert.AreEqual(DateTimeKind.Local, localDateTime.Kind);
            Assert.AreEqual(nowTruncated, localDateTime);
        }

        [Test]
        public void TestAsUniversalTime()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var bsonDateTime = new BsonDateTime(utcNow);
            var utcDateTime = bsonDateTime.ToUniversalTime();
            Assert.AreEqual(DateTimeKind.Utc, utcDateTime.Kind);
            Assert.AreEqual(utcNowTruncated, utcDateTime);
        }

        [Test]
        public void TestDateTimeMaxValue()
        {
            foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified, DateTimeKind.Utc })
            {
                var bsonDateTime = new BsonDateTime(DateTime.SpecifyKind(DateTime.MaxValue, kind));
                Assert.AreEqual(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch, bsonDateTime.MillisecondsSinceEpoch);

                var utcDateTime = bsonDateTime.ToUniversalTime();
                Assert.AreEqual(DateTimeKind.Utc, utcDateTime.Kind);
                Assert.AreEqual(DateTime.MaxValue, utcDateTime);

                var localDateTime = bsonDateTime.ToLocalTime();
                Assert.AreEqual(DateTimeKind.Local, localDateTime.Kind);
                Assert.AreEqual(DateTime.MaxValue, localDateTime);
            }
        }

        [Test]
        public void TestDateTimeMinValue()
        {
            foreach (var kind in new[] { DateTimeKind.Local, DateTimeKind.Unspecified, DateTimeKind.Utc })
            {
                var bsonDateTime = new BsonDateTime(DateTime.SpecifyKind(DateTime.MinValue, kind));
                Assert.AreEqual(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch, bsonDateTime.MillisecondsSinceEpoch);

                var utcDateTime = bsonDateTime.ToUniversalTime();
                Assert.AreEqual(DateTimeKind.Utc, utcDateTime.Kind);
                Assert.AreEqual(DateTime.MinValue, utcDateTime);

                var localDateTime = bsonDateTime.ToLocalTime();
                Assert.AreEqual(DateTimeKind.Local, localDateTime.Kind);
                Assert.AreEqual(DateTime.MinValue, localDateTime);
            }
        }

        [Test]
        public void TestIsValidDateTime()
        {
            Assert.IsFalse(new BsonDateTime(long.MinValue).IsValidDateTime);
            Assert.IsFalse(new BsonDateTime(long.MinValue + 1).IsValidDateTime);
            Assert.IsFalse(new BsonDateTime(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch - 1).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(BsonConstants.DateTimeMinValueMillisecondsSinceEpoch + 1).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(-1).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(0).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(1).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch - 1).IsValidDateTime);
            Assert.IsTrue(new BsonDateTime(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch).IsValidDateTime);
            Assert.IsFalse(new BsonDateTime(BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1).IsValidDateTime);
            Assert.IsFalse(new BsonDateTime(long.MaxValue - 1).IsValidDateTime);
            Assert.IsFalse(new BsonDateTime(long.MaxValue).IsValidDateTime);
        }

        [Test]
        public void TestMillisecondsSinceEpochValues()
        {
            var values = new long[] 
            {
                long.MinValue,
                long.MinValue + 1,
                -1,
                0,
                1,
                long.MaxValue - 1,
                long.MaxValue
            };

            foreach (var value in values)
            {
                var bsonDateTime = new BsonDateTime(value);
                Assert.AreEqual(value, bsonDateTime.MillisecondsSinceEpoch);
#pragma warning disable 618
                Assert.AreEqual(value, bsonDateTime.RawValue);
#pragma warning restore
            }
        }

        [Test]
        public void TestValues()
        {
            var maxValueTruncated = new DateTime(9999, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);

            var tests = new object[][]
            {
                new object[] { long.MinValue, false, DateTime.MinValue },
                new object[] { long.MinValue + 1, false, DateTime.MinValue },
                new object[] { BsonConstants.DateTimeMinValueMillisecondsSinceEpoch - 1, false, DateTime.MinValue },
                new object[] { BsonConstants.DateTimeMinValueMillisecondsSinceEpoch, true, DateTime.MinValue },
                new object[] { BsonConstants.DateTimeMinValueMillisecondsSinceEpoch + 1, true, DateTime.MinValue.AddMilliseconds(1) },
                new object[] { -1L, true, BsonConstants.UnixEpoch.AddMilliseconds(-1) },
                new object[] { 0L, true, BsonConstants.UnixEpoch },
                new object[] { 1L, true, BsonConstants.UnixEpoch.AddMilliseconds(1) },
                new object[] { BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch - 1, true, maxValueTruncated.AddMilliseconds(-1) },
                new object[] { BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch, true, DateTime.MaxValue },
                new object[] { BsonConstants.DateTimeMaxValueMillisecondsSinceEpoch + 1, false, DateTime.MaxValue },
                new object[] { long.MaxValue - 1, false, DateTime.MaxValue },
                new object[] { long.MaxValue, false, DateTime.MaxValue },
            };

            foreach (var test in tests)
            {
                var millisecondsSinceEpoch = (long)test[0];
                var expectedIsValidDateTime = (bool)test[1];
                var expectedDateTime = (DateTime)test[2];

                var bsonDateTime = new BsonDateTime(millisecondsSinceEpoch);
                if (expectedIsValidDateTime)
                {
                    Assert.IsTrue(bsonDateTime.IsValidDateTime);
                    var value = bsonDateTime.ToUniversalTime();
                    Assert.AreEqual(DateTimeKind.Utc, value.Kind);
                    Assert.AreEqual(expectedDateTime, value);
                }
                else
                {
                    Assert.IsFalse(bsonDateTime.IsValidDateTime);
                    Assert.Throws<ArgumentOutOfRangeException>(() => { var value = bsonDateTime.ToUniversalTime(); });
                }
            }
        }
    }
}
