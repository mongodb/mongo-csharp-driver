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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class CompareMethodToAggregationExpressionTranslator
    {
        private static readonly MethodInfo[] __stringCompareMethods = new[]
        {
            StringMethod.Compare,
            StringMethod.CompareWithIgnoreCase,
            StringMethod.CompareWithIgnoreCaseAndCulture,
            StringMethod.CompareWithCultureAndOptions,
            StringMethod.CompareWithComparisonType,
            StringMethod.CompareWithIndexesAndLength,
            StringMethod.CompareWithIndexesAndLengthAndIgnoreCase,
            StringMethod.CompareWithIndexesAndLengthAndIgnoreCaseAndCulture,
            StringMethod.CompareWithIndexesAndLengthAndCultureAndOptions,
            StringMethod.CompareWithIndexesAndLengthAndComparisonType
        };

        private static readonly MethodInfo[] __stringCompareWithIndexesAndLengthMethods = new[]
        {
            StringMethod.CompareWithIndexesAndLength,
            StringMethod.CompareWithIndexesAndLengthAndIgnoreCase,
            StringMethod.CompareWithIndexesAndLengthAndIgnoreCaseAndCulture,
            StringMethod.CompareWithIndexesAndLengthAndCultureAndOptions,
            StringMethod.CompareWithIndexesAndLengthAndComparisonType
        };

        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            if (method.IsOneOf(__stringCompareMethods))
            {
                var (strAExpression, strBExpression, ignoreCase) = GetCommonArguments(expression, method, arguments);
                var strATranslation = ExpressionToAggregationExpressionTranslator.Translate(context, strAExpression);
                var strBTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, strBExpression);

                AstExpression expression1Ast, expression2Ast;
                if (HasIndexesAndLength(expression, method, arguments, out var indexAExpression, out var indexBExpression, out var lengthExpression))
                {
                    var indexATranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexAExpression);
                    var indexBTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexBExpression);
                    var lengthTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lengthExpression);
                    expression1Ast = AstExpression.SubstrCP(strATranslation.Ast, indexATranslation.Ast, lengthTranslation.Ast);
                    expression2Ast = AstExpression.SubstrCP(strBTranslation.Ast, indexBTranslation.Ast, lengthTranslation.Ast);

                }
                else
                {
                    expression1Ast = strATranslation.Ast;
                    expression2Ast = strBTranslation.Ast;
                }

                var @operator = ignoreCase ? AstBinaryOperator.StrCaseCmp : AstBinaryOperator.Cmp;
                var translation = AstExpression.Binary(@operator, expression1Ast, expression2Ast);
                return new AggregationExpression(expression, translation, Int32Serializer.Instance);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static void EnsureCurrentCulture(Expression expression, Expression cultureExpression)
        {
            if (cultureExpression is ConstantExpression constantExpression)
            {
                var culture = (CultureInfo)constantExpression.Value;
                if (!culture.Equals(CultureInfo.CurrentCulture))
                {
                    throw new ExpressionNotSupportedException(expression, because: "culture must be CultureInfo.CurrentCulture");
                }
            }
            else
            {
                throw new ExpressionNotSupportedException(expression, because: "culture must be a constant");
            }
        }

        private static (Expression, Expression, bool) GetCommonArguments(Expression expression, MethodInfo method, ReadOnlyCollection<Expression> arguments)
        {
            if (method.Is(StringMethod.Compare))
            {
                return (arguments[0], arguments[1], false);
            }

            if (method.Is(StringMethod.CompareWithIgnoreCase))
            {
                var ignoreCase = GetIgnoreCaseFromBool(expression, arguments[2]);
                return (arguments[0], arguments[1], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithIgnoreCaseAndCulture))
            {
                var ignoreCase = GetIgnoreCaseFromBool(expression, arguments[2]);
                EnsureCurrentCulture(expression, arguments[3]);
                return (arguments[0], arguments[1], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithCultureAndOptions))
            {
                EnsureCurrentCulture(expression, arguments[2]);
                var ignoreCase = GetIgnoreCaseFromOptions(expression, arguments[3]);
                return (arguments[0], arguments[1], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithComparisonType))
            {
                var ignoreCase = GetIgnoreCaseFromComparisonType(expression, arguments[2]);
                return (arguments[0], arguments[1], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithIndexesAndLength))
            {
                return (arguments[0], arguments[2], false);
            }

            if (method.Is(StringMethod.CompareWithIndexesAndLengthAndIgnoreCase))
            {
                var ignoreCase = GetIgnoreCaseFromBool(expression, arguments[5]);
                return (arguments[0], arguments[2], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithIndexesAndLengthAndIgnoreCaseAndCulture))
            {
                var ignoreCase = GetIgnoreCaseFromBool(expression, arguments[5]);
                EnsureCurrentCulture(expression, arguments[6]);
                return (arguments[0], arguments[2], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithIndexesAndLengthAndCultureAndOptions))
            {
                EnsureCurrentCulture(expression, arguments[5]);
                var ignoreCase = GetIgnoreCaseFromOptions(expression, arguments[6]);
                return (arguments[0], arguments[2], ignoreCase);
            }

            if (method.Is(StringMethod.CompareWithIndexesAndLengthAndComparisonType))
            {
                var ignoreCase = GetIgnoreCaseFromComparisonType(expression, arguments[5]);
                return (arguments[0], arguments[2], ignoreCase);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool GetIgnoreCaseFromBool(Expression expression, Expression ignoreCaseExpression)
        {
            if (ignoreCaseExpression is ConstantExpression constantExpression)
            {
                return (bool)constantExpression.Value;
            }
            else
            {
                throw new ExpressionNotSupportedException(expression, because: "ignoreCase must be a constant");
            }
        }

        private static bool GetIgnoreCaseFromComparisonType(Expression expression, Expression comparisonTypeExpression)
        {
            if (comparisonTypeExpression is ConstantExpression constantExpression)
            {
                var comparisonType = (StringComparison)constantExpression.Value;
                return comparisonType switch
                {
                    StringComparison.CurrentCulture => false,
                    StringComparison.CurrentCultureIgnoreCase => true,
                    _ => throw new ExpressionNotSupportedException(comparisonTypeExpression, expression, because: "comparisonType must be StringComparison.CurrentCulture or StringComparison.CurrentCultureIgnoreCase")
                };
            }
            else
            {
                throw new ExpressionNotSupportedException(expression, because: "comparisonType must be a constant");
            }
        }

        private static bool GetIgnoreCaseFromOptions(Expression expression, Expression optionsExpression)
        {
            if (optionsExpression is ConstantExpression constantExpression)
            {
                var options = (CompareOptions)constantExpression.Value;
                return options switch
                {
                    CompareOptions.None => false,
                    CompareOptions.IgnoreCase => true,
                    _ => throw new ExpressionNotSupportedException(optionsExpression, expression, because: "options must be CompareOptions.None or CompareOptions.IgnoreCase")
                };
            }
            else
            {
                throw new ExpressionNotSupportedException(expression, because: "options must be a constant");
            }
        }

        private static bool HasIndexesAndLength(Expression expression, MethodInfo method, ReadOnlyCollection<Expression> arguments, out Expression indexAExpression, out Expression indexBExpression, out Expression lengthExpression)
        {
            if (method.IsOneOf(__stringCompareWithIndexesAndLengthMethods))
            {
                indexAExpression = arguments[1];
                indexBExpression = arguments[3];
                lengthExpression = arguments[4];
                return true;
            }
            else
            {
                indexAExpression = null;
                indexBExpression = null;
                lengthExpression = null;
                return false;
            }
        }
    }
}
