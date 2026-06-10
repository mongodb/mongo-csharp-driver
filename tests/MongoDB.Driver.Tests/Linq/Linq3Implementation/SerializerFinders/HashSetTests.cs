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

public class HashSetTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_hashset_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.ItemSet.Contains(1)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringSet.Contains("a")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.ItemSet.IsSubsetOf(model.OtherSet)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringSet.IsSubsetOf(model.OtherStringSet)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.ItemSet.SetEquals(model.OtherSet)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.StringSet.SetEquals(model.OtherStringSet)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.ItemSet.ToArray()), typeof(ArraySerializer<int>)],
        [TestHelpers.MakeLambda((MyModel model) => model.ItemSet.ToList()), typeof(ListSerializer<int>)],
    ];

    private class MyModel
    {
        public HashSet<int> ItemSet { get; set; }
        public HashSet<int> OtherSet { get; set; }
        public HashSet<string> StringSet { get; set; }
        public HashSet<string> OtherStringSet { get; set; }
    }
}
