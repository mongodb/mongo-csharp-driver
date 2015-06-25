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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using FluentAssertions;
using NUnit.Framework;

namespace MongoDB.Bson.Tests.IO
{
    [TestFixture]
    public class JsonReaderTests
    {
        private IBsonReader _bsonReader;

        [Test]
        public void JsonReader_should_support_reading_multiple_documents(
            [Range(0, 3)]
            int numberOfDocuments)
        {
            var document = new BsonDocument("x", 1);
            var json = document.ToJson();
            var input = Enumerable.Repeat(json, numberOfDocuments).Aggregate("", (a, j) => a + j);
            var expectedResult = Enumerable.Repeat(document, numberOfDocuments);

            using (var jsonReader = new JsonReader(input))
            {
                var result = new List<BsonDocument>();

                while (!jsonReader.IsAtEndOfFile())
                {
                    jsonReader.ReadStartDocument();
                    var name = jsonReader.ReadName();
                    var value = jsonReader.ReadInt32();
                    jsonReader.ReadEndDocument();

                    var resultDocument = new BsonDocument(name, value);
                    result.Add(resultDocument);
                }

                result.Should().Equal(expectedResult);
            }
        }


        [Test]
        public void TestArrayEmpty()
        {
            var json = "[]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Test]
        public void TestArrayOneElement()
        {
            var json = "[1]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Test]
        public void TestArrayTwoElements()
        {
            var json = "[1, 2]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [TestCase("{ $binary : \"AQ==\", $type : 0 }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [TestCase("{ $binary : \"AQ==\", $type : \"0\" }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [TestCase("{ $binary : \"AQ==\", $type : \"00\" }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [TestCase("BinData(0, \"AQ==\")", new byte[] { 1 }, BsonBinarySubType.Binary)]
        public void TestBinaryData(string json, byte[] expectedBytes, BsonBinarySubType expectedSubType)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadBinaryData();

                result.Should().Be(new BsonBinaryData(expectedBytes, expectedSubType));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestBookmark()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                // do everything twice returning to bookmark in between
                var bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadStartDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadStartDocument();

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual("x", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("x", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(1, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual("y", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("y", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(2, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadEndDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadEndDocument();

                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);

            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestBooleanFalse()
        {
            var json = "false";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.AreEqual(false, _bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [Test]
        public void TestBooleanTrue()
        {
            var json = "true";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.AreEqual(true, _bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [TestCase("{ $date : 0 }", 0L)]
        [TestCase("{ $date : -9223372036854775808 }", -9223372036854775808L)]
        [TestCase("{ $date : 9223372036854775807 }", 9223372036854775807L)]
        [TestCase("{ $date : { $numberLong : 0 } }", 0L)]
        [TestCase("{ $date : { $numberLong : -9223372036854775808 } }", -9223372036854775808L)]
        [TestCase("{ $date : { $numberLong : 9223372036854775807 } }", 9223372036854775807L)]
        [TestCase("{ $date : { $numberLong : \"0\" } }", 0L)]
        [TestCase("{ $date : { $numberLong : \"-9223372036854775808\" } }", -9223372036854775808L)]
        [TestCase("{ $date : { $numberLong : \"9223372036854775807\" } }", 9223372036854775807L)]
        [TestCase("{ $date : \"1970-01-01T00:00:00Z\" }", 0L)]
        [TestCase("{ $date : \"0001-01-01T00:00:00Z\" }", -62135596800000L)]
        [TestCase("{ $date : \"1970-01-01T00:00:00.000Z\" }", 0L)]
        [TestCase("{ $date : \"0001-01-01T00:00:00.000Z\" }", -62135596800000L)]
        [TestCase("{ $date : \"9999-12-31T23:59:59.999Z\" }", 253402300799999L)]
        [TestCase("new Date(0)", 0L)]
        [TestCase("new Date(9223372036854775807)", 9223372036854775807L)]
        [TestCase("new Date(-9223372036854775808)", -9223372036854775808L)]
        [TestCase("ISODate(\"1970-01-01T00:00:00Z\")", 0L)]
        [TestCase("ISODate(\"0001-01-01T00:00:00Z\")", -62135596800000L)]
        [TestCase("ISODate(\"1970-01-01T00:00:00.000Z\")", 0L)]
        [TestCase("ISODate(\"0001-01-01T00:00:00.000Z\")", -62135596800000L)]
        [TestCase("ISODate(\"9999-12-31T23:59:59.999Z\")", 253402300799999L)]
        public void TestDateTime(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadDateTime();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestDateTimeMinBson()
        {
            var json = "new Date(-9223372036854775808)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(-9223372036854775808, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Test]
        public void TestDateTimeMaxBson()
        {
            var json = "new Date(9223372036854775807)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(9223372036854775807, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Test]
        public void TestDateTimeShell()
        {
            var json = "ISODate(\"1970-01-01T00:00:00Z\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Shell };
            Assert.AreEqual(json, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDateTimeStrict()
        {
            var json = "{ \"$date\" : 0 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDateTimeStrictIso8601()
        {
            var json = "{ \"$date\" : \"1970-01-01T00:00:00Z\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.AreEqual(0, _bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var expected = "{ \"$date\" : 0 }"; // it's still not ISO8601 on the way out
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(expected, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Test]
        public void TestDocumentEmpty()
        {
            var json = "{ }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentNested()
        {
            var json = "{ \"a\" : { \"x\" : 1 }, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("y", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentOneElement()
        {
            var json = "{ \"x\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDocumentTwoElements()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("x", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("y", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestDouble()
        {
            var json = "1.5";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Double, _bsonReader.ReadBsonType());
                Assert.AreEqual(1.5, _bsonReader.ReadDouble());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<double>(json).ToJson());
        }

        [Test]
        public void TestGuid()
        {
            var guid = new Guid("B5F21E0C2A0D42D6AD03D827008D8AB6");
            var json = "CSUUID(\"B5F21E0C2A0D42D6AD03D827008D8AB6\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Binary, _bsonReader.ReadBsonType());
                var binaryData = _bsonReader.ReadBinaryData();
                Assert.IsTrue(binaryData.Bytes.SequenceEqual(guid.ToByteArray()));
                Assert.AreEqual(BsonBinarySubType.UuidLegacy, binaryData.SubType);
                Assert.AreEqual(GuidRepresentation.CSharpLegacy, binaryData.GuidRepresentation);
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var expected = "CSUUID(\"b5f21e0c-2a0d-42d6-ad03-d827008d8ab6\")";
            Assert.AreEqual(expected, BsonSerializer.Deserialize<Guid>(json).ToJson());
        }

        [Test]
        public void TestHexData()
        {
            var expectedBytes = new byte[] { 0x01, 0x23 };
            var json = "HexData(0, \"123\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Binary, _bsonReader.ReadBsonType());
                var bytes = _bsonReader.ReadBytes();
                Assert.IsTrue(expectedBytes.SequenceEqual(bytes));
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var expectedJson = "new BinData(0, \"ASM=\")";
            Assert.AreEqual(expectedJson, BsonSerializer.Deserialize<byte[]>(json).ToJson());
        }

        [Test]
        public void TestInt32()
        {
            var json = "123";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<int>(json).ToJson());
        }

        [TestCase("Number(123)")]
        [TestCase("NumberInt(123)")]
        public void TestInt32Constructor(string json)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt32());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "123";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<int>(new StringReader(json)).ToJson());
        }

        [TestCase("{ $numberLong: 1 }", 1L)]
        [TestCase("{ $numberLong: -9223372036854775808 }", -9223372036854775808L)]
        [TestCase("{ $numberLong: 9223372036854775807 }", 9223372036854775807L)]
        [TestCase("{ $numberLong: \"1\" }", 1L)]
        [TestCase("{ $numberLong: \"-9223372036854775808\" }", -9223372036854775808L)]
        [TestCase("{ $numberLong: \"9223372036854775807\" }", 9223372036854775807L)]
        [TestCase("NumberLong(1)", 1L)]
        [TestCase("NumberLong(-9223372036854775808)", -9223372036854775808L)]
        [TestCase("NumberLong(9223372036854775807)", 9223372036854775807L)]
        [TestCase("NumberLong(\"1\")", 1L)]
        [TestCase("NumberLong(\"-9223372036854775808\")", -9223372036854775808L)]
        [TestCase("NumberLong(\"9223372036854775807\")", 9223372036854775807L)]
        public void TestInt64(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadInt64();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestInt64ConstructorQuoted()
        {
            var json = "NumberLong(\"123456789012\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123456789012, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Test]
        public void TestInt64ConstructorUnqutoed()
        {
            var json = "NumberLong(123)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Test]
        public void TestIsAtEndOfFileWithTwoArrays()
        {
            var json = "[1,2][1,2]";

            using (var jsonReader = new JsonReader(json))
            {
                var count = 0;
                while (!jsonReader.IsAtEndOfFile())
                {
                    var array = BsonSerializer.Deserialize<BsonArray>(jsonReader);
                    var expected = new BsonArray { 1, 2 };
                    Assert.AreEqual(expected, array);
                    count += 1;
                }
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestIsAtEndOfFileWithTwoDocuments()
        {
            var json = "{x:1}{x:1}";

            using (var jsonReader = new JsonReader(json))
            {
                var count = 0;
                while (!jsonReader.IsAtEndOfFile())
                {
                    var document = BsonSerializer.Deserialize<BsonDocument>(jsonReader);
                    var expected = new BsonDocument("x", 1);
                    Assert.AreEqual(expected, document);
                    count += 1;
                }
                Assert.AreEqual(2, count);
            }
        }

        [Test]
        public void TestInt64ExtendedJson()
        {
            var json = "{ \"$numberLong\" : \"123\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.AreEqual(123, _bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "NumberLong(123)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<long>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestJavaScript()
        {
            string json = "{ \"$code\" : \"function f() { return 1; }\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.JavaScript, _bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return 1; }", _bsonReader.ReadJavaScript());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScript>(json).ToJson());
        }

        [Test]
        public void TestJavaScriptWithScope()
        {
            string json = "{ \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.JavaScriptWithScope, _bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return n; }", _bsonReader.ReadJavaScriptWithScope());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.AreEqual("n", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScriptWithScope>(json).ToJson());
        }

        [TestCase("{ $maxKey : 1 }")]
        [TestCase("MaxKey")]
        public void TestMaxKey(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadMaxKey();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestMaxKeyExtendedJson()
        {
            var json = "{ \"$maxkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMaxKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$maxKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMaxKeyKeyword()
        {
            var json = "MaxKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [TestCase("{ $minKey : 1 }")]
        [TestCase("MinKey")]
        public void TestMinKey(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadMinKey();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestMinKeyExtendedJson()
        {
            var json = "{ \"$minkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$minKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKeyKeyword()
        {
            var json = "MinKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestNestedArray()
        {
            var json = "{ \"a\" : [1, 2] }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Array, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartArray();
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndArray();
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestNestedDocument()
        {
            var json = "{ \"a\" : { \"b\" : 1, \"c\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.AreEqual("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.AreEqual("b", _bsonReader.ReadName());
                Assert.AreEqual(1, _bsonReader.ReadInt32());
                Assert.AreEqual("c", _bsonReader.ReadName());
                Assert.AreEqual(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                _bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Test]
        public void TestNull()
        {
            var json = "null";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Null, _bsonReader.ReadBsonType());
                _bsonReader.ReadNull();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonNull>(json).ToJson());
        }

        [TestCase("{ $oid : \"0102030405060708090a0b0c\" }", "0102030405060708090a0b0c")]
        [TestCase("ObjectId(\"0102030405060708090a0b0c\")", "0102030405060708090a0b0c")]
        public void TestObjectId(string json, string expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadObjectId();

                result.Should().Be(ObjectId.Parse(expectedResult));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestObjectIdShell()
        {
            var json = "ObjectId(\"4d0ce088e447ad08b4721a37\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.AreEqual("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson());
        }

        [Test]
        public void TestObjectIdStrict()
        {
            var json = "{ \"$oid\" : \"4d0ce088e447ad08b4721a37\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.AreEqual("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson(jsonSettings));
        }

        [TestCase("{ $regex : \"abc\", $options : \"i\" }", "abc", "i")]
        [TestCase("{ $regex : \"abc/\", $options : \"i\" }", "abc/", "i")]
        [TestCase("/abc/i", "abc", "i")]
        [TestCase("/abc\\//i", "abc/", "i")]
        public void TestRegularExpression(string json, string expectedPattern, string expectedOptions)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadRegularExpression();

                result.Should().Be(new BsonRegularExpression(expectedPattern, expectedOptions));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestRegularExpressionShell()
        {
            var json = "/pattern/imxs";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.AreEqual("pattern", regex.Pattern);
                Assert.AreEqual("imxs", regex.Options);
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson());
        }

        [Test]
        public void TestRegularExpressionStrict()
        {
            var json = "{ \"$regex\" : \"pattern\", \"$options\" : \"imxs\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.AreEqual("pattern", regex.Pattern);
                Assert.AreEqual("imxs", regex.Options);
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var settings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson(settings));
        }

        [Test]
        public void TestString()
        {
            var json = "\"abc\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.String, _bsonReader.ReadBsonType());
                Assert.AreEqual("abc", _bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Test]
        public void TestStringEmpty()
        {
            var json = "\"\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.String, _bsonReader.ReadBsonType());
                Assert.AreEqual("", _bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Test]
        public void TestSymbol()
        {
            var json = "{ \"$symbol\" : \"symbol\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Symbol, _bsonReader.ReadBsonType());
                Assert.AreEqual("symbol", _bsonReader.ReadSymbol());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonSymbol>(json).ToJson());
        }

        [TestCase("{ $timestamp : { t : 1, i : 2 } }", 0x100000002L)]
        [TestCase("{ $timestamp : { t : -2147483648, i : -2147483648 } }", unchecked((long)0x8000000080000000UL))]
        [TestCase("{ $timestamp : { t : 2147483647, i : 2147483647 } }", 0x7fffffff7fffffff)]
        [TestCase("Timestamp(1, 2)", 0x100000002L)]
        [TestCase("Timestamp(-2147483648, -2147483648)", unchecked((long)0x8000000080000000UL))]
        [TestCase("Timestamp(2147483647, 2147483647)", 0x7fffffff7fffffff)]
        public void TestTimestamp(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadTimestamp();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestTimestampConstructor()
        {
            var json = "Timestamp(1, 2)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestTimestampExtendedJsonNewRepresentation()
        {
            var json = "{ \"$timestamp\" : { \"t\" : 1, \"i\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(1, 2)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestTimestampExtendedJsonOldRepresentation()
        {
            var json = "{ \"$timestamp\" : NumberLong(1234) }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.AreEqual(1234L, _bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(0, 1234)";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [TestCase("{ $undefined : true }")]
        [TestCase("undefined")]
        public void TestUndefined(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadUndefined();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Test]
        public void TestUndefinedExtendedJson()
        {
            var json = "{ \"$undefined\" : true }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "undefined";
            Assert.AreEqual(canonicalJson, BsonSerializer.Deserialize<BsonUndefined>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestUndefinedKeyword()
        {
            var json = "undefined";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.AreEqual(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.AreEqual(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonUndefined>(json).ToJson());
        }

        [Test]
        public void TestUtf16BigEndian()
        {
            var encoding = new UnicodeEncoding(true, false, true);

            var bytes = BsonUtils.ParseHexString("007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16BigEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("feff007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16LittleEndian()
        {
            var encoding = new UnicodeEncoding(false, false, true);

            var bytes = BsonUtils.ParseHexString("7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf16LittleEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("fffe7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf8()
        {
            var encoding = Utf8Encodings.Strict;

            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }

        [Test]
        public void TestUtf8AutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.AreEqual(1, document["x"].AsInt32);
            }
        }
    }
}
