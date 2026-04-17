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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class MqlTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_mql_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => Mql.DateFromString(model.DateString)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.DateFromString(model.DateString, "yyyy-MM-dd")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.DateFromString(model.DateString, "yyyy-MM-dd", "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.DateFromString(model.DateString, "yyyy-MM-dd", "UTC", null, null)), typeof(NullableSerializer<DateTime>)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.Exists(model.Field)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.IsMissing(model.Field)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.IsNullOrMissing(model.Field)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.Sigmoid(model.Value)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.SimilarityCosine(model.Vector1, model.Vector2, false)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.SimilarityDotProduct(model.Vector1, model.Vector2, false)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.SimilarityEuclidean(model.Vector1, model.Vector2, false)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.Subtype(model.Data)), typeof(NullableSerializer<BsonBinarySubType>)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.Hash(model.Data, MqlHashAlgorithm.SHA256)), typeof(BsonBinaryDataSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.ToHashedIndexKey(model.Field)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => Mql.HexHash(model.Data, MqlHashAlgorithm.SHA256)), typeof(StringSerializer)]
    ];

    private class MyModel
    {
        public string Field { get; set; }
        public string DateString { get; set; }
        public byte[] Data { get; set; }
        public double Value { get; set; }
        public double[] Vector1 { get; set; }
        public double[] Vector2 { get; set; }
    }
}
