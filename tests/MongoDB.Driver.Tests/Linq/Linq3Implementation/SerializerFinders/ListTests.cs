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
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class ListTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_list_operations(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Contains(1)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringItems.Contains("a")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.Exists(x => x > 0)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringItems.Exists(s => s.StartsWith("a"))), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.ToArray()), typeof(ArraySerializer<int>)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringItems.ToArray()), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.IntArray.ToList()), typeof(ListSerializer<int>)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringArray.ToList()), typeof(ListSerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Items.ToList()), typeof(ListSerializer<int>)],
    ];

    private class MyModel
    {
        public List<int> Items { get; set; }
        public List<string> StringItems { get; set; }
        public int[] IntArray { get; set; }
        public string[] StringArray { get; set; }
    }
}
