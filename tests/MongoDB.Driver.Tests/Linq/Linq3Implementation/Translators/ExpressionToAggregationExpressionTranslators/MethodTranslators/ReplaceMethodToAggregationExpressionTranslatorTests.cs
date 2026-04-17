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

#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
using System;
using System.Globalization;
#endif

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;

public class ReplaceMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = ReplaceMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<StringSerializer>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void Translate_should_throw_on_non_supported_expressions(LambdaExpression expression)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var exception = Record.Exception(() => ReplaceMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    public static IEnumerable<object[]> SupportedTestCases =
    [
        // string.Replace(char, char)
        [
            TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace('a', 'b')),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: 'a', replacement: 'b' } }"
        ],
        // string.Replace(string, string)
        [
            TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace("old", "new")),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: 'old', replacement: 'new' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => "input".Replace(model.StringField, "new")),
            "{ $replaceAll: { input: 'input', find: { $getField: { field: 'StringField', input: '$$ROOT' } }, replacement: 'new' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => "input".Replace("old", model.StringField)),
            "{ $replaceAll: { input: 'input', find: 'old', replacement: { $getField: { field: 'StringField', input: '$$ROOT' } } } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace("old", "")),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: 'old', replacement: '' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace("old", null)),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: 'old', replacement: '' } }"
        ],
        // Regex.Replace(input, pattern, replacement) — static, no options
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace(model.StringField, "pattern", "replacement")),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: /pattern/, replacement: 'replacement' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace("input", "pattern", model.StringField)),
            "{ $replaceAll: { input: 'input', find: /pattern/, replacement:  { $getField: { field: 'StringField', input: '$$ROOT' } } } }"
        ],
        // Regex.Replace(input, pattern, replacement, options) — static, with IgnoreCase
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace(model.StringField, "pattern", "replacement", RegexOptions.IgnoreCase)),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: /pattern/i, replacement: 'replacement' } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace("input", "pattern", model.StringField, RegexOptions.IgnoreCase)),
            "{ $replaceAll: { input: 'input', find: /pattern/i, replacement: { $getField: { field: 'StringField', input: '$$ROOT' } } } }"
        ],
        // regex.Replace(input, replacement) — instance regex stored as BsonType.RegularExpression
        [
            TestHelpers.MakeLambda<MyModel, string>(model => model.RegexField.Replace(model.StringField, "replacement")),
            "{ $replaceAll: { input: { $getField: { field: 'StringField', input: '$$ROOT' } }, find: { $getField: { field: 'RegexField', input: '$$ROOT' } }, replacement: 'replacement' } }"
        ],
    ];

    public static IEnumerable<object[]> NonSupportedTestCases =
    [
        // Instance regex Replace where regex field is stored as BsonType.String (not RegularExpression)
        [TestHelpers.MakeLambda<MyModel, string>(model => model.RegexFieldAsString.Replace(model.StringField, "replacement"))],
        // MatchEvaluator overloads cannot be translated to an aggregation expression
        [TestHelpers.MakeLambda<MyModel, string>(model => model.RegexField.Replace(model.StringField, m => m.Value.ToUpper()))],
        [TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace(model.StringField, "pattern", m => m.Value.ToUpper()))],
        [TestHelpers.MakeLambda<MyModel, string>(model => Regex.Replace(model.StringField, "pattern", m => m.Value.ToUpper(), RegexOptions.IgnoreCase))],
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
        [TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace("old", "new", StringComparison.Ordinal))],
        [TestHelpers.MakeLambda<MyModel, string>(model => model.StringField.Replace("old", "new", false, CultureInfo.InvariantCulture))],
#endif
    ];

    public class MyModel
    {
        public string StringField { get; set; }
        public Regex RegexField { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Regex RegexFieldAsString { get; set; }
    }
}
