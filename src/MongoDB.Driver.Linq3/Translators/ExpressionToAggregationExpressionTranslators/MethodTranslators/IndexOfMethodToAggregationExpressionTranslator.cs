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
using MongoDB.Driver.Linq3.Methods;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class IndexOfMethodToAggregationExpressionTranslator
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
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValue,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndex,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndexAndCount
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
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndex,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndexAndCount
       };

        private static readonly MethodInfo[] __indexOfWithCountMethods =
        {
            StringMethod.IndexOfWithCharAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndexAndCount,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndexAndCount
        };

        private static readonly MethodInfo[] __indexOfWithStringComparisonMethods =
        {
            StringMethod.IndexOfWithStringAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndComparisonType,
            StringMethod.IndexOfWithStringAndStartIndexAndCountAndComparisonType
        };

        private static readonly MethodInfo[] __indexOfBytesMethods =
        {
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValue,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndex,
            MongoDBLinqExtensionsMethod.IndexOfBytesWithValueAndStartIndexAndCount
       };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsStringIndexOfMethod(expression, out var objectExpression, out var valueExpression, out var startIndexExpression, out var countExpression, out var comparisonTypeExpression))
            {
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);

                AggregationExpression valueTranslation;
                if (valueExpression.Type == typeof(char))
                {
                    if (!(valueExpression is ConstantExpression constantExpression))
                    {
                        goto notSupported;
                    }
                    var c = (char)constantExpression.Value;
                    var value = new string(c, 1);
                    valueTranslation = new AggregationExpression(valueExpression, value, new StringSerializer());
                }
                else
                {
                    valueTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                }

                AggregationExpression startIndexTranslation = null;
                if (startIndexExpression != null)
                {
                    startIndexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, startIndexExpression);
                }

                AggregationExpression countTranslation = null;
                if (countExpression != null)
                {
                    countTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                }

                if (comparisonTypeExpression != null)
                {
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
                if (countTranslation != null)
                {
                    endAst = new AstNaryExpression(AstNaryOperator.Add, startIndexTranslation.Ast, countTranslation.Ast);
                }

                AstExpression ast;
                if (expression.Method.IsOneOf(__indexOfBytesMethods))
                {
                    ast = new AstIndexOfBytesExpression(objectTranslation.Ast, valueTranslation.Ast, startIndexTranslation?.Ast, endAst);
                }
                else
                {
                    ast = new AstIndexOfCPExpression(objectTranslation.Ast, valueTranslation.Ast, startIndexTranslation?.Ast, endAst);
                }

                return new AggregationExpression(expression, ast, new Int32Serializer());
            }

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsStringIndexOfMethod(
            MethodCallExpression expression,
            out Expression instanceExpression,
            out Expression valueExpression,
            out Expression startIndexExpression,
            out Expression countExpression,
            out Expression comparisonTypeExpression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__indexOfMethods))
            {
                if (method.IsOneOf(__indexOfBytesMethods))
                {
                    instanceExpression = arguments[0];
                    valueExpression = arguments[1];
                    startIndexExpression = method.IsOneOf(__indexOfWithStartIndexMethods) ? arguments[2] : null;
                    countExpression = method.IsOneOf(__indexOfWithCountMethods) ? arguments[3] : null;
                    comparisonTypeExpression = null;
                    return true;
                }
                else
                {
                    instanceExpression = expression.Object;
                    valueExpression = arguments[0];
                    startIndexExpression = method.IsOneOf(__indexOfWithStartIndexMethods) ? arguments[1] : null;
                    countExpression = method.IsOneOf(__indexOfWithCountMethods) ? arguments[2] : null;
                    comparisonTypeExpression = method.IsOneOf(__indexOfWithStringComparisonMethods) ? arguments.Last() : null;
                    return true;
                }
            }

            instanceExpression = null;
            valueExpression = null;
            startIndexExpression = null;
            countExpression = null;
            comparisonTypeExpression = null;
            return false;
        }
    }
}
