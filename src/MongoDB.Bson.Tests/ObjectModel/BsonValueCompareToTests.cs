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
    public class BsonValueCompareToTests
    {
        [Fact]
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
                Assert.Equal(-1, values[i].CompareTypeTo(values[i + 1]));
                Assert.Equal(1, values[i + 1].CompareTypeTo(values[i]));
                Assert.True(values[i] < values[i + 1]);
                Assert.True(values[i] <= values[i + 1]);
                Assert.True(values[i] != values[i + 1]);
                Assert.False(values[i] == values[i + 1]);
                Assert.False(values[i] > values[i + 1]);
                Assert.False(values[i] >= values[i + 1]);
                Assert.Equal(1, values[i].CompareTypeTo(null));
            }
        }

        [Fact]
        public void TestCompareTwoCsharpNulls()
        {
            BsonValue null1 = null;
            BsonValue null2 = null;
            Assert.False(null1 < null2);
            Assert.True(null1 <= null2);
            Assert.False(null1 != null2);
            Assert.True(null1 == null2);
            Assert.False(null1 > null2);
            Assert.True(null1 >= null2);
        }

        [Fact]
        public void TestCompareTwoMaxKeys()
        {
            Assert.False(BsonMaxKey.Value < BsonMaxKey.Value);
            Assert.True(BsonMaxKey.Value <= BsonMaxKey.Value);
            Assert.False(BsonMaxKey.Value != BsonMaxKey.Value);
            Assert.True(BsonMaxKey.Value == BsonMaxKey.Value);
            Assert.False(BsonMaxKey.Value > BsonMaxKey.Value);
            Assert.True(BsonMaxKey.Value >= BsonMaxKey.Value);
        }

        [Fact]
        public void TestCompareTwoMinKeys()
        {
            Assert.False(BsonMinKey.Value < BsonMinKey.Value);
            Assert.True(BsonMinKey.Value <= BsonMinKey.Value);
            Assert.False(BsonMinKey.Value != BsonMinKey.Value);
            Assert.True(BsonMinKey.Value == BsonMinKey.Value);
            Assert.False(BsonMinKey.Value > BsonMinKey.Value);
            Assert.True(BsonMinKey.Value >= BsonMinKey.Value);
        }

        [Fact]
        public void TestCompareTwoBsonNulls()
        {
            Assert.False(BsonNull.Value < BsonNull.Value);
            Assert.True(BsonNull.Value <= BsonNull.Value);
            Assert.False(BsonNull.Value != BsonNull.Value);
            Assert.True(BsonNull.Value == BsonNull.Value);
            Assert.False(BsonNull.Value > BsonNull.Value);
            Assert.True(BsonNull.Value >= BsonNull.Value);
        }

        [Fact]
        public void TestCompareTwoOnes()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt32(1);
            Assert.False(n1 < n2);
            Assert.True(n1 <= n2);
            Assert.False(n1 != n2);
            Assert.True(n1 == n2);
            Assert.False(n1 > n2);
            Assert.True(n1 >= n2);
        }

        [Fact]
        public void TestCompareOneAndTwo()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt32(2);
            Assert.True(n1 < n2);
            Assert.True(n1 <= n2);
            Assert.True(n1 != n2);
            Assert.False(n1 == n2);
            Assert.False(n1 > n2);
            Assert.False(n1 >= n2);
        }

        [Fact]
        public void TestCompareDifferentTypeOnes()
        {
            var n1 = new BsonInt32(1);
            var n2 = new BsonInt64(1);
            var n3 = new BsonDouble(1.0);
            Assert.True(n1 == n2);
            Assert.True(n1 == n3);
            Assert.True(n2 == n1);
            Assert.True(n2 == n3);
            Assert.True(n3 == n1);
            Assert.True(n3 == n2);

            var v1 = (BsonValue)new BsonInt32(1);
            var v2 = (BsonValue)new BsonInt64(1);
            var v3 = (BsonValue)new BsonDouble(1.0);
            Assert.True(v1 == v2);
            Assert.True(v1 == v3);
            Assert.True(v2 == v1);
            Assert.True(v2 == v3);
            Assert.True(v3 == v1);
            Assert.True(v3 == v2);
        }
    }
}
