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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class ToHashedIndexKeyMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = ToHashedIndexKeyMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<Int64Serializer>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        [TestHelpers.MakeLambda<MyModel, long>(model => Mql.ToHashedIndexKey(model.StringValue)), "{ $toHashedIndexKey : { $getField : { field : 'StringValue', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, long>(model => Mql.ToHashedIndexKey(model.IntValue)), "{ $toHashedIndexKey : { $getField : { field : 'IntValue', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, long>(model => Mql.ToHashedIndexKey(model.DoubleValue)), "{ $toHashedIndexKey : { $getField : { field : 'DoubleValue', input : '$$ROOT' } } }"],
    ];

    public class MyModel
    {
        public string StringValue { get; set; }
        public int IntValue { get; set; }
        public double DoubleValue { get; set; }
    }
}
