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

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonValueCompareToTests
    {
        [Test]
        public void TestCompareTypeTo()
        {
            BsonValue[] values =
            {
                BsonMinKey.Value,
                BsonNull.Value,
                new BsonInt32(0),
                BsonString.Empty,
                new BsonDocument(),
                new BsonArray(),
                new BsonBinaryData(new byte[] { 1 }),
                ObjectId.GenerateNewId(),
                BsonBoolean.False,
                new BsonDateTime(DateTime.UtcNow),
                new BsonRegularExpression("pattern")
            };
            for (int i = 0; i < values.Length - 2; i++)
            {
                Assert.AreEqual(-1, values[i].CompareTypeTo(values[i + 1]));
                Assert.AreEqual(1, values[i + 1].CompareTypeTo(values[i]));
                Assert.IsTrue(values[i] < values[i + 1]);
                Assert.IsTrue(values[i] <= values[i + 1]);
                Assert.IsTrue(values[i] != values[i + 1]);
                Assert.IsFalse(values[i] == values[i + 1]);
                Assert.IsFalse(values[i] > values[i + 1]);
                Assert.IsFalse(values[i] >= values[i + 1]);
                Assert.AreEqual(1, values[i].CompareTypeTo(null));
            }
        }

        [Test]
        public void TestCompareTwoCsharpNulls()
        {
            BsonValue null1 = null;
            BsonValue null2 = null;
            Assert.IsFalse(null1 < null2);
            Assert.IsTrue(null1 <= null2);
            Assert.IsFalse(null1 != null2);
            Assert.IsTrue(null1 == null2);
            Assert.IsFalse(null1 > null2);
            Assert.IsTrue(null1 >= null2);
        }

        [Test]
        public void TestCompareTwoMaxKeys()
        {
            Assert.IsFalse(BsonMaxKey.Value < BsonMaxKey.Value);
            Assert.IsTrue(BsonMaxKey.Value <= BsonMaxKey.Value);
            Assert.IsFalse(BsonMaxKey.Value != BsonMaxKey.Value);
            Assert.IsTrue(BsonMaxKey.Value == BsonMaxKey.Value);
            Assert.IsFalse(BsonMaxKey.Value > BsonMaxKey.Value);
            Assert.IsTrue(BsonMaxKey.Value >= BsonMaxKey.Value);
        }

        [Test]
        public void TestCompareTwoMinKeys()
        {
            Assert.IsFalse(BsonMinKey.Value < BsonMinKey.Value);
            Assert.IsTrue(BsonMinKey.Value <= BsonMinKey.Value);
            Assert.IsFalse(BsonMinKey.Value != BsonMinKey.Value);
            Assert.IsTrue(BsonMinKey.Value == BsonMinKey.Value);
            Assert.IsFalse(BsonMinKey.Value > BsonMinKey.Value);
            Assert.IsTrue(BsonMinKey.Value >= BsonMinKey.Value);
        }

        [Test]
        public void TestCompareTwoBsonNulls()
        {
            Assert.IsFalse(BsonNull.Value < BsonNull.Value);
            Assert.IsTrue(BsonNull.Value <= BsonNull.Value);
            Assert.IsFalse(BsonNull.Value != BsonNull.Value);
            Assert.IsTrue(BsonNull.Value == BsonNull.Value);
            Assert.IsFalse(BsonNull.Value > BsonNull.Value);
            Assert.IsTrue(BsonNull.Value >= BsonNull.Value);
        }

        [Test]
        public void TestCompareTwoOnes()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt32(1);
            Assert.IsFalse(n1 < n2);
            Assert.IsTrue(n1 <= n2);
            Assert.IsFalse(n1 != n2);
            Assert.IsTrue(n1 == n2);
            Assert.IsFalse(n1 > n2);
            Assert.IsTrue(n1 >= n2);
        }

        [Test]
        public void TestCompareOneAndTwo()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt32(2);
            Assert.IsTrue(n1 < n2);
            Assert.IsTrue(n1 <= n2);
            Assert.IsTrue(n1 != n2);
            Assert.IsFalse(n1 == n2);
            Assert.IsFalse(n1 > n2);
            Assert.IsFalse(n1 >= n2);
        }

        [Test]
        public void TestCompareDifferentTypeOnes()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt64(1);
            var n3 = new BsonDouble(1.0);
            Assert.IsTrue(n1 == n2);
            Assert.IsTrue(n1 == n3);
            Assert.IsTrue(n2 == n1);
            Assert.IsTrue(n2 == n3);
            Assert.IsTrue(n3 == n1);
            Assert.IsTrue(n3 == n2);

            var v1 = (BsonValue)new BsonInt32(1);
            var v2 = (BsonValue)new BsonInt64(1);
            var v3 = (BsonValue)new BsonDouble(1.0);
            Assert.IsTrue(v1 == v2);
            Assert.IsTrue(v1 == v3);
            Assert.IsTrue(v2 == v1);
            Assert.IsTrue(v2 == v3);
            Assert.IsTrue(v3 == v1);
            Assert.IsTrue(v3 == v2);
        }
    }
}
