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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.BsonUnitTests.IO {
    [TestFixture]
    public class JsonReaderTests {
        private BsonReader bsonReader;

        [Test]
        public void TestArrayEmpty() {
            var json = "[]";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestArrayOneElement() {
            var json = "[1]";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestArrayTwoElements() {
            var json = "[1, 2]";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }


        [Test]
        public void TestBookmark() {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (bsonReader = BsonReader.Create(json)) {
                // do everything twice returning to bookmark in between
                var bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());

                bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                bsonReader.ReturnToBookmark(bookmark);
                bsonReader.ReadStartDocument();

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual("x", bsonReader.ReadName());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("x", bsonReader.ReadName());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(1, bsonReader.ReadInt32());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(1, bsonReader.ReadInt32());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual("y", bsonReader.ReadName());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual("y", bsonReader.ReadName());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(2, bsonReader.ReadInt32());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(2, bsonReader.ReadInt32());

                bookmark = bsonReader.GetBookmark();
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReturnToBookmark(bookmark);
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());

                bookmark = bsonReader.GetBookmark();
                bsonReader.ReadEndDocument();
                bsonReader.ReturnToBookmark(bookmark);
                bsonReader.ReadEndDocument();

                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);

            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestBooleanFalse() {
            var json = "false";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Boolean, bsonReader.ReadBsonType());
                Assert.AreEqual(false, bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestBooleanTrue() {
            var json = "true";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Boolean, bsonReader.ReadBsonType());
                Assert.AreEqual(true, bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDateTime() {
            var json = "{ \"$date\" : 0 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.DateTime, bsonReader.ReadBsonType());
                Assert.AreEqual(BsonConstants.UnixEpoch, bsonReader.ReadDateTime());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<DateTime>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentEmpty() {
            var json = "{ }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentNested() {
            var json = "{ \"a\" : { \"x\" : 1 }, \"y\" : 2 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                Assert.AreEqual("a", bsonReader.ReadName());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("x", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("y", bsonReader.ReadName());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentOneElement() {
            var json = "{ \"x\" : 1 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("x", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentTwoElements() {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("x", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("y", bsonReader.ReadName());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDouble() {
            var json = "1.5";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Double, bsonReader.ReadBsonType());
                Assert.AreEqual(1.5, bsonReader.ReadDouble());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<double>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestGuid() {
            var guid = new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6");
            string json = "{ \"$binary\" : \"DB7ytQ0q1kKtA9gnAI2Ktg==\", \"$type\" : \"03\" }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Binary, bsonReader.ReadBsonType());
                byte[] bytes;
                BsonBinarySubType subType;
                bsonReader.ReadBinaryData(out bytes, out subType);
                Assert.IsTrue(bytes.SequenceEqual(guid.ToByteArray()));
                Assert.AreEqual(BsonBinarySubType.Uuid, subType);
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<Guid>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestInt32() {
            var json = "123";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(123, bsonReader.ReadInt32());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<int>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestInt64() {
            var json = "123456789012";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Int64, bsonReader.ReadBsonType());
                Assert.AreEqual(123456789012, bsonReader.ReadInt64());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestJavaScript() {
            string json = "{ \"$code\" : \"function f() { return 1; }\" }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.JavaScript, bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return 1; }", bsonReader.ReadJavaScript());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScript>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestJavaScriptWithScope() {
            string json = "{ \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.JavaScriptWithScope, bsonReader.ReadBsonType());
                Assert.AreEqual("function f() { return n; }", bsonReader.ReadJavaScriptWithScope());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("n", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonJavaScriptWithScope>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMaxKey() {
            var json = "{ \"$maxkey\" : 1 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.MaxKey, bsonReader.ReadBsonType());
                bsonReader.ReadMaxKey();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMaxKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestMinKey() {
            var json = "{ \"$minkey\" : 1 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.MinKey, bsonReader.ReadBsonType());
                bsonReader.ReadMinKey();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonMinKey>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestNestedArray() {
            var json = "{ \"a\" : [1, 2] }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                Assert.AreEqual("a", bsonReader.ReadName());
                bsonReader.ReadStartArray();
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                bsonReader.ReadEndArray();
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestNestedDocument() {
            var json = "{ \"a\" : { \"b\" : 1, \"c\" : 2 } }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                Assert.AreEqual("a", bsonReader.ReadName());
                bsonReader.ReadStartDocument();
                Assert.AreEqual("b", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual("c", bsonReader.ReadName());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                bsonReader.ReadEndDocument();
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestNull() {
            var json = "null";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Null, bsonReader.ReadBsonType());
                bsonReader.ReadNull();
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonNull>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestObjectId() {
            var json = "{ \"$oid\" : \"4d0ce088e447ad08b4721a37\" }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.ObjectId, bsonReader.ReadBsonType());
                int timestamp, machine, increment;
                short pid;
                bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                var objectId = new ObjectId(timestamp, machine, pid, increment);
                Assert.AreEqual("4d0ce088e447ad08b4721a37", objectId.ToString());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<ObjectId>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestRegularExpressionStrict() {
            var json = "{ \"$regex\" : \"pattern\", \"$options\" : \"gim\" }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.RegularExpression, bsonReader.ReadBsonType());
                string pattern, options;
                bsonReader.ReadRegularExpression(out pattern, out options);
                Assert.AreEqual("pattern", pattern);
                Assert.AreEqual("gim", options);
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestRegularExpressionTenGen() {
            var json = "/pattern/gim";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.RegularExpression, bsonReader.ReadBsonType());
                string pattern, options;
                bsonReader.ReadRegularExpression(out pattern, out options);
                Assert.AreEqual("pattern", pattern);
                Assert.AreEqual("gim", options);
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            var tenGen = new JsonWriterSettings { OutputMode = JsonOutputMode.TenGen };
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonRegularExpression>(new StringReader(json)).ToJson(tenGen));
        }

        [Test]
        public void TestString() {
            var json = "\"abc\"";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("abc", bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestStringEmpty() {
            var json = "\"\"";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("", bsonReader.ReadString());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestSymbol() {
            var json = "{ \"$symbol\" : \"symbol\" }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Symbol, bsonReader.ReadBsonType());
                Assert.AreEqual("symbol", bsonReader.ReadSymbol());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonSymbol>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestTimestamp() {
            var json = "{ \"$timestamp\" : 1234 }";
            using (bsonReader = BsonReader.Create(json)) {
                Assert.AreEqual(BsonType.Timestamp, bsonReader.ReadBsonType());
                Assert.AreEqual(1234L, bsonReader.ReadTimestamp());
                Assert.AreEqual(BsonReaderState.Done, bsonReader.State);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonTimestamp>(new StringReader(json)).ToJson());
        }
    }
}
