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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class MathTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_math_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.DecimalValue)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.ShortValue)), typeof(Int16Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.IntValue)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.LongValue)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.SByteValue)), typeof(SByteSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Abs(model.FloatValue)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Acos(model.DoubleValue)), typeof(DoubleSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => Math.Acosh(model.DoubleValue)), typeof(DoubleSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => Math.Asin(model.DoubleValue)), typeof(DoubleSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => Math.Asinh(model.DoubleValue)), typeof(DoubleSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => Math.Atan(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Atan2(model.DoubleValue, 1.0)), typeof(DoubleSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => Math.Atanh(model.DoubleValue)), typeof(DoubleSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => Math.Ceiling(model.DecimalValue)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Ceiling(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Cos(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Cosh(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Exp(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Floor(model.DecimalValue)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Floor(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Log(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Log(model.DoubleValue, 2.0)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Log10(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Pow(model.DoubleValue, 2.0)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Round(model.DecimalValue)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Round(model.DecimalValue, 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Round(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Round(model.DoubleValue, 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Sin(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Sinh(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Sqrt(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Tan(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Tanh(model.DoubleValue)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Truncate(model.DecimalValue)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Math.Truncate(model.DoubleValue)), typeof(DoubleSerializer)],
    ];

    private class MyModel
    {
        public decimal DecimalValue { get; set; }
        public double DoubleValue { get; set; }
        public short ShortValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public sbyte SByteValue { get; set; }
        public float FloatValue { get; set; }
    }
}
