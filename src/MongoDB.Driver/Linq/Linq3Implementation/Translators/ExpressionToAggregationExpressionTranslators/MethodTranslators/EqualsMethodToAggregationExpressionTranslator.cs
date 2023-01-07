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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    internal static class EqualsMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;

            if (IsStringEqualsMethod(method))
            {
                return TranslateStringEqualsMethod(context, expression);
            }

            if (IsInstanceEqualsMethod(method))
            {
                return TranslateInstanceEqualsMethod(context, expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsInstanceEqualsMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();
            return
                !method.IsStatic &&
                method.ReturnParameter.ParameterType == typeof(bool) &&
                parameters.Length == 1 &&
                parameters[0].ParameterType == method.DeclaringType;
        }

        private static bool IsStringEqualsMethod(MethodInfo method)
        {
            return method.DeclaringType == typeof(string);
        }

        private static AggregationExpression TranslateInstanceEqualsMethod(TranslationContext context, MethodCallExpression expression)
        {
            var lhsExpression = expression.Object;
            var lhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lhsExpression);
            var rhsExpression = expression.Arguments[0];
            var rhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rhsExpression);
            var ast = AstExpression.Eq(lhsTranslation.Ast, rhsTranslation.Ast);
            return new AggregationExpression(expression, ast, new BooleanSerializer());
        }

        private static AggregationExpression TranslateStringEqualsMethod(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;

            Expression lhsExpression;
            Expression rhsExpression;
            Expression comparisonTypeExpression = null;
            if (method.IsStatic)
            {
                lhsExpression = arguments[0];
                rhsExpression = arguments[1];
                if (arguments.Count == 3)
                {
                    comparisonTypeExpression = arguments[2];
                }
            }
            else
            {
                lhsExpression = expression.Object;
                rhsExpression = arguments[0];
                if (arguments.Count == 2)
                {
                    comparisonTypeExpression = arguments[1];
                }
            }

            var ignoreCase = false;
            if (comparisonTypeExpression != null)
            {
                ignoreCase = GetIgnoreCaseFromComparisonType(expression, comparisonTypeExpression);
            }

            var lhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lhsExpression);
            var rhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rhsExpression);

            var ast = ignoreCase ?
                AstExpression.Eq(AstExpression.StrCaseCmp(lhsTranslation.Ast, rhsTranslation.Ast), 0) :
                AstExpression.Eq(lhsTranslation.Ast, rhsTranslation.Ast);

            return new AggregationExpression(expression, ast, new BooleanSerializer());
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
                throw new ExpressionNotSupportedException(comparisonTypeExpression, expression, because: "comparisonType must be a constant");
            }
        }
    }
}
