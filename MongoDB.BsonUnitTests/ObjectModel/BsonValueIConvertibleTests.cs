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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonValueIConvertibleTests
    {
        [Test]
        public void TestBsonArray()
        {
            var value = new BsonArray();
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonBinaryData()
        {
            var value = new BsonBinaryData(new byte[] { 1, 2 });
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonBoolean()
        {
            var value = BsonBoolean.True;
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.AreEqual(true, Convert.ToBoolean(value));
            Assert.AreEqual(1, Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.AreEqual(1m, Convert.ToDecimal(value));
            Assert.AreEqual(1.0, Convert.ToDouble(value));
            Assert.AreEqual(1, Convert.ToInt16(value));
            Assert.AreEqual(1, Convert.ToInt32(value));
            Assert.AreEqual(1, Convert.ToInt64(value));
            Assert.AreEqual(1, Convert.ToSByte(value));
            Assert.AreEqual(1.0F, Convert.ToSingle(value));
            Assert.AreEqual("True", Convert.ToString(value));
            Assert.AreEqual(1, Convert.ToUInt16(value));
            Assert.AreEqual(1, Convert.ToUInt32(value));
            Assert.AreEqual(1, Convert.ToUInt64(value));
        }

        [Test]
        public void TestBsonDateTime()
        {
            var dateTime = DateTime.SpecifyKind(new DateTime(2011, 1, 20), DateTimeKind.Utc);
            var value = new BsonDateTime(dateTime);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToBoolean(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.AreEqual(dateTime, Convert.ToDateTime(value));
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

        [Test]
        public void TestBsonDocument()
        {
            var value = new BsonDocument();
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonDouble()
        {
            var value = new BsonDouble(1.5);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.AreEqual(true, Convert.ToBoolean(value));
            Assert.AreEqual(2, Convert.ToByte(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.AreEqual(1.5m, Convert.ToDecimal(value));
            Assert.AreEqual(1.5, Convert.ToDouble(value));
            Assert.AreEqual(2, Convert.ToInt16(value));
            Assert.AreEqual(2, Convert.ToInt32(value));
            Assert.AreEqual(2, Convert.ToInt64(value));
            Assert.AreEqual(2, Convert.ToSByte(value));
            Assert.AreEqual(1.5F, Convert.ToSingle(value));
            Assert.AreEqual("1.5", Convert.ToString(value, CultureInfo.InvariantCulture));
            Assert.AreEqual(2, Convert.ToUInt16(value));
            Assert.AreEqual(2, Convert.ToUInt32(value));
            Assert.AreEqual(2, Convert.ToUInt64(value));
        }

        [Test]
        public void TestBsonInt32()
        {
            var value = new BsonInt32(1);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.AreEqual(true, Convert.ToBoolean(value));
            Assert.AreEqual(1, Convert.ToByte(value));
            Assert.AreEqual(1, Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.AreEqual(1m, Convert.ToDecimal(value));
            Assert.AreEqual(1.0, Convert.ToDouble(value));
            Assert.AreEqual(1, Convert.ToInt16(value));
            Assert.AreEqual(1, Convert.ToInt32(value));
            Assert.AreEqual(1, Convert.ToInt64(value));
            Assert.AreEqual(1, Convert.ToSByte(value));
            Assert.AreEqual(1.0F, Convert.ToSingle(value));
            Assert.AreEqual("1", Convert.ToString(value));
            Assert.AreEqual(1, Convert.ToUInt16(value));
            Assert.AreEqual(1, Convert.ToUInt32(value));
            Assert.AreEqual(1, Convert.ToUInt64(value));
        }

        [Test]
        public void TestBsonInt64()
        {
            var value = new BsonInt64(1);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
            Assert.AreEqual(true, Convert.ToBoolean(value));
            Assert.AreEqual(1, Convert.ToByte(value));
            Assert.AreEqual(1, Convert.ToChar(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(value));
            Assert.AreEqual(1m, Convert.ToDecimal(value));
            Assert.AreEqual(1.0, Convert.ToDouble(value));
            Assert.AreEqual(1, Convert.ToInt16(value));
            Assert.AreEqual(1, Convert.ToInt32(value));
            Assert.AreEqual(1, Convert.ToInt64(value));
            Assert.AreEqual(1, Convert.ToSByte(value));
            Assert.AreEqual(1.0F, Convert.ToSingle(value));
            Assert.AreEqual("1", Convert.ToString(value));
            Assert.AreEqual(1, Convert.ToUInt16(value));
            Assert.AreEqual(1, Convert.ToUInt32(value));
            Assert.AreEqual(1, Convert.ToUInt64(value));
        }

        [Test]
        public void TestBsonJavaScript()
        {
            var value = new BsonJavaScript("code");
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonJavaScriptWithScope()
        {
            var scope = new BsonDocument();
            var value = new BsonJavaScriptWithScope("code", scope);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonMaxKey()
        {
            var value = BsonMaxKey.Value;
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonMinKey()
        {
            var value = BsonMinKey.Value;
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonNull()
        {
            var value = BsonNull.Value;
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonObjectId()
        {
            var value = new BsonObjectId(ObjectId.Parse("0102030405060708090a0b0c"));
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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
            Assert.AreEqual("0102030405060708090a0b0c", Convert.ToString(value));
            Assert.AreEqual("0102030405060708090a0b0c", ((IConvertible)value).ToType(typeof(string), null));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt16(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt32(value));
            Assert.Throws<InvalidCastException>(() => Convert.ToUInt64(value));
        }

        [Test]
        public void TestBsonRegularExpression()
        {
            var value = new BsonRegularExpression("pattern", "imxs");
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonString()
        {
            var booleanString = new BsonString("true");
            var dateTimeString = new BsonString("2011-01-20");
            var doubleString = new BsonString("1.5");
            var intString = new BsonString("1");
            Assert.AreSame(booleanString, ((IConvertible)booleanString).ToType(typeof(object), null));
            Assert.AreEqual(true, Convert.ToBoolean(booleanString));
            Assert.AreEqual(1, Convert.ToByte(intString));
            Assert.AreEqual('1', Convert.ToChar(intString));
            Assert.AreEqual(new DateTime(2011, 1, 20), Convert.ToDateTime(dateTimeString));
            Assert.AreEqual(1.5m, Convert.ToDecimal(doubleString, CultureInfo.InvariantCulture));
            Assert.AreEqual(1.5, Convert.ToDouble(doubleString, CultureInfo.InvariantCulture));
            Assert.AreEqual(1, Convert.ToInt16(intString));
            Assert.AreEqual(1, Convert.ToInt32(intString));
            Assert.AreEqual(1, Convert.ToInt64(intString));
            Assert.AreEqual(1, Convert.ToSByte(intString));
            Assert.AreEqual(1.5F, Convert.ToSingle(doubleString, CultureInfo.InvariantCulture));
            Assert.AreEqual("1.5", Convert.ToString(doubleString, CultureInfo.InvariantCulture));
            Assert.AreEqual(1, Convert.ToUInt16(intString));
            Assert.AreEqual(1, Convert.ToUInt32(intString));
            Assert.AreEqual(1, Convert.ToUInt64(intString));
        }

        [Test]
        public void TestBsonSymbol()
        {
            var value = BsonSymbolTable.Lookup("name");
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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

        [Test]
        public void TestBsonTimestamp()
        {
            var value = new BsonTimestamp(123);
            Assert.AreSame(value, ((IConvertible)value).ToType(typeof(object), null));
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
