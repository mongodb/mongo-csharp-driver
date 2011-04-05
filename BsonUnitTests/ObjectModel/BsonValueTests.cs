﻿/* Copyright 2010-2011 10gen Inc.
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
    public class BsonValueTests {
        [Test]
        public void TestAsBoolean() {
            BsonValue v = true;
            BsonValue s = "";
            var b = v.AsBoolean;
            Assert.AreEqual(true, b);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBoolean; });
        }

        public void TestAsBsonArray() {
            BsonValue v = new BsonArray { 1, 2 };
            BsonValue s = "";
            var a = v.AsBsonArray;
            Assert.AreEqual(2, a.Count);
            Assert.AreEqual(1, a[0]);
            Assert.AreEqual(2, a[1]);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonArray; });
        }

        public void TestAsBinaryData() {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue s = "";
            var b = v.AsBsonBinaryData;
            Assert.AreEqual(2, b.AsByteArray.Length);
            Assert.AreEqual(1, b.AsByteArray[0]);
            Assert.AreEqual(2, b.AsByteArray[1]);
            Assert.AreEqual(BsonBinarySubType.Binary, b.SubType);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonBinaryData; });
        }

        public void TestAsBsonDocument() {
            BsonValue v = new BsonDocument("x", 1);
            BsonValue s = "";
            var d = v.AsBsonDocument;
            Assert.AreEqual(1, d.ElementCount);
            Assert.AreEqual("x", d.GetElement(0).Name);
            Assert.AreEqual(1, d[0].AsInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonDocument; });
        }

        public void TestAsBsonJavaScript() {
            BsonValue v = new BsonJavaScript("code");
            BsonValue s = "";
            var js = v.AsBsonJavaScript;
            Assert.AreEqual("code", js.Code);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonJavaScript; });
        }

        public void TestAsBsonJavaScriptWithScode() {
            var scope = new BsonDocument("x", 1);
            BsonValue s = "";
            BsonValue v = new BsonJavaScriptWithScope("code", scope);
            var js = v.AsBsonJavaScriptWithScope;
            Assert.AreEqual("code", js.Code);
            Assert.AreEqual(1, js.Scope.ElementCount);
            Assert.AreEqual("name", js.Scope.GetElement(0).Name);
            Assert.AreEqual(1, js.Scope["name"].AsInt32);
            Assert.AreSame(v.AsBsonJavaScript, v.AsBsonJavaScriptWithScope);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonJavaScriptWithScope; });
        }

        public void TestAsBsonMaxKey() {
            BsonValue v = BsonMaxKey.Value;
            BsonValue s = "";
            var m = v.AsBsonMaxKey;
            Assert.AreSame(BsonMaxKey.Value, m);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonMaxKey; });
        }

        public void TestAsBsonMinKey() {
            BsonValue v = BsonMinKey.Value;
            BsonValue s = "";
            var m = v.AsBsonMinKey;
            Assert.AreSame(BsonMinKey.Value, m);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonMinKey; });
        }

        public void TestAsBsonNull() {
            BsonValue v = BsonNull.Value;
            BsonValue s = "";
            var n = v.AsBsonNull;
            Assert.AreSame(BsonNull.Value, n);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonNull; });
        }

        public void TestAsBsonRegularExpression() {
            BsonValue v = new BsonRegularExpression("pattern", "options");
            BsonValue s = "";
            var r = v.AsBsonRegularExpression;
            Assert.AreEqual("pattern", r.Pattern);
            Assert.AreEqual("options", r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonRegularExpression; });
        }

        public void TestAsBsonSymbol() {
            BsonValue v = BsonSymbol.Create("name");
            BsonValue s = "";
            var sym = v.AsBsonSymbol;
            Assert.AreEqual("name", sym.Name);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonSymbol; });
        }

        public void TestAsBsonTimestamp() {
            BsonValue v = new BsonTimestamp(1234);
            BsonValue s = "";
            var ts = v.AsBsonTimestamp;
            Assert.AreEqual(1234, ts.Value);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsBsonTimestamp; });
        }

        public void TestAsByteArray() {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue s = "";
            var a = v.AsByteArray;
            Assert.AreEqual(2, a.Length);
            Assert.AreEqual(1, a[0]);
            Assert.AreEqual(2, a[1]);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsByteArray; });
        }

        public void TestAsDateTime() {
            var utcNow = DateTime.UtcNow;
            BsonValue v = utcNow;
            BsonValue s = "";
            var dt = v.AsDateTime;
            Assert.AreEqual(utcNow, dt);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDateTime; });
        }

        public void TestAsDouble() {
            BsonValue v = 1.5;
            BsonValue s = "";
            var d = v.AsDouble;
            Assert.AreEqual(1.5, d);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsDouble; });
        }

        public void TestAsGuid() {
            var guid = Guid.NewGuid();
            BsonValue v = guid;
            BsonValue s = "";
            var g = v.AsGuid;
            Assert.AreEqual(guid, g);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsGuid; });
        }

        public void TestAsInt32() {
            BsonValue v = 1;
            BsonValue s = "";
            var i = v.AsInt32;
            Assert.AreEqual(1, i);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsInt32; });
        }

        public void TestAsInt64() {
            BsonValue v = 1L;
            BsonValue s = "";
            var i = v.AsInt64;
            Assert.AreEqual(1L, i);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsInt64; });
        }

        [Test]
        public void TestAsNullableBoolean() {
            BsonValue v = true;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(true, v.AsNullableBoolean);
            Assert.AreEqual(null, n.AsNullableBoolean);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableBoolean; });
        }

        [Test]
        public void TestAsNullableDateTime() {
            var utcNow = DateTime.UtcNow;
            BsonValue v = utcNow;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(utcNow, v.AsNullableDateTime);
            Assert.AreEqual(null, n.AsNullableDateTime);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDateTime; });
        }

        [Test]
        public void TestAsNullableDouble() {
            BsonValue v = 1.5;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1.5, v.AsNullableDouble);
            Assert.AreEqual(null, n.AsNullableDouble);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableDouble; });
        }

        [Test]
        public void TestAsNullableGuid() {
            Guid guid = Guid.NewGuid();
            BsonValue v = guid;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(guid, v.AsNullableGuid);
            Assert.AreEqual(null, n.AsNullableGuid);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableGuid; });
        }

        [Test]
        public void TestAsNullableInt32() {
            BsonValue v = 1;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1, v.AsNullableInt32);
            Assert.AreEqual(null, n.AsNullableInt32);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt32; });
        }

        [Test]
        public void TestAsNullableInt64() {
            BsonValue v = 1L;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(1L, v.AsNullableInt64);
            Assert.AreEqual(null, n.AsNullableInt64);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableInt64; });
        }

        [Test]
        public void TestAsNullableObjectId() {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue n = BsonNull.Value;
            BsonValue s = "";
            Assert.AreEqual(objectId, v.AsNullableObjectId);
            Assert.AreEqual(null, n.AsNullableObjectId);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsNullableObjectId; });
        }

        public void TestAsObjectId() {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue s = "";
            var o = v.AsObjectId;
            Assert.AreEqual(objectId, o);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsObjectId; });
        }

        public void TestAsRegex() {
            BsonValue v = new BsonRegularExpression("xyz", "i");
            BsonValue s = "";
            var r = v.AsRegex;
            Assert.AreEqual(RegexOptions.IgnoreCase, r.Options);
            Assert.Throws<InvalidCastException>(() => { var x = s.AsRegex; });
        }

        public void TestAsString() {
            BsonValue v = "Hello";
            BsonValue i = 1;
            var s = v.AsString;
            Assert.AreEqual("Hello", s);
            Assert.Throws<InvalidCastException>(() => { var x = i.AsString; });
        }

        [Test]
        public void TestBsonRegularExpressionConstructors() {
            var regex = BsonRegularExpression.Create("pattern");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("", regex.Options);

            regex = BsonRegularExpression.Create("/pattern/i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create(@"/pattern\/withslash/i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern/withslash", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create("pattern", "i");
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);

            regex = BsonRegularExpression.Create(new Regex("pattern"));
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("", regex.Options);

            regex = BsonRegularExpression.Create(new Regex("pattern", RegexOptions.IgnoreCase));
            Assert.IsInstanceOf<BsonRegularExpression>(regex);
            Assert.AreEqual("pattern", regex.Pattern);
            Assert.AreEqual("i", regex.Options);
        }

        [Test]
        public void TestBsonValueEqualsFalse() {
            BsonValue a = false;
            Assert.IsTrue(a == false);
            Assert.IsFalse(a != false);
            Assert.IsFalse(a == true);
            Assert.IsTrue(a != true);
        }

        [Test]
        public void TestBsonValueEqualsTrue() {
            BsonValue a = true;
            Assert.IsTrue(a == true);
            Assert.IsFalse(a != true);
            Assert.IsFalse(a == false);
            Assert.IsTrue(a != false);
        }

        [Test]
        public void TestBsonValueEqualsDouble() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1.0);
            Assert.IsFalse(a != 1.0);
            Assert.IsFalse(a == 2.0);
            Assert.IsTrue(a != 2.0);
        }

        [Test]
        public void TestBsonValueEqualsInt32() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1);
            Assert.IsFalse(a != 1);
            Assert.IsFalse(a == 2);
            Assert.IsTrue(a != 2);
        }

        [Test]
        public void TestBsonValueEqualsInt64() {
            BsonValue a = 1;
            Assert.IsTrue(a == 1);
            Assert.IsFalse(a != 1);
            Assert.IsFalse(a == 2);
            Assert.IsTrue(a != 2);
        }

        [Test]
        public void TestImplicitConversionFromBoolean() {
            BsonValue v = true;
            Assert.IsInstanceOf<BsonBoolean>(v);
            var b = (BsonBoolean) v;
            Assert.AreEqual(true, b.Value);
        }

        [Test]
        public void TestImplicitConversionFromByteArray() {
            BsonValue v = new byte[] { 1, 2 };
            BsonValue n = (byte[]) null;
            Assert.IsInstanceOf<BsonBinaryData>(v);
            Assert.IsNull(n);
            var b = (BsonBinaryData) v;
            Assert.AreEqual(BsonBinarySubType.Binary, b.SubType);
            Assert.AreEqual(1, b.AsByteArray[0]);
            Assert.AreEqual(2, b.AsByteArray[1]);
        }

        [Test]
        public void TestImplicitConversionFromDateTime() {
            var utcNow = DateTime.UtcNow;
            BsonValue v = utcNow;
            Assert.IsInstanceOf<BsonDateTime>(v);
            var dt = (BsonDateTime) v;
            Assert.AreEqual(utcNow, dt.Value);
        }

        [Test]
        public void TestImplicitConversionFromDouble() {
            BsonValue v = 1.5;
            Assert.IsInstanceOf<BsonDouble>(v);
            var d = (BsonDouble) v;
            Assert.AreEqual(1.5, d.Value);
        }

        [Test]
        public void TestImplicitConversionFromGuid() {
            var guid = Guid.NewGuid();
            BsonValue v = guid;
            Assert.IsInstanceOf<BsonBinaryData>(v);
            var b = (BsonBinaryData) v;
            Assert.IsTrue(guid.ToByteArray().SequenceEqual(b.AsByteArray));
            Assert.AreEqual(BsonBinarySubType.Uuid, b.SubType);
        }

        [Test]
        public void TestImplicitConversionFromInt16Enum() {
            BsonValue v = Int16Enum.A;
            Assert.IsInstanceOf<BsonInt32>(v);
            var n = (BsonInt32) v;
            Assert.AreEqual((int) Int16Enum.A, n.Value);
        }

        [Test]
        public void TestImplicitConversionFromInt32() {
            BsonValue v = 1;
            Assert.IsInstanceOf<BsonInt32>(v);
            var i = (BsonInt32) v;
            Assert.AreEqual(1, i.Value);
        }

        [Test]
        public void TestImplicitConversionFromInt32Enum() {
            BsonValue v = Int32Enum.A;
            Assert.IsInstanceOf<BsonInt32>(v);
            var n = (BsonInt32) v;
            Assert.AreEqual((int) Int32Enum.A, n.Value);
        }

        [Test]
        public void TestImplicitConversionFromInt64() {
            BsonValue v = 1L;
            Assert.IsInstanceOf<BsonInt64>(v);
            var i = (BsonInt64) v;
            Assert.AreEqual(1L, i.Value);
        }

        [Test]
        public void TestImplicitConversionFromInt64Enum() {
            BsonValue v = Int64Enum.A;
            Assert.IsInstanceOf<BsonInt64>(v);
            var n = (BsonInt64) v;
            Assert.AreEqual((int) Int64Enum.A, n.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableBoolean() {
            BsonValue v = (bool?) true;
            BsonValue n = (bool?) null;
            Assert.IsInstanceOf<BsonBoolean>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var b = (BsonBoolean) v;
            Assert.AreEqual(true, b.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableDateTime() {
            var utcNow = DateTime.UtcNow;
            BsonValue v = (DateTime?) utcNow;
            BsonValue n = (DateTime?) null;
            Assert.IsInstanceOf<BsonDateTime>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var dt = (BsonDateTime) v;
            Assert.AreEqual(utcNow, dt.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableDouble() {
            BsonValue v = (double?) 1.5;
            BsonValue n = (double?) null;
            Assert.IsInstanceOf<BsonDouble>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var d = (BsonDouble) v;
            Assert.AreEqual(1.5, d.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableGuid() {
            var guid = Guid.NewGuid();
            BsonValue v = (Guid?) guid;
            BsonValue n = (Guid?) null;
            Assert.IsInstanceOf<BsonBinaryData>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var b = (BsonBinaryData) v;
            Assert.IsTrue(guid.ToByteArray().SequenceEqual(b.AsByteArray));
            Assert.AreEqual(BsonBinarySubType.Uuid, b.SubType);
        }

        [Test]
        public void TestImplicitConversionFromNullableInt32() {
            BsonValue v = (int?) 1;
            BsonValue n = (int?) null;
            Assert.IsInstanceOf<BsonInt32>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var i = (BsonInt32) v;
            Assert.AreEqual(1, i.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableInt64() {
            BsonValue v = (long?) 1L;
            BsonValue n = (long?) null;
            Assert.IsInstanceOf<BsonInt64>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var i = (BsonInt64) v;
            Assert.AreEqual(1, i.Value);
        }

        [Test]
        public void TestImplicitConversionFromNullableObjectId() {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            BsonValue n = (Guid?) null;
            Assert.IsInstanceOf<BsonObjectId>(v);
            Assert.IsInstanceOf<BsonNull>(n);
            var o = (BsonObjectId) v;
            Assert.AreEqual(objectId, o.Value);
        }

        [Test]
        public void TestImplicitConversionFromObjectId() {
            var objectId = ObjectId.GenerateNewId();
            BsonValue v = objectId;
            Assert.IsInstanceOf<BsonObjectId>(v);
            var o = (BsonObjectId) v;
            Assert.AreEqual(objectId, o.Value);
        }

        [Test]
        public void TestImplicitConversionFromRegex() {
            BsonValue v = new Regex("xyz", RegexOptions.IgnoreCase);
            BsonValue n = (Regex) null;
            Assert.IsInstanceOf<BsonRegularExpression>(v);
            Assert.IsNull(n);
            var r = (BsonRegularExpression) v;
            Assert.AreEqual("xyz", r.Pattern);
            Assert.AreEqual("i", r.Options);
        }

        [Test]
        public void TestImplicitConversionFromString() {
            BsonValue v = "xyz";
            BsonValue n = (string) null;
            Assert.IsInstanceOf<BsonString>(v);
            Assert.IsNull(n);
            var s = (BsonString) v;
            Assert.AreEqual("xyz", s.Value);
        }

        #region nested types
        public enum Int16Enum : short {
            A = 1,
            B
        }

        public enum Int32Enum {
            A = 1,
            B
        }

        public enum Int64Enum : long {
            A = 1,
            B
        }
        #endregion
    }
}
