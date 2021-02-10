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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators
{
    public static class StringIndexOfComparisonExpressionToFilterTranslator
    {
        private static MethodInfo[] __indexOfAnyMethods;
        private static MethodInfo[] __indexOfMethods;
        private static MethodInfo[] __indexOfWithCharMethods;
        private static MethodInfo[] __indexOfWithStringMethods;

        static StringIndexOfComparisonExpressionToFilterTranslator()
        {
            __indexOfMethods = new[]
            {
                StringMethod.IndexOfAny,
                StringMethod.IndexOfAnyWithStartIndex,
                StringMethod.IndexOfAnyWithStartIndexAndCount,
                StringMethod.IndexOfWithChar,
                StringMethod.IndexOfWithCharAndStartIndex,
                StringMethod.IndexOfWithCharAndStartIndexAndCount,
                StringMethod.IndexOfWithString,
                StringMethod.IndexOfWithStringAndStartIndex,
                StringMethod.IndexOfWithStringAndStartIndexAndCount
            };

            __indexOfAnyMethods = new[]
            {
                StringMethod.IndexOfAny,
                StringMethod.IndexOfAnyWithStartIndex,
                StringMethod.IndexOfAnyWithStartIndexAndCount
            };

            __indexOfWithCharMethods = new[]
            {
                StringMethod.IndexOfWithChar,
                StringMethod.IndexOfWithCharAndStartIndex,
                StringMethod.IndexOfWithCharAndStartIndexAndCount
            };

            __indexOfWithStringMethods = new[]
            {
                StringMethod.IndexOfWithString,
                StringMethod.IndexOfWithStringAndStartIndex,
                StringMethod.IndexOfWithStringAndStartIndexAndCount
            };
        }

        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out MethodCallExpression indexOfExpression, out Expression comparandExpression)
        {
            if (leftExpression.NodeType == ExpressionType.Call)
            {
                var leftMethodCallExpression = (MethodCallExpression)leftExpression;
                if (leftMethodCallExpression.Method.IsOneOf(__indexOfMethods))
                {
                    indexOfExpression = leftMethodCallExpression;
                    comparandExpression = rightExpression;
                    return true;
                }
            }

            indexOfExpression = null;
            comparandExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, MethodCallExpression indexOfExpression, Expression comparandExpression)
        {
            var method = indexOfExpression.Method;
            var arguments = indexOfExpression.Arguments;

            if (method.IsOneOf(__indexOfMethods))
            {
                var objectExpression = indexOfExpression.Object;
                var startIndexExpression = arguments.Count >= 2 ? arguments[1] : null;
                var countExpression = arguments.Count >= 3 ? arguments[2] : null;

                var field = ExpressionToFilterFieldTranslator.Translate(context, objectExpression);
                var startIndex = startIndexExpression != null ? GetConstantValue<int>(startIndexExpression) : 0;
                var count = countExpression != null ? GetConstantValue<int>(countExpression) : -1;
                var comparand = GetConstantValue<int>(comparandExpression);

                var pattern = new StringBuilder();
                pattern.Append("^");
                if (startIndex != 0)
                {
                    pattern.Append($".{{{startIndex}}}");
                }
                if (count != -1)
                {
                    pattern.Append($"(?=.{{{count}}})");
                }

                if (method.IsOneOf(__indexOfAnyMethods))
                {
                    var anyOfExpression = arguments[0];
                    var anyOf = new string(GetConstantValue<char[]>(anyOfExpression));
                    var anyOfEscaped = EscapeCharacterClass(anyOf);

                    var exclusionCount = comparand - startIndex;
                    if (exclusionCount > 0)
                    {
                        pattern.Append($"[^{anyOfEscaped}]{{{exclusionCount}}}");
                    }

                    pattern.Append($"[{anyOfEscaped}]");
                }

                if (method.IsOneOf(__indexOfWithCharMethods))
                {
                    var valueExpression = arguments[0];
                    var value = new string(GetConstantValue<char>(valueExpression), 1);

                    var exclusionCount = comparand - startIndex;
                    if (exclusionCount > 0)
                    {
                        var valueEscaped = EscapeCharacterClass(value);
                        pattern.Append($"[^{valueEscaped}]{{{exclusionCount}}}");
                    }

                    pattern.Append(Regex.Escape(value));
                }

                if (method.IsOneOf(__indexOfWithStringMethods))
                {
                    var valueExpression = arguments[0];
                    var value = GetConstantValue<string>(valueExpression);
                    var escapedValue = Regex.Escape(value);

                    var exclusionCount = comparand - startIndex;
                    if (exclusionCount > 0)
                    {
                        if (exclusionCount > 1)
                        {
                            pattern.Append($"(?!.{{0,{exclusionCount - 1}}}{escapedValue})");
                        }
                        pattern.Append($".{{{exclusionCount}}}");
                    }

                    pattern.Append(escapedValue);
                }

                return new AstRegexFilter(field, pattern.ToString(), options: "s");
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static string EscapeCharacterClass(string value)
        {
            return
                value
                .Replace("-", "\\-")
                .Replace("]", "\\]");
        }

        private static T GetConstantValue<T>(Expression expression)
        {
            if (expression is ConstantExpression constantExpression)
            {
                return (T)constantExpression.Value;
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
