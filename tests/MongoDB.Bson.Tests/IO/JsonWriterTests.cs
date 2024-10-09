/* Copyright 2010-present MongoDB Inc.
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
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

        [Theory]
        [ParameterAttributeData]
        public void JsonWriter_should_support_writing_multiple_documents(
            [Range(0, 3)]
            int numberOfDocuments,
            [Values("", " ", "\r\n")]
            string documentSeparator)
        {
            var document = new BsonDocument("x", 1);
            var json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expectedResult = Enumerable.Repeat(json, numberOfDocuments).Aggregate("", (a, j) => a + j + documentSeparator);

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonWriter(stringWriter))
            {
                for (var n = 0; n < numberOfDocuments; n++)
                {
                    jsonWriter.WriteStartDocument();
                    jsonWriter.WriteName("x");
                    jsonWriter.WriteInt32(1);
                    jsonWriter.WriteEndDocument();
                    jsonWriter.BaseTextWriter.Write(documentSeparator);
                }

                var result = stringWriter.ToString();
                result.Should().Be(expectedResult);
            }
        }

        [Fact]
        public void JsonWriter_should_have_relaxed_extended_json_as_default()
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonWriter(stringWriter);

            jsonWriter.Settings.OutputMode.Should().Be(JsonOutputMode.RelaxedExtendedJson);
        }

        [Fact]
        public void TestEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            string json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            string expected = "{ }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestSingleString()
        {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            string json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            string expected = "{ \"abc\" : \"xyz\" }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestIndentedEmptyDocument()
        {
            BsonDocument document = new BsonDocument();
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{ }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestIndentedOneElement()
        {
            BsonDocument document = new BsonDocument() { { "name", "value" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"name\" : \"value\"\r\n}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestIndentedTwoElements()
        {
            BsonDocument document = new BsonDocument() { { "a", "x" }, { "b", "y" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"a\" : \"x\",\r\n  \"b\" : \"y\"\r\n}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestDecimal128Shell()
        {
            var tests = new TestData<Decimal128>[]
            {
                new TestData<Decimal128>(Decimal128.Parse("0"), "NumberDecimal(\"0\")"),
                new TestData<Decimal128>(Decimal128.Parse("0.0"), "NumberDecimal(\"0.0\")"),
                new TestData<Decimal128>(Decimal128.Parse("0.0005"), "NumberDecimal(\"0.0005\")"),
                new TestData<Decimal128>(Decimal128.Parse("0.5"), "NumberDecimal(\"0.5\")"),
                new TestData<Decimal128>(Decimal128.Parse("1.0"), "NumberDecimal(\"1.0\")"),
                new TestData<Decimal128>(Decimal128.Parse("1.5"), "NumberDecimal(\"1.5\")"),
                new TestData<Decimal128>(Decimal128.Parse("1.5E+40"), "NumberDecimal(\"1.5E+40\")"),
                new TestData<Decimal128>(Decimal128.Parse("1.5E-40"), "NumberDecimal(\"1.5E-40\")"),
                new TestData<Decimal128>(Decimal128.Parse("1234567890.1234568E+123"), "NumberDecimal(\"1.2345678901234568E+132\")"),


                new TestData<Decimal128>(Decimal128.Parse("NaN"), "NumberDecimal(\"NaN\")"),
                new TestData<Decimal128>(Decimal128.Parse("-Infinity"), "NumberDecimal(\"-Infinity\")"),
                new TestData<Decimal128>(Decimal128.Parse("Infinity"), "NumberDecimal(\"Infinity\")")
            };
            foreach (var test in tests)
            {
                var json = new BsonDecimal128(test.Value).ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<Decimal128>(json));
            }
        }

        [Fact]
        public void TestDouble()
        {
            var tests = new TestData<double>[]
            {
                new TestData<double>(0.0, "0.0"),
                new TestData<double>(0.0005, "0.00050000000000000001"),
                new TestData<double>(0.5, "0.5"),
                new TestData<double>(1.0, "1.0"),
                new TestData<double>(1.5, "1.5"),
                new TestData<double>(1.5E+40, "1.5000000000000001E+40"),
                new TestData<double>(1.5E-40, "1.5000000000000001E-40"),
                new TestData<double>(1234567890.1234568E+123, "1.2345678901234568E+132"),
                new TestData<double>(double.Epsilon, "4.9406564584124654E-324"),
                new TestData<double>(double.MaxValue, "1.7976931348623157E+308"),
                new TestData<double>(double.MinValue, "-1.7976931348623157E+308"),

                new TestData<double>(-0.0005, "-0.00050000000000000001"),
                new TestData<double>(-0.5, "-0.5"),
                new TestData<double>(-1.0, "-1.0"),
                new TestData<double>(-1.5, "-1.5"),
                new TestData<double>(-1.5E+40, "-1.5000000000000001E+40"),
                new TestData<double>(-1.5E-40, "-1.5000000000000001E-40"),
                new TestData<double>(-1234567890.1234568E+123, "-1.2345678901234568E+132"),
                new TestData<double>(-double.Epsilon, "-4.9406564584124654E-324"),

                new TestData<double>(double.NaN, "NaN"),
                new TestData<double>(double.NegativeInfinity, "-Infinity"),
                new TestData<double>(double.PositiveInfinity, "Infinity")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<double>(json));
            }
        }

        [Fact]
        public void TestDoubleRoundTripOn64BitProcess()
        {
            RequireProcess.Check().Bits(64);
            var value = 0.6822871999174; // see: https://msdn.microsoft.com/en-us/library/dwhawy9k(v=vs.110).aspx#RFormatString

            var json = value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var rehydrated = BsonSerializer.Deserialize<double>(json);

            rehydrated.Should().Be(value);
        }

        [Fact]
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
                var json = test.Value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<long>(json));
            }
        }

        [Fact]
        public void TestEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            string json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            string expected = "{ \"doc\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestIndentedEmbeddedDocument()
        {
            BsonDocument document = new BsonDocument
            {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"doc\" : {\r\n    \"a\" : 1,\r\n    \"b\" : 2\r\n  }\r\n}";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestArray()
        {
            BsonDocument document = new BsonDocument
            {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            string json = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            string expected = "{ \"array\" : [1, 2, 3] }";
            Assert.Equal(expected, json);
        }

        [Fact]
        public void TestBinaryShell()
        {
#pragma warning disable 618
            var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
            var tests = new List<TestData<BsonBinaryData>>
            {
                new TestData<BsonBinaryData>(new byte[] { }, "new BinData(0, \"\")"),
                new TestData<BsonBinaryData>(new byte[] { 1 }, "new BinData(0, \"AQ==\")"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2 }, "new BinData(0, \"AQI=\")"),
                new TestData<BsonBinaryData>(new byte[] { 1, 2, 3 }, "new BinData(0, \"AQID\")")
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<BsonBinaryData>(json));
            }
#pragma warning restore 618
        }

        [Fact]
        public void TestDateTimeShell()
        {
            var utcNow = DateTime.UtcNow;
            var utcNowTruncated = utcNow.AddTicks(-(utcNow.Ticks % 10000));
            var isoDate = string.Format("ISODate(\"{0}\")", utcNowTruncated.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ", CultureInfo.InvariantCulture));
            var tests = new TestData<BsonDateTime>[]
            {
                new TestData<BsonDateTime>(new BsonDateTime(long.MinValue), "new Date(-9223372036854775808)"),
                new TestData<BsonDateTime>(new BsonDateTime(0), "ISODate(\"1970-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(new BsonDateTime(long.MaxValue), "new Date(9223372036854775807)"),
                new TestData<BsonDateTime>(new BsonDateTime(DateTime.MinValue), "ISODate(\"0001-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(new BsonDateTime(BsonConstants.UnixEpoch), "ISODate(\"1970-01-01T00:00:00Z\")"),
                new TestData<BsonDateTime>(new BsonDateTime(utcNowTruncated), isoDate),
                new TestData<BsonDateTime>(new BsonDateTime(DateTime.MaxValue), "ISODate(\"9999-12-31T23:59:59.999Z\")"),
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<BsonDateTime>(json));
            }
        }

        [Fact]
        public void TestJavaScript()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return 1; }\" } }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestJavaScriptWithScope()
        {
            var document = new BsonDocument
            {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } } }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestUuidStandardWhenGuidRepresentationIsUnspecified()
        {
            var guid = new Guid("00112233445566778899aabbccddeeff");
            var guidBytes = GuidConverter.ToBytes(guid, GuidRepresentation.Standard);

            var binary = new BsonBinaryData(guidBytes, BsonBinarySubType.UuidStandard); // GuidRepresentation is Unspecified
            var result = binary.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            result.Should().Be("UUID(\"00112233-4455-6677-8899-aabbccddeeff\")");
        }

        [Fact]
        public void TestMaxKey()
        {
            var document = new BsonDocument
            {
                { "maxkey", BsonMaxKey.Value }
            };
            string expected = "{ \"maxkey\" : MaxKey }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestMinKey()
        {
            var document = new BsonDocument
            {
                { "minkey", BsonMinKey.Value }
            };
            string expected = "{ \"minkey\" : MinKey }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNull()
        {
            var document = new BsonDocument
            {
                { "null", BsonNull.Value }
            };
            string expected = "{ \"null\" : null }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestObjectIdShell()
        {
            var objectId = new ObjectId("4d0ce088e447ad08b4721a37");
            var json = objectId.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            var expected = "ObjectId(\"4d0ce088e447ad08b4721a37\")";
            Assert.Equal(expected, json);
            Assert.Equal(objectId, BsonSerializer.Deserialize<ObjectId>(json));
        }

        [Fact]
        public void TestRegularExpressionShell()
        {
            var tests = new TestData<BsonRegularExpression>[]
            {
                new TestData<BsonRegularExpression>(new BsonRegularExpression(""), "/(?:)/"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a"), "/a/"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a/b"), "/a\\/b/"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a\\b"), "/a\\b/"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a", "i"), "/a/i"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a", "m"), "/a/m"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a", "x"), "/a/x"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a", "s"), "/a/s"),
                new TestData<BsonRegularExpression>(new BsonRegularExpression("a", "imxs"), "/a/imsx"),
            };
            foreach (var test in tests)
            {
                var json = test.Value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<BsonRegularExpression>(json));
            }
        }

        [Fact]
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
                var json = test.Value.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
                Assert.Equal(test.Expected, json);
                Assert.Equal(test.Value, BsonSerializer.Deserialize<string>(json));
            }
        }

        [Fact]
        public void TestSymbol()
        {
            var document = new BsonDocument
            {
                { "symbol", BsonSymbolTable.Lookup("name") }
            };
            string expected = "{ \"symbol\" : { \"$symbol\" : \"name\" } }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestTimestamp()
        {
            var document = new BsonDocument
            {
                { "timestamp", new BsonTimestamp(1, 2) }
            };
            string expected = "{ \"timestamp\" : Timestamp(1, 2) }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestUndefined()
        {
            var document = new BsonDocument
            {
                { "undefined", BsonUndefined.Value }
            };
            string expected = "{ \"undefined\" : undefined }";
            string actual = document.ToJson(writerSettings: new JsonWriterSettings { OutputMode = JsonOutputMode.Shell });
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestUtf16BigEndian()
        {
            var encoding = new UnicodeEncoding(true, true, true);

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, encoding))
                using (var jsonWriter = new JsonWriter(streamWriter, JsonWriterSettings.Defaults))
                {
                    var document = new BsonDocument("x", 1);
                    BsonSerializer.Serialize(jsonWriter, document);
                }

                var bytes = memoryStream.ToArray();
                var bom = new byte[] { 0xfe, 0xff };
                var expected = bom.Concat(encoding.GetBytes("{ \"x\" : 1 }")).ToArray();

                Assert.Equal(expected, bytes);
            }
        }

        [Fact]
        public void TestUtf16LittleEndian()
        {
            var encoding = new UnicodeEncoding(false, true, true);

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, encoding))
                using (var jsonWriter = new JsonWriter(streamWriter, JsonWriterSettings.Defaults))
                {
                    var document = new BsonDocument("x", 1);
                    BsonSerializer.Serialize(jsonWriter, document);
                }

                var bytes = memoryStream.ToArray();
                var bom = new byte[] { 0xff, 0xfe };
                var expected = bom.Concat(encoding.GetBytes("{ \"x\" : 1 }")).ToArray();

                Assert.Equal(expected, bytes);
            }
        }

        [Fact]
        public void TestUtf8()
        {
            var encoding = new UTF8Encoding(true, true); // emit UTF8 identifier

            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream, encoding))
                using (var jsonWriter = new JsonWriter(streamWriter, JsonWriterSettings.Defaults))
                {
                    var document = new BsonDocument("x", 1);
                    BsonSerializer.Serialize(jsonWriter, document);
                }

                var bytes = memoryStream.ToArray();
                var bom = new byte[] { 0xef, 0xbb, 0xbf };
                var expected = bom.Concat(encoding.GetBytes("{ \"x\" : 1 }")).ToArray();

                Assert.Equal(expected, bytes);
            }
        }

        [Fact]
        public void WriteGuid_should_work()
        {
            using var stringWriter = new StringWriter();
            using var writer = new JsonWriter(stringWriter);
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("v");
            writer.WriteGuid(guid);
            writer.WriteEndDocument();
            var result = stringWriter.ToString();

            result.Should().Be("{ \"v\" : UUID(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }");
        }

        [Theory]
        [InlineData(GuidRepresentation.Standard, "UUID")]
        [InlineData(GuidRepresentation.CSharpLegacy, "CSUUID")]
        [InlineData(GuidRepresentation.JavaLegacy, "JUUID")]
        [InlineData(GuidRepresentation.PythonLegacy, "PYUUID")]
        public void WriteGuid_GuidRepresentation_should_work(GuidRepresentation guidRepresentation, string representatinoConstructor)
        {
            using var stringWriter = new StringWriter();
            using var writer = new JsonWriter(stringWriter);
            var guid = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10");

            writer.WriteStartDocument();
            writer.WriteName("v");
            writer.WriteGuid(guid, guidRepresentation);
            writer.WriteEndDocument();
            var result = stringWriter.ToString();

            result.Should().Be($"{{ \"v\" : {representatinoConstructor}(\"01020304-0506-0708-090a-0b0c0d0e0f10\") }}");
        }
    }
}
