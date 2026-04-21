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
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class DateTimeTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_date_time_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Add(TimeSpan.Zero)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Add(TimeSpan.Zero, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Add(1L, DateTimeUnit.Day)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Add(1L, DateTimeUnit.Day, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddDays(1.0)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddDays(1.0, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddHours(1.0)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddHours(1.0, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMilliseconds(1.0)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMilliseconds(1.0, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMinutes(1.0)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMinutes(1.0, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMonths(1)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddMonths(1, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddQuarters(1)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddQuarters(1, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddSeconds(1.0)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddSeconds(1.0, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddTicks(1L)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddWeeks(1)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddWeeks(1, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddYears(1)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.AddYears(1, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => DateTime.Parse(model.DateString)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(TimeSpan.Zero)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(TimeSpan.Zero, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(1L, DateTimeUnit.Day)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(1L, DateTimeUnit.Day, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(model.OtherDate)), typeof(TimeSpanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(model.OtherDate, "UTC")), typeof(TimeSpanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(model.OtherDate, DateTimeUnit.Day)), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Subtract(model.OtherDate, DateTimeUnit.Day, "UTC")), typeof(Int64Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.ToString("yyyy-MM-dd")), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.ToString("yyyy-MM-dd", "UTC")), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Truncate(DateTimeUnit.Day)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Truncate(DateTimeUnit.Day, 1L)), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Truncate(DateTimeUnit.Day, 1L, "UTC")), typeof(DateTimeSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Week()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Date.Week("UTC")), typeof(Int32Serializer)],
    ];

    private class MyModel
    {
        public DateTime Date { get; set; }
        public DateTime OtherDate { get; set; }
        public string DateString { get; set; }
    }
}
