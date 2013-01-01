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
using System.Linq;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class ObjectIdTests
    {
        [Test]
        public void TestByteArrayConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new ObjectId(bytes);
            Assert.AreEqual(0x01020304, objectId.Timestamp);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.AreEqual("0102030405060708090a0b0c", objectId.ToString());
            Assert.IsTrue(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Test]
        public void TestIntIntShortIntConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new ObjectId(0x01020304, 0x050607, 0x0809, 0x0a0b0c);
            Assert.AreEqual(0x01020304, objectId.Timestamp);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.AreEqual("0102030405060708090a0b0c", objectId.ToString());
            Assert.IsTrue(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Test]
        public void TestDateTimeConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var timestamp = BsonConstants.UnixEpoch.AddSeconds(0x01020304);
            var objectId = new ObjectId(timestamp, 0x050607, 0x0809, 0x0a0b0c);
            Assert.AreEqual(0x01020304, objectId.Timestamp);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.AreEqual("0102030405060708090a0b0c", objectId.ToString());
            Assert.IsTrue(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Test]
        public void TestStringConstructor()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId = new ObjectId("0102030405060708090a0b0c");
            Assert.AreEqual(0x01020304, objectId.Timestamp);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(0x050607, objectId.Machine);
            Assert.AreEqual(0x0809, objectId.Pid);
            Assert.AreEqual(0x0a0b0c, objectId.Increment);
            Assert.AreEqual(BsonConstants.UnixEpoch.AddSeconds(0x01020304), objectId.CreationTime);
            Assert.AreEqual("0102030405060708090a0b0c", objectId.ToString());
            Assert.IsTrue(bytes.SequenceEqual(objectId.ToByteArray()));
        }

        [Test]
        public void TestGenerateNewId()
        {
            // compare against two timestamps in case seconds since epoch changes in middle of test
            var timestamp1 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
            var objectId = ObjectId.GenerateNewId();
            var timestamp2 = (int)Math.Floor((DateTime.UtcNow - BsonConstants.UnixEpoch).TotalSeconds);
            Assert.IsTrue(objectId.Timestamp == timestamp1 || objectId.Timestamp == timestamp2);
            Assert.IsTrue(objectId.Machine != 0);
            Assert.IsTrue(objectId.Pid != 0);
        }

        [Test]
        public void TestGenerateNewIdWithDateTime()
        {
            var timestamp = new DateTime(2011, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var objectId = ObjectId.GenerateNewId(timestamp);
            Assert.IsTrue(objectId.CreationTime == timestamp);
            Assert.IsTrue(objectId.Machine != 0);
            Assert.IsTrue(objectId.Pid != 0);
        }

        [Test]
        public void TestGenerateNewIdWithTimestamp()
        {
            var timestamp = 0x01020304;
            var objectId = ObjectId.GenerateNewId(timestamp);
            Assert.IsTrue(objectId.Timestamp == timestamp);
            Assert.IsTrue(objectId.Machine != 0);
            Assert.IsTrue(objectId.Pid != 0);
        }

        [Test]
        public void TestIComparable()
        {
            var objectId1 = ObjectId.GenerateNewId();
            var objectId2 = ObjectId.GenerateNewId();
            Assert.AreEqual(0, objectId1.CompareTo(objectId1));
            Assert.AreEqual(-1, objectId1.CompareTo(objectId2));
            Assert.AreEqual(1, objectId2.CompareTo(objectId1));
            Assert.AreEqual(0, objectId2.CompareTo(objectId2));
        }

        [Test]
        public void TestCompareEqualGeneratedIds()
        {
            var objectId1 = ObjectId.GenerateNewId();
            var objectId2 = objectId1;
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsFalse(objectId1 != objectId2);
            Assert.IsTrue(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareSmallerTimestamp()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0c");
            var objectId2 = new ObjectId("0102030505060708090a0b0c");
            Assert.IsTrue(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsFalse(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareSmallerMachine()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0c");
            var objectId2 = new ObjectId("0102030405060808090a0b0c");
            Assert.IsTrue(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsFalse(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareSmallerPid()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0c");
            var objectId2 = new ObjectId("01020304050607080a0a0b0c");
            Assert.IsTrue(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsFalse(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareSmallerIncrement()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0c");
            var objectId2 = new ObjectId("0102030405060708090a0b0d");
            Assert.IsTrue(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsFalse(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareSmallerGeneratedId()
        {
            var objectId1 = ObjectId.GenerateNewId();
            var objectId2 = ObjectId.GenerateNewId();
            Assert.IsTrue(objectId1 < objectId2);
            Assert.IsTrue(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsFalse(objectId1 > objectId2);
            Assert.IsFalse(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareLargerTimestamp()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0c");
            var objectId2 = new ObjectId("0102030305060708090a0b0c");
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareLargerMachine()
        {
            var objectId1 = new ObjectId("0102030405060808090a0b0c");
            var objectId2 = new ObjectId("0102030405060708090a0b0c");
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareLargerPid()
        {
            var objectId1 = new ObjectId("01020304050607080a0a0b0c");
            var objectId2 = new ObjectId("0102030405060708090a0b0c");
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareLargerIncrement()
        {
            var objectId1 = new ObjectId("0102030405060708090a0b0d");
            var objectId2 = new ObjectId("0102030405060708090a0b0c");
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestCompareLargerGeneratedId()
        {
            var objectId2 = ObjectId.GenerateNewId(); // generate before objectId2
            var objectId1 = ObjectId.GenerateNewId();
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }

        [Test]
        public void TestParse()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var objectId1 = ObjectId.Parse("0102030405060708090a0b0c"); // lower case
            var objectId2 = ObjectId.Parse("0102030405060708090A0B0C"); // upper case
            Assert.IsTrue(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
            Assert.IsTrue(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
            Assert.IsTrue(objectId1.ToString() == objectId2.ToString());
            Assert.Throws<FormatException>(() => ObjectId.Parse("102030405060708090a0b0c")); // too short
            Assert.Throws<FormatException>(() => ObjectId.Parse("x102030405060708090a0b0c")); // invalid character
            Assert.Throws<FormatException>(() => ObjectId.Parse("00102030405060708090a0b0c")); // too long
        }

        [Test]
        public void TestTryParse()
        {
            byte[] bytes = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            ObjectId objectId1, objectId2;
            Assert.IsTrue(ObjectId.TryParse("0102030405060708090a0b0c", out objectId1)); // lower case
            Assert.IsTrue(ObjectId.TryParse("0102030405060708090A0B0C", out objectId2)); // upper case
            Assert.IsTrue(objectId1.ToByteArray().SequenceEqual(objectId2.ToByteArray()));
            Assert.IsTrue(objectId1.ToString() == "0102030405060708090a0b0c"); // ToString returns lower case
            Assert.IsTrue(objectId1.ToString() == objectId2.ToString());
            Assert.IsFalse(ObjectId.TryParse("102030405060708090a0b0c", out objectId1)); // too short
            Assert.IsFalse(ObjectId.TryParse("x102030405060708090a0b0c", out objectId1)); // invalid character
            Assert.IsFalse(ObjectId.TryParse("00102030405060708090a0b0c", out objectId1)); // too long
            Assert.IsFalse(ObjectId.TryParse(null, out objectId1)); // should return false not throw ArgumentNullException
        }
    }
}
