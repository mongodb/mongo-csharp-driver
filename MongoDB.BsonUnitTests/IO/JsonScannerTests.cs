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

using MongoDB.Bson.IO;
using NUnit.Framework;

namespace MongoDB.BsonUnitTests.IO
{
    [TestFixture]
    public class JsonScannerTests
    {
        [Test]
        public void TestEndOfFile()
        {
            var json = "\t ";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.EndOfFile, token.Type);
            Assert.AreEqual("<eof>", token.Lexeme);
            Assert.AreEqual(-1, buffer.Read());
        }

        [Test]
        public void TestBeginObject()
        {
            var json = "\t {x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.BeginObject, token.Type);
            Assert.AreEqual("{", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestEndObject()
        {
            var json = "\t }x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.EndObject, token.Type);
            Assert.AreEqual("}", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestBeginArray()
        {
            var json = "\t [x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.BeginArray, token.Type);
            Assert.AreEqual("[", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestEndArray()
        {
            var json = "\t ]x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.EndArray, token.Type);
            Assert.AreEqual("]", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestNameSeparator()
        {
            var json = "\t :x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Colon, token.Type);
            Assert.AreEqual(":", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestValueSeparator()
        {
            var json = "\t ,x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Comma, token.Type);
            Assert.AreEqual(",", token.Lexeme);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestEmptyString()
        {
            var json = "\t \"\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("", token.StringValue);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void Test1CharacterString()
        {
            var json = "\t \"1\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("1", token.StringValue);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void Test2CharacterString()
        {
            var json = "\t \"12\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("12", token.StringValue);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void Test3CharacterString()
        {
            var json = "\t \"123\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("123", token.StringValue);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestEscapeSequences()
        {
            var json = "\t \"x\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0030y\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.String, token.Type);
            Assert.AreEqual("x\"\\/\b\f\n\r\t0y", token.StringValue);
            Assert.AreEqual('x', buffer.Read());
        }

        [Test]
        public void TestTrue()
        {
            var json = "\t true,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("true", token.StringValue);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestFalse()
        {
            var json = "\t false,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("false", token.StringValue);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestNull()
        {
            var json = "\t null,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("null", token.StringValue);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestUndefined()
        {
            var json = "\t undefined,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("undefined", token.StringValue);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestUnquotedString()
        {
            var json = "\t name123:1";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.UnquotedString, token.Type);
            Assert.AreEqual("name123", token.StringValue);
            Assert.AreEqual(':', buffer.Read());
        }

        [Test]
        public void TestZero()
        {
            var json = "\t 0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("0", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZero()
        {
            var json = "\t -0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("-0", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestOne()
        {
            var json = "\t 1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusOne()
        {
            var json = "\t -1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("-1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestTwelve()
        {
            var json = "\t 12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusTwelve()
        {
            var json = "\t -12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Int32, token.Type);
            Assert.AreEqual("-12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestZeroPointZero()
        {
            var json = "\t 0.0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("0.0", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZeroPointZero()
        {
            var json = "\t -0.0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-0.0", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestZeroExponentOne()
        {
            var json = "\t 0e1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("0e1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZeroExponentOne()
        {
            var json = "\t -0e1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-0e1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestZeroExponentMinusOne()
        {
            var json = "\t 0e-1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("0e-1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZeroExponentMinusOne()
        {
            var json = "\t -0e-1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-0e-1", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestOnePointTwo()
        {
            var json = "\t 1.2,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("1.2", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusOnePointTwo()
        {
            var json = "\t -1.2,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-1.2", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestOneExponentTwelve()
        {
            var json = "\t 1e12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("1e12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZeroExponentTwelve()
        {
            var json = "\t -1e12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-1e12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestOneExponentMinuesTwelve()
        {
            var json = "\t 1e-12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("1e-12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestMinusZeroExponentMinusTwelve()
        {
            var json = "\t -1e-12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.Double, token.Type);
            Assert.AreEqual("-1e-12", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestRegularExpressionEmpty()
        {
            var json = "\t //,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.RegularExpression, token.Type);
            Assert.AreEqual("//", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestRegularExpressionPattern()
        {
            var json = "\t /pattern/,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.RegularExpression, token.Type);
            Assert.AreEqual("/pattern/", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }

        [Test]
        public void TestRegularExpressionPatternAndOptions()
        {
            var json = "\t /pattern/imxs,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.AreEqual(JsonTokenType.RegularExpression, token.Type);
            Assert.AreEqual("/pattern/imxs", token.Lexeme);
            Assert.AreEqual(',', buffer.Read());
        }
    }
}
