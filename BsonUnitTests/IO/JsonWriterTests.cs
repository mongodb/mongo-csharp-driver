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
    public class JsonWriterTests {
        [Test]
        public void TestEmptyDocument() {
            BsonDocument document = new BsonDocument();
            string json = document.ToJson();
            string expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestSingleString() {
            BsonDocument document = new BsonDocument() { { "abc", "xyz" } };
            string json = document.ToJson();
            string expected = "{ \"abc\" : \"xyz\" }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedEmptyDocument() {
            BsonDocument document = new BsonDocument();
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{ }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedOneElement() {
            BsonDocument document = new BsonDocument() { { "name", "value" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"name\" : \"value\"\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedTwoElements() {
            BsonDocument document = new BsonDocument() { { "a", "x" }, { "b", "y" } };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"a\" : \"x\",\r\n  \"b\" : \"y\"\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestEmbeddedDocument() {
            BsonDocument document = new BsonDocument() {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            string json = document.ToJson();
            string expected = "{ \"doc\" : { \"a\" : 1, \"b\" : 2 } }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestIndentedEmbeddedDocument() {
            BsonDocument document = new BsonDocument() {
                { "doc", new BsonDocument { { "a", 1 }, { "b", 2 } } }
            };
            var settings = new JsonWriterSettings { Indent = true };
            string json = document.ToJson(settings);
            string expected = "{\r\n  \"doc\" : {\r\n    \"a\" : 1,\r\n    \"b\" : 2\r\n  }\r\n}";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestArray() {
            BsonDocument document = new BsonDocument() {
                { "array", new BsonArray { 1, 2, 3 } }
            };
            string json = document.ToJson();
            string expected = "{ \"array\" : [1, 2, 3] }";
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestDateTime() {
            DateTime jan_1_2010 = new DateTime(2010, 1, 1);
        	double expectedValue = (jan_1_2010.ToUniversalTime() - BsonConstants.UnixEpoch).TotalMilliseconds;
            BsonDocument document = new BsonDocument() {
                { "date", jan_1_2010 }
            };
            var settings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            string json = document.ToJson(settings);
        	string expected = "{ \"date\" : { \"$date\" : # } }".Replace("#", expectedValue.ToString());
            Assert.AreEqual(expected, json);
            settings = new JsonWriterSettings { OutputMode = JsonOutputMode.JavaScript };
            json = document.ToJson(settings);
			expected = "{ \"date\" : Date(#) }".Replace("#", expectedValue.ToString());
            Assert.AreEqual(expected, json);
        }

        [Test]
        public void TestBinary() {
            var document = new BsonDocument {
                { "bin", new BsonBinaryData(new byte[] { 1, 2, 3 }) }
            };
            string expected = "{ \"bin\" : { \"$binary\" : \"AQID\", \"$type\" : \"00\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestJavaScript() {
            var document = new BsonDocument {
                { "f", new BsonJavaScript("function f() { return 1; }") }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return 1; }\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestJavaScriptWithScope() {
            var document = new BsonDocument {
                { "f", new BsonJavaScriptWithScope("function f() { return n; }", new BsonDocument("n", 1)) }
            };
            string expected = "{ \"f\" : { \"$code\" : \"function f() { return n; }\", \"$scope\" : { \"n\" : 1 } } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestGuid() {
            var document = new BsonDocument {
                { "guid", new Guid("B5F21E0C2A0D42d6AD03D827008D8AB6") }
            };
            string expected = "{ \"guid\" : { \"$binary\" : \"DB7ytQ0q1kKtA9gnAI2Ktg==\", \"$type\" : \"03\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMaxKey() {
            var document = new BsonDocument {
                { "maxkey", BsonMaxKey.Value }
            };
            string expected = "{ \"maxkey\" : { \"$maxkey\" : 1 } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestMinKey() {
            var document = new BsonDocument {
                { "minkey", BsonMinKey.Value }
            };
            string expected = "{ \"minkey\" : { \"$minkey\" : 1 } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestNull() {
            var document = new BsonDocument {
                { "maxkey", BsonNull.Value }
            };
            string expected = "{ \"maxkey\" : null }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestSymbol() {
            var document = new BsonDocument {
                { "symbol", BsonSymbol.Create("name") }
            };
            string expected = "{ \"symbol\" : { \"$symbol\" : \"name\" } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void TestTimestamp() {
            var document = new BsonDocument {
                { "timestamp", new BsonTimestamp(1234567890) }
            };
            string expected = "{ \"timestamp\" : { \"$timestamp\" : 1234567890 } }";
            string actual = document.ToJson();
            Assert.AreEqual(expected, actual);
        }
    }
}
