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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators
{
    public static class EqualsMethodToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, MethodCallExpression expression)
        {
            if (IsStringEqualsMethod(expression.Method))
            {
                return TranslateStringEqualsMethod(context, expression);
            }

            if (IsInstanceEqualsMethod(expression.Method))
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
            var rhsExpression = expression.Arguments[0];

            var lhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lhsExpression);
            var rhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rhsExpression);
            var ast = new AstBinaryExpression(AstBinaryOperator.Eq, lhsTranslation.Ast, rhsTranslation.Ast);

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

            StringComparison comparisonType = StringComparison.Ordinal;
            if (comparisonTypeExpression != null)
            {
                var constantExpression = comparisonTypeExpression as ConstantExpression;
                if (constantExpression == null)
                {
                    goto notSupported;
                }

                comparisonType = (StringComparison)constantExpression.Value;
            }

            var lhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, lhsExpression);
            var rhsTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rhsExpression);

            AstExpression ast;
            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                case StringComparison.Ordinal:
                    ast = new AstBinaryExpression(AstBinaryOperator.Eq, lhsTranslation.Ast, rhsTranslation.Ast);
                    break;

                case StringComparison.CurrentCultureIgnoreCase:
                case StringComparison.OrdinalIgnoreCase:
                    ast = new AstBinaryExpression(
                        AstBinaryOperator.Eq,
                        new AstBinaryExpression(AstBinaryOperator.StrCaseCmp, lhsTranslation.Ast, rhsTranslation.Ast),
                        0);
                    break;

                default:
                    goto notSupported;
            }

            return new AggregationExpression(expression, ast, new BooleanSerializer());

        notSupported:
            throw new ExpressionNotSupportedException(expression);
        }
    }
}
