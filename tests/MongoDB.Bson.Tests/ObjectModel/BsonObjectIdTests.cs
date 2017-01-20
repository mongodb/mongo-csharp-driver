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
using System.Linq;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonObjectIdTests
    {
        [Fact]
        public void TestByteArrayConstructor()
        {
#pragma warning disable 618
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new BsonObjectId(bytes);
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
#pragma warning restore
        }

        [Fact]
        public void TestIntIntShortIntConstructor()
        {
#pragma warning disable 618
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new BsonObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
#pragma warning restore
        }

        [Fact]
        public void TestDateTimeConstructor()
        {
#pragma warning disable 618
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var timestamp = BsonConstants.UnixEpoch.AddSeconds(0x01020304);
            var objectId = new BsonObjectId(timestamp, 0x050607, 0x0809, 0x0a0b0c);
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
#pragma warning restore
        }

        [Fact]
        public void TestStringConstructor()
        {
#pragma warning disable 618
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new BsonObjectId("0102030405060708090a0b0c");
            Assert.Equal(0x01020304, objectId.Timestamp);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(0x050607, objectId.Machine);
            Assert.Equal(0x0809, objectId.Pid);
            Assert.Equal(0x0a0b0c, objectId.Increment);
            Assert.Equal(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.Equal("0102030405060708090a0b0c", objectId.ToString());
            Assert.True(bytes.SequenceEqual(objectId.ToByteArray()));
#pragma warning restore
        }

        [Fact]
        public void TestCreateNull()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => { BsonObjectId.Create(obj); });
        }

        [Fact]
        public void TestGenerateNewId()
        {
#pragma warning disable 618
            // compare against two timestamps in case seconds since epoch changes in middle of test
            var timestamp1 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
            var objectId = BsonObjectId.GenerateNewId();
            var timestamp2 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
            Assert.True(objectId.Timestamp == timestamp1 || objectId.Timestamp == timestamp2);
            Assert.True(objectId.Machine != 0);
            Assert.True(objectId.Pid != 0);
#pragma warning restore
        }

        [Fact]
        public void TestGenerateNewIdWithDateTime()
        {
#pragma warning disable 618
            var timestamp = new DateTime(2011, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var objectId = BsonObjectId.GenerateNewId(timestamp);
            Assert.True(objectId.CreationTime == timestamp);
            Assert.True(objectId.Machine != 0);
            Assert.True(objectId.Pid != 0);
#pragma warning restore
        }

        [Fact]
        public void TestGenerateNewIdWithTimestamp()
        {
#pragma warning disable 618
            var timestamp = 0x01020304;
            var objectId = BsonObjectId.GenerateNewId(timestamp);
            Assert.True(objectId.Timestamp == timestamp);
            Assert.True(objectId.Machine != 0);
            Assert.True(objectId.Pid != 0);
#pragma warning restore
        }

        [Fact]
        public void TestIComparable()
        {
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId();
            Assert.Equal(0, objectId1.CompareTo(objectId1));
            Assert.Equal(-1, objectId1.CompareTo(objectId2));
            Assert.Equal(1, objectId2.CompareTo(objectId1));
            Assert.Equal(0, objectId2.CompareTo(objectId2));
        }

        [Fact]
        public void TestCompareEqualGeneratedIds()
        {
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            var objectId2 = objectId1;
            Assert.False(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.False(objectId1 != objectId2);
            Assert.True(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareSmallerTimestamp()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030505060708090a0b0c");
            Assert.True(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.False(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareSmallerMachine()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060808090a0b0c");
            Assert.True(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.False(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareSmallerPid()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("01020304050607080a0a0b0c");
            Assert.True(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.False(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareSmallerIncrement()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0d");
            Assert.True(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.False(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareSmallerGeneratedId()
        {
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId();
            Assert.True(objectId1 < objectId2);
            Assert.True(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.False(objectId1 > objectId2);
            Assert.False(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareLargerTimestamp()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030305060708090a0b0c");
            Assert.False(objectId1 < objectId2);
            Assert.False(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.True(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareLargerMachine()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060808090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            Assert.False(objectId1 < objectId2);
            Assert.False(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.True(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareLargerPid()
        {
            var objectId1 = (BsonObjectId)new ObjectId("01020304050607080a0a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            Assert.False(objectId1 < objectId2);
            Assert.False(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.True(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareLargerIncrement()
        {
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0d");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            Assert.False(objectId1 < objectId2);
            Assert.False(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.True(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestCompareLargerGeneratedId()
        {
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId(); // generate before objectId2
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            Assert.False(objectId1 < objectId2);
            Assert.False(objectId1 <= objectId2);
            Assert.True(objectId1 != objectId2);
            Assert.False(objectId1 == objectId2);
            Assert.True(objectId1 > objectId2);
            Assert.True(objectId1 >= objectId2);
        }

        [Fact]
        public void TestParse()
        {
#pragma warning disable 618
            var objectId1 = BsonObjectId.Parse("0102030405060708090a0b0c"); // lower case
            var objectId2 = BsonObjectId.Parse("0102030405060708090A0B0C"); // upper case
            Assert.True(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
            Assert.True(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
            Assert.True(objectId1.ToString() == objectId2.ToString());
            Assert.Throws<FormatException>(() => BsonObjectId.Parse("102030405060708090a0b0c")); // too short
            Assert.Throws<FormatException>(() => BsonObjectId.Parse("x102030405060708090a0b0c")); // invalid character
            Assert.Throws<FormatException>(() => BsonObjectId.Parse("00102030405060708090a0b0c")); // too long
#pragma warning restore
        }

        [Fact]
        public void TestTryParse()
        {
#pragma warning disable 618
            BsonObjectId objectId1, objectId2;
            Assert.True(BsonObjectId.TryParse("0102030405060708090a0b0c", out objectId1)); // lower case
            Assert.True(BsonObjectId.TryParse("0102030405060708090A0B0C", out objectId2)); // upper case
            Assert.True(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
            Assert.True(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
            Assert.True(objectId1.ToString() == objectId2.ToString());
            Assert.False(BsonObjectId.TryParse("102030405060708090a0b0c", out objectId1)); // too short
            Assert.False(BsonObjectId.TryParse("x102030405060708090a0b0c", out objectId1)); // invalid character
            Assert.False(BsonObjectId.TryParse("00102030405060708090a0b0c", out objectId1)); // too long
            Assert.False(BsonObjectId.TryParse(null, out objectId1)); // should return false not throw ArgumentNullException
#pragma warning restore
        }
    }
}
