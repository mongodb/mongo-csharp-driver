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
using Xunit;

namespace MongoDB.Bson.Tests
{
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

        [Fact]
        public void TestMapBoolean()
        {
            var value = true;
            var bsonValue = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value);
            Assert.True(bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.True(bsonBoolean.Value);
        }

        [Fact]
        public void TestMapBsonArray()
        {
            var value = new BsonArray();
            var bsonValue = (BsonArray)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonArray = (BsonArray)BsonTypeMapper.MapToBsonValue(value, BsonType.Array);
            Assert.Same(value, bsonArray);
        }

        [Fact]
        public void TestMapBsonBinaryData()
        {
            var value = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.Same(value, bsonBinary);
        }

        [Fact]
        public void TestMapBsonBoolean()
        {
            var value = BsonBoolean.True;
            var bsonValue = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Same(value, bsonBoolean);
        }

        [Fact]
        public void TestMapBsonDateTime()
        {
            var value = new BsonDateTime(DateTime.UtcNow);
            var bsonValue = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.Same(value, bsonDateTime);
        }

        [Fact]
        public void TestMapBsonDecimal128()
        {
            var value = new BsonDecimal128(1.2M);
            var bsonValue = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Same(value, bsonDecimal128);
        }

        [Fact]
        public void TestMapBsonDocument()
        {
            var value = new BsonDocument();
            var bsonValue = (BsonDocument)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonDocument = (BsonDocument)BsonTypeMapper.MapToBsonValue(value, BsonType.Document);
            Assert.Same(value, bsonDocument);
        }

        [Fact]
        public void TestMapBsonDouble()
        {
            var value = new BsonDouble(1.2);
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Same(value, bsonDouble);
        }

        [Fact]
        public void TestMapBsonInt32()
        {
            var value = new BsonInt32(1);
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Same(value, bsonInt32);
        }

        [Fact]
        public void TestMapBsonInt64()
        {
            var value = new BsonInt64(1L);
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Same(value, bsonInt64);
        }

        [Fact]
        public void TestMapBsonJavaScript()
        {
            var value = new BsonJavaScript("code");
            var bsonValue = (BsonJavaScript)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonJavaScript = (BsonJavaScript)BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScript);
            Assert.Same(value, bsonJavaScript);
            var bsonJavaScriptWithScope = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScriptWithScope);
            Assert.Equal(value.Code, bsonJavaScriptWithScope.Code);
            Assert.Equal(new BsonDocument(), bsonJavaScriptWithScope.Scope);
        }

        [Fact]
        public void TestMapBsonJavaScriptWithScope()
        {
            var value = new BsonJavaScriptWithScope("code", new BsonDocument());
            var bsonValue = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonJavaScriptWithScope = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue(value, BsonType.JavaScriptWithScope);
            Assert.Same(value, bsonJavaScriptWithScope);
        }

        [Fact]
        public void TestMapBsonMaxKey()
        {
            var value = BsonMaxKey.Value;
            var bsonValue = (BsonMaxKey)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.True(bsonBoolean.Value);
            var bsonMaxKey = (BsonMaxKey)BsonTypeMapper.MapToBsonValue(value, BsonType.MaxKey);
            Assert.Same(value, bsonMaxKey);
        }

        [Fact]
        public void TestMapBsonMinKey()
        {
            var value = BsonMinKey.Value;
            var bsonValue = (BsonMinKey)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.True(bsonBoolean.Value);
            var bsonMinKey = (BsonMinKey)BsonTypeMapper.MapToBsonValue(value, BsonType.MinKey);
            Assert.Same(value, bsonMinKey);
        }

        [Fact]
        public void TestMapBsonNull()
        {
            var value = BsonNull.Value;
            var bsonValue = (BsonNull)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(false, bsonBoolean.Value);
            var bsonNull = (BsonNull)BsonTypeMapper.MapToBsonValue(value, BsonType.Null);
            Assert.Same(value, bsonNull);
        }

        [Fact]
        public void TestMapBsonObjectId()
        {
            var value = new BsonObjectId(ObjectId.GenerateNewId());
            var bsonValue = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.Same(value, bsonObjectId);
        }

        [Fact]
        public void TestMapBsonRegularExpression()
        {
            var value = new BsonRegularExpression("pattern", "options");
            var bsonValue = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            Assert.Same(value, bsonRegularExpression);
        }

        [Fact]
        public void TestMapBsonString()
        {
            var value = new BsonString("hello");
            var bsonValue = (BsonString)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonString = (BsonString)BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            Assert.Same(value, bsonString);
        }

        [Fact]
        public void TestMapBsonSymbol()
        {
            var value = BsonSymbolTable.Lookup("symbol");
            var bsonValue = (BsonSymbol)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonSymbol = (BsonSymbol)BsonTypeMapper.MapToBsonValue(value, BsonType.Symbol);
            Assert.Same(value, bsonSymbol);
        }

        [Fact]
        public void TestMapBsonTimestamp()
        {
            var value = new BsonTimestamp(1234L);
            var bsonValue = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.Same(value, bsonTimestamp);
        }

        [Fact]
        public void TestMapBsonUndefined()
        {
            var value = BsonUndefined.Value;
            var bsonValue = (BsonUndefined)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.False(bsonBoolean.Value);
            var bsonUndefined = (BsonUndefined)BsonTypeMapper.MapToBsonValue(value, BsonType.Undefined);
            Assert.Same(value, bsonUndefined);
        }

        [Fact]
        public void TestMapBsonValue()
        {
            var value = BsonValue.Create(1234);
            var bsonValue = (BsonValue)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue);
        }

        [Fact]
        public void TestMapByte()
        {
            var value = (byte)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal((Decimal128)1M, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(1.0, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(1, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(1L, bsonInt64.Value);
        }

        [Fact]
        public void TestMapByteArray()
        {
            var value = ObjectId.GenerateNewId().ToByteArray();
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.Same(value, bsonValue.Bytes);
            Assert.Equal(BsonBinarySubType.Binary, bsonValue.SubType);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.Same(value, bsonBinary.Bytes);
            Assert.Equal(BsonBinarySubType.Binary, bsonBinary.SubType);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.True(value.SequenceEqual(((ObjectId)bsonObjectId).ToByteArray()));
        }

        [Fact]
        public void TestMapByteEnum()
        {
            var value = ByteEnum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((int)(byte)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)(byte)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(byte)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapChar()
        {
            var value = (char)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal((Decimal128)1M, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(1.0, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(1, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(1L, bsonInt64.Value);
        }

        [Fact]
        public void TestMapDateTime()
        {
            var value = DateTime.UtcNow;
            var valueTruncated = value.AddTicks(-(value.Ticks % 10000));
            var bsonValue = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(valueTruncated, bsonValue.ToUniversalTime());
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.Equal(valueTruncated, bsonDateTime.ToUniversalTime());
        }

        [Fact]
        public void TestMapDateTimeOffset()
        {
            var value = DateTimeOffset.UtcNow;
            var valueTruncated = value.AddTicks(-(value.Ticks % 10000));
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue(value, BsonType.DateTime);
            Assert.Equal(valueTruncated.DateTime, bsonDateTime.ToUniversalTime());
        }

        [Fact]
        public void TestMapDecimal()
        {
            var value = 1.2M;
            var bsonValue = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((Decimal128)value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal((Decimal128)value, bsonDecimal128.Value);
        }

        [Fact]
        public void TestMapDecimal128()
        {
            var value = (Decimal128)1.2M;
            var bsonValue = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
        }

        [Fact]
        public void TestMapDouble()
        {
            var value = 1.2;
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal((Decimal128)value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
        }

        [Fact]
        public void TestMapGuid()
        {
            var value = Guid.NewGuid();
            var bsonValue = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value);
            Assert.True(value.ToByteArray().SequenceEqual(bsonValue.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, bsonValue.SubType);
            var bsonBinary = (BsonBinaryData)BsonTypeMapper.MapToBsonValue(value, BsonType.Binary);
            Assert.True(value.ToByteArray().SequenceEqual(bsonBinary.Bytes));
            Assert.Equal(BsonBinarySubType.UuidLegacy, bsonBinary.SubType);
        }

        [Fact]
        public void TestMapInt16()
        {
            var value = (short)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapInt16Enum()
        {
            var value = Int16Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((int)(short)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)(short)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(short)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapInt32()
        {
            var value = 1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapInt32Enum()
        {
            var value = Int32Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((int)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(int)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapInt64()
        {
            var value = 1L;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.Equal(value, bsonTimestamp.Value);
        }

        [Fact]
        public void TestMapInt64Enum()
        {
            var value = Int64Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((long)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapObjectId()
        {
            var value = ObjectId.GenerateNewId(); ;
            var bsonValue = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(value, BsonType.ObjectId);
            Assert.Equal(value, bsonObjectId.Value);
        }

        [Fact]
        public void TestMapRegex()
        {
            var value = new Regex("pattern");
            var bsonValue = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal("pattern", bsonValue.Pattern);
            Assert.Equal("", bsonValue.Options);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(value, BsonType.RegularExpression);
            Assert.Equal("pattern", bsonRegularExpression.Pattern);
            Assert.Equal("", bsonRegularExpression.Options);
        }

        [Fact]
        public void TestMapSByte()
        {
            var value = (sbyte)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapSByteEnum()
        {
            var value = SByteEnum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((int)(sbyte)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)(sbyte)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(sbyte)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapSingle()
        {
            var value = (float)1.2;
            var bsonValue = (BsonDouble)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal((Decimal128)value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
        }

        [Fact]
        public void TestMapString()
        {
            var value = "hello";
            var bsonValue = (BsonString)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue("1", BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDateTime = (BsonDateTime)BsonTypeMapper.MapToBsonValue("2010-01-02", BsonType.DateTime);
            Assert.Equal(new DateTime(2010, 1, 2), bsonDateTime.ToUniversalTime());
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue("1.2", BsonType.Decimal128);
            Assert.Equal((Decimal128)1.2M, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue("1.2", BsonType.Double);
            Assert.Equal(1.2, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue("1", BsonType.Int32);
            Assert.Equal(1, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue("1", BsonType.Int64);
            Assert.Equal(1L, bsonInt64.Value);
            var bsonJavaScript = (BsonJavaScript)BsonTypeMapper.MapToBsonValue("code", BsonType.JavaScript);
            Assert.Equal("code", bsonJavaScript.Code);
            var bsonJavaScriptWithScope = (BsonJavaScriptWithScope)BsonTypeMapper.MapToBsonValue("code", BsonType.JavaScriptWithScope);
            Assert.Equal("code", bsonJavaScriptWithScope.Code);
            Assert.Equal(0, bsonJavaScriptWithScope.Scope.ElementCount);
            var objectId = ObjectId.GenerateNewId();
            var bsonObjectId = (BsonObjectId)BsonTypeMapper.MapToBsonValue(objectId.ToString(), BsonType.ObjectId);
            Assert.Equal(objectId, bsonObjectId.Value);
            var bsonRegularExpression = (BsonRegularExpression)BsonTypeMapper.MapToBsonValue(new Regex("pattern"), BsonType.RegularExpression);
            Assert.Equal("pattern", bsonRegularExpression.Pattern);
            Assert.Equal("", bsonRegularExpression.Options);
            var bsonString = (BsonString)BsonTypeMapper.MapToBsonValue(value, BsonType.String);
            Assert.Equal(value, bsonString.Value);
            var bsonSymbol = (BsonSymbol)BsonTypeMapper.MapToBsonValue("symbol", BsonType.Symbol);
            Assert.Same(BsonSymbolTable.Lookup("symbol"), bsonSymbol);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue("1", BsonType.Timestamp);
            Assert.Equal(1L, bsonTimestamp.Value);
        }

        [Fact]
        public void TestMapUInt16()
        {
            var value = (ushort)1;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal(value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapUInt16Enum()
        {
            var value = UInt16Enum.V;
            var bsonValue = (BsonInt32)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((int)(ushort)value, bsonValue.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)(ushort)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(ushort)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapUInt32()
        {
            var value = (uint)1;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal(value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt32 = (BsonInt32)BsonTypeMapper.MapToBsonValue(value, BsonType.Int32);
            Assert.Equal((int)value, bsonInt32.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal(value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapUInt32Enum()
        {
            var value = UInt32Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((long)(uint)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(uint)value, bsonInt64.Value);
        }

        [Fact]
        public void TestMapUInt64()
        {
            var value = (ulong)1;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((long)value, bsonValue.Value);
            var bsonBoolean = (BsonBoolean)BsonTypeMapper.MapToBsonValue(value, BsonType.Boolean);
            Assert.Equal(true, bsonBoolean.Value);
            var bsonDecimal128 = (BsonDecimal128)BsonTypeMapper.MapToBsonValue(value, BsonType.Decimal128);
            Assert.Equal(value, bsonDecimal128.Value);
            var bsonDouble = (BsonDouble)BsonTypeMapper.MapToBsonValue(value, BsonType.Double);
            Assert.Equal(value, bsonDouble.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)value, bsonInt64.Value);
            var bsonTimestamp = (BsonTimestamp)BsonTypeMapper.MapToBsonValue(value, BsonType.Timestamp);
            Assert.Equal((long)value, bsonTimestamp.Value);
        }

        [Fact]
        public void TestMapUInt64Enum()
        {
            var value = UInt64Enum.V;
            var bsonValue = (BsonInt64)BsonTypeMapper.MapToBsonValue(value);
            Assert.Equal((long)(ulong)value, bsonValue.Value);
            var bsonInt64 = (BsonInt64)BsonTypeMapper.MapToBsonValue(value, BsonType.Int64);
            Assert.Equal((long)(ulong)value, bsonInt64.Value);
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

        [Fact]
        public void TestCustomTypeMapper()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var customDateTime = new CustomDateTime { DateTime = utcNow };
            BsonValue bsonValue;
            Assert.Equal(true, BsonTypeMapper.TryMapToBsonValue(customDateTime, out bsonValue));
            Assert.Equal(utcNowTruncated, bsonValue.ToUniversalTime());
        }
    }
}
