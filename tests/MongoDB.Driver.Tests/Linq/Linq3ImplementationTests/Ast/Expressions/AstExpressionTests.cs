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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3ImplementationTests.Ast.Expressions
{
    public class AstExpressionTests
    {
        [Theory]
        [InlineData("bool", (int)AstUnaryOperator.ToBool)]
        [InlineData("date", (int)AstUnaryOperator.ToDate)]
        [InlineData("decimal", (int)AstUnaryOperator.ToDecimal)]
        [InlineData("double", (int)AstUnaryOperator.ToDouble)]
        [InlineData("int", (int)AstUnaryOperator.ToInt)]
        [InlineData("long", (int)AstUnaryOperator.ToLong)]
        [InlineData("objectId", (int)AstUnaryOperator.ToObjectId)]
        [InlineData("string", (int)AstUnaryOperator.ToString)]
        public void Convert_with_to_constant_should_return_short_form_when_possible(string toValue, int expectedOperator)
        {
            var input = AstExpression.Constant(BsonNull.Value);
            var to = AstExpression.Constant(toValue);

            var result = AstExpression.Convert(input, to, onError: null, onNull: null);

            var unaryExpression = result.Should().BeOfType<AstUnaryExpression>().Subject;
            unaryExpression.Operator.Should().Be((AstUnaryOperator)expectedOperator);
            unaryExpression.Arg.Should().BeSameAs(input);
        }

        [Theory]
        [InlineData("xyz")]
        public void Convert_with_to_constant_should_return_long_form_when_necessary(string toValue)
        {
            var input = AstExpression.Constant(BsonNull.Value);
            var to = AstExpression.Constant(toValue);

            var result = AstExpression.Convert(input, to, onError: null, onNull: null);

            var convertExpression = result.Should().BeOfType<AstConvertExpression>().Subject;
            convertExpression.Input.Should().BeSameAs(input);
            convertExpression.To.Should().BeSameAs(to);
            convertExpression.OnError.Should().BeNull();
            convertExpression.OnNull.Should().BeNull();
        }

        [Theory]
        [InlineData("bool")]
        [InlineData("date")]
        [InlineData("decimal")]
        [InlineData("double")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("objectId")]
        [InlineData("string")]
        public void Convert_with_to_expression_should_return_long_form(string toValue)
        {
            var input = AstExpression.Constant(BsonNull.Value);
            var to = AstExpression.FieldPath("$To");

            var result = AstExpression.Convert(input, to, onError: null, onNull: null);

            var convertExpression = result.Should().BeOfType<AstConvertExpression>().Subject;
            convertExpression.Input.Should().BeSameAs(input);
            convertExpression.To.Should().BeSameAs(to);
            convertExpression.OnError.Should().BeNull();
            convertExpression.OnNull.Should().BeNull();
        }

        [Theory]
        [InlineData("bool")]
        [InlineData("date")]
        [InlineData("decimal")]
        [InlineData("double")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("objectId")]
        [InlineData("string")]
        public void Convert_with_on_error_should_return_long_form(string toValue)
        {
            var input = AstExpression.Constant(BsonNull.Value);
            var to = AstExpression.Constant(toValue);
            var onError = AstExpression.Constant(BsonNull.Value);

            var result = AstExpression.Convert(input, to, onError, onNull: null);

            var convertExpression = result.Should().BeOfType<AstConvertExpression>().Subject;
            convertExpression.Input.Should().BeSameAs(input);
            convertExpression.To.Should().BeSameAs(to);
            convertExpression.OnError.Should().BeSameAs(onError);
            convertExpression.OnNull.Should().BeNull();
        }

        [Theory]
        [InlineData("bool")]
        [InlineData("date")]
        [InlineData("decimal")]
        [InlineData("double")]
        [InlineData("int")]
        [InlineData("long")]
        [InlineData("objectId")]
        [InlineData("string")]
        public void Convert_with_on_null_should_return_long_form(string toValue)
        {
            var input = AstExpression.Constant(BsonNull.Value);
            var to = AstExpression.Constant(toValue);
            var onNull = AstExpression.Constant(BsonNull.Value);

            var result = AstExpression.Convert(input, to, onError: null, onNull);

            var convertExpression = result.Should().BeOfType<AstConvertExpression>().Subject;
            convertExpression.Input.Should().BeSameAs(input);
            convertExpression.To.Should().BeSameAs(to);
            convertExpression.OnError.Should().BeNull();
            convertExpression.OnNull.Should().BeSameAs(onNull);
        }

        [Fact]
        public void Unary_should_return_expected_result()
        {
            var @operator = AstUnaryOperator.Abs;
            var arg = AstExpression.Constant(-1);

            var result = AstExpression.Unary(@operator, arg);

            var unaryExpression = result.Should().BeOfType<AstUnaryExpression>().Subject;
            unaryExpression.Operator.Should().Be(@operator);
            unaryExpression.Arg.Should().BeSameAs(arg);
        }
    }
}
