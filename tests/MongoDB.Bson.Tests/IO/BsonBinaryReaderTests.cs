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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonBinaryReaderTests
    {
        [SkippableFact]
        public void BsonBinaryReader_should_support_reading_more_than_2GB()
        {
            RequireEnvironment.Check().EnvironmentVariable("EXPLICIT");

            var binaryData = new BsonBinaryData(new byte[1024 * 1024]);

            var tempFileName = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempFileName, FileMode.Open))
                {
                    using (var binaryWriter = new BsonBinaryWriter(stream))
                    {
                        while (stream.Position < (long)int.MaxValue * 4)
                        {
                            binaryWriter.WriteStartDocument();
                            binaryWriter.WriteName("x");
                            binaryWriter.WriteBinaryData(binaryData);
                            binaryWriter.WriteEndDocument();
                        }
                    }

                    var endOfFilePosition = stream.Position;
                    stream.Position = 0;

                    using (var binaryReader = new BsonBinaryReader(stream))
                    {
                        while (!binaryReader.IsAtEndOfFile())
                        {
                            binaryReader.ReadStartDocument();
                            var bookmark = binaryReader.GetBookmark();

                            binaryReader.ReadName("x");
                            binaryReader.ReturnToBookmark(bookmark);

                            binaryReader.ReadName("x");
                            var readBinaryData = binaryReader.ReadBinaryData();
                            Assert.Equal(binaryData.Bytes.Length, readBinaryData.Bytes.Length);

                            binaryReader.ReadEndDocument();
                        }
                    }

                    Assert.Equal(endOfFilePosition, stream.Position);
                }
            }
            finally
            {
                try
                {
                    File.Delete(tempFileName);
                }
                catch
                {
                    // ignore exceptions
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void BsonBinaryReader_should_support_reading_multiple_documents(
            [Range(0, 3)]
            int numberOfDocuments)
        {
            var document = new BsonDocument("x", 1);
            var bson = document.ToBson();
            var input = Enumerable.Repeat(bson, numberOfDocuments).Aggregate(Enumerable.Empty<byte>(), (a, b) => a.Concat(b)).ToArray();
            var expectedResult = Enumerable.Repeat(document, numberOfDocuments);

            using (var stream = new MemoryStream(input))
            using (var binaryReader = new BsonBinaryReader(stream))
            {
                var result = new List<BsonDocument>();

                while (!binaryReader.IsAtEndOfFile())
                {
                    binaryReader.ReadStartDocument();
                    var name = binaryReader.ReadName();
                    var value = binaryReader.ReadInt32();
                    binaryReader.ReadEndDocument();

                    var resultDocument = new BsonDocument(name, value);
                    result.Add(resultDocument);
                }

                result.Should().Equal(expectedResult);
            }
        }

        [Theory]
        [InlineData("00000000 f0 6100", "a")]
        [InlineData("00000000 08 6100 00 f0 6200", "b")]
        [InlineData("00000000 03 6100 00000000 f0 6200", "a.b")]
        [InlineData("00000000 03 6100 00000000 08 6200 00 f0 6300", "a.c")]
        [InlineData("00000000 04 6100 00000000 f0", "a.0")]
        [InlineData("00000000 04 6100 00000000 08 3000 00 f0", "a.1")]
        [InlineData("00000000 04 6100 00000000 03 3000 00000000 f0 6200", "a.0.b")]
        [InlineData("00000000 04 6100 00000000 03 3000 00000000 08 6200 00 f0 6300", "a.0.c")]
        [InlineData("00000000 04 6100 00000000 08 3000 00 03 3100 00000000 f0 6200", "a.1.b")]
        [InlineData("00000000 04 6100 00000000 08 3000 00 03 3200 00000000 08 6200 00 f0 6300", "a.1.c")]
        public void ReadBsonType_should_throw_when_bson_type_is_invalid(string hexBytes, string expectedElementName)
        {
            var bytes = BsonUtils.ParseHexString(hexBytes.Replace(" ", ""));
            var expectedMessage = $"Detected unknown BSON type \"\\xf0\" for fieldname \"{expectedElementName}\". Are you using the latest driver version?";

            using (var memoryStream = new MemoryStream(bytes))
            using (var subject = new BsonBinaryReader(memoryStream))
            {
                Action action = () => BsonSerializer.Deserialize<BsonDocument>(subject);

                action.ShouldThrow<FormatException>().WithMessage(expectedMessage);
            }
        }

        [Fact]
        public void TestHelloWorld()
        {
            string byteString = @"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            var stream = new MemoryStream(bytes);
            using (var bsonReader = new BsonBinaryReader(stream))
            {
                bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.String, bsonReader.ReadBsonType());
                Assert.Equal("hello", bsonReader.ReadName());
                Assert.Equal("world", bsonReader.ReadString());
                bsonReader.ReadEndDocument();
            }
        }

        [Fact]
        public void TestBsonAwesome()
        {
            string byteString = @"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            var stream = new MemoryStream(bytes);
            using (var bsonReader = new BsonBinaryReader(stream))
            {
                bsonReader.ReadStartDocument();
                Assert.Equal(BsonType.Array, bsonReader.ReadBsonType());
                Assert.Equal("BSON", bsonReader.ReadName());
                bsonReader.ReadStartArray();
                Assert.Equal(BsonType.String, bsonReader.ReadBsonType());
                Assert.Equal("awesome", bsonReader.ReadString());
                Assert.Equal(BsonType.Double, bsonReader.ReadBsonType());
                Assert.Equal(5.05, bsonReader.ReadDouble());
                Assert.Equal(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.Equal(1986, bsonReader.ReadInt32());
                bsonReader.ReadEndArray();
                bsonReader.ReadEndDocument();
            }
        }

        [Fact]
        public void TestIsAtEndOfFileWithTwoDocuments()
        {
            var expected = new BsonDocument("x", 1);

            byte[] bson;
            using (var stream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(stream))
            {
                BsonSerializer.Serialize(writer, expected);
                BsonSerializer.Serialize(writer, expected);
                bson = stream.ToArray();
            }

            using (var stream = new MemoryStream(bson))
            using (var reader = new BsonBinaryReader(stream))
            {
                var count = 0;
                while (!reader.IsAtEndOfFile())
                {
                    var document = BsonSerializer.Deserialize<BsonDocument>(reader);
                    Assert.Equal(expected, document);
                    count++;
                }
                Assert.Equal(2, count);
            }
        }

        [Fact]
        public void TestReadRawBsonArray()
        {
            var bsonDocument = new BsonDocument { { "_id", 1 }, { "A", new BsonArray { 1, 2 } } };
            var bson = bsonDocument.ToBson();

            using (var document = BsonSerializer.Deserialize<CWithRawBsonArray>(bson))
            {
                Assert.Equal(1, document.Id);
                Assert.Equal(2, document.A.Count);
                Assert.Equal(1, document.A[0].AsInt32);
                Assert.Equal(2, document.A[1].AsInt32);
                Assert.True(bson.SequenceEqual(document.ToBson()));
            }
        }

        [Fact]
        public void TestReadRawBsonDocument()
        {
            var document = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var bson = document.ToBson();

            using (var rawDocument = BsonSerializer.Deserialize<RawBsonDocument>(bson))
            {
                Assert.Equal(1, rawDocument["x"].ToInt32());
                Assert.Equal(2, rawDocument["y"].ToInt32());
                Assert.True(bson.SequenceEqual(rawDocument.ToBson()));
            }
        }

        // private methods
        private static string __hexDigits = "0123456789abcdef";

        private byte[] DecodeByteString(string byteString)
        {
            List<byte> bytes = new List<byte>(byteString.Length);
            for (int i = 0; i < byteString.Length; )
            {
                char c = byteString[i++];
                if (c == '\\' && ((c = byteString[i++]) != '\\'))
                {
                    int x = __hexDigits.IndexOf(char.ToLower(byteString[i++]));
                    int y = __hexDigits.IndexOf(char.ToLower(byteString[i++]));
                    bytes.Add((byte)(16 * x + y));
                }
                else
                {
                    bytes.Add((byte)c);
                }
            }
            return bytes.ToArray();
        }

        // nested classes
        private class CWithRawBsonArray : IDisposable
        {
            public int Id { get; set; }
            public RawBsonArray A { get; set; }

            public void Dispose()
            {
                if (A != null)
                {
                    A.Dispose();
                    A = null;
                }
            }
        }
    }
}
