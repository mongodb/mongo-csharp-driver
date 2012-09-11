/* Copyright 2010-2012 10gen Inc.
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
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonObjectIdTests
    {
        [Test]
        public void TestIComparable()
        {
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId();
            Assert.AreEqual(0, objectId1.CompareTo(objectId1));
            Assert.AreEqual(-1, objectId1.CompareTo(objectId2));
            Assert.AreEqual(1, objectId2.CompareTo(objectId1));
            Assert.AreEqual(0, objectId2.CompareTo(objectId2));
        }

        [Test]
        public void TestCompareEqualGeneratedIds()
        {
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030505060708090a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060808090a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("01020304050607080a0a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0d");
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
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId();
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030305060708090a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060808090a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("01020304050607080a0a0b0c");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
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
            var objectId1 = (BsonObjectId)new ObjectId("0102030405060708090a0b0d");
            var objectId2 = (BsonObjectId)new ObjectId("0102030405060708090a0b0c");
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
            var objectId2 = (BsonObjectId)ObjectId.GenerateNewId(); // generate before objectId2
            var objectId1 = (BsonObjectId)ObjectId.GenerateNewId();
            Assert.IsFalse(objectId1 < objectId2);
            Assert.IsFalse(objectId1 <= objectId2);
            Assert.IsTrue(objectId1 != objectId2);
            Assert.IsFalse(objectId1 == objectId2);
            Assert.IsTrue(objectId1 > objectId2);
            Assert.IsTrue(objectId1 >= objectId2);
        }
    }
}
