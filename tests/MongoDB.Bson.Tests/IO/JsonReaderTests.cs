/* Copyright 2010-2016 MongoDB Inc.
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
using Xunit;
using MongoDB.Bson.TestHelpers.XunitExtensions;

namespace MongoDB.Bson.Tests.IO
{
    public class JsonReaderTests
    {
        private IBsonReader _bsonReader;

        [Theory]
        [ParameterAttributeData]
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


        [Fact]
        public void TestArrayEmpty()
        {
            var json = "[]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Fact]
        public void TestArrayOneElement()
        {
            var json = "[1]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Fact]
        public void TestArrayTwoElements()
        {
            var json = "[1, 2]";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Array, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartArray();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal(2, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndArray();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonArray>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $binary : \"AQ==\", $type : 0 }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [InlineData("{ $binary : \"AQ==\", $type : \"0\" }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [InlineData("{ $binary : \"AQ==\", $type : \"00\" }", new byte[] { 1 }, BsonBinarySubType.Binary)]
        [InlineData("BinData(0, \"AQ==\")", new byte[] { 1 }, BsonBinarySubType.Binary)]
        public void TestBinaryData(string json, byte[] expectedBytes, BsonBinarySubType expectedSubType)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadBinaryData();

                result.Should().Be(new BsonBinaryData(expectedBytes, expectedSubType));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestBookmark()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                // do everything twice returning to bookmark in between
                var bookmark = _bsonReader.GetBookmark();
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadStartDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadStartDocument();

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal("x", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal("x", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal(1, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(1, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal("y", _bsonReader.ReadName());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal("y", _bsonReader.ReadName());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal(2, _bsonReader.ReadInt32());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(2, _bsonReader.ReadInt32());

                bookmark = _bsonReader.GetBookmark();
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReturnToBookmark(bookmark);
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());

                bookmark = _bsonReader.GetBookmark();
                _bsonReader.ReadEndDocument();
                _bsonReader.ReturnToBookmark(bookmark);
                _bsonReader.ReadEndDocument();

                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);

            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestBooleanFalse()
        {
            var json = "false";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.Equal(false, _bsonReader.ReadBoolean());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [Fact]
        public void TestBooleanTrue()
        {
            var json = "true";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Boolean, _bsonReader.ReadBsonType());
                Assert.Equal(true, _bsonReader.ReadBoolean());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<bool>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $date : 0 }", 0L)]
        [InlineData("{ $date : -9223372036854775808 }", -9223372036854775808L)]
        [InlineData("{ $date : 9223372036854775807 }", 9223372036854775807L)]
        [InlineData("{ $date : { $numberLong : 0 } }", 0L)]
        [InlineData("{ $date : { $numberLong : -9223372036854775808 } }", -9223372036854775808L)]
        [InlineData("{ $date : { $numberLong : 9223372036854775807 } }", 9223372036854775807L)]
        [InlineData("{ $date : { $numberLong : \"0\" } }", 0L)]
        [InlineData("{ $date : { $numberLong : \"-9223372036854775808\" } }", -9223372036854775808L)]
        [InlineData("{ $date : { $numberLong : \"9223372036854775807\" } }", 9223372036854775807L)]
        [InlineData("{ $date : \"1970-01-01T00:00:00Z\" }", 0L)]
        [InlineData("{ $date : \"0001-01-01T00:00:00Z\" }", -62135596800000L)]
        [InlineData("{ $date : \"1970-01-01T00:00:00.000Z\" }", 0L)]
        [InlineData("{ $date : \"0001-01-01T00:00:00.000Z\" }", -62135596800000L)]
        [InlineData("{ $date : \"9999-12-31T23:59:59.999Z\" }", 253402300799999L)]
        [InlineData("new Date(0)", 0L)]
        [InlineData("new Date(9223372036854775807)", 9223372036854775807L)]
        [InlineData("new Date(-9223372036854775808)", -9223372036854775808L)]
        [InlineData("ISODate(\"1970-01-01T00:00:00Z\")", 0L)]
        [InlineData("ISODate(\"0001-01-01T00:00:00Z\")", -62135596800000L)]
        [InlineData("ISODate(\"1970-01-01T00:00:00.000Z\")", 0L)]
        [InlineData("ISODate(\"0001-01-01T00:00:00.000Z\")", -62135596800000L)]
        [InlineData("ISODate(\"9999-12-31T23:59:59.999Z\")", 253402300799999L)]
        public void TestDateTime(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadDateTime();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestDateTimeMinBson()
        {
            var json = "new Date(-9223372036854775808)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.Equal(-9223372036854775808, _bsonReader.ReadDateTime());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Fact]
        public void TestDateTimeMaxBson()
        {
            var json = "new Date(9223372036854775807)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.Equal(9223372036854775807, _bsonReader.ReadDateTime());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDateTime>(json).ToJson());
        }

        [Theory]
        [InlineData("ISODate(\"1970\")", 0L, "ISODate(\"1970-01-01T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01\")", 0L, "ISODate(\"1970-01-01T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02.003\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"1970-01-02T00Z\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01Z\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02Z\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02.003Z\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"1970-01-02T00+00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01+00\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02+00\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02.003+00\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"1970-01-02T00+00:00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01+00:00\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02+00:00\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"1970-01-02T00:01:02.003+00:00\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"19700102\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"19700102T00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"19700102T0001\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"19700102T000102\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"19700102T000102.003\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"19700102T00Z\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"19700102T0001Z\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"19700102T000102Z\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"19700102T000102.003Z\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"19700102T00+00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"19700102T0001+00\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"19700102T000102+00\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"19700102T000102.003+00\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        [InlineData("ISODate(\"19700102T00+00:00\")", 86400000L, "ISODate(\"1970-01-02T00:00:00Z\")")]
        [InlineData("ISODate(\"19700102T0001+00:00\")", 86460000L, "ISODate(\"1970-01-02T00:01:00Z\")")]
        [InlineData("ISODate(\"19700102T000102+00:00\")", 86462000L, "ISODate(\"1970-01-02T00:01:02Z\")")]
        [InlineData("ISODate(\"19700102T000102.003+00:00\")", 86462003L, "ISODate(\"1970-01-02T00:01:02.003Z\")")]
        public void TestDateTimeShell(string json, long expectedResult, string canonicalJson)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.Equal(expectedResult, _bsonReader.ReadDateTime());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Shell };
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Fact]
        public void TestDateTimeStrict()
        {
            var json = "{ \"$date\" : 0 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.Equal(0, _bsonReader.ReadDateTime());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.Equal(json, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Fact]
        public void TestDateTimeStrictIso8601()
        {
            var json = "{ \"$date\" : \"1970-01-01T00:00:00Z\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.DateTime, _bsonReader.ReadBsonType());
                Assert.Equal(0, _bsonReader.ReadDateTime());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var expected = "{ \"$date\" : 0 }"; // it's still not ISO8601 on the way out
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.Equal(expected, BsonSerializer.Deserialize<DateTime>(json).ToJson(jsonSettings));
        }

        [Theory]
        [InlineData("NumberDecimal(1)", "1", "NumberDecimal(\"1\")")]
        [InlineData("NumberDecimal(2147483648)", "2147483648", "NumberDecimal(\"2147483648\")")]
        [InlineData("NumberDecimal(\"1.5\")", "1.5", "NumberDecimal(\"1.5\")")]
        public void TestDecimal128Constructor(string json, string expectedValueString, string expectedJson)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Decimal128, _bsonReader.ReadBsonType());
                Assert.Equal(Decimal128.Parse(expectedValueString), _bsonReader.ReadDecimal128());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(expectedJson, BsonSerializer.Deserialize<BsonDecimal128>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $numberDecimal : 1 }", "1", "NumberDecimal(\"1\")")]
        [InlineData("{ $numberDecimal : 2147483648 })", "2147483648", "NumberDecimal(\"2147483648\")")]
        [InlineData("{ $numberDecimal : \"1.5\" }", "1.5", "NumberDecimal(\"1.5\")")]
        public void TestDecimal128ExtendedJson(string json, string expectedValueString, string expectedJson)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Decimal128, _bsonReader.ReadBsonType());
                Assert.Equal(Decimal128.Parse(expectedValueString), _bsonReader.ReadDecimal128());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(expectedJson, BsonSerializer.Deserialize<BsonDecimal128>(json).ToJson());
        }

        [Fact]
        public void TestDocumentEmpty()
        {
            var json = "{ }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestDocumentNested()
        {
            var json = "{ \"a\" : { \"x\" : 1 }, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.Equal("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("x", _bsonReader.ReadName());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("y", _bsonReader.ReadName());
                Assert.Equal(2, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestDocumentOneElement()
        {
            var json = "{ \"x\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("x", _bsonReader.ReadName());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestDocumentTwoElements()
        {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("x", _bsonReader.ReadName());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("y", _bsonReader.ReadName());
                Assert.Equal(2, _bsonReader.ReadInt32());
                Assert.Equal(BsonType.EndOfDocument, _bsonReader.ReadBsonType());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestDouble()
        {
            var json = "1.5";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Double, _bsonReader.ReadBsonType());
                Assert.Equal(1.5, _bsonReader.ReadDouble());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<double>(json).ToJson());
        }

        [Fact]
        public void TestGuid()
        {
            var guid = new Guid("B5F21E0C2A0D42D6AD03D827008D8AB6");
            var json = "CSUUID(\"B5F21E0C2A0D42D6AD03D827008D8AB6\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Binary, _bsonReader.ReadBsonType());
                var binaryData = _bsonReader.ReadBinaryData();
                Assert.True(binaryData.Bytes.SequenceEqual(guid.ToByteArray()));
                Assert.Equal(BsonBinarySubType.UuidLegacy, binaryData.SubType);
                Assert.Equal(GuidRepresentation.CSharpLegacy, binaryData.GuidRepresentation);
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var expected = "CSUUID(\"b5f21e0c-2a0d-42d6-ad03-d827008d8ab6\")";
            Assert.Equal(expected, BsonSerializer.Deserialize<Guid>(json).ToJson());
        }

        [Fact]
        public void TestHexData()
        {
            var expectedBytes = new byte[] { 0x01, 0x23 };
            var json = "HexData(0, \"123\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Binary, _bsonReader.ReadBsonType());
                var bytes = _bsonReader.ReadBytes();
                Assert.True(expectedBytes.SequenceEqual(bytes));
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var expectedJson = "new BinData(0, \"ASM=\")";
            Assert.Equal(expectedJson, BsonSerializer.Deserialize<byte[]>(json).ToJson());
        }

        [Fact]
        public void TestInt32()
        {
            var json = "123";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal(123, _bsonReader.ReadInt32());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<int>(json).ToJson());
        }

        [Theory]
        [InlineData("Number(123)")]
        [InlineData("NumberInt(123)")]
        public void TestInt32Constructor(string json)
        {
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal(123, _bsonReader.ReadInt32());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "123";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<int>(new StringReader(json)).ToJson());
        }

        [Theory]
        [InlineData("{ $numberLong: 1 }", 1L)]
        [InlineData("{ $numberLong: -9223372036854775808 }", -9223372036854775808L)]
        [InlineData("{ $numberLong: 9223372036854775807 }", 9223372036854775807L)]
        [InlineData("{ $numberLong: \"1\" }", 1L)]
        [InlineData("{ $numberLong: \"-9223372036854775808\" }", -9223372036854775808L)]
        [InlineData("{ $numberLong: \"9223372036854775807\" }", 9223372036854775807L)]
        [InlineData("NumberLong(1)", 1L)]
        [InlineData("NumberLong(-9223372036854775808)", -9223372036854775808L)]
        [InlineData("NumberLong(9223372036854775807)", 9223372036854775807L)]
        [InlineData("NumberLong(\"1\")", 1L)]
        [InlineData("NumberLong(\"-9223372036854775808\")", -9223372036854775808L)]
        [InlineData("NumberLong(\"9223372036854775807\")", 9223372036854775807L)]
        public void TestInt64(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadInt64();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestInt64ConstructorQuoted()
        {
            var json = "NumberLong(\"123456789012\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.Equal(123456789012, _bsonReader.ReadInt64());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Fact]
        public void TestInt64ConstructorUnqutoed()
        {
            var json = "NumberLong(123)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.Equal(123, _bsonReader.ReadInt64());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<long>(json).ToJson());
        }

        [Fact]
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
                    Assert.Equal(expected, array);
                    count += 1;
                }
                Assert.Equal(2, count);
            }
        }

        [Fact]
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
                    Assert.Equal(expected, document);
                    count += 1;
                }
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestInt64ExtendedJson()
        {
            var json = "{ \"$numberLong\" : \"123\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Int64, _bsonReader.ReadBsonType());
                Assert.Equal(123, _bsonReader.ReadInt64());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "NumberLong(123)";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<long>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestJavaScript()
        {
            string json = "{ \"$code\" : \"function f() { return 1; }\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.JavaScript, _bsonReader.ReadBsonType());
                Assert.Equal("function f() { return 1; }", _bsonReader.ReadJavaScript());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonJavaScript>(json).ToJson());
        }

        [Fact]
        public void TestJavaScriptWithScope()
        {
            string json = "{ \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.JavaScriptWithScope, _bsonReader.ReadBsonType());
                Assert.Equal("function f() { return n; }", _bsonReader.ReadJavaScriptWithScope());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Int32, _bsonReader.ReadBsonType());
                Assert.Equal("n", _bsonReader.ReadName());
                Assert.Equal(1, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonJavaScriptWithScope>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $maxKey : 1 }")]
        [InlineData("MaxKey")]
        public void TestMaxKey(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadMaxKey();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestMaxKeyExtendedJson()
        {
            var json = "{ \"$maxkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestMaxKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$maxKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MaxKey";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestMaxKeyKeyword()
        {
            var json = "MaxKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MaxKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMaxKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Theory]
        [InlineData("{ $minKey : 1 }")]
        [InlineData("MinKey")]
        public void TestMinKey(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadMinKey();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestMinKeyExtendedJson()
        {
            var json = "{ \"$minkey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestMinKeyExtendedJsonWithCapitalK()
        {
            var json = "{ \"$minKey\" : 1 }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "MinKey";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestMinKeyKeyword()
        {
            var json = "MinKey";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.MinKey, _bsonReader.ReadBsonType());
                _bsonReader.ReadMinKey();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestNestedArray()
        {
            var json = "{ \"a\" : [1, 2] }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Array, _bsonReader.ReadBsonType());
                Assert.Equal("a", _bsonReader.ReadName());
                _bsonReader.ReadStartArray();
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndArray();
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestNestedDocument()
        {
            var json = "{ \"a\" : { \"b\" : 1, \"c\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                _bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Document, _bsonReader.ReadBsonType());
                Assert.Equal("a", _bsonReader.ReadName());
                _bsonReader.ReadStartDocument();
                Assert.Equal("b", _bsonReader.ReadName());
                Assert.Equal(1, _bsonReader.ReadInt32());
                Assert.Equal("c", _bsonReader.ReadName());
                Assert.Equal(2, _bsonReader.ReadInt32());
                _bsonReader.ReadEndDocument();
                _bsonReader.ReadEndDocument();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonDocument>(json).ToJson());
        }

        [Fact]
        public void TestNull()
        {
            var json = "null";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Null, _bsonReader.ReadBsonType());
                _bsonReader.ReadNull();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonNull>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $oid : \"0102030405060708090a0b0c\" }", "0102030405060708090a0b0c")]
        [InlineData("ObjectId(\"0102030405060708090a0b0c\")", "0102030405060708090a0b0c")]
        public void TestObjectId(string json, string expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadObjectId();

                result.Should().Be(ObjectId.Parse(expectedResult));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestObjectIdShell()
        {
            var json = "ObjectId(\"4d0ce088e447ad08b4721a37\")";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.Equal("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson());
        }

        [Fact]
        public void TestObjectIdStrict()
        {
            var json = "{ \"$oid\" : \"4d0ce088e447ad08b4721a37\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.ObjectId, _bsonReader.ReadBsonType());
                var objectId = _bsonReader.ReadObjectId();
                Assert.Equal("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var jsonSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.Equal(json, BsonSerializer.Deserialize<ObjectId>(json).ToJson(jsonSettings));
        }

        [Theory]
        [InlineData("{ $regex : \"abc\", $options : \"i\" }", "abc", "i")]
        [InlineData("{ $regex : \"abc/\", $options : \"i\" }", "abc/", "i")]
        [InlineData("/abc/i", "abc", "i")]
        [InlineData("/abc\\//i", "abc/", "i")]
        public void TestRegularExpression(string json, string expectedPattern, string expectedOptions)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadRegularExpression();

                result.Should().Be(new BsonRegularExpression(expectedPattern, expectedOptions));
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestRegularExpressionShell()
        {
            var json = "/pattern/imxs";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.Equal("pattern", regex.Pattern);
                Assert.Equal("imxs", regex.Options);
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson());
        }

        [Fact]
        public void TestRegularExpressionStrict()
        {
            var json = "{ \"$regex\" : \"pattern\", \"$options\" : \"imxs\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.RegularExpression, _bsonReader.ReadBsonType());
                var regex = _bsonReader.ReadRegularExpression();
                Assert.Equal("pattern", regex.Pattern);
                Assert.Equal("imxs", regex.Options);
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var settings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            Assert.Equal(json, BsonSerializer.Deserialize<BsonRegularExpression>(json).ToJson(settings));
        }

        [Fact]
        public void TestString()
        {
            var json = "\"abc\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.String, _bsonReader.ReadBsonType());
                Assert.Equal("abc", _bsonReader.ReadString());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Fact]
        public void TestStringEmpty()
        {
            var json = "\"\"";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.String, _bsonReader.ReadBsonType());
                Assert.Equal("", _bsonReader.ReadString());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<string>(json).ToJson());
        }

        [Fact]
        public void TestSymbol()
        {
            var json = "{ \"$symbol\" : \"symbol\" }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Symbol, _bsonReader.ReadBsonType());
                Assert.Equal("symbol", _bsonReader.ReadSymbol());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonSymbol>(json).ToJson());
        }

        [Theory]
        [InlineData("{ $timestamp : { t : 1, i : 2 } }", 0x100000002L)]
        [InlineData("{ $timestamp : { t : -2147483648, i : -2147483648 } }", unchecked((long)0x8000000080000000UL))]
        [InlineData("{ $timestamp : { t : 2147483647, i : 2147483647 } }", 0x7fffffff7fffffff)]
        [InlineData("Timestamp(1, 2)", 0x100000002L)]
        [InlineData("Timestamp(-2147483648, -2147483648)", unchecked((long)0x8000000080000000UL))]
        [InlineData("Timestamp(2147483647, 2147483647)", 0x7fffffff7fffffff)]
        public void TestTimestamp(string json, long expectedResult)
        {
            using (var reader = new JsonReader(json))
            {
                var result = reader.ReadTimestamp();

                result.Should().Be(expectedResult);
                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestTimestampConstructor()
        {
            var json = "Timestamp(1, 2)";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.Equal(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestTimestampExtendedJsonNewRepresentation()
        {
            var json = "{ \"$timestamp\" : { \"t\" : 1, \"i\" : 2 } }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.Equal(new BsonTimestamp(1, 2).Value, _bsonReader.ReadTimestamp());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(1, 2)";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestTimestampExtendedJsonOldRepresentation()
        {
            var json = "{ \"$timestamp\" : NumberLong(1234) }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Timestamp, _bsonReader.ReadBsonType());
                Assert.Equal(1234L, _bsonReader.ReadTimestamp());
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "Timestamp(0, 1234)";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }

        [Theory]
        [InlineData("{ $undefined : true }")]
        [InlineData("undefined")]
        public void TestUndefined(string json)
        {
            using (var reader = new JsonReader(json))
            {
                reader.ReadUndefined();

                reader.IsAtEndOfFile().Should().BeTrue();
            }
        }

        [Fact]
        public void TestUndefinedExtendedJson()
        {
            var json = "{ \"$undefined\" : true }";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            var canonicalJson = "undefined";
            Assert.Equal(canonicalJson, BsonSerializer.Deserialize<BsonUndefined>(new StringReader(json)).ToJson());
        }

        [Fact]
        public void TestUndefinedKeyword()
        {
            var json = "undefined";
            using (_bsonReader = new JsonReader(json))
            {
                Assert.Equal(BsonType.Undefined, _bsonReader.ReadBsonType());
                _bsonReader.ReadUndefined();
                Assert.Equal(BsonReaderState.Initial, _bsonReader.State);
            }
            Assert.Equal(json, BsonSerializer.Deserialize<BsonUndefined>(json).ToJson());
        }

        [Fact]
        public void TestUtf16BigEndian()
        {
            var encoding = new UnicodeEncoding(true, false, true);

            var bytes = BsonUtils.ParseHexString("007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestUtf16BigEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("feff007b00200022007800220020003a002000310020007d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestUtf16LittleEndian()
        {
            var encoding = new UnicodeEncoding(false, false, true);

            var bytes = BsonUtils.ParseHexString("7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestUtf16LittleEndianAutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("fffe7b00200022007800220020003a002000310020007d00");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestUtf8()
        {
            var encoding = Utf8Encodings.Strict;

            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, encoding))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }

        [Fact]
        public void TestUtf8AutoDetect()
        {
            var bytes = BsonUtils.ParseHexString("7b20227822203a2031207d");
            using (var memoryStream = new MemoryStream(bytes))
            using (var streamReader = new StreamReader(memoryStream, true))
            {
                var document = BsonSerializer.Deserialize<BsonDocument>(streamReader);
                Assert.Equal(1, document["x"].AsInt32);
            }
        }
    }
}
