/* Copyright 2010-2014 MongoDB Inc.
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
using Xunit;

namespace MongoDB.Bson.Tests.IO
{
    public class JsonScannerTests
    {
        [Fact]
        public void TestEndOfFile()
        {
            var json = "\t ";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.EndOfFile, token.Type);
            Assert.Equal("<eof>", token.Lexeme);
            Assert.Equal(-1, buffer.Read());
        }

        [Fact]
        public void TestBeginObject()
        {
            var json = "\t {x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.BeginObject, token.Type);
            Assert.Equal("{", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestEndObject()
        {
            var json = "\t }x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.EndObject, token.Type);
            Assert.Equal("}", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestBeginArray()
        {
            var json = "\t [x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.BeginArray, token.Type);
            Assert.Equal("[", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestEndArray()
        {
            var json = "\t ]x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.EndArray, token.Type);
            Assert.Equal("]", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestNameSeparator()
        {
            var json = "\t :x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Colon, token.Type);
            Assert.Equal(":", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestValueSeparator()
        {
            var json = "\t ,x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Comma, token.Type);
            Assert.Equal(",", token.Lexeme);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestEmptyString()
        {
            var json = "\t \"\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.String, token.Type);
            Assert.Equal("", token.StringValue);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void Test1CharacterString()
        {
            var json = "\t \"1\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.String, token.Type);
            Assert.Equal("1", token.StringValue);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void Test2CharacterString()
        {
            var json = "\t \"12\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.String, token.Type);
            Assert.Equal("12", token.StringValue);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void Test3CharacterString()
        {
            var json = "\t \"123\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.String, token.Type);
            Assert.Equal("123", token.StringValue);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestEscapeSequences()
        {
            var json = "\t \"x\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0030y\"x";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.String, token.Type);
            Assert.Equal("x\"\\/\b\f\n\r\t0y", token.StringValue);
            Assert.Equal('x', buffer.Read());
        }

        [Fact]
        public void TestTrue()
        {
            var json = "\t true,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.UnquotedString, token.Type);
            Assert.Equal("true", token.StringValue);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestFalse()
        {
            var json = "\t false,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.UnquotedString, token.Type);
            Assert.Equal("false", token.StringValue);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestNull()
        {
            var json = "\t null,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.UnquotedString, token.Type);
            Assert.Equal("null", token.StringValue);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestUndefined()
        {
            var json = "\t undefined,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.UnquotedString, token.Type);
            Assert.Equal("undefined", token.StringValue);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestUnquotedString()
        {
            var json = "\t name123:1";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.UnquotedString, token.Type);
            Assert.Equal("name123", token.StringValue);
            Assert.Equal(':', buffer.Read());
        }

        [Fact]
        public void TestZero()
        {
            var json = "\t 0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("0", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZero()
        {
            var json = "\t -0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("-0", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestOne()
        {
            var json = "\t 1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusOne()
        {
            var json = "\t -1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("-1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestTwelve()
        {
            var json = "\t 12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusTwelve()
        {
            var json = "\t -12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Int32, token.Type);
            Assert.Equal("-12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestZeroPointZero()
        {
            var json = "\t 0.0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("0.0", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZeroPointZero()
        {
            var json = "\t -0.0,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-0.0", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestZeroExponentOne()
        {
            var json = "\t 0e1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("0e1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZeroExponentOne()
        {
            var json = "\t -0e1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-0e1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestZeroExponentMinusOne()
        {
            var json = "\t 0e-1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("0e-1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZeroExponentMinusOne()
        {
            var json = "\t -0e-1,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-0e-1", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestOnePointTwo()
        {
            var json = "\t 1.2,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("1.2", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusOnePointTwo()
        {
            var json = "\t -1.2,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-1.2", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestOneExponentTwelve()
        {
            var json = "\t 1e12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("1e12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZeroExponentTwelve()
        {
            var json = "\t -1e12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-1e12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestOneExponentMinuesTwelve()
        {
            var json = "\t 1e-12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("1e-12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestMinusZeroExponentMinusTwelve()
        {
            var json = "\t -1e-12,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.Double, token.Type);
            Assert.Equal("-1e-12", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestRegularExpressionEmpty()
        {
            var json = "\t //,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.RegularExpression, token.Type);
            Assert.Equal("//", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestRegularExpressionPattern()
        {
            var json = "\t /pattern/,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.RegularExpression, token.Type);
            Assert.Equal("/pattern/", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }

        [Fact]
        public void TestRegularExpressionPatternAndOptions()
        {
            var json = "\t /pattern/imxs,";
            var buffer = new JsonBuffer(json);
            var token = JsonScanner.GetNextToken(buffer);
            Assert.Equal(JsonTokenType.RegularExpression, token.Type);
            Assert.Equal("/pattern/imxs", token.Lexeme);
            Assert.Equal(',', buffer.Read());
        }
    }
}
