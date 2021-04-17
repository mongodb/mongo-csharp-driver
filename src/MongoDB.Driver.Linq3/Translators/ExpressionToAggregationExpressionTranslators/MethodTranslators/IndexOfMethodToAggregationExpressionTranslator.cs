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
using MongoDB.Driver.Linq3.Reflection;

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
                var valueTranslation = TranslateValue();
                var startIndexTranslation = startIndexExpression == null ? null : ExpressionToAggregationExpressionTranslator.Translate(context, startIndexExpression);
                var countTranslation = countExpression == null ? null : ExpressionToAggregationExpressionTranslator.Translate(context, countExpression);
                var ordinal = GetOrdinalFromComparisonType();

                var endAst = CreateEndAst(startIndexTranslation?.Ast, countTranslation?.Ast);

                AstExpression ast;
                if (expression.Method.IsOneOf(__indexOfBytesMethods) || ordinal)
                {
                    ast = AstExpression.IndexOfBytes(objectTranslation.Ast, valueTranslation.Ast, startIndexTranslation?.Ast, endAst);
                }
                else
                {
                    ast = AstExpression.IndexOfCP(objectTranslation.Ast, valueTranslation.Ast, startIndexTranslation?.Ast, endAst);
                }

                return new AggregationExpression(expression, ast, new Int32Serializer());
            }

            throw new ExpressionNotSupportedException(expression);

            AggregationExpression TranslateValue()
            {
                if (valueExpression.Type == typeof(char))
                {
                    if (valueExpression is ConstantExpression constantExpression)
                    {
                        var c = (char)constantExpression.Value;
                        var value = new string(c, 1);
                        return new AggregationExpression(valueExpression, value, new StringSerializer());
                    }
                }
                else
                {
                    return ExpressionToAggregationExpressionTranslator.Translate(context, valueExpression);
                }

                throw new ExpressionNotSupportedException(expression);
            }

            bool GetOrdinalFromComparisonType()
            {
                if (comparisonTypeExpression == null)
                {
                    return false;
                }

                if (comparisonTypeExpression is ConstantExpression comparisonTypeConstantExpression)
                {
                    var comparisonType = (StringComparison)comparisonTypeConstantExpression.Value;
                    switch (comparisonType)
                    {
                        case StringComparison.CurrentCulture: return false;
                        case StringComparison.Ordinal: return true;
                    }
                }

                throw new ExpressionNotSupportedException(expression);
            }

            AstExpression CreateEndAst(AstExpression startIndexAst, AstExpression countAst)
            {
                if (startIndexAst == null || countAst == null)
                {
                    return null;
                }

                return AstExpression.Add(startIndexAst, countAst);
            }
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
