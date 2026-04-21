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

public class HashMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst, Type expectedSerializerType)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = HashMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType(expectedSerializerType);
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void Translate_should_throw_on_non_supported_expressions(LambdaExpression expression)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var exception = Record.Exception(() => HashMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.ByteArray, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'ByteArray', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.BsonBinary, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'BsonBinary', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.Guid, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'Guid', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.GuidWithStandardRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'GuidWithStandardRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.GuidWithCSharpLegacyRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'GuidWithCSharpLegacyRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.GuidWithStringRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'GuidWithStringRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.StringData, MqlHashAlgorithm.SHA256)),
            "{ $hash : { input : { $getField : { field : 'StringData', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(BsonBinaryDataSerializer)
        ],

        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.ByteArray, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'ByteArray', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.BsonBinary, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'BsonBinary', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.Guid, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'Guid', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.GuidWithStandardRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'GuidWithStandardRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.GuidWithCSharpLegacyRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'GuidWithCSharpLegacyRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.GuidWithStringRepresentation, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'GuidWithStringRepresentation', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.StringData, MqlHashAlgorithm.SHA256)),
            "{ $hexHash : { input : { $getField : { field : 'StringData', input : '$$ROOT' } }, algorithm : 'sha256' } }",
            typeof(StringSerializer)
        ]
    ];

    public static IEnumerable<object[]> NonSupportedTestCases =
    [
        [TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.ByteArray, MqlHashAlgorithm.Undefined))],
        [TestHelpers.MakeLambda<MyModel, BsonBinaryData>(model => Mql.Hash(model.Int, MqlHashAlgorithm.SHA256))],

        [TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.ByteArray, MqlHashAlgorithm.Undefined))],
        [TestHelpers.MakeLambda<MyModel, string>(model => Mql.HexHash(model.Int, MqlHashAlgorithm.SHA256))],
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
        public string StringData { get; set; }
    }
}

