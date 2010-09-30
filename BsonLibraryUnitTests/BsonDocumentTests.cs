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
    public class BsonDocumentTests {
        [Test]
        public void TestBenchmarks() {
            int iterations;
            DateTime start;
            DateTime end;
            TimeSpan duration;

            iterations = 1;
            start = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++) {
                // about 2.06 on my machine
                //var doc = new BsonDocument {
                //    { "a", 1 },
                //    { "b", 2.0 },
                //    { "c", "hello" },
                //    { "d", DateTime.UtcNow },
                //    { "e", true }
                // };
                byte[] value = { 1, 2, 3, 4 };
                MemoryStream stream = new MemoryStream();
                for (int n = 0; n < 100000; n++) {
                    stream.Write(value, 0, 4);
                }
            }
            end = DateTime.UtcNow;
            duration = end - start;
            System.Diagnostics.Debug.WriteLine(duration);

            start = DateTime.UtcNow;
            for (int i = 0; i < iterations; i++) {
                // about 2.22 on my machine
                //var doc = new BsonDocument {
                //    { "a", BsonValue.Create((object) 1) },
                //    { "b", BsonValue.Create((object) 2.0) },
                //    { "c", BsonValue.Create((object) "hello") },
                //    { "d", BsonValue.Create((object) DateTime.UtcNow) },
                //    { "e", BsonValue.Create((object) true) }
                //};
                byte[] value = { 1, 2, 3, 4 };
                var buffer = new BsonBuffer();
                for (int n = 0; n < 100000; n++) {
                    buffer.WriteByteArray(value);
                }
            }
            end = DateTime.UtcNow;
            duration = end - start;
            System.Diagnostics.Debug.WriteLine(duration);
        }

        [Test]
        public void TestHelloWorldWithBsonWriter() {
            MemoryStream stream = new MemoryStream();
            using (BsonWriter bsonWriter = BsonWriter.Create(stream)) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("hello", "world");
                bsonWriter.WriteEndDocument();
            }
            byte[] bytes = stream.ToArray();
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bytes);
        }

        [Test]
        public void TestElementAccess() {
            var book = new BsonDocument {
                { "author", "Ernest Hemingway" },
                { "title", "For Whom the Bell Tolls" },
                { "pages", 123 },
                { "price", 9.95 },
                { "ok", Bson.Null }
            };
            Assert.AreEqual("Ernest Hemingway", book["author"].AsString);
            Assert.AreEqual(123, book["pages"].AsInt32);
            Assert.AreEqual(9.95, book["price"].AsDouble, 0.0);
            Assert.AreEqual(false, book["ok"].ToBoolean());

            book["err"] = "";
            Assert.AreEqual(false, book["err"].ToBoolean());
            book["err"] = "Error message.";
            Assert.AreEqual(true, book["err"].ToBoolean());

            book["price"] = (double) book["price"] * 1.1;
            double price = book["price"].AsDouble;
        }

        [Test]
        public void TestHelloWorldWithBsonDocument() {
            // this test is from http://bsonspec.org/#/specification
            BsonDocument document = new BsonDocument(
                new BsonElement("hello", "world")
            );
            byte[] bytes = WriteDocument(document);
            AssertAreEqual(@"\x16\x00\x00\x00\x02hello\x00\x06\x00\x00\x00world\x00\x00", bytes);
        }

        [Test]
        // this test is from http://bsonspec.org/#/specification
        public void TestBsonAwesomeWithBsonWriter() {
            MemoryStream stream = new MemoryStream();
            using (BsonWriter bsonWriter = BsonWriter.Create(stream)) {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteArrayName("BSON");
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("0", "awesome");
                bsonWriter.WriteDouble("1", 5.05);
                bsonWriter.WriteInt32("2", 1986);
                bsonWriter.WriteEndDocument();
                bsonWriter.WriteEndDocument();
            }
            byte[] bytes = stream.ToArray();
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bytes);
        }

        [Test]
        // this test is from http://bsonspec.org/#/specification
        public void TestBsonAwesomeWithBsonDocument() {
            BsonDocument document = new BsonDocument(
                new BsonElement("BSON", new BsonArray { "awesome", 5.05, 1986 })
            );
            byte[] bytes = WriteDocument(document);
            AssertAreEqual(@"1\x00\x00\x00\x04BSON\x00&\x00\x00\x00\x020\x00\x08\x00\x00\x00awesome\x00\x011\x00333333\x14@\x102\x00\xc2\x07\x00\x00\x00\x00", bytes);
        }

        private byte[] WriteDocument(
            BsonDocument document
        ) {
            MemoryStream stream = new MemoryStream();
            document.WriteTo(stream);
            return stream.ToArray();
        }

        private void AssertAreEqual(
            string expected,
            byte[] actual
        ) {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in actual) {
                if (b >= 0x20 && b <= 0x7e) {
                    sb.Append((char) b);
                } else {
                    string hex = "0123456789abcdef";
                    int x = b >> 4;
                    int y = b & 0x0f;
                    sb.AppendFormat(@"\x{0}{1}", hex[x], hex[y]);
                }
            }
            Assert.AreEqual(expected, sb.ToString());
        }
    }
}
