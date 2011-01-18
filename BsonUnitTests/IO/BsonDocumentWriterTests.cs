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
using System.Linq;
using System.Text;
using NUnit.Framework;

using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace MongoDB.BsonUnitTests.IO {
    [TestFixture]
    public class BsonDocumentWriterTests {
        // Empty Document tests
        [Test]
        public void TestEmptyDocument() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneEmptyDocument() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("a");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedEmptyDocument() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteStartDocument("a");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoEmptyDocuments() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("a");
            writer.WriteEndDocument();
            writer.WriteStartDocument("b");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { }, 'b' : { } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedEmptyDocuments() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteStartDocument("a");
            writer.WriteEndDocument();
            writer.WriteStartDocument("b");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { }, 'b' : { } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Boolean tests
        [Test]
        public void TestOneBoolean() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteBoolean("a", true);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : true }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedBoolean() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteBoolean("a", true);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : true } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoBooleans() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteBoolean("a", true);
            writer.WriteBoolean("b", false);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : true, 'b' : false }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedBooleans() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteBoolean("a", true);
            writer.WriteBoolean("b", false);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : true, 'b' : false } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Double tests
        [Test]
        public void TestOneDouble() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteDouble("a", 1.5);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedDouble() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteDouble("a", 1.5);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1.5 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoDoubles() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteDouble("a", 1.5);
            writer.WriteDouble("b", 2.5);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1.5, 'b' : 2.5 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedDoubles() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteDouble("a", 1.5);
            writer.WriteDouble("b", 2.5);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1.5, 'b' : 2.5 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Int32 tests
        [Test]
        public void TestOneInt32() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteInt32("a", 1);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedInt32() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteInt32("a", 1);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoInt32s() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteInt32("a", 1);
            writer.WriteInt32("b", 2);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1, 'b' : 2 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedInt32s() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteInt32("a", 1);
            writer.WriteInt32("b", 2);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1, 'b' : 2 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Int64 tests
        [Test]
        public void TestOneInt64() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteInt64("a", 1);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedInt64() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteInt64("a", 1);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoInt64s() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteInt64("a", 1);
            writer.WriteInt64("b", 2);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 1, 'b' : 2 }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedInt64s() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteInt64("a", 1);
            writer.WriteInt64("b", 2);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 1, 'b' : 2 } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // MaxKey tests
        [Test]
        public void TestOneMaxKey() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteMaxKey("a");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$maxkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedMaxKey() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteMaxKey("a");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$maxkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoMaxKeys() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteMaxKey("a");
            writer.WriteMaxKey("b");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$maxkey' : 1 }, 'b' : { '$maxkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedMaxKeys() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteMaxKey("a");
            writer.WriteMaxKey("b");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$maxkey' : 1 }, 'b' : { '$maxkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // MinKey tests
        [Test]
        public void TestOneMinKey() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteMinKey("a");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$minkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedMinKey() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteMinKey("a");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$minkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoMinKeys() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteMinKey("a");
            writer.WriteMinKey("b");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$minkey' : 1 }, 'b' : { '$minkey' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedMinKeys() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteMinKey("a");
            writer.WriteMinKey("b");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$minkey' : 1 }, 'b' : { '$minkey' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Null tests
        [Test]
        public void TestOneNull() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteNull("a");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : null }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedNull() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteNull("a");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : null } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNulls() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteNull("a");
            writer.WriteNull("b");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : null, 'b' : null }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedNulls() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteNull("a");
            writer.WriteNull("b");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : null, 'b' : null } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // ObjectId tests
        [Test]
        public void TestOneObjectId() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            var id = ObjectId.Empty;
            writer.WriteObjectId("a", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$oid' : '000000000000000000000000' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedObjectId() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            var id = ObjectId.Empty;
            writer.WriteObjectId("a", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$oid' : '000000000000000000000000' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoObjectIds() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            var id = ObjectId.Empty;
            writer.WriteObjectId("a", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteObjectId("b", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$oid' : '000000000000000000000000' }, 'b' : { '$oid' : '000000000000000000000000' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedObjectIds() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            var id = ObjectId.Empty;
            writer.WriteObjectId("a", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteObjectId("b", id.Timestamp, id.Machine, id.Pid, id.Increment);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$oid' : '000000000000000000000000' }, 'b' : { '$oid' : '000000000000000000000000' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // RegularExpression tests
        [Test]
        public void TestOneRegularExpression() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteRegularExpression("a", "p", "o");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$regex' : 'p', '$options' : 'o' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedRegularExpression() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteRegularExpression("a", "p", "o");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$regex' : 'p', '$options' : 'o' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoRegularExpressions() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteRegularExpression("a", "p", "o");
            writer.WriteRegularExpression("b", "q", "r");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$regex' : 'p', '$options' : 'o' }, 'b' : { '$regex' : 'q', '$options' : 'r' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedRegularExpressions() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteRegularExpression("a", "p", "o");
            writer.WriteRegularExpression("b", "q", "r");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$regex' : 'p', '$options' : 'o' }, 'b' : { '$regex' : 'q', '$options' : 'r' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // String tests
        [Test]
        public void TestOneString() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteString("a", "x");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 'x' }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedString() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteString("a", "x");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 'x' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoStrings() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteString("a", "x");
            writer.WriteString("b", "y");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : 'x', 'b' : 'y' }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedStrings() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteString("a", "x");
            writer.WriteString("b", "y");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : 'x', 'b' : 'y' } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Symbol tests
        [Test]
        public void TestOneSymbol() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteSymbol("a", "x");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedSymbol() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteSymbol("a", "x");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$symbol' : 'x' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoSymbols() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteSymbol("a", "x");
            writer.WriteSymbol("b", "y");
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$symbol' : 'x' }, 'b' : { '$symbol' : 'y' } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedSymbols() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteSymbol("a", "x");
            writer.WriteSymbol("b", "y");
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$symbol' : 'x' }, 'b' : { '$symbol' : 'y' } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        // Timestamp tests
        [Test]
        public void TestOneTimestamp() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteTimestamp("a", 1);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$timestamp' : 1 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestOneNestedTimestamp() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteTimestamp("a", 1);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$timestamp' : 1 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoTimestamps() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteTimestamp("a", 1);
            writer.WriteTimestamp("b", 2);
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'a' : { '$timestamp' : 1 }, 'b' : { '$timestamp' : 2 } }".Replace("'", "\""); ;
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestTwoNestedTimestamps() {
            var document = new BsonDocument();
            var writer = BsonWriter.Create(document);
            writer.WriteStartDocument();
            writer.WriteStartDocument("nested");
            writer.WriteTimestamp("a", 1);
            writer.WriteTimestamp("b", 2);
            writer.WriteEndDocument();
            writer.WriteEndDocument();
            var json = document.ToJson();
            var expected = "{ 'nested' : { 'a' : { '$timestamp' : 1 }, 'b' : { '$timestamp' : 2 } } }".Replace("'", "\"");
            Assert.AreEqual(expected, json);
        }
    }
}
