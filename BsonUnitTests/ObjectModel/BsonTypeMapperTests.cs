/* Copyright 2010 10gen Inc.
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
using System.Text.RegularExpressions;
using NUnit.Framework;

using MongoDB.Bson;

namespace MongoDB.BsonUnitTests {
    [TestFixture]
    public class BsonTypeMapperTests {
        private enum ByteEnum : byte {
            V = 1
        }

        private enum Int16Enum : short {
            V = 1
        }

        private enum Int32Enum : int {
            V = 1
        }

        private enum Int64Enum : long {
            V = 1
        }

        private enum SByteEnum : sbyte {
            V = 1
        }

        private enum UInt16Enum : ushort {
            V = 1
        }

        private enum UInt32Enum : uint {
            V = 1
        }

        private enum UInt64Enum : ulong {
            V = 1
        }

        [Test]
        public void TestMapBoolean() {
            var value = true;
            var bsonValue = (BsonBoolean) BsonTypeMapper.MapToBsonValue(value);
            Assert.IsTrue(bsonValue.Value);
        }

        [Test]
        public void TestMapBsonArray() {
            var value = new BsonArray();
            var bsonValue = (BsonArray) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonBinaryData() {
            var value = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var bsonValue = (BsonBinaryData) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonBoolean() {
            var value = BsonBoolean.True;
            var bsonValue = (BsonBoolean) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonDateTime() {
            var value = new BsonDateTime(DateTime.UtcNow);
            var bsonValue = (BsonDateTime) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonDocument() {
            var value = new BsonDocument();
            var bsonValue = (BsonDocument) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonDouble() {
            var value = new BsonDouble(1.2);
            var bsonValue = (BsonDouble) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonInt32() {
            var value = new BsonInt32(1);
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonInt64() {
            var value = new BsonInt64(1L);
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapJavaScript() {
            var value = new BsonJavaScript("code");
            var bsonValue = (BsonJavaScript) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapJavaScriptWithScope() {
            var value = new BsonJavaScriptWithScope("code", new BsonDocument());
            var bsonValue = (BsonJavaScriptWithScope) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonMaxKey() {
            var value = BsonMaxKey.Value;
            var bsonValue = (BsonMaxKey) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonMinKey() {
            var value = BsonMinKey.Value;
            var bsonValue = (BsonMinKey) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonNull() {
            var value = BsonNull.Value;
            var bsonValue = (BsonNull) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonObjectId() {
            var value = BsonObjectId.GenerateNewId();
            var bsonValue = (BsonObjectId) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonRegularExpression() {
            var value = new BsonRegularExpression("pattern", "options");
            var bsonValue = (BsonRegularExpression) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonString() {
            var value = new BsonString("hello");
            var bsonValue = (BsonString) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonSymbol() {
            var value = BsonSymbol.Create("symbol");
            var bsonValue = (BsonSymbol) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonTimestamp() {
            var value = new BsonTimestamp(1234L);
            var bsonValue = (BsonTimestamp) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapBsonValue() {
            var value = BsonValue.Create(1234);
            var bsonValue = (BsonValue) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapByte() {
            var value = (byte) 1;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapByteArray() {
            var value = new byte[] { 1, 2, 3 };
            var bsonValue = (BsonBinaryData) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue.Bytes);
            Assert.AreEqual(BsonBinarySubType.Binary, bsonValue.SubType);
        }

        [Test]
        public void TestMapByteEnum() {
            var value = ByteEnum.V;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int) (byte) value, bsonValue.Value);
        }

        [Test]
        public void TestMapDateTime() {
            var value = DateTime.UtcNow;
            var bsonValue = (BsonDateTime) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapDouble() {
            var value = 1.2;
            var bsonValue = (BsonDouble) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapGuid() {
            var value = Guid.NewGuid();
            var bsonValue = (BsonBinaryData) BsonTypeMapper.MapToBsonValue(value);
            Assert.IsTrue(value.ToByteArray().SequenceEqual(bsonValue.Bytes));
            Assert.AreEqual(BsonBinarySubType.Uuid, bsonValue.SubType);
        }

        [Test]
        public void TestMapInt16() {
            var value = (short) 1;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapInt16Enum() {
            var value = Int16Enum.V;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int) (short) value, bsonValue.Value);
        }

        [Test]
        public void TestMapInt32() {
            var value = 1;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapInt32Enum() {
            var value = Int32Enum.V;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int) value, bsonValue.Value);
        }

        [Test]
        public void TestMapInt64() {
            var value = 1L;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapInt64Enum() {
            var value = Int64Enum.V;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long) value, bsonValue.Value);
        }

        [Test]
        public void TestMapObjectId() {
            var value = ObjectId.GenerateNewId(); ;
            var bsonValue = (BsonObjectId) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapRegex() {
            var value = new Regex("pattern");
            var bsonValue = (BsonRegularExpression) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual("pattern", bsonValue.Pattern);
            Assert.AreEqual("", bsonValue.Options);
        }

        [Test]
        public void TestMapSByte() {
            var value = (sbyte) 1;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapSByteEnum() {
            var value = SByteEnum.V;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int) (sbyte) value, bsonValue.Value);
        }

        [Test]
        public void TestMapSingle() {
            var value = (float) 1.2;
            var bsonValue = (BsonDouble) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapString() {
            var value = "hello";
            var bsonValue = (BsonString) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt16() {
            var value = (ushort) 1;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt16Enum() {
            var value = UInt16Enum.V;
            var bsonValue = (BsonInt32) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int) (ushort) value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt32() {
            var value = (uint) 1;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt32Enum() {
            var value = UInt32Enum.V;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long) (uint) value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt64() {
            var value = (ulong) 1;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
        }

        [Test]
        public void TestMapUInt64Enum() {
            var value = UInt64Enum.V;
            var bsonValue = (BsonInt64) BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long) (ulong) value, bsonValue.Value);
        }
    }
}
