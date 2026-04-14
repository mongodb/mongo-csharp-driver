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
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class EnumerableTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_enumerable_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Aggregate((a, b) => a + b)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Aggregate(42, (a, b) => a + b)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Aggregate(42, (a, b) => a + b, result => result.ToString())), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Aggregate((a, b) => a + b)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Aggregate(42, (a, b) => a + b)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Aggregate(42, (a, b) => a + b, result => result.ToString())), typeof(StringSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.All(x => x > 0)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.All(x => x > 0)), typeof(BooleanSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.AllElements()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.AllElements()), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.AllMatchingElements("id")), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.AllMatchingElements("id")), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Any()), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Any(x => x > 0)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Any()), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Any(x => x > 0)), typeof(BooleanSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Average()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Average(x => x * 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Average()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Average(x => x * 2)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Average()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Average(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Average()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Average(x => x * 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Average()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Average(x => x * 2)), typeof(SingleSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Concat(model.OtherItems)), typeof(IEnumerableDeserializingAsCollectionSerializer<IEnumerable<int>,int,List<int>>)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Concat(model.OtherItems)), typeof(EnumerableInterfaceImplementerSerializer<IQueryable<int>,int>)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Contains(1)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Contains(1)), typeof(BooleanSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Count()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Count(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Count()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Count(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.ElementAt(0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.ElementAt(0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.ElementAtOrDefault(0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.ElementAtOrDefault(0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.First()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.First(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.First()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.First(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.FirstMatchingElement()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.FirstMatchingElement()), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.FirstOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.FirstOrDefault(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.FirstOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.FirstOrDefault(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Last()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Last(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Last()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Last(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.LastOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.LastOrDefault(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.LastOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.LastOrDefault(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.LongCount()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.LongCount(x => x > 0)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.LongCount()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.LongCount(x => x > 0)), typeof(Int64Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Max()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Max(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Max()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Max()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Max()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Max()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Max()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Max(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Max()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Max()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Max()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Max()), typeof(SingleSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Median()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Median(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Median()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Median(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Median()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Median(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Median()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Median(x => x * 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Median()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Median(x => x * 2)), typeof(SingleSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Min()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Min(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Min()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Min()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Min()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Min()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Min()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Min(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Min()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Min()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Min()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Min()), typeof(SingleSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Percentile(new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Percentile(x => x * 2, new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Percentile(new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Percentile(x => x * 2, new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Percentile(new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Percentile(x => x * 2, new double[] { 0.5 })), typeof(ArraySerializer<double>)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Percentile(new double[] { 0.5 })), typeof(ArraySerializer<decimal>)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Percentile(x => x * 2, new double[] { 0.5 })), typeof(ArraySerializer<decimal>)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Percentile(new double[] { 0.5 })), typeof(ArraySerializer<float>)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Percentile(x => x * 2, new double[] { 0.5 })), typeof(ArraySerializer<float>)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Select(x => x + 1)), typeof(IEnumerableSerializer<int>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Select((x, i) => x + i)), typeof(IEnumerableSerializer<int>)],

        [TestHelpers.MakeLambda((MyModel model) => model.NestedItems.SelectMany(x => x)), typeof(IEnumerableDeserializingAsCollectionSerializer<IEnumerable<int>, int, List<int>>)],
        [TestHelpers.MakeLambda((MyModel model) => model.NestedItems.SelectMany((x, i) => x)), typeof(IEnumerableDeserializingAsCollectionSerializer<IEnumerable<int>, int, List<int>>)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.SequenceEqual(model.OtherItems)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.SequenceEqual(model.OtherItems)), typeof(BooleanSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Single()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Single(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Single()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Single(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.SingleOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.SingleOrDefault(x => x > 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.SingleOrDefault()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.SingleOrDefault(x => x > 0)), typeof(Int32Serializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Sum()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Sum(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Sum()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.LongItems.Sum(x => x * 2)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Sum()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DoubleItems.Sum(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Sum()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.DecimalItems.Sum(x => x * 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Sum()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.FloatItems.Sum(x => x * 2)), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Sum()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.Sum(x => x * 2)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Sum()), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.LongItems.Sum(x => x * 2)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Sum()), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DoubleItems.Sum(x => x * 2)), typeof(DoubleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Sum()), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.DecimalItems.Sum(x => x * 2)), typeof(DecimalSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Sum()), typeof(SingleSerializer)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.FloatItems.Sum(x => x * 2)), typeof(SingleSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.ToArray()), typeof(ArraySerializer<int>)],
        [TestHelpers.MakeLambda((MyQueryableModel model) => model.Items.ToArray()), typeof(ArraySerializer<int>)],

        [TestHelpers.MakeLambda((MyModel model) => model.Items.Where(x => x > 1)), typeof(IEnumerableDeserializingAsCollectionSerializer<IEnumerable<int>, int, List<int>>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Where((x, i) => i < 2)), typeof(IEnumerableDeserializingAsCollectionSerializer<IEnumerable<int>, int, List<int>>)],
    ];

    private class MyModel
    {
        public IEnumerable<int> Items { get; set; }
        public IEnumerable<int> OtherItems { get; set; }
        public IEnumerable<double> DoubleItems { get; set; }
        public IEnumerable<decimal> DecimalItems { get; set; }
        public IEnumerable<long> LongItems { get; set; }
        public IEnumerable<float> FloatItems { get; set; }
        public IEnumerable<IEnumerable<int>> NestedItems { get; set; }
    }

    private class MyQueryableModel
    {
        public IQueryable<int> Items { get; set; }
        public IQueryable<int> OtherItems { get; set; }
        public IQueryable<double> DoubleItems { get; set; }
        public IQueryable<decimal> DecimalItems { get; set; }
        public IQueryable<long> LongItems { get; set; }
        public IQueryable<float> FloatItems { get; set; }
    }
}
