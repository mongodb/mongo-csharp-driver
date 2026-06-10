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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class WindowTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_window_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = CreatePartitionSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    private static SerializerMap CreatePartitionSerializerMap(LambdaExpression expression)
    {
        var partitionParameter = expression.Parameters.Single();
        var inputType = partitionParameter.Type.GetGenericArguments()[0];
        var lookupMethod = typeof(BsonSerializer)
            .GetMethod(nameof(BsonSerializer.LookupSerializer), Type.EmptyTypes)!
            .MakeGenericMethod(inputType);
        var inputSerializer = (IBsonSerializer)lookupMethod.Invoke(null, null)!;
        var partitionSerializerType = typeof(ISetWindowFieldsPartitionSerializer<>).MakeGenericType(inputType);
        var partitionSerializer = (IBsonSerializer)Activator.CreateInstance(partitionSerializerType, inputSerializer)!;
        return TestHelpers.CreateSerializerMap(expression, partitionSerializer);
    }

    public static readonly object[][] TestCases =
    [
        // AddToSet
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.AddToSet(x => x.IntField, null)), typeof(IEnumerableSerializer<int>)],

        // Average
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.DoubleField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.FloatField, null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.IntField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.LongField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.NullableDecimalField, null)), typeof(NullableSerializer<decimal>)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Average(x => x.NullableIntField, null)), typeof(NullableSerializer<double>)],

        // Count
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Count(null)), typeof(Int64Serializer)],

        // CovariancePopulation
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.CovariancePopulation(x => x.DecimalField, x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.CovariancePopulation(x => x.IntField, x => x.IntField, null)), typeof(DoubleSerializer)],

        // CovarianceSample
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.CovarianceSample(x => x.DecimalField, x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.CovarianceSample(x => x.IntField, x => x.IntField, null)), typeof(DoubleSerializer)],

        // DenseRank
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.DenseRank()), typeof(DecimalSerializer)],

        // Derivative
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Derivative(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Derivative(x => x.DecimalField, WindowTimeUnit.Day, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Derivative(x => x.IntField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Derivative(x => x.IntField, WindowTimeUnit.Day, null)), typeof(DoubleSerializer)],

        // DocumentNumber
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.DocumentNumber()), typeof(DecimalSerializer)],

        // ExponentialMovingAverage
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.ExponentialMovingAverage(x => x.DecimalField, ExponentialMovingAverageWeighting.Alpha(0.1), null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.ExponentialMovingAverage(x => x.FloatField, ExponentialMovingAverageWeighting.Alpha(0.1), null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.ExponentialMovingAverage(x => x.IntField, ExponentialMovingAverageWeighting.Alpha(0.1), null)), typeof(DoubleSerializer)],

        // First
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.First(x => x.IntField, null)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.First(x => x.DecimalField, null)), typeof(DecimalSerializer)],

        // Integral
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Integral(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Integral(x => x.DecimalField, WindowTimeUnit.Day, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Integral(x => x.IntField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Integral(x => x.IntField, WindowTimeUnit.Day, null)), typeof(DoubleSerializer)],

        // Last
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Last(x => x.IntField, null)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Last(x => x.DecimalField, null)), typeof(DecimalSerializer)],

        // Locf
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Locf(x => x.IntField, null)), typeof(Int32Serializer)],

        // Max
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Max(x => x.IntField, null)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Max(x => x.DecimalField, null)), typeof(DecimalSerializer)],

        // Median
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Median(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Median(x => x.FloatField, null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Median(x => x.IntField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Median(x => x.NullableDecimalField, null)), typeof(NullableSerializer<decimal>)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Median(x => x.NullableIntField, null)), typeof(NullableSerializer<double>)],

        // Min
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Min(x => x.IntField, null)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Min(x => x.DecimalField, null)), typeof(DecimalSerializer)],

        // Percentile
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Percentile(x => x.DecimalField, new double[] { 0.5 }, null)), typeof(ArraySerializer<decimal>)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Percentile(x => x.IntField, new double[] { 0.5 }, null)), typeof(ArraySerializer<double>)],

        // Push
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Push(x => x.IntField, null)), typeof(IEnumerableSerializer<int>)],

        // Rank
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Rank()), typeof(DecimalSerializer)],

        // Shift
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Shift(x => x.IntField, 1)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Shift(x => x.IntField, 1, 0)), typeof(Int32Serializer)],

        // StandardDeviationPopulation
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationPopulation(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationPopulation(x => x.FloatField, null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationPopulation(x => x.IntField, null)), typeof(DoubleSerializer)],

        // StandardDeviationSample
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationSample(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationSample(x => x.FloatField, null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.StandardDeviationSample(x => x.IntField, null)), typeof(DoubleSerializer)],

        // Sum
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Sum(x => x.DecimalField, null)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Sum(x => x.DoubleField, null)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Sum(x => x.FloatField, null)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Sum(x => x.IntField, null)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((ISetWindowFieldsPartition<MyModel> p) => p.Sum(x => x.LongField, null)), typeof(Int64Serializer)],
    ];

    private class MyModel
    {
        public int IntField { get; set; }
        public long LongField { get; set; }
        public double DoubleField { get; set; }
        public decimal DecimalField { get; set; }
        public float FloatField { get; set; }
        public int? NullableIntField { get; set; }
        public decimal? NullableDecimalField { get; set; }
    }
}
