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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests {
    [TestFixture]
    public class BsonBinaryDataTests {
        [Test]
        public void TestGuidLittleEndian() {
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidByteOrder.LittleEndian);
            var expected = new byte[] { 4, 3, 2, 1, 6, 5, 8, 7, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.IsTrue(expected.SequenceEqual(binaryData.Bytes));
            Assert.AreEqual(BsonBinarySubType.Uuid, binaryData.SubType);
            Assert.AreEqual(GuidByteOrder.LittleEndian, binaryData.GuidByteOrder);
            Assert.AreEqual(guid, binaryData.AsGuid);
            Assert.AreEqual(guid, binaryData.RawValue);
        }

        [Test]
        public void TestGuidBigEndian() {
            var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
            var binaryData = new BsonBinaryData(guid, GuidByteOrder.BigEndian);
            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            Assert.IsTrue(expected.SequenceEqual(binaryData.Bytes));
            Assert.AreEqual(BsonBinarySubType.Uuid, binaryData.SubType);
            Assert.AreEqual(GuidByteOrder.BigEndian, binaryData.GuidByteOrder);
            Assert.AreEqual(guid, binaryData.AsGuid);
            Assert.AreEqual(guid, binaryData.RawValue);
        }
    }
}
