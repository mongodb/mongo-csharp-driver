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
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class DeserializeEJsonMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = DeserializeEJsonMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        // No options (defaults)
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.DeserializeEJson<BsonDocument, BsonDocument>(model.Document, null)),
            "{ $deserializeEJSON : { input : { $getField : { field : 'Document', input : '$$ROOT' } } } }"
        ],
        // With BsonValue input
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.DeserializeEJson<BsonValue, BsonDocument>(model.Value, null)),
            "{ $deserializeEJSON : { input : { $getField : { field : 'Value', input : '$$ROOT' } } } }"
        ],
        // With onError
        [
            TestHelpers.MakeLambda<MyModel, BsonValue>(model => Mql.DeserializeEJson<BsonDocument, BsonValue>(model.Document, new DeserializeEJsonOptions<BsonValue> { OnError = "fallback" })),
            "{ $deserializeEJSON : { input : { $getField : { field : 'Document', input : '$$ROOT' } }, onError : 'fallback' } }"
        ],
    ];

    public class MyModel
    {
        public BsonDocument Document { get; set; }
        public BsonValue Value { get; set; }
    }
}
