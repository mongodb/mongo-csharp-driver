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

using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class EncStrMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = EncStrMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<BooleanSerializer>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        [
            TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrStartsWith(model.Text, "pre")),
            "{ $encStrStartsWith : { input : { $getField : { field : 'Text', input : '$$ROOT' } }, prefix : 'pre' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrEndsWith(model.Text, "suf")),
            "{ $encStrEndsWith : { input : { $getField : { field : 'Text', input : '$$ROOT' } }, suffix : 'suf' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrContains(model.Text, "sub")),
            "{ $encStrContains : { input : { $getField : { field : 'Text', input : '$$ROOT' } }, substring : 'sub' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrNormalizedEq(model.Text, "eq")),
            "{ $encStrNormalizedEq : { input : { $getField : { field : 'Text', input : '$$ROOT' } }, string : 'eq' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrStartsWith(model.Sub.Text, "pre")),
            "{ $encStrStartsWith : { input : { $getField : { field : 'Text', input : { $getField : { field : 'Sub', input : '$$ROOT' } } } }, prefix : 'pre' } }"
        ],
    ];

    [Fact]
    public void Translate_should_throw_when_value_is_not_serialized_as_string()
    {
        var expression = TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrStartsWith(model.Text, model.ObjectIdString));
        var translationContext = TestHelpers.CreateTranslationContext(expression);

        var exception = Record.Exception(() =>
            EncStrMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("it is not serialized as a string");
    }

    [Fact]
    public void Translate_should_throw_when_input_is_not_serialized_as_string()
    {
        var expression = TestHelpers.MakeLambda<MyModel, bool>(model => Mql.EncStrStartsWith(model.ObjectIdString, "pre"));
        var translationContext = TestHelpers.CreateTranslationContext(expression);

        var exception = Record.Exception(() =>
            EncStrMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
        exception.Message.Should().Contain("it is not serialized as a string");
    }

    public class MyModel
    {
        public string Text { get; set; }
        public SubModel Sub { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string ObjectIdString { get; set; }
    }

    public class SubModel
    {
        public string Text { get; set; }
    }
}
