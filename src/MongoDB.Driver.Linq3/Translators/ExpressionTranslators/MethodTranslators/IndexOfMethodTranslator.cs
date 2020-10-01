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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionTranslators.MethodTranslators
{
    public static class IndexOfMethodTranslator
    {
        private static readonly MethodInfo[] __indexOfMethods =
        {
            StringMethod.IndexOfWithChar,
            StringMethod.IndexOfWithCharAndStartIndex,
            StringMethod.IndexOfWithCharAndStartIndexAndCount,
            StringMethod.IndexOfWithString,
            StringMethod.IndexOfWithStringAndStartIndex,
            StringMethod.IndexOfWithStringAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType
        };

        private static readonly MethodInfo[] __indexOfWithCharMethods =
        {
            StringMethod.IndexOfWithChar,
            StringMethod.IndexOfWithCharAndStartIndex,
            StringMethod.IndexOfWithCharAndStartIndexAndCount
        };

        private static readonly MethodInfo[] __indexOfWithStartIndexMethods =
        {
            StringMethod.IndexOfWithCharAndStartIndex,
            StringMethod.IndexOfWithCharAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndex,
            StringMethod.IndexOfWithStringAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndexAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType
        };

        private static readonly MethodInfo[] __indexOfWithCountMethods =
        {
            StringMethod.IndexOfWithCharAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType
        };

        private static readonly MethodInfo[] __indexOfWithStringComparisonMethods =
        {
            StringMethod.IndexOfWithStringAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType
        };

        public static ExpressionTranslation Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__indexOfMethods))
            {
                var objectExpression = expression.Object;
                var objectTranslation = ExpressionTranslator.Translate(context, objectExpression);

                var valueExpression = arguments[0];
                ExpressionTranslation valueTranslation;
                if (valueExpression.Type == typeof(char))
                {
                    if (!(valueExpression is ConstantExpression constantExpression))
                    {
                        goto notSupported;
                    }
                    var c = (char)constantExpression.Value;
                    var value = new string(c, 1);
                    valueTranslation = new ExpressionTranslation(valueExpression, value, new StringSerializer());
                }
                else
                {
                    valueTranslation = ExpressionTranslator.Translate(context, valueExpression);
                }

                ExpressionTranslation startIndexTranslation = null;
                if (method.IsOneOf(__indexOfWithStartIndexMethods))
                {
                    var startIndexExpression = arguments[1];
                    startIndexTranslation = ExpressionTranslator.Translate(context, startIndexExpression);
                }

                ExpressionTranslation countTranslation = null;
                if (method.IsOneOf(__indexOfWithCountMethods))
                {
                    var countExpression = arguments[2];
                    countTranslation = ExpressionTranslator.Translate(context, countExpression);
                }

                if (method.IsOneOf(__indexOfWithStringComparisonMethods))
                {
                    var comparisonTypeExpression = arguments.Last();
                    if (!(comparisonTypeExpression is ConstantExpression constantExpression))
                    {
                        goto notSupported;
                    }

                    var comparisonType = (StringComparison)constantExpression.Value;
                    switch (comparisonType)
                    {
                        case StringComparison.CurrentCulture:
                        case StringComparison.Ordinal:
                            break;

                        default:
                            goto notSupported;
                    }
                }

                AstExpression endAst = null;
                if (method.IsOneOf(__indexOfWithCountMethods))
                {
                    endAst = new AstNaryExpression(AstNaryOperator.Add, startIndexTranslation.Ast, countTranslation.Ast);
                }

                var translation = new AstIndexOfCPExpression(objectTranslation.Ast, valueTranslation.Ast, startIndexTranslation?.Ast, endAst);

                return new ExpressionTranslation(expression, translation, new Int32Serializer());
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
