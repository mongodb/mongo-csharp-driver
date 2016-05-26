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
using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class BsonBufferTests
    {
        [Fact]
        public void TestReadCStringEmpty()
        {
            var bytes = new byte[] { 8, 0, 0, 0, (byte)BsonType.Boolean, 0, 0, 0 };
            Assert.Equal(8, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("", document.GetElement(0).Name);
        }

        [Fact]
        public void TestReadCStringOneCharacter()
        {
            var bytes = new byte[] { 9, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', 0, 0, 0 };
            Assert.Equal(9, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("b", document.GetElement(0).Name);
        }

        [Fact]
        public void TestReadCStringOneCharacterDecoderException()
        {
            var bytes = new byte[] { 9, 0, 0, 0, (byte)BsonType.Boolean, 0x80, 0, 0, 0 };
            Assert.Equal(9, bytes.Length);
            Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Fact]
        public void TestReadCStringTwoCharacters()
        {
            var bytes = new byte[] { 10, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', (byte)'b', 0, 0, 0 };
            Assert.Equal(10, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("bb", document.GetElement(0).Name);
        }

        [Fact]
        public void TestReadCStringTwoCharactersDecoderException()
        {
            var bytes = new byte[] { 10, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', 0x80, 0, 0, 0 };
            Assert.Equal(10, bytes.Length);
            Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Fact]
        public void TestReadStringEmpty()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 1, 0, 0, 0, 0, 0 };
            Assert.Equal(13, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("", document["s"].AsString);
        }

        [Fact]
        public void TestReadStringInvalidLength()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 0, 0, 0, 0, 0, 0 };
            Assert.Equal(13, bytes.Length);
            var ex = Assert.Throws<FormatException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
            Assert.Equal("Invalid string length: 0.", ex.Message);
        }

        [Fact]
        public void TestReadStringMissingNullTerminator()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 1, 0, 0, 0, 123, 0 };
            Assert.Equal(13, bytes.Length);
            var ex = Assert.Throws<FormatException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
            Assert.Equal("String is missing terminating null byte.", ex.Message);
        }

        [Fact]
        public void TestReadStringOneCharacter()
        {
            var bytes = new byte[] { 14, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 2, 0, 0, 0, (byte)'x', 0, 0 };
            Assert.Equal(14, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("x", document["s"].AsString);
        }

        [Fact]
        public void TestReadStringOneCharacterDecoderException()
        {
            var bytes = new byte[] { 14, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 2, 0, 0, 0, 0x80, 0, 0 };
            Assert.Equal(14, bytes.Length);
            Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Fact]
        public void TestReadStringTwoCharacters()
        {
            var bytes = new byte[] { 15, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 3, 0, 0, 0, (byte)'x', (byte)'y', 0, 0 };
            Assert.Equal(15, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.Equal("xy", document["s"].AsString);
        }

        [Fact]
        public void TestReadStringTwoCharactersDecoderException()
        {
            var bytes = new byte[] { 15, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 3, 0, 0, 0, (byte)'x', 0x80, 0, 0 };
            Assert.Equal(15, bytes.Length);
            Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }
    }
}
