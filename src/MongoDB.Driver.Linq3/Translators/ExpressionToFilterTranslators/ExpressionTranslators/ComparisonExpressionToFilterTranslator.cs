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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public static class ComparisonExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, BinaryExpression expression)
        {
            var comparisonOperator = GetComparisonOperator(expression);
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            if (leftExpression.NodeType == ExpressionType.Constant && rightExpression.NodeType != ExpressionType.Constant)
            {
                comparisonOperator = GetComparisonOperatorForSwappedLeftAndRight(expression);
                (leftExpression, rightExpression) = (rightExpression, leftExpression);
            }

            if (ArrayLengthComparisonExpressionToFilterTranslator.CanTranslate(expression, out var arrayLengthExpression, out var sizeExpression))
            {
                return ArrayLengthComparisonExpressionToFilterTranslator.Translate(context, expression, arrayLengthExpression, sizeExpression);
            }

            if (rightExpression is ConstantExpression constantValueExpression)
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, leftExpression);
                var value = constantValueExpression.Value;
                var serializedValue = SerializationHelper.SerializeValue(field.Serializer, value);
                return new AstComparisonFilter(comparisonOperator, field, serializedValue);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstComparisonFilterOperator GetComparisonOperator(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal: return AstComparisonFilterOperator.Eq;
                case ExpressionType.GreaterThan: return AstComparisonFilterOperator.Gt;
                case ExpressionType.GreaterThanOrEqual: return AstComparisonFilterOperator.Gte;
                case ExpressionType.LessThan: return AstComparisonFilterOperator.Lt;
                case ExpressionType.LessThanOrEqual: return AstComparisonFilterOperator.Lte;
                case ExpressionType.NotEqual: return AstComparisonFilterOperator.Ne;
                default: throw new ExpressionNotSupportedException(expression);
            }
        }

        private static AstComparisonFilterOperator GetComparisonOperatorForSwappedLeftAndRight(Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal: return AstComparisonFilterOperator.Eq;
                case ExpressionType.GreaterThan: return AstComparisonFilterOperator.Lte;
                case ExpressionType.GreaterThanOrEqual: return AstComparisonFilterOperator.Lt;
                case ExpressionType.LessThan: return AstComparisonFilterOperator.Gte;
                case ExpressionType.LessThanOrEqual: return AstComparisonFilterOperator.Gt;
                case ExpressionType.NotEqual: return AstComparisonFilterOperator.Ne;
                default: throw new ExpressionNotSupportedException(expression);
            }
        }
    }
}
