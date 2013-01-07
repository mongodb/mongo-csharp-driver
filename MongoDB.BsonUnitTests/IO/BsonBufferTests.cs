/* Copyright 2010-2013 10gen Inc.
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

using System.IO;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class BsonBufferTests
    {
        [Test]
        public void TestReadCStringEmpty()
        {
            var bytes = new byte[] { 8, 0, 0, 0, (byte)BsonType.Boolean, 0, 0, 0 };
            Assert.AreEqual(8, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("", document.GetElement(0).Name);
        }

        [Test]
        public void TestReadCStringOneCharacter()
        {
            var bytes = new byte[] { 9, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', 0, 0, 0 };
            Assert.AreEqual(9, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("b", document.GetElement(0).Name);
        }

        [Test]
        public void TestReadCStringOneCharacterDecoderException()
        {
            var bytes = new byte[] { 9, 0, 0, 0, (byte)BsonType.Boolean, 0x80, 0, 0, 0 };
            Assert.AreEqual(9, bytes.Length);
            var ex = Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Test]
        public void TestReadCStringTwoCharacters()
        {
            var bytes = new byte[] { 10, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', (byte)'b', 0, 0, 0 };
            Assert.AreEqual(10, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("bb", document.GetElement(0).Name);
        }

        [Test]
        public void TestReadCStringTwoCharactersDecoderException()
        {
            var bytes = new byte[] { 10, 0, 0, 0, (byte)BsonType.Boolean, (byte)'b', 0x80, 0, 0, 0 };
            Assert.AreEqual(10, bytes.Length);
            var ex = Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Test]
        public void TestReadStringEmpty()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 1, 0, 0, 0, 0, 0 };
            Assert.AreEqual(13, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("", document["s"].AsString);
        }

        [Test]
        public void TestReadStringInvalidLength()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 0, 0, 0, 0, 0, 0 };
            Assert.AreEqual(13, bytes.Length);
            var ex = Assert.Throws<FileFormatException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
            Assert.AreEqual("Invalid string length: 0 (the length includes the null terminator so it must be greater than or equal to 1).", ex.Message);
        }

        [Test]
        public void TestReadStringMissingNullTerminator()
        {
            var bytes = new byte[] { 13, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 1, 0, 0, 0, 123, 0 };
            Assert.AreEqual(13, bytes.Length);
            var ex = Assert.Throws<FileFormatException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
            Assert.AreEqual("String is missing null terminator.", ex.Message);
        }

        [Test]
        public void TestReadStringOneCharacter()
        {
            var bytes = new byte[] { 14, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 2, 0, 0, 0, (byte)'x', 0, 0 };
            Assert.AreEqual(14, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("x", document["s"].AsString);
        }

        [Test]
        public void TestReadStringOneCharacterDecoderException()
        {
            var bytes = new byte[] { 14, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 2, 0, 0, 0, 0x80, 0, 0 };
            Assert.AreEqual(14, bytes.Length);
            var ex = Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }

        [Test]
        public void TestReadStringTwoCharacters()
        {
            var bytes = new byte[] { 15, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 3, 0, 0, 0, (byte)'x', (byte)'y', 0, 0 };
            Assert.AreEqual(15, bytes.Length);
            var document = BsonSerializer.Deserialize<BsonDocument>(bytes);
            Assert.AreEqual("xy", document["s"].AsString);
        }

        [Test]
        public void TestReadStringTwoCharactersDecoderException()
        {
            var bytes = new byte[] { 15, 0, 0, 0, (byte)BsonType.String, (byte)'s', 0, 3, 0, 0, 0, (byte)'x', 0x80, 0, 0 };
            Assert.AreEqual(15, bytes.Length);
            var ex = Assert.Throws<DecoderFallbackException>(() => { BsonSerializer.Deserialize<BsonDocument>(bytes); });
        }
    }
}
