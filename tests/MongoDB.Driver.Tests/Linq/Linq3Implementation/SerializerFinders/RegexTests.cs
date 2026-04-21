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
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class RegexTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_regex_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void SerializerFinder_should_set_unknowable_serializer_for_unsupported_regex_methods(LambdaExpression expression)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out var serializer).Should().BeTrue();
        serializer.Should().BeOfType(typeof(UnknowableSerializer<>).MakeGenericType(expression.Body.Type));
        var exception = Record.Exception(() => serializerMap.GetSerializer(expression.Body));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").IsMatch(model.Name)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.IsMatch(model.Name, "pattern")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.IsMatch(model.Name, "pattern", RegexOptions.IgnoreCase)), typeof(BooleanSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").Replace(model.Name, "replacement")), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", "replacement")), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", "replacement", RegexOptions.IgnoreCase)), typeof(StringSerializer)],

        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").Split(model.Name)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Split(model.Name, "pattern")), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Split(model.Name, "pattern", RegexOptions.IgnoreCase)), typeof(ArraySerializer<string>)],
    ];

    public static readonly object[][] NonSupportedTestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").Replace(model.Name, m => m.Value))],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", m => m.Value))],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", m => m.Value, RegexOptions.IgnoreCase))],

        [TestHelpers.MakeLambda((MyModel model) => Regex.Split(model.Name, "pattern", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(42)))]
    ];

    private class MyModel
    {
        public string Name { get; set; }
    }
}
