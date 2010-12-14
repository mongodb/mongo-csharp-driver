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

namespace MongoDB.BsonUnitTests.IO {
    [TestFixture]
    public class BsonJsonScannerTests {
        [Test]
        public void TestEndOfFile() {
            var json = "\t ";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.EndOfFile, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestBeginObject() {
            var json = "\t {x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.BeginObject, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestEndObject() {
            var json = "\t }x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.EndObject, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestBeginArray() {
            var json = "\t [x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.BeginArray, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestEndArray() {
            var json = "\t ]x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.EndArray, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestNameSeparator() {
            var json = "\t :x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.NameSeparator, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestValueSeparator() {
            var json = "\t ,x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.ValueSeparator, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestEmptyString() {
            var json = "\t \"\"x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("", token.Value);
        }

        [Test]
        public void Test1CharacterString() {
            var json = "\t \"1\"x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("1", token.Value);
        }

        [Test]
        public void Test2CharacterString() {
            var json = "\t \"12\"x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("12", token.Value);
        }

        [Test]
        public void Test3CharacterString() {
            var json = "\t \"123\"x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("123", token.Value);
        }

        [Test]
        public void TestEscapeSequences() {
            var json = "\t \"x\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0030y\"x";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("x\"\\/\b\f\n\r\t0y", token.Value);
        }

        [Test]
        public void TestTrue() {
            var json = "\t true";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Boolean, token.Type);
            Assert.AreEqual("true", token.Value);
        }

        [Test]
        public void TestFalse() {
            var json = "\t false";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Boolean, token.Type);
            Assert.AreEqual("false", token.Value);
        }

        [Test]
        public void TestNull() {
            var json = "\t null";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Null, token.Type);
            Assert.IsNull(token.Value);
        }

        [Test]
        public void TestUnquotedString() {
            var json = "\t name123:1";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("name123", token.Value);
        }

        [Test]
        public void TestZero() {
            var json = "\t 0,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("0", token.Value);
        }

        [Test]
        public void TestMinusZero() {
            var json = "\t -0,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("-0", token.Value);
        }

        [Test]
        public void TestOne() {
            var json = "\t 1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("1", token.Value);
        }

        [Test]
        public void TestMinusOne() {
            var json = "\t -1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("-1", token.Value);
        }

        [Test]
        public void TestTwelve() {
            var json = "\t 12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("12", token.Value);
        }

        [Test]
        public void TestMinusTwelve() {
            var json = "\t -12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.Integer, token.Type);
            Assert.AreEqual("-12", token.Value);
        }

        [Test]
        public void TestZeroPointZero() {
            var json = "\t 0.0,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("0.0", token.Value);
        }

        [Test]
        public void TestMinusZeroPointZero() {
            var json = "\t -0.0,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-0.0", token.Value);
        }

        [Test]
        public void TestZeroExponentOne() {
            var json = "\t 0e1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("0e1", token.Value);
        }

        [Test]
        public void TestMinusZeroExponentOne() {
            var json = "\t -0e1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-0e1", token.Value);
        }

        [Test]
        public void TestZeroExponentMinusOne() {
            var json = "\t 0e-1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("0e-1", token.Value);
        }

        [Test]
        public void TestMinusZeroExponentMinusOne() {
            var json = "\t -0e-1,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-0e-1", token.Value);
        }

        [Test]
        public void TestOnePointTwo() {
            var json = "\t 1.2,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("1.2", token.Value);
        }

        [Test]
        public void TestMinusOnePointTwo() {
            var json = "\t -1.2,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-1.2", token.Value);
        }

        [Test]
        public void TestOneExponentTwelve() {
            var json = "\t 1e12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("1e12", token.Value);
        }

        [Test]
        public void TestMinusZeroExponentTwelve() {
            var json = "\t -1e12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-1e12", token.Value);
        }

        [Test]
        public void TestOneExponentMinuesTwelve() {
            var json = "\t 1e-12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("1e-12", token.Value);
        }

        [Test]
        public void TestMinusZeroExponentMinusTwelve() {
            var json = "\t -1e-12,";
            var stringReader = new StringReader(json);
            var token = BsonJsonScanner.GetNextToken(stringReader);
            Assert.AreEqual(JsonTokenType.FloatingPoint, token.Type);
            Assert.AreEqual("-1e-12", token.Value);
        }
    }
}
