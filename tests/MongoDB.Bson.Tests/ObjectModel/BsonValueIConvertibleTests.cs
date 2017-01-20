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
using System.Globalization;
using MongoDB.Bson;
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonValueIConvertibleTests
    {
        [Fact]
        public void TestBsonArray()
        {
            var value = new BsonArray();
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonBinaryData()
        {
            var value = new BsonBinaryData(new byte[] { 1, 2 });
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonBoolean()
        {
            var value = BsonBoolean.True;
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Equal(true, Convert.ToBoolean(value));
            Assert.Equal(1, Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Equal(1m, Convert.ToDecimal(value));
            Assert.Equal(1.0, Convert.ToDouble(value));
            Assert.Equal(1, Convert.ToInt16(value));
            Assert.Equal(1, Convert.ToInt32(value));
            Assert.Equal(1, Convert.ToInt64(value));
            Assert.Equal(1, Convert.ToSByte(value));
            Assert.Equal(1.0F, Convert.ToSingle(value));
            Assert.Equal("True", Convert.ToString(value));
            Assert.Equal(1, Convert.ToUInt16(value));
            Assert.Equal(1U, Convert.ToUInt32(value));
            Assert.Equal(1UL, Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonDateTime()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2011, 1, 20), DateTimeKind.Utc);
            var value = new BsonDateTime(dateTime);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Equal(dateTime, Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonDocument()
        {
            var value = new BsonDocument();
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonDouble()
        {
            var value = new BsonDouble(1.5);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Equal(true, Convert.ToBoolean(value));
            Assert.Equal(2, Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Equal(1.5m, Convert.ToDecimal(value));
            Assert.Equal(1.5, Convert.ToDouble(value));
            Assert.Equal(2, Convert.ToInt16(value));
            Assert.Equal(2, Convert.ToInt32(value));
            Assert.Equal(2, Convert.ToInt64(value));
            Assert.Equal(2, Convert.ToSByte(value));
            Assert.Equal(1.5F, Convert.ToSingle(value));
            Assert.Equal("1.5", Convert.ToString(value, CultureInfo.InvariantCulture));
            Assert.Equal(2, Convert.ToUInt16(value));
            Assert.Equal(2U, Convert.ToUInt32(value));
            Assert.Equal(2UL, Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonInt32()
        {
            var value = new BsonInt32(1);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Equal(true, Convert.ToBoolean(value));
            Assert.Equal(1, Convert.ToByte(value));
            Assert.Equal(1, Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Equal(1m, Convert.ToDecimal(value));
            Assert.Equal(1.0, Convert.ToDouble(value));
            Assert.Equal(1, Convert.ToInt16(value));
            Assert.Equal(1, Convert.ToInt32(value));
            Assert.Equal(1, Convert.ToInt64(value));
            Assert.Equal(1, Convert.ToSByte(value));
            Assert.Equal(1.0F, Convert.ToSingle(value));
            Assert.Equal("1", Convert.ToString(value));
            Assert.Equal(1, Convert.ToUInt16(value));
            Assert.Equal(1U, Convert.ToUInt32(value));
            Assert.Equal(1UL, Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonInt64()
        {
            var value = new BsonInt64(1);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Equal(true, Convert.ToBoolean(value));
            Assert.Equal(1, Convert.ToByte(value));
            Assert.Equal(1, Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Equal(1m, Convert.ToDecimal(value));
            Assert.Equal(1.0, Convert.ToDouble(value));
            Assert.Equal(1, Convert.ToInt16(value));
            Assert.Equal(1, Convert.ToInt32(value));
            Assert.Equal(1, Convert.ToInt64(value));
            Assert.Equal(1, Convert.ToSByte(value));
            Assert.Equal(1.0F, Convert.ToSingle(value));
            Assert.Equal("1", Convert.ToString(value));
            Assert.Equal(1, Convert.ToUInt16(value));
            Assert.Equal(1U, Convert.ToUInt32(value));
            Assert.Equal(1UL, Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonJavaScript()
        {
            var value = new BsonJavaScript("code");
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonJavaScriptWithScope()
        {
            var scope = new BsonDocument();
            var value = new BsonJavaScriptWithScope("code", scope);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonMaxKey()
        {
            var value = BsonMaxKey.Value;
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonMinKey()
        {
            var value = BsonMinKey.Value;
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonNull()
        {
            var value = BsonNull.Value;
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonObjectId()
        {
            var value = new BsonObjectId(ObjectId.Parse("0102030405060708090a0b0c"));
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Equal("0102030405060708090a0b0c", Convert.ToString(value));
            Assert.Equal("0102030405060708090a0b0c", ((IConvertible)value).ToType(typeof(string), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonRegularExpression()
        {
            var value = new BsonRegularExpression("pattern", "imxs");
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonString()
        {
            var booleanString = new BsonString("true");
            var dateTimeString = new BsonString("2011-01-20");
            var doubleString = new BsonString("1.5");
            var intString = new BsonString("1");
            Assert.Same(booleanString, ((IConvertible)booleanString).ToType(typeof(object), null));
            Assert.Equal(true, Convert.ToBoolean(booleanString));
            Assert.Equal(1, Convert.ToByte(intString));
            Assert.Equal('1', Convert.ToChar(intString));
            Assert.Equal(new DateTime(2011, 1, 20), Convert.ToDateTime(dateTimeString));
            Assert.Equal(1.5m, Convert.ToDecimal(doubleString, CultureInfo.InvariantCulture));
            Assert.Equal(1.5, Convert.ToDouble(doubleString, CultureInfo.InvariantCulture));
            Assert.Equal(1, Convert.ToInt16(intString));
            Assert.Equal(1, Convert.ToInt32(intString));
            Assert.Equal(1, Convert.ToInt64(intString));
            Assert.Equal(1, Convert.ToSByte(intString));
            Assert.Equal(1.5F, Convert.ToSingle(doubleString, CultureInfo.InvariantCulture));
            Assert.Equal("1.5", Convert.ToString(doubleString, CultureInfo.InvariantCulture));
            Assert.Equal(1, Convert.ToUInt16(intString));
            Assert.Equal(1U, Convert.ToUInt32(intString));
            Assert.Equal(1UL, Convert.ToUInt64(intString));
        }

        [Fact]
        public void TestBsonSymbol()
        {
            var value = BsonSymbolTable.Lookup("name");
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Fact]
        public void TestBsonTimestamp()
        {
            var value = new BsonTimestamp(123);
            Assert.Same(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDecimal(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDouble(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToInt64(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToSingle(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToString(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }
    }
}
