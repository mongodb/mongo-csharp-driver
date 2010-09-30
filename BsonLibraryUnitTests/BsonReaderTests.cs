/* Copyright 2010 10gen Inc.
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
using NUnit.Framework;

using MongoDB.BsonLibrary;
using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.UnitTests {
    [TestFixture]
    public class BsonReaderTests {
        [Test]
        public void TestHelloWorld() {
            string byteString = @"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream)) {
                bsonReader.ReadStartDocument();
                string name;
                string value = bsonReader.ReadString(out name);
                bsonReader.ReadEndDocument();
            }
        }

        [Test]
        public void TestBsonAwesome() {
            string byteString = @"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00";
            byte[] bytes = DecodeByteString(byteString);
            MemoryStream stream = new MemoryStream(bytes);
            using (BsonReader bsonReader = BsonReader.Create(stream)) {
                bsonReader.ReadStartDocument();
                string name;
                bsonReader.ReadArrayName(out name);
                bsonReader.ReadStartDocument();
                string awesome = bsonReader.ReadString(out name);
                double fiveOhFive = bsonReader.ReadDouble(out name);
                int year = bsonReader.ReadInt32(out name);
                bsonReader.ReadEndDocument();
                bsonReader.ReadEndDocument();
            }
        }

        private static string hexDigits = "0123456789abcdef";

        private byte[] DecodeByteString(
            string byteString
        ) {
            List<byte> bytes = new List<byte>(byteString.Length);
            for (int i = 0; i < byteString.Length; ) {
                char c = byteString[i++];
                if (c == '\\' && ((c = byteString[i++]) != '\\')) {
                    int x = hexDigits.IndexOf(char.ToLower(byteString[i++]));
                    int y = hexDigits.IndexOf(char.ToLower(byteString[i++]));
                    bytes.Add((byte) (16 * x + y));
                } else {
                    bytes.Add((byte) c);
                }
            }
            return bytes.ToArray();
        }
    }
}
