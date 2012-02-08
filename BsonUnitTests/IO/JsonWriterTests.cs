﻿/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class JsonWriterTests
    {
        private class TestData<T>
        {
            public T Value;
            public string Expected;
            public TestData(T value, string expected)
            {
                this.Value = value;
                this.Expected = expected;
            }
        }

        [Test]
        public void TestEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            string json = document.ToJson();
            string expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSingleString()
        {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            string json = document.ToJson();
            string expected = "{ \"abc\" : \"xyz\" }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedOneElement()
        {
            BsonDocument document = new BsonDocument() { { "name", "value" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"name\" : \"value\"\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedTwoElements()
        {
            BsonDocument document = new BsonDocument() { { "a", "x" }, { "b", "y" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"a\" : \"x\",\r\n  \"b\" : \"y\"\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestDouble()
        {
            var tests = new TestData<double>[]
            {
                new TestData<double>(0.0, "0.0"),
                new TestData<double>(0.0005, "0.0005"),
                new TestData<double>(0.5, "0.5"),
                new TestData<double>(1.0, "1.0"),
                new TestData<double>(1.5, "1.5"),
                new TestData<double>(1.5E+40, "1.5E+40"),
                new TestData<double>(1.5E-40, "1.5E-40"),
                new TestData<double>(1234567890.1234568E+123, "1.2345678901234568E+132"),
                new TestData<double>(double.Epsilon, "4.94065645841247E-324"),
                new TestData<double>(double.MaxValue, "1.7976931348623157E+308"),
                new TestData<double>(double.MinValue, "-1.7976931348623157E+308"),

                new TestData<double>(-0.0005, "-0.0005"),
                new TestData<double>(-0.5, "-0.5"),
                new TestData<double>(-1.0, "-1.0"),
                new TestData<double>(-1.5, "-1.5"),
                new TestData<double>(-1.5E+40, "-1.5E+40"),
                new TestData<double>(-1.5E-40, "-1.5E-40"),
                new TestData<double>(-1234567890.1234568E+123, "-1.2345678901234568E+132"),
                new TestData<double>(-double.Epsilon, "-4.94065645841247E-324"),

                new TestData<double>(double.NaN, "NaN"),
                new TestData<double>(double.NegativeInfinity, "-Infinity"),
                new TestData<double>(double.PositiveInfinity, "Infinity")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<double>(json));
            }
        }

        [Test]
        public void TestInt64Shell()
        {
            var tests = new TestData<long>[]
            {
                new TestData<long>(long.MinValue, "NumberLong(\"-9223372036854775808\")"),
                new TestData<long>(int.MinValue - 1L, "NumberLong(\"-2147483649\")"),
                new TestData<long>(int.MinValue, "NumberLong(-2147483648)"),
                new TestData<long>(0, "NumberLong(0)"),
                new TestData<long>(int.MaxValue, "NumberLong(2147483647)"),
                new TestData<long>(int.MaxValue + 1L, "NumberLong(\"2147483648\")"),
                new TestData<long>(long.MaxValue, "NumberLong(\"9223372036854775807\")")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<long>(json));
            }
        }

        [Test]
        public void TestInt64Strict()
        {
            var tests = new TestData<long>[]
            {
                new TestData<long>(long.MinValue, "-9223372036854775808"),
                new TestData<long>(int.MinValue - 1L, "-2147483649"),
                new TestData<long>(int.MinValue, "-2147483648"),
                new TestData<long>(0, "0"),
                new TestData<long>(int.MaxValue, "2147483647"),
                new TestData<long>(int.MaxValue + 1L, "2147483648"),
                new TestData<long>(long.MaxValue, "9223372036854775807")
            };
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(jsonSettings);
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<long>(json));
            }
        }

        [Test]
        public void TestEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            string json = document.ToJson();
            string expected = "{ \"doc\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"doc\" : {\r\n    \"a\" : 1,\r\n    \"b\" : 2\r\n  }\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArray()
        {
            BsonDocument document = new BsonDocument
            {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            string json = document.ToJson();
            string expected = "{ \"array\" : [1, 2, 3] }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestBinaryShell()
        {
            var tests = new TestData<BsonBinaryData>[]
            {
                new TestData<BsonBinaryData>(null, "null"),
                new TestData<BsonBinaryData>(new byte[] { }, "new BinData(0, \"\")"),
                new TestData<BsonBinaryData>(new byte[] { 1 }, "new BinData(0, \"AQ==\")"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2 }, "new BinData(0, \"AQI=\")"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2, 3 }, "new BinData(0, \"AQID\")"),
                new TestData<BsonBinaryData>(Guid.Empty, "CSUUID(\"00000000-0000-0000-0000-000000000000\")")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonBinaryData>(json));
            }
        }

        [Test]
        public void TestBinaryStrict()
        {
            var tests = new TestData<BsonBinaryData>[]
            {
                new TestData<BsonBinaryData>(null, "null"),
                new TestData<BsonBinaryData>(new byte[] { }, "{ \"$binary\" : \"\", \"$type\" : \"00\" }"),
                new TestData<BsonBinaryData>(new byte[] { 1 }, "{ \"$binary\" : \"AQ==\", \"$type\" : \"00\" }"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2 }, "{ \"$binary\" : \"AQI=\", \"$type\" : \"00\" }"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2, 3 }, "{ \"$binary\" : \"AQID\", \"$type\" : \"00\" }"),
                new TestData<BsonBinaryData>(Guid.Empty, "{ \"$binary\" : \"AAAAAAAAAAAAAAAAAAAAAA==\", \"$type\" : \"03\" }")
            };
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(jsonSettings);
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonBinaryData>(json));
            }
        }

        [Test]
        public void TestDateTimeShell()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var isoDate = string.Format("ISODate(\"{0}\")", utcNowTruncated.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"));
            var tests = new TestData<BsonDateTime>[]
            {
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MinValue), "new Date(-9223372036854775808)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(0), "ISODate(\"1970-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MaxValue), "new Date(9223372036854775807)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MinValue), "ISODate(\"0001-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(BsonDateTime.Create(BsonConstants.UnixEpoch), "ISODate(\"1970-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(BsonDateTime.Create(utcNowTruncated), isoDate),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MaxValue), "ISODate(\"9999-12-31T23:59:59.999Z\")"),
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonDateTime>(json));
            }
        }

        [Test]
        public void TestDateTimeStrict()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var ms = BsonUtils.ToMillisecondsSinceEpoch(utcNowTruncated);
            var strictDate = string.Format("{{ \"$date\" : {0} }}", ms);
            var tests = new TestData<BsonDateTime>[]
            {
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MinValue), "{ \"$date\" : -9223372036854775808 }"),
                new TestData<BsonDateTime>(BsonDateTime.Create(0), "{ \"$date\" : 0 }"),
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MaxValue), "{ \"$date\" : 9223372036854775807 }"),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MinValue), "{ \"$date\" : -62135596800000 }"),
                new TestData<BsonDateTime>(BsonDateTime.Create(BsonConstants.UnixEpoch), "{ \"$date\" : 0 }"),
                new TestData<BsonDateTime>(BsonDateTime.Create(utcNowTruncated), strictDate),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MaxValue), "{ \"$date\" : 253402300799999 }"),
            };
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(jsonSettings);
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonDateTime>(json));
            }
        }

        [Test]
        public void TestDateTimeTenGen()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var ms = BsonUtils.ToMillisecondsSinceEpoch(utcNowTruncated);
            var tenGenDate = string.Format("new Date({0})", ms);
            var tests = new TestData<BsonDateTime>[]
            {
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MinValue), "new Date(-9223372036854775808)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(0), "new Date(0)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(long.MaxValue), "new Date(9223372036854775807)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MinValue), "new Date(-62135596800000)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(BsonConstants.UnixEpoch), "new Date(0)"),
                new TestData<BsonDateTime>(BsonDateTime.Create(utcNowTruncated), tenGenDate),
                new TestData<BsonDateTime>(BsonDateTime.Create(DateTime.MaxValue), "new Date(253402300799999)"),
            };
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.TenGen };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(jsonSettings);
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonDateTime>(json));
            }
        }

        [Test]
        public void TestJavaScript()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return 1; }\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestJavaScriptWithScope()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGuid()
        {
            var document = new BsonDocument
            {
                { "guid", new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6") }
            };
            string expected = "{ \"guid\" : CSUUID(\"b5f21e0c-2a0d-42d6-ad03-d827008d8ab6\") }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMaxKey()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonMaxKey.Value }
            };
            string expected = "{ \"maxkey\" : { \"$maxkey\" : 1 } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMinKey()
        {
            var document = new BsonDocument
            {
                { "minkey", BsonMinKey.Value }
            };
            string expected = "{ \"minkey\" : { \"$minkey\" : 1 } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNull()
        {
            var document = new BsonDocument
            {
                { "null", BsonNull.Value }
            };
            string expected = "{ \"null\" : null }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestObjectIdShell()
        {
            var objectId = new ObjectId("4d0ce088e447ad08b4721a37");
            var json = objectId.ToJson();
            var expected = "ObjectId(\"4d0ce088e447ad08b4721a37\")";
            Assert.AreEqual(expected, json);
            Assert.AreEqual(objectId, BsonSerializer.Deserialize<ObjectId>(json));
        }

        [Test]
        public void TestObjectIdStrict()
        {
            var objectId = new ObjectId("4d0ce088e447ad08b4721a37");
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            var json = objectId.ToJson(jsonSettings);
            var expected = "{ \"$oid\" : \"4d0ce088e447ad08b4721a37\" }";
            Assert.AreEqual(expected, json);
            Assert.AreEqual(objectId, BsonSerializer.Deserialize<ObjectId>(json));
        }

        [Test]
        public void TestRegularExpressionShell()
        {
            var tests = new TestData<BsonRegularExpression>[]
            {
                new TestData<BsonRegularExpression>(null, "null"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create(""), "/(?:)/"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a"), "/a/"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a/b"), "/a\\/b/"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a\\b"), "/a\\\\b/"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "i"), "/a/i"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "m"), "/a/m"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "x"), "/a/x"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "s"), "/a/s"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "imxs"), "/a/imxs"),
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonRegularExpression>(json));
            }
        }

        [Test]
        public void TestRegularExpressionStrict()
        {
            var tests = new TestData<BsonRegularExpression>[]
            {
                new TestData<BsonRegularExpression>(null, "null"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create(""), "{ \"$regex\" : \"\", \"$options\" : \"\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a"), "{ \"$regex\" : \"a\", \"$options\" : \"\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a/b"), "{ \"$regex\" : \"a/b\", \"$options\" : \"\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a\\b"), "{ \"$regex\" : \"a\\\\b\", \"$options\" : \"\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "i"), "{ \"$regex\" : \"a\", \"$options\" : \"i\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "m"), "{ \"$regex\" : \"a\", \"$options\" : \"m\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "x"), "{ \"$regex\" : \"a\", \"$options\" : \"x\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "s"), "{ \"$regex\" : \"a\", \"$options\" : \"s\" }"),
                new TestData<BsonRegularExpression>(BsonRegularExpression.Create("a", "imxs"), "{ \"$regex\" : \"a\", \"$options\" : \"imxs\" }"),
            };
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(jsonSettings);
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<BsonRegularExpression>(json));
            }
        }

        [Test]
        public void TestString()
        {
            var tests = new TestData<string>[]
            {
                new TestData<string>(null, "null"),
                new TestData<string>("", "\"\""),
                new TestData<string>(" ", "\" \""),
                new TestData<string>("a", "\"a\""),
                new TestData<string>("ab", "\"ab\""),
                new TestData<string>("abc", "\"abc\""),
                new TestData<string>("abc\0def", "\"abc\\u0000def\""),
                new TestData<string>("\'", "\"'\""),
                new TestData<string>("\"", "\"\\\"\""),
                new TestData<string>("\0", "\"\\u0000\""),
                new TestData<string>("\a", "\"\\u0007\""),
                new TestData<string>("\b", "\"\\b\""),
                new TestData<string>("\f", "\"\\f\""),
                new TestData<string>("\n", "\"\\n\""),
                new TestData<string>("\r", "\"\\r\""),
                new TestData<string>("\t", "\"\\t\""),
                new TestData<string>("\v", "\"\\u000b\""),
                new TestData<string>("\u0080", "\"\\u0080\""),
                new TestData<string>("\u0080\u0081", "\"\\u0080\\u0081\""),
                new TestData<string>("\u0080\u0081\u0082", "\"\\u0080\\u0081\\u0082\"")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson();
                Assert.AreEqual(test.Expected, json);
                Assert.AreEqual(test.Value, BsonSerializer.Deserialize<string>(json));
            }
        }

        [Test]
        public void TestSymbol()
        {
            var document = new BsonDocument
            {
                { "symbol", BsonSymbol.Create("name") }
            };
            string expected = "{ \"symbol\" : { \"$symbol\" : \"name\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestTimestamp()
        {
            var document = new BsonDocument
            {
                { "timestamp", new BsonTimestamp(1234567890) }
            };
            string expected = "{ \"timestamp\" : { \"$timestamp\" : NumberLong(1234567890) } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestUndefined()
        {
            var document = new BsonDocument
            {
                { "undefined", BsonUndefined.Value }
            };
            string expected = "{ \"undefined\" : undefined }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }
    }
}
