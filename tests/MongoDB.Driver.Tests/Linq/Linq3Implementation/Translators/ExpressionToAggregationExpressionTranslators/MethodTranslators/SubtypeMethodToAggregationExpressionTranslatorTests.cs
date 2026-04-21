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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class SubtypeMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = SubtypeMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<NullableSerializer<BsonBinarySubType>>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void Translate_should_throw_on_non_supported_expressions(LambdaExpression expression)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var exception = Record.Exception(() => SubtypeMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.ByteArray)), "{ $subtype : { $getField : { field : 'ByteArray', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.BsonBinary)), "{ $subtype : { $getField : { field : 'BsonBinary', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.Guid)), "{ $subtype : { $getField : { field : 'Guid', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.GuidWithStandardRepresentation)), "{ $subtype : { $getField : { field : 'GuidWithStandardRepresentation', input : '$$ROOT' } } }"],
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.GuidWithCSharpLegacyRepresentation)), "{ $subtype : { $getField : { field : 'GuidWithCSharpLegacyRepresentation', input : '$$ROOT' } } }"],
    ];

    public static IEnumerable<object[]> NonSupportedTestCases =
    [
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.Int))],
        [TestHelpers.MakeLambda<MyModel, BsonBinarySubType?>(model => Mql.Subtype(model.GuidWithStringRepresentation))],
    ];

    public class MyModel
    {
        public int Int { get; set; }
        public byte[] ByteArray { get; set; }
        public BsonBinaryData BsonBinary { get; set; }
        public Guid Guid { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid GuidWithStandardRepresentation { get; set; }
        [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
        public Guid GuidWithCSharpLegacyRepresentation { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Guid GuidWithStringRepresentation { get; set; }
    }
}

