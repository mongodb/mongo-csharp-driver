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

public class SplitMethodToAggregationExpressionTranslatorTests
{
    [Theory]
    [MemberData(nameof(SupportedTestCases))]
    public void Translate_should_produce_proper_ast(LambdaExpression expression, string expectedAst)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var translation = SplitMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body);

        translation.Serializer.Should().BeOfType<ArraySerializer<string>>();
        translation.Ast.Render().Should().Be(BsonDocument.Parse(expectedAst));
    }

    [Theory]
    [MemberData(nameof(NonSupportedTestCases))]
    public void Translate_should_throw_on_non_supported_expressions(LambdaExpression expression)
    {
        var translationContext = TestHelpers.CreateTranslationContext(expression);
        var exception = Record.Exception(() => SplitMethodToAggregationExpressionTranslator.Translate(translationContext, (MethodCallExpression)expression.Body));

        exception.Should().BeOfType<ExpressionNotSupportedException>();
    }

    private static char[] __singleCharsSeparator = [ ',' ];
    private static char[] __multipleCharsSeparator = [ ',', ';' ];
    private static string[] __singleStringsSeparator = [ "," ];
    private static string[] __multipleStringsSeparator = [ ",", ";" ];
    public static IEnumerable<object[]> SupportedTestCases =
    [
#if NETCOREAPP || NET6_0_OR_GREATER
        // string.Split(char, StringSplitOptions.None).
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(',', StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        // string.Split(char, StringSplitOptions.RemoveEmptyEntries) — wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(',', StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        // string.Split(char, int, StringSplitOptions.None) — limits result count via $slice
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(',', 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
#endif
        // string.Split(char[]) — single char separator
#if NET472
        // It's impossible to make such a call in modern .NET, because it resolves to another overload which requires SplitOptions.
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(',')),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
#endif
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' })),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        // string.Split(char[], StringSplitOptions.None)
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' }, StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator, StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        // string.Split(char[], StringSplitOptions.RemoveEmptyEntries) — wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator, StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        // string.Split(char[], int) — limits result count via $slice
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' }, 3)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator, 3)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        // string.Split(char[], int, StringSplitOptions.None)
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' }, 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator, 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        // string.Split(char[], int, StringSplitOptions.RemoveEmptyEntries) - limits result count via $slice and wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',' }, 3, StringSplitOptions.RemoveEmptyEntries)),
            "{ $slice: [{ $filter : { input : { $split : [{ $getField : { field : 'StringField', input : '$$ROOT' } }, ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 3]}"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleCharsSeparator, 3, StringSplitOptions.RemoveEmptyEntries)),
            "{ $slice: [{ $filter : { input : { $split : [{ $getField : { field : 'StringField', input : '$$ROOT' } }, ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 3]}"
        ],
        // TODO: Missed cases when StringSplitOptions.TrimEntries. Currently, it is silently ignored. Need jira ticket for that.

#if NETCOREAPP || NET6_0_OR_GREATER
        // string.Split(string, StringSplitOptions.None).
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(",", StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        // string.Split(string, StringSplitOptions.RemoveEmptyEntries) — wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(",", StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        // string.Split(string, int, StringSplitOptions.None) — limits result count via $slice
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(",", 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
#endif
        // string.Split(string[], StringSplitOptions.None)
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { "," }, StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleStringsSeparator, StringSplitOptions.None)),
            "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }"
        ],
        // string.Split(string[], StringSplitOptions.RemoveEmptyEntries) — wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleStringsSeparator, StringSplitOptions.RemoveEmptyEntries)),
            "{ $filter: { input: { $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, as: 'item', cond: { $ne: ['$$item', ''] } } }"
        ],
        // string.Split(string[], int, StringSplitOptions.None)
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { "," }, 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleStringsSeparator, 3, StringSplitOptions.None)),
            "{ $slice: [{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, ','] }, 3] }"
        ],
        // string.Split(string[], int, StringSplitOptions.RemoveEmptyEntries) - limits result count via $slice and wraps result in $filter
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { "," }, 3, StringSplitOptions.RemoveEmptyEntries)),
            "{ $slice: [{ $filter : { input : { $split : [{ $getField : { field : 'StringField', input : '$$ROOT' } }, ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 3]}"
        ],
        [
            TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__singleStringsSeparator, 3, StringSplitOptions.RemoveEmptyEntries)),
            "{ $slice: [{ $filter : { input : { $split : [{ $getField : { field : 'StringField', input : '$$ROOT' } }, ','] }, as : 'item', cond : { $ne : ['$$item', ''] } } }, 3]}"
        ],
        // TODO: Missed cases when StringSplitOptions.TrimEntries. Currently, it is silently ignored. Need jira ticket for that.

        // Regex.Split(input, pattern) — static, no options
        [
         TestHelpers.MakeLambda<MyModel, string[]>(model => Regex.Split(model.StringField, "pattern")),
         "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, /pattern/] }"
        ],
        // Regex.Split(input, pattern, options) — static, with IgnoreCase
        [
         TestHelpers.MakeLambda<MyModel, string[]>(model => Regex.Split(model.StringField, "pattern", RegexOptions.IgnoreCase)),
         "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, /pattern/i] }"
        ],
        // regex.Split(input) — instance regex stored as BsonType.RegularExpression
        [
         TestHelpers.MakeLambda<MyModel, string[]>(model => model.RegexField.Split(model.StringField)),
         "{ $split: [{ $getField: { field: 'StringField', input: '$$ROOT' } }, { $getField: { field: 'RegexField',  input: '$$ROOT' } }] }"
],
    ];

    public static IEnumerable<object[]> NonSupportedTestCases =
    [
        // Multiple char separators are not supported
#if !CSHARP_14
        // C# 14 cannot have such statement with the following compilation error occurs:
        // error CS8640: Expression tree cannot contain value of ref struct or restricted type 'ReadOnlySpan'.
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(',', ';'))],
#endif
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ',', ';' }))],
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__multipleCharsSeparator))],
        // Multiple string separators are not supported
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(__multipleStringsSeparator, StringSplitOptions.None))],
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.StringField.Split(new[] { ", ", "; " }, StringSplitOptions.None))],
        // Instance regex Split where regex field is stored as BsonType.String (not RegularExpression)
        [TestHelpers.MakeLambda<MyModel, string[]>(model => model.RegexFieldAsString.Split(model.StringField))],
    ];

    public class MyModel
    {
        public string StringField { get; set; }
        public Regex RegexField { get; set; }
        [BsonRepresentation(BsonType.String)]
        public Regex RegexFieldAsString { get; set; }
    }
}
