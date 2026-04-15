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
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.SerializerFinders;

public class StringTests
{
    [Theory]
    [MemberData(nameof(TestCases))]
    public void SerializerFinder_should_resolve_string_methods(LambdaExpression expression, Type expectedSerializerType)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out _).Should().BeTrue();
        serializerMap.GetSerializer(expression.Body).Should().BeOfType(expectedSerializerType);
    }

    public static readonly object[][] TestCases =
    [
        [TestHelpers.MakeLambda((MyModel model) => model.Tags.AnyStringIn("tag1", "tag2")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Tags.AnyStringIn(new StringOrRegularExpression[] { "tag1" })), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Tags.AnyStringNin("tag1", "tag2")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Tags.AnyStringNin(new StringOrRegularExpression[] { "tag1" })), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name[0]), typeof(CharSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Compare(model.Name, "other")), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Compare(model.Name, "other", true)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.CompareTo("other")), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat((object)model.Name)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat((object)model.A, (object)model.B)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat((object)model.A, (object)model.B, (object)model.C)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat(new object[] { model.A, model.B })), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat(model.A, model.B)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat(model.A, model.B, model.C)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat(model.A, model.B, model.C, model.D)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Concat(new string[] { model.A, model.B })), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Contains("test")), typeof(BooleanSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Contains('x')), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Contains('x', StringComparison.Ordinal)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Contains("test", StringComparison.Ordinal)), typeof(BooleanSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => model.Name.EndsWith("suffix")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.EndsWith("suffix", StringComparison.Ordinal)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.EndsWith("suffix", true, CultureInfo.InvariantCulture)), typeof(BooleanSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => model.Name.EndsWith('x')), typeof(BooleanSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Equals("other")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Equals("other", StringComparison.Ordinal)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Equals(model.Name, "other")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.Equals(model.Name, "other", StringComparison.Ordinal)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf('x')), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf('x', 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf('x', 0, 1)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test")), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test", 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test", 0, 1)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test", StringComparison.Ordinal)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test", 0, StringComparison.Ordinal)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOf("test", 0, 1, StringComparison.Ordinal)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOfBytes("x")), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOfBytes("x", 0)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.IndexOfBytes("x", 0, 1)), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.IsNullOrEmpty(model.Name)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => string.IsNullOrWhiteSpace(model.Name)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Replace('a', 'b')), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Replace("old", "new")), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' })), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' }, 2)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' }, StringSplitOptions.None)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' }, 2, StringSplitOptions.None)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',' }, 2, StringSplitOptions.RemoveEmptyEntries)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new char[] { ',', ';' })), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new string[] { "," }, StringSplitOptions.None)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new string[] { "," }, 2, StringSplitOptions.None)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new string[] { "," }, 2, StringSplitOptions.RemoveEmptyEntries)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Split(new string[] { ",", ";" }, StringSplitOptions.None)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").Split(model.Name)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Split(model.Name, "pattern")), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Split(model.Name, "pattern", RegexOptions.IgnoreCase)), typeof(ArraySerializer<string>)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StartsWith("prefix")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StartsWith("prefix", StringComparison.Ordinal)), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StartsWith("prefix", true, CultureInfo.InvariantCulture)), typeof(BooleanSerializer)],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StartsWith('x')), typeof(BooleanSerializer)],
#endif
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StringIn("a", "b")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StringIn(new StringOrRegularExpression[] { "a" })), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StringNin("a", "b")), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StringNin(new StringOrRegularExpression[] { "a" })), typeof(BooleanSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.StrLenBytes()), typeof(Int32Serializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.SubstrBytes(0, 3)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Substring(1)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Substring(1, 3)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToLower()), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToLower(CultureInfo.InvariantCulture)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToLowerInvariant()), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToUpper()), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToUpper(CultureInfo.InvariantCulture)), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.ToUpperInvariant()), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Trim()), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Trim(new char[] { ' ' })), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.TrimStart(new char[] { ' ' })), typeof(StringSerializer)],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.TrimEnd(new char[] { ' ' })), typeof(StringSerializer)],
    ];

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void SerializerFinder_should_set_unknowable_serializer_for_unsupported_string_methods(LambdaExpression expression)
    {
        var serializerMap = TestHelpers.CreateSerializerMap(expression);

        SerializerFinder.FindSerializers(expression.Body, null, serializerMap);

        serializerMap.IsKnown(expression.Body, out var serializer).Should().BeTrue();
        serializer.Should().BeOfType(typeof(UnknowableSerializer<>).MakeGenericType(expression.Body.Type));
        var exception = Record.Exception(() => serializerMap.GetSerializer(expression.Body));
        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static readonly object[][] NonSupportedTestCases =
    [
        // MatchEvaluator overloads are not translatable to aggregation expressions
        [TestHelpers.MakeLambda((MyModel model) => new Regex("pattern").Replace(model.Name, m => m.Value))],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", m => m.Value))],
        [TestHelpers.MakeLambda((MyModel model) => Regex.Replace(model.Name, "pattern", m => m.Value, RegexOptions.IgnoreCase))],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        // StringComparison and CultureInfo overloads are not in ReplaceOverloads
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Replace("old", "new", StringComparison.Ordinal))],
        [TestHelpers.MakeLambda((MyModel model) => model.Name.Replace("old", "new", false, CultureInfo.InvariantCulture))],
#endif
    ];

    private class MyModel
    {
        public string Name { get; set; }
        public string A { get; set; }
        public string B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
        public string[] Tags { get; set; }
    }
}
