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

using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToAggregationExpressionTranslators
{
    public static class StringGetCharsComparisonExpressionToAggregationExpressionTranslator
    {
        // public static methods
        public static bool CanTranslate(BinaryExpression expression, out MethodCallExpression getCharsExpression, out string comparand)
        {
            if (IsConvertGetCharsExpression(expression.Left, out getCharsExpression) &&
                IsConstantComparandExpression(expression.Right, out comparand) &&
                TryGetComparisonOperator(expression.NodeType, out _))
            {
                return true;
            }

            getCharsExpression = null;
            comparand = null;
            return false;
        }

        public static AggregationExpression Translate(TranslationContext context, Expression expression, MethodCallExpression getCharsExpression, string comparand)
        {
            var method = getCharsExpression.Method;
            var arguments = getCharsExpression.Arguments;

            if (method.Is(StringMethod.GetChars))
            {
                var objectExpression = getCharsExpression.Object;
                var objectTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, objectExpression);

                var indexExpression = arguments[0];
                var indexTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, indexExpression);

                if (TryGetComparisonOperator(expression.NodeType, out var comparisonOperator))
                {
                    var ast = new AstBinaryExpression(
                        comparisonOperator,
                        new AstTernaryExpression(AstTernaryOperator.SubstrCP, objectTranslation.Ast, indexTranslation.Ast, 1),
                        comparand);
                    return new AggregationExpression(expression, ast, new BooleanSerializer());
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        // private static methods
        private static bool IsConstantComparandExpression(Expression expression, out string comparand)
        {
            if (expression is ConstantExpression constantExpression)
            {
                var c = (char)(int)constantExpression.Value;
                comparand = new string(c, 1);
                return true;
            }

            comparand = null;
            return false;
        }

        private static bool IsConvertGetCharsExpression(Expression expression, out MethodCallExpression getCharsExpression)
        {
            if (expression is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Type == typeof(int) &&
                unaryExpression.Operand is MethodCallExpression operandMethodCallExpression &&
                operandMethodCallExpression.Method.Is(StringMethod.GetChars))
            {
                getCharsExpression = operandMethodCallExpression;
                return true;
            }

            getCharsExpression = null;
            return false;
        }

        private static bool TryGetComparisonOperator(ExpressionType nodeType, out AstBinaryOperator result)
        {
            switch (nodeType)
            {
                case ExpressionType.Equal: result = AstBinaryOperator.Eq; return true;
                case ExpressionType.NotEqual: result = AstBinaryOperator.Ne; return true;
                case ExpressionType.LessThan: result = AstBinaryOperator.Lt; return true;
                case ExpressionType.LessThanOrEqual: result = AstBinaryOperator.Lte; return true;
                case ExpressionType.GreaterThan: result = AstBinaryOperator.Gt; return true;
                case ExpressionType.GreaterThanOrEqual: result = AstBinaryOperator.Gte; return true;
            }

            result = default;
            return false;
        }
    }
}
