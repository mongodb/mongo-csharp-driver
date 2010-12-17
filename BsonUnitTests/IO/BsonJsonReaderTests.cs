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
    public class BsonJsonReaderTests {
        private BsonReader bsonReader;

        [Test]
        public void TestArrayEmpty() {
            var json = "[]";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestArrayOneElement() {
            var json = "[1]";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestArrayTwoElements() {
            var json = "[1, 2]";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Array, bsonReader.ReadBsonType());
                bsonReader.ReadStartArray();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(2, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndArray();
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonArray>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestBooleanFalse() {
            var json = "false";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Boolean, bsonReader.ReadBsonType());
                Assert.AreEqual(false, bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestBooleanTrue() {
            var json = "true";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Boolean, bsonReader.ReadBsonType());
                Assert.AreEqual(true, bsonReader.ReadBoolean());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<bool>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentEmpty() {
            var json = "{ }";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentOneElement() {
            var json = "{ \"x\" : 1 }";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Document, bsonReader.ReadBsonType());
                bsonReader.ReadStartDocument();
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual("x", bsonReader.ReadName());
                Assert.AreEqual(1, bsonReader.ReadInt32());
                Assert.AreEqual(BsonType.EndOfDocument, bsonReader.ReadBsonType());
                bsonReader.ReadEndDocument();
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDocumentTwoElements() {
            var json = "{ \"x\" : 1, \"y\" : 2 }";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
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
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<BsonDocument>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestDouble() {
            var json = "1.5";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Double, bsonReader.ReadBsonType());
                Assert.AreEqual(1.5, bsonReader.ReadDouble());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<double>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestInt32() {
            var json = "123";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Int32, bsonReader.ReadBsonType());
                Assert.AreEqual(123, bsonReader.ReadInt32());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<int>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestInt64() {
            var json = "123456789012";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.Int64, bsonReader.ReadBsonType());
                Assert.AreEqual(123456789012, bsonReader.ReadInt64());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<long>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestString() {
            var json = "\"abc\"";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("abc", bsonReader.ReadString());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(new StringReader(json)).ToJson());
        }

        [Test]
        public void TestStringEmpty() {
            var json = "\"\"";
            var stringReader = new StringReader(json);
            using (bsonReader = BsonReader.Create(stringReader)) {
                Assert.AreEqual(BsonType.String, bsonReader.ReadBsonType());
                Assert.AreEqual("", bsonReader.ReadString());
                Assert.AreEqual(BsonReadState.Done, bsonReader.ReadState);
            }
            Assert.AreEqual(json, BsonSerializer.Deserialize<string>(new StringReader(json)).ToJson());
        }
    }
}
