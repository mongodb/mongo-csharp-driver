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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class GenerateNewIdMethodToAggregationExpressionTranslatorTests
{
    [Fact]
    public void Translate_should_produce_proper_ast()
    {
        var expression = TestHelpers.MakeLambda<MyModel, ObjectId>(model => ObjectId.GenerateNewId());
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = GenerateNewIdMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<ObjectIdSerializer>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse("{ $createObjectId: { } }"));
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void Translate_should_throw_on_non_supported_expressions(LambdaExpression expression)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var exception = Record.Exception(() => GenerateNewIdMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static IEnumerable<object[]> NonSupportedTestCases =
    [
        [TestHelpers.MakeLambda<MyModel, ObjectId>(model => ObjectId.GenerateNewId(42))],
        [TestHelpers.MakeLambda<MyModel, ObjectId>(model => ObjectId.GenerateNewId(DateTime.Parse("2026-01-01T00:00:00Z")))],
    ];

    public class MyModel
    {
    }
}

