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
using NUnit.Framework;

namespace MongoDB.BsonUnitTests
{
    [TestFixture]
    public class BsonValueEqualsTests
    {
        [Test]
        public void TestBsonArrayEquals()
        {
            var a = new BsonArray { "a", 1 };
            var b = new BsonArray { "a", 1 };
            var c = new BsonArray { "b", 1 };
            var n = (BsonArray)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonBinaryDataEquals()
        {
            var a = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var b = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var c = new BsonBinaryData(new byte[] { 2, 3, 4 });
            var n = (BsonBinaryData)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonBooleanEquals()
        {
            var a = BsonBoolean.False;
            var b = BsonBoolean.False;
            var c = BsonBoolean.True;
            var n = (BsonBoolean)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonDateTimeEquals()
        {
            var a = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 1, DateTimeKind.Utc));
            var b = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 1, DateTimeKind.Utc));
            var c = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 2, DateTimeKind.Utc));
            var n = (BsonDateTime)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonDocumentEquals()
        {
            var a = new BsonDocument("a", 1);
            var b = new BsonDocument("a", 1);
            var c = new BsonDocument("b", 1);
            var n = (BsonDocument)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonDoubleEquals()
        {
            var a = new BsonDouble(1.0);
            var b = new BsonDouble(1.0);
            var c = new BsonInt32(1);
            var d = new BsonInt64(1L);
            var e = new BsonDouble(2.0);
            var f = new BsonInt32(2);
            var g = new BsonInt64(2L);
            var n = (BsonDouble)null;
            var nan = new BsonDouble(double.NaN);

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(a, e));
            Assert.IsFalse(object.Equals(a, f));
            Assert.IsFalse(object.Equals(a, g));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals(nan));
            Assert.IsTrue(nan.Equals(nan));

            Assert.IsTrue(a == b);
            Assert.IsTrue(a == c);
            Assert.IsTrue(a == d);
            Assert.IsFalse(a == e);
            Assert.IsFalse(a == f);
            Assert.IsFalse(a == g);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);
            Assert.IsFalse(a == nan);
            Assert.IsFalse(nan == a);
            Assert.IsFalse(nan == double.NaN);
            Assert.IsFalse(double.NaN == nan);

            Assert.IsFalse(a != b);
            Assert.IsFalse(a != c);
            Assert.IsFalse(a != d);
            Assert.IsTrue(a != e);
            Assert.IsTrue(a != f);
            Assert.IsTrue(a != g);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
            Assert.IsTrue(a != nan);
            Assert.IsTrue(nan != a);
            Assert.IsTrue(nan != double.NaN);
            Assert.IsTrue(double.NaN != nan);
        }

        [Test]
        public void TestBsonDoubleAsBsonValueEquals()
        {
            var a = (BsonValue)new BsonDouble(1.0);
            var b = (BsonValue)new BsonDouble(1.0);
            var c = (BsonValue)new BsonInt32(1);
            var d = (BsonValue)new BsonInt64(1L);
            var e = (BsonValue)new BsonDouble(2.0);
            var f = (BsonValue)new BsonInt32(2);
            var g = (BsonValue)new BsonInt64(2L);
            var n = (BsonValue)null;
            var nan = (BsonValue)new BsonDouble(double.NaN);

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(a, e));
            Assert.IsFalse(object.Equals(a, f));
            Assert.IsFalse(object.Equals(a, g));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));
            Assert.IsFalse(a.Equals(nan));
            Assert.IsTrue(nan.Equals(nan));

            Assert.IsTrue(a == b);
            Assert.IsTrue(a == c);
            Assert.IsTrue(a == d);
            Assert.IsFalse(a == e);
            Assert.IsFalse(a == f);
            Assert.IsFalse(a == g);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);
            Assert.IsFalse(a == nan);
            Assert.IsFalse(nan == a);
            Assert.IsFalse(nan == double.NaN);
            Assert.IsFalse(double.NaN == nan);

            Assert.IsFalse(a != b);
            Assert.IsFalse(a != c);
            Assert.IsFalse(a != d);
            Assert.IsTrue(a != e);
            Assert.IsTrue(a != f);
            Assert.IsTrue(a != g);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
            Assert.IsTrue(a != nan);
            Assert.IsTrue(nan != a);
            Assert.IsTrue(nan != double.NaN);
            Assert.IsTrue(double.NaN != nan);
        }

        [Test]
        public void TestBsonElementEquals()
        {
            var a = new BsonElement("a", 1);
            var b = new BsonElement("a", 1);
            var c = new BsonElement("b", 1);
            var d = new BsonElement("b", 2);
            var n = (BsonElement)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(c, d));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == d);
            Assert.IsFalse(c == d);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
            Assert.IsTrue(c != d);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonInt32Equals()
        {
            var a = new BsonInt32(1);
            var b = new BsonInt32(1);
            var c = new BsonInt64(1L);
            var d = new BsonDouble(1.0);
            var e = new BsonInt32(2);
            var f = new BsonInt64(2L);
            var g = new BsonDouble(2.0);
            var n = (BsonInt32)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(a, e));
            Assert.IsFalse(object.Equals(a, f));
            Assert.IsFalse(object.Equals(a, g));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsTrue(a == c);
            Assert.IsTrue(a == d);
            Assert.IsFalse(a == e);
            Assert.IsFalse(a == f);
            Assert.IsFalse(a == g);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsFalse(a != c);
            Assert.IsFalse(a != d);
            Assert.IsTrue(a != e);
            Assert.IsTrue(a != f);
            Assert.IsTrue(a != g);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonInt64Equals()
        {
            var a = new BsonInt64(1L);
            var b = new BsonInt64(1L);
            var c = new BsonInt32(1);
            var d = new BsonDouble(1.0);
            var e = new BsonInt64(2L);
            var f = new BsonInt32(2);
            var g = new BsonDouble(2.0);
            var n = (BsonInt64)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(a, e));
            Assert.IsFalse(object.Equals(a, f));
            Assert.IsFalse(object.Equals(a, g));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsTrue(a == c);
            Assert.IsTrue(a == d);
            Assert.IsFalse(a == e);
            Assert.IsFalse(a == f);
            Assert.IsFalse(a == g);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsFalse(a != c);
            Assert.IsFalse(a != d);
            Assert.IsTrue(a != e);
            Assert.IsTrue(a != f);
            Assert.IsTrue(a != g);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonJavaScriptEquals()
        {
            var a = new BsonJavaScript("script 1");
            var b = new BsonJavaScript("script 1");
            var c = new BsonJavaScript("script 2");
            var n = (BsonJavaScript)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonJavaScriptWithScopeEquals()
        {
            var a = new BsonJavaScriptWithScope("script 1", new BsonDocument("x", 1));
            var b = new BsonJavaScriptWithScope("script 1", new BsonDocument("x", 1));
            var c = new BsonJavaScriptWithScope("script 2", new BsonDocument("x", 1));
            var d = new BsonJavaScriptWithScope("script 2", new BsonDocument("x", 2));
            var n = (BsonJavaScript)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(c, d));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(b == d);
            Assert.IsFalse(c == d);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(b != d);
            Assert.IsTrue(c != d);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonMaxKeyEquals()
        {
            var a = BsonMaxKey.Value;
            var b = BsonMaxKey.Value;
            var n = (BsonMaxKey)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonMinKeyEquals()
        {
            var a = BsonMinKey.Value;
            var b = BsonMinKey.Value;
            var n = (BsonMinKey)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonNullEquals()
        {
            var a = BsonNull.Value;
            var b = BsonNull.Value;
            var n = (BsonNull)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, BsonMaxKey.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(b == BsonMaxKey.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(b != BsonMaxKey.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonObjectIdEquals()
        {
            var a = new BsonObjectId(ObjectId.GenerateNewId());
            var b = new BsonObjectId(a.Value);
            var c = new BsonObjectId(ObjectId.GenerateNewId());
            var n = (BsonObjectId)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonRegularExpressionEquals()
        {
            var a = new BsonRegularExpression("pattern 1", "options 1");
            var b = new BsonRegularExpression("pattern 1", "options 1");
            var c = new BsonRegularExpression("pattern 2", "options 1");
            var d = new BsonRegularExpression("pattern 2", "options 2");
            var n = (BsonJavaScript)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, d));
            Assert.IsFalse(object.Equals(c, d));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(b == d);
            Assert.IsFalse(c == d);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(b != d);
            Assert.IsTrue(c != d);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonStringEquals()
        {
            var a = new BsonString("string 1");
            var b = new BsonString("string 1");
            var c = new BsonString("string 2");
            var n = (BsonString)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonSymbolEquals()
        {
            var a = BsonSymbolTable.Lookup("symbol 1");
            var b = BsonSymbolTable.Lookup("symbol 1");
            var c = BsonSymbolTable.Lookup("symbol 2");
            var n = (BsonString)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonTimestampEquals()
        {
            var a = new BsonTimestamp(1);
            var b = new BsonTimestamp(1);
            var c = new BsonTimestamp(2);
            var n = (BsonTimestamp)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestBsonUndefinedEquals()
        {
            var a = BsonUndefined.Value;
            var b = BsonUndefined.Value;
            var n = (BsonUndefined)null;

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, BsonNull.Value));
            Assert.IsFalse(a.Equals(n));
            Assert.IsFalse(a.Equals(null));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == BsonNull.Value);
            Assert.IsFalse(a == null);
            Assert.IsFalse(null == a);
            Assert.IsTrue(n == null);
            Assert.IsTrue(null == n);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != BsonNull.Value);
            Assert.IsTrue(a != null);
            Assert.IsTrue(null != a);
            Assert.IsFalse(n != null);
            Assert.IsFalse(null != n);
        }

        [Test]
        public void TestObjectIdEquals()
        {
            var a = ObjectId.GenerateNewId();
            var b = new ObjectId(a.ToByteArray());
            var c = ObjectId.GenerateNewId();

            Assert.IsTrue(object.Equals(a, b));
            Assert.IsFalse(object.Equals(a, c));

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
        }
    }
}
