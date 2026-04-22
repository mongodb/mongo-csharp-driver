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

public class SByteTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_sbyte_operations(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.Value.CompareTo((sbyte)1)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Value.CompareTo(model.Other)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Value.Equals((sbyte)1)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Value.Equals(model.Other)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => sbyte.Parse(model.StringValue)), typeof(SByteSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Value.ToString()), typeof(StringSerializer)],
    ];

    private class MyModel
    {
        public sbyte Value { get; set; }
        public sbyte Other { get; set; }
        public string StringValue { get; set; }
    }
}
