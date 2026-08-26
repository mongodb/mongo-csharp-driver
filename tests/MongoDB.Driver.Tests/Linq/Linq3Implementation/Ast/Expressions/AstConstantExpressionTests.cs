/* Copyright 2010-present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Linq3Implementation.Ast;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Ast.Expressions
{
    public class AstConstantExpressionTests
    {
        [Fact]
        public void constructor_should_return_expected_result()
        {
            var value = new BsonInt32(0);

            var subject = new AstConstantExpression(value);

            subject.NodeType.Should().Be(AstNodeType.ConstantExpression);
            subject.Value.Should().BeSameAs(value);
        }

        [Theory]
        // values the server would not reinterpret are rendered as is
        [InlineData("1", "1")]
        [InlineData("1.5", "1.5")]
        [InlineData("true", "true")]
        [InlineData("null", "null")]
        [InlineData("''", "''")]
        [InlineData("'abc'", "'abc'")]
        [InlineData("'a$b'", "'a$b'")]
        [InlineData("{ }", "{ }")]
        [InlineData("{ a : 1 }", "{ a : 1 }")]
        [InlineData("{ a : { b : 1 } }", "{ a : { b : 1 } }")]
        [InlineData("[]", "[]")]
        [InlineData("[1, 'abc']", "[1, 'abc']")]
        [InlineData("[{ a : 1 }]", "[{ a : 1 }]")]
        // strings the server would interpret as field paths or variables are quoted
        [InlineData("'$abc'", "{ $literal : '$abc' }")]
        [InlineData("'$$ROOT'", "{ $literal : '$$ROOT' }")]
        // documents containing values the server would interpret as field paths are quoted
        [InlineData("{ a : '$b' }", "{ $literal : { a : '$b' } }")]
        [InlineData("{ a : { b : '$c' } }", "{ $literal : { a : { b : '$c' } } }")]
        [InlineData("{ a : ['$b'] }", "{ $literal : { a : ['$b'] } }")]
        // documents containing keys the server would interpret as operators are quoted
        [InlineData("{ $cond : 1 }", "{ $literal : { $cond : 1 } }")]
        [InlineData("{ a : { $function : 1 } }", "{ $literal : { a : { $function : 1 } } }")]
        // documents containing keys the server would interpret as paths to nested fields are quoted
        [InlineData("{ 'a.b' : 1 }", "{ $literal : { 'a.b' : 1 } }")]
        [InlineData("{ a : { 'b.c' : 1 } }", "{ $literal : { a : { 'b.c' : 1 } } }")]
        [InlineData("[{ 'a.b' : 1 }]", "{ $literal : [{ 'a.b' : 1 }] }")]
        // arrays containing values the server would interpret as field paths or operators are quoted
        [InlineData("['$a']", "{ $literal : ['$a'] }")]
        [InlineData("[1, '$a']", "{ $literal : [1, '$a'] }")]
        [InlineData("[{ a : '$b' }]", "{ $literal : [{ a : '$b' }] }")]
        [InlineData("[{ $cond : 1 }]", "{ $literal : [{ $cond : 1 }] }")]
        [InlineData("[['$a']]", "{ $literal : [['$a']] }")]
        public void Render_should_return_expected_result(string valueJson, string expectedResultJson)
        {
            var value = ParseValue(valueJson);
            var subject = new AstConstantExpression(value);

            var result = subject.Render();

            result.Should().Be(ParseValue(expectedResultJson));
        }

        private static BsonValue ParseValue(string json)
        {
            return BsonDocument.Parse($"{{ v : {json} }}")["v"];
        }
    }
}
