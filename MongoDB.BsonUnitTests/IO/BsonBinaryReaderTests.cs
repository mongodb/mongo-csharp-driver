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

using System.Collections.Generic;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class BsonBinaryReaderTests
    {
        [Test]
        public void TestHelloWorld()
        {
            string byteString = @"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream))
            {
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("hello", bsonReader.ReadName());
                Assert.AreEqual("world", bsonReader.ReadString());
                bsonReader.ReadEndDocument();
            }
        }

        [Test]
        public void TestBsonAwesome()
        {
            string byteString = @"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream))
            {
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                Assert.AreEqual("BSON", bsonReader.ReadName());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("awesome", bsonReader.ReadString());
                Assert.AreEqual(BsonType.Double, bsonReader.ReadBsonType());
                Assert.AreEqual(5.05, bsonReader.ReadDouble());
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(1986, bsonReader.ReadInt32());
                bsonReader.ReadEndArray();
                bsonReader.ReadEndDocument();
            }
        }

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
    }
}
