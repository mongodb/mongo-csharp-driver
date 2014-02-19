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
using System.Text.RegularExpressions;
using MongoDB.Bson;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonTypeMapperTests
    {
        private enum ByteEnum : byte
        {
            V = 1
        }

        private enum Int16Enum : short
        {
            V = 1
        }

        private enum Int32Enum : int
        {
            V = 1
        }

        private enum Int64Enum : long
        {
            V = 1
        }

        private enum SByteEnum : sbyte
        {
            V = 1
        }

        private enum UInt16Enum : ushort
        {
            V = 1
        }

        private enum UInt32Enum : uint
        {
            V = 1
        }

        private enum UInt64Enum : ulong
        {
            V = 1
        }

        [Test]
        public void TestMapBoolean()
        {
            var value = true;
            var bsonValue = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value);
            Assert.IsTrue(bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.IsTrue(bsonBoolean.Value);
        }

        [Test]
        public void TestMapBsonArray()
        {
            var value = new BsonArray();
            var bsonValue = (BsonArray)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonArray = (BsonArray)BsonTypeMapper.MapToBsonValue(value, BsonType.Array);
            Assert.AreSame(value, bsonArray);
        }

        [Test]
        public void TestMapBsonBinaryData()
        {
            var value = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.AreSame(value, bsonBinary);
        }

        [Test]
        public void TestMapBsonBoolean()
        {
            var value = BsonBoolean.True;
            var bsonValue = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreSame(value, bsonBoolean);
        }

        [Test]
        public void TestMapBsonDateTime()
        {
            var value = new BsonDateTime(DateTime.UtcNow);
            var bsonValue = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.AreSame(value, bsonDateTime);
        }

        [Test]
        public void TestMapBsonDocument()
        {
            var value = new BsonDocument();
            var bsonValue = (BsonDocument)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonDocument = (BsonDocument)BsonTypeMapper.MapToBsonValue(value, BsonType.Document);
            Assert.AreSame(value, bsonDocument);
        }

        [Test]
        public void TestMapBsonDouble()
        {
            var value = new BsonDouble(1.2);
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreSame(value, bsonDouble);
        }

        [Test]
        public void TestMapBsonInt32()
        {
            var value = new BsonInt32(1);
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreSame(value, bsonInt32);
        }

        [Test]
        public void TestMapBsonInt64()
        {
            var value = new BsonInt64(1L);
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreSame(value, bsonInt64);
        }

        [Test]
        public void TestMapJavaScript()
        {
            var value = new BsonJavaScript("code");
            var bsonValue = (BsonJavaScript)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonJavaScript = (BsonJavaScript)BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScript);
            Assert.AreSame(value, bsonJavaScript);
        }

        [Test]
        public void TestMapJavaScriptWithScope()
        {
            var value = new BsonJavaScriptWithScope("code", new BsonDocument());
            var bsonValue = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonJavaScriptWithScope = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScriptWithScope);
            Assert.AreSame(value, bsonJavaScriptWithScope);
        }

        [Test]
        public void TestMapBsonMaxKey()
        {
            var value = BsonMaxKey.Value;
            var bsonValue = (BsonMaxKey)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonMaxKey = (BsonMaxKey)BsonTypeMapper.MapToBsonValue(value, BsonType.MaxKey);
            Assert.AreSame(value, bsonMaxKey);
        }

        [Test]
        public void TestMapBsonMinKey()
        {
            var value = BsonMinKey.Value;
            var bsonValue = (BsonMinKey)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonMinKey = (BsonMinKey)BsonTypeMapper.MapToBsonValue(value, BsonType.MinKey);
            Assert.AreSame(value, bsonMinKey);
        }

        [Test]
        public void TestMapBsonNull()
        {
            var value = BsonNull.Value;
            var bsonValue = (BsonNull)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(false, bsonBoolean.Value);
            var bsonNull = (BsonNull)BsonTypeMapper.MapToBsonValue(value, BsonType.Null);
            Assert.AreSame(value, bsonNull);
        }

        [Test]
        public void TestMapBsonObjectId()
        {
            var value = new BsonObjectId(ObjectId.GenerateNewId());
            var bsonValue = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.AreSame(value, bsonObjectId);
        }

        [Test]
        public void TestMapBsonRegularExpression()
        {
            var value = new BsonRegularExpression("pattern", "options");
            var bsonValue = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            Assert.AreSame(value, bsonRegularExpression);
        }

        [Test]
        public void TestMapBsonString()
        {
            var value = new BsonString("hello");
            var bsonValue = (BsonString)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonString = (BsonString)BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            Assert.AreSame(value, bsonString);
        }

        [Test]
        public void TestMapBsonSymbol()
        {
            var value = BsonSymbolTable.Lookup("symbol");
            var bsonValue = (BsonSymbol)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonSymbol = (BsonSymbol)BsonTypeMapper.MapToBsonValue(value, BsonType.Symbol);
            Assert.AreSame(value, bsonSymbol);
        }

        [Test]
        public void TestMapBsonTimestamp()
        {
            var value = new BsonTimestamp(1234L);
            var bsonValue = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.AreSame(value, bsonTimestamp);
        }

        [Test]
        public void TestMapBsonValue()
        {
            var value = BsonValue.Create(1234);
            var bsonValue = (BsonValue)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue);
        }

        [Test]
        public void TestMapByte()
        {
            var value = (byte)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(1.0, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(1.0, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(1.0, bsonInt64.Value);
        }

        [Test]
        public void TestMapByteArray()
        {
            var value = ObjectId.GenerateNewId().ToByteArray();
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreSame(value, bsonValue.Bytes);
            Assert.AreEqual(BsonBinarySubType.Binary, bsonValue.SubType);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.AreSame(value, bsonBinary.Bytes);
            Assert.AreEqual(BsonBinarySubType.Binary, bsonBinary.SubType);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.IsTrue(value.SequenceEqual(((ObjectId)bsonObjectId).ToByteArray()));
        }

        [Test]
        public void TestMapByteEnum()
        {
            var value = ByteEnum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int)(byte)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual((int)(byte)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(byte)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapDateTime()
        {
            var value = DateTime.UtcNow;
            var valueTruncated = value.AddTicks(-(value.Ticks % 10000));
            var bsonValue = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(valueTruncated, bsonValue.ToUniversalTime());
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.AreEqual(valueTruncated, bsonDateTime.ToUniversalTime());
        }

        [Test]
        public void TestMapDateTimeOffset()
        {
            var value = DateTimeOffset.UtcNow;
            var valueTruncated = value.AddTicks(-(value.Ticks % 10000));
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.AreEqual(valueTruncated.DateTime, bsonDateTime.ToUniversalTime());
        }

        [Test]
        public void TestMapDouble()
        {
            var value = 1.2;
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
        }

        [Test]
        public void TestMapGuid()
        {
            var value = Guid.NewGuid();
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.IsTrue(value.ToByteArray().SequenceEqual(bsonValue.Bytes));
            Assert.AreEqual(BsonBinarySubType.UuidLegacy, bsonValue.SubType);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.IsTrue(value.ToByteArray().SequenceEqual(bsonBinary.Bytes));
            Assert.AreEqual(BsonBinarySubType.UuidLegacy, bsonBinary.SubType);
        }

        [Test]
        public void TestMapInt16()
        {
            var value = (short)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
        }

        [Test]
        public void TestMapInt16Enum()
        {
            var value = Int16Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int)(short)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual((int)(short)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(short)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapInt32()
        {
            var value = 1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
        }

        [Test]
        public void TestMapInt32Enum()
        {
            var value = Int32Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual((int)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(int)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapInt64()
        {
            var value = 1L;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.AreEqual(value, bsonTimestamp.Value);
        }

        [Test]
        public void TestMapInt64Enum()
        {
            var value = Int64Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapObjectId()
        {
            var value = ObjectId.GenerateNewId(); ;
            var bsonValue = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.AreEqual(value, bsonObjectId.Value);
        }

        [Test]
        public void TestMapRegex()
        {
            var value = new Regex("pattern");
            var bsonValue = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual("pattern", bsonValue.Pattern);
            Assert.AreEqual("", bsonValue.Options);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            Assert.AreEqual("pattern", bsonRegularExpression.Pattern);
            Assert.AreEqual("", bsonRegularExpression.Options);
        }

        [Test]
        public void TestMapSByte()
        {
            var value = (sbyte)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
        }

        [Test]
        public void TestMapSByteEnum()
        {
            var value = SByteEnum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int)(sbyte)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual((int)(sbyte)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(sbyte)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapSingle()
        {
            var value = (float)1.2;
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
        }

        [Test]
        public void TestMapString()
        {
            var value = "hello";
            var bsonValue = (BsonString)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue("1", BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue("2010-01-02", BsonType.DateTime);
            Assert.AreEqual(new DateTime(2010, 1, 2), bsonDateTime.ToUniversalTime());
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue("1.2", BsonType.Double);
            Assert.AreEqual(1.2, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue("1", BsonType.Int32);
            Assert.AreEqual(1, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue("1", BsonType.Int64);
            Assert.AreEqual(1L, bsonInt64.Value);
            var bsonJavaScript = (BsonJavaScript)BsonTypeMapper.MapToBsonValue("code", BsonType.JavaScript);
            Assert.AreEqual("code", bsonJavaScript.Code);
            var bsonJavaScriptWithScope = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue("code", BsonType.JavaScriptWithScope);
            Assert.AreEqual("code", bsonJavaScriptWithScope.Code);
            Assert.AreEqual(0, bsonJavaScriptWithScope.Scope.ElementCount);
            var objectId = ObjectId.GenerateNewId();
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(objectId.ToString(), BsonType.ObjectId);
            Assert.AreEqual(objectId, bsonObjectId.Value);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(new Regex("pattern"), BsonType.RegularExpression);
            Assert.AreEqual("pattern", bsonRegularExpression.Pattern);
            Assert.AreEqual("", bsonRegularExpression.Options);
            var bsonString = (BsonString)BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            Assert.AreEqual(value, bsonString.Value);
            var bsonSymbol = (BsonSymbol)BsonTypeMapper.MapToBsonValue("symbol", BsonType.Symbol);
            Assert.AreSame(BsonSymbolTable.Lookup("symbol"), bsonSymbol);
        }

        [Test]
        public void TestMapUInt16()
        {
            var value = (ushort)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
        }

        [Test]
        public void TestMapUInt16Enum()
        {
            var value = UInt16Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((int)(ushort)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual((int)(ushort)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(ushort)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapUInt32()
        {
            var value = (uint)1;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.AreEqual(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
        }

        [Test]
        public void TestMapUInt32Enum()
        {
            var value = UInt32Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long)(uint)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(uint)value, bsonInt64.Value);
        }

        [Test]
        public void TestMapUInt64()
        {
            var value = (ulong)1;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.AreEqual(true, bsonBoolean.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.AreEqual(value, bsonDouble.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual(value, bsonInt64.Value);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.AreEqual(value, bsonTimestamp.Value);
        }

        [Test]
        public void TestMapUInt64Enum()
        {
            var value = UInt64Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.AreEqual((long)(ulong)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.AreEqual((long)(ulong)value, bsonInt64.Value);
        }

        // used by TestCustomTypeMapper
        public struct CustomDateTime
        {
            static CustomDateTime()
            {
                BsonTypeMapper.RegisterCustomTypeMapper(typeof(CustomDateTime), new CustomDateTimeMapper());
            }

            public DateTime DateTime { get; set; } // note: static constructor doesn't get called if this is a field instead of a property!?
        }

        public class CustomDateTimeMapper : ICustomBsonTypeMapper
        {
            public bool TryMapToBsonValue(object value, out BsonValue bsonValue)
            {
                bsonValue = new BsonDateTime(((CustomDateTime)value).DateTime);
                return true;
            }
        }

        [Test]
        public void TestCustomTypeMapper()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var customDateTime = new CustomDateTime { DateTime = utcNow };
            BsonValue bsonValue;
            Assert.AreEqual(true, BsonTypeMapper.TryMapToBsonValue(customDateTime, out bsonValue));
            Assert.AreEqual(utcNowTruncated, bsonValue.ToUniversalTime());
        }
    }
}
