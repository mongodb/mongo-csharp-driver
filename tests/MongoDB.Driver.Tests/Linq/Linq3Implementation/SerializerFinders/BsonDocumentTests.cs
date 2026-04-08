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

using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class BsonDocumentTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void SerializerFinder_resolve_serializer_for_supported_BsonDocument_members(LambdaExpression expression, IBsonSerializer expected)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(NotSupportedTestCases))]
    public void SerializerFinder_resolve_unknowable_serializer_for_unsupported_BsonDocument_members(LambdaExpression expression)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out var serializer).Should().BeTrue();
        serializer.Should().BeOfType(typeof(UnknowableSerializer<>).MakeGenericType(expression.Body.Type));
        var exception = Record.Exception(() => serializerMap.GetSerializer(expression.Body));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static readonly object[][] SupportedTestCases =
    [
        [TestHelpers.MakeLambda((BsonDocument d) => d["name"]), BsonValueSerializer.Instance]
    ];

    public static readonly object[][] NotSupportedTestCases =
    [
        [TestHelpers.MakeLambda((BsonDocument d) => d.ElementCount)],
        [TestHelpers.MakeLambda((BsonDocument d) => d.Elements)],
        [TestHelpers.MakeLambda((BsonDocument d) => d.Values)],
        [TestHelpers.MakeLambda((BsonDocument d) => d.Names)],

        [TestHelpers.MakeLambda((RawBsonDocument d) => d.ElementCount)],
        [TestHelpers.MakeLambda((RawBsonDocument d) => d.Elements)],
        [TestHelpers.MakeLambda((RawBsonDocument d) => d.Values)],
        [TestHelpers.MakeLambda((RawBsonDocument d) => d.Names)],
    ];
}

