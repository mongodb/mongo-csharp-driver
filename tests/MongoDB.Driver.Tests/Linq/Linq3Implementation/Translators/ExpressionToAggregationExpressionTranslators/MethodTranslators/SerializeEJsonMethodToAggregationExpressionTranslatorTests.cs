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

public class SerializeEJsonMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = SerializeEJsonMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        // No options (defaults)
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<int, BsonDocument>(model.IntValue, null)),
            "{ $serializeEJSON : { input : { $getField : { field : 'IntValue', input : '$$ROOT' } } } }"
        ],
        // With relaxed = false (canonical)
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<int, BsonDocument>(model.IntValue, new SerializeEJsonOptions<BsonDocument> { Relaxed = false })),
            "{ $serializeEJSON : { input : { $getField : { field : 'IntValue', input : '$$ROOT' } }, relaxed : false } }"
        ],
        // With relaxed = true
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<int, BsonDocument>(model.IntValue, new SerializeEJsonOptions<BsonDocument> { Relaxed = true })),
            "{ $serializeEJSON : { input : { $getField : { field : 'IntValue', input : '$$ROOT' } }, relaxed : true } }"
        ],
        // With string input
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<string, BsonDocument>(model.StringValue, null)),
            "{ $serializeEJSON : { input : { $getField : { field : 'StringValue', input : '$$ROOT' } } } }"
        ],
        // With BsonDocument input
        [
            TestHelpers.MakeLambda<MyModel, BsonDocument>(model => Mql.SerializeEJson<BsonDocument, BsonDocument>(model.Document, null)),
            "{ $serializeEJSON : { input : { $getField : { field : 'Document', input : '$$ROOT' } } } }"
        ],
        // With relaxed and onError
        [
            TestHelpers.MakeLambda<MyModel, BsonValue>(model => Mql.SerializeEJson<int, BsonValue>(model.IntValue, new SerializeEJsonOptions<BsonValue> { Relaxed = false, OnError = "error" })),
            "{ $serializeEJSON : { input : { $getField : { field : 'IntValue', input : '$$ROOT' } }, relaxed : false, onError : 'error' } }"
        ],
    ];

    public class MyModel
    {
        public int IntValue { get; set; }
        public string StringValue { get; set; }
        public BsonDocument Document { get; set; }
    }
}
