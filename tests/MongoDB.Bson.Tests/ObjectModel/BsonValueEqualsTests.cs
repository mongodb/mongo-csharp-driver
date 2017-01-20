/* Copyright 2010-2015 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests
{
    public class BsonValueEqualsTests
    {
        [Fact]
        public void TestBsonArrayEquals()
        {
            var a = new BsonArray { "a", 1 };
            var b = new BsonArray { "a", 1 };
            var c = new BsonArray { "b", 1 };
            var n = (BsonArray)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonBinaryDataEquals()
        {
            var a = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var b = new BsonBinaryData(new byte[] { 1, 2, 3 });
            var c = new BsonBinaryData(new byte[] { 2, 3, 4 });
            var n = (BsonBinaryData)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonBooleanEquals()
        {
            var a = BsonBoolean.False;
            var b = BsonBoolean.False;
            var c = BsonBoolean.True;
            var n = (BsonBoolean)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonDateTimeEquals()
        {
            var a = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 1, DateTimeKind.Utc));
            var b = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 1, DateTimeKind.Utc));
            var c = new BsonDateTime(new DateTime(2011, 10, 12, 15, 13, 2, DateTimeKind.Utc));
            var n = (BsonDateTime)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonDocumentEquals()
        {
            var a = new BsonDocument("a", 1);
            var b = new BsonDocument("a", 1);
            var c = new BsonDocument("b", 1);
            var n = (BsonDocument)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
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

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(a, e));
            Assert.False(object.Equals(a, f));
            Assert.False(object.Equals(a, g));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));
            Assert.False(a.Equals(nan));
            Assert.True(nan.Equals(nan));

            Assert.True(a == b);
            Assert.True(a == c);
            Assert.True(a == d);
            Assert.False(a == e);
            Assert.False(a == f);
            Assert.False(a == g);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);
            Assert.False(a == nan);
            Assert.False(nan == a);
            Assert.False(nan == double.NaN);
            Assert.False(double.NaN == nan);

            Assert.False(a != b);
            Assert.False(a != c);
            Assert.False(a != d);
            Assert.True(a != e);
            Assert.True(a != f);
            Assert.True(a != g);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
            Assert.True(a != nan);
            Assert.True(nan != a);
            Assert.True(nan != double.NaN);
            Assert.True(double.NaN != nan);
        }

        [Fact]
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

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(a, e));
            Assert.False(object.Equals(a, f));
            Assert.False(object.Equals(a, g));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));
            Assert.False(a.Equals(nan));
            Assert.True(nan.Equals(nan));

            Assert.True(a == b);
            Assert.True(a == c);
            Assert.True(a == d);
            Assert.False(a == e);
            Assert.False(a == f);
            Assert.False(a == g);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);
            Assert.False(a == nan);
            Assert.False(nan == a);
            Assert.False(nan == double.NaN);
            Assert.False(double.NaN == nan);

            Assert.False(a != b);
            Assert.False(a != c);
            Assert.False(a != d);
            Assert.True(a != e);
            Assert.True(a != f);
            Assert.True(a != g);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
            Assert.True(a != nan);
            Assert.True(nan != a);
            Assert.True(nan != double.NaN);
            Assert.True(double.NaN != nan);
        }

        [Fact]
        public void TestBsonElementEquals()
        {
            var a = new BsonElement("a", 1);
            var b = new BsonElement("a", 1);
            var c = new BsonElement("b", 1);
            var d = new BsonElement("b", 2);

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(c, d));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == d);
            Assert.False(c == d);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != d);
            Assert.True(c != d);
        }

        [Fact]
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

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(a, e));
            Assert.False(object.Equals(a, f));
            Assert.False(object.Equals(a, g));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.True(a == c);
            Assert.True(a == d);
            Assert.False(a == e);
            Assert.False(a == f);
            Assert.False(a == g);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.False(a != c);
            Assert.False(a != d);
            Assert.True(a != e);
            Assert.True(a != f);
            Assert.True(a != g);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
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

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(a, e));
            Assert.False(object.Equals(a, f));
            Assert.False(object.Equals(a, g));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.True(a == c);
            Assert.True(a == d);
            Assert.False(a == e);
            Assert.False(a == f);
            Assert.False(a == g);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.False(a != c);
            Assert.False(a != d);
            Assert.True(a != e);
            Assert.True(a != f);
            Assert.True(a != g);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonJavaScriptEquals()
        {
            var a = new BsonJavaScript("script 1");
            var b = new BsonJavaScript("script 1");
            var c = new BsonJavaScript("script 2");
            var n = (BsonJavaScript)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonJavaScriptWithScopeEquals()
        {
            var a = new BsonJavaScriptWithScope("script 1", new BsonDocument("x", 1));
            var b = new BsonJavaScriptWithScope("script 1", new BsonDocument("x", 1));
            var c = new BsonJavaScriptWithScope("script 2", new BsonDocument("x", 1));
            var d = new BsonJavaScriptWithScope("script 2", new BsonDocument("x", 2));
            var n = (BsonJavaScript)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(c, d));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(b == d);
            Assert.False(c == d);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(b != d);
            Assert.True(c != d);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonMaxKeyEquals()
        {
            var a = BsonMaxKey.Value;
            var b = BsonMaxKey.Value;
            var n = (BsonMaxKey)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonMinKeyEquals()
        {
            var a = BsonMinKey.Value;
            var b = BsonMinKey.Value;
            var n = (BsonMinKey)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonNullEquals()
        {
            var a = BsonNull.Value;
            var b = BsonNull.Value;
            var n = (BsonNull)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, BsonMaxKey.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(b == BsonMaxKey.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(b != BsonMaxKey.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonObjectIdEquals()
        {
            var a = new BsonObjectId(ObjectId.GenerateNewId());
            var b = new BsonObjectId(a.Value);
            var c = new BsonObjectId(ObjectId.GenerateNewId());
            var n = (BsonObjectId)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonRegularExpressionEquals()
        {
            var a = new BsonRegularExpression("pattern 1", "options 1");
            var b = new BsonRegularExpression("pattern 1", "options 1");
            var c = new BsonRegularExpression("pattern 2", "options 1");
            var d = new BsonRegularExpression("pattern 2", "options 2");
            var n = (BsonJavaScript)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, d));
            Assert.False(object.Equals(c, d));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(b == d);
            Assert.False(c == d);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(b != d);
            Assert.True(c != d);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonStringEquals()
        {
            var a = new BsonString("string 1");
            var b = new BsonString("string 1");
            var c = new BsonString("string 2");
            var n = (BsonString)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonSymbolEquals()
        {
            var a = BsonSymbolTable.Lookup("symbol 1");
            var b = BsonSymbolTable.Lookup("symbol 1");
            var c = BsonSymbolTable.Lookup("symbol 2");
            var n = (BsonString)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonTimestampEquals()
        {
            var a = new BsonTimestamp(1);
            var b = new BsonTimestamp(1);
            var c = new BsonTimestamp(2);
            var n = (BsonTimestamp)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == c);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != c);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestBsonUndefinedEquals()
        {
            var a = BsonUndefined.Value;
            var b = BsonUndefined.Value;
            var n = (BsonUndefined)null;

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, BsonNull.Value));
            Assert.False(a.Equals(n));
            Assert.False(a.Equals(null));

            Assert.True(a == b);
            Assert.False(a == BsonNull.Value);
            Assert.False(a == null);
            Assert.False(null == a);
            Assert.True(n == null);
            Assert.True(null == n);

            Assert.False(a != b);
            Assert.True(a != BsonNull.Value);
            Assert.True(a != null);
            Assert.True(null != a);
            Assert.False(n != null);
            Assert.False(null != n);
        }

        [Fact]
        public void TestObjectIdEquals()
        {
            var a = ObjectId.GenerateNewId();
            var b = new ObjectId(a.ToByteArray());
            var c = ObjectId.GenerateNewId();

            Assert.True(object.Equals(a, b));
            Assert.False(object.Equals(a, c));

            Assert.True(a == b);
            Assert.False(a == c);

            Assert.False(a != b);
            Assert.True(a != c);
        }
    }
}
