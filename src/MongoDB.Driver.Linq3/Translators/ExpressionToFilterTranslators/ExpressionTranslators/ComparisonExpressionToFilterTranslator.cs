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
using MongoDB.Driver.Linq3.ExtensionMethods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.MethodTranslators;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class ComparisonExpressionToFilterTranslator
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

            if (ArrayLengthComparisonExpressionToFilterTranslator.CanTranslate(leftExpression, rightExpression, out var arrayLengthExpression, out var sizeExpression))
            {
                return ArrayLengthComparisonExpressionToFilterTranslator.Translate(context, expression, arrayLengthExpression, sizeExpression);
            }

            if (BitMaskComparisonExpressionToFilterTranslator.CanTranslate(leftExpression))
            {
                return BitMaskComparisonExpressionToFilterTranslator.Translate(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            if (CompareToComparisonExpressionToFilterTranslator.CanTranslate(leftExpression))
            {
                return CompareToComparisonExpressionToFilterTranslator.Translate(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            if (CountComparisonExpressionToFilterTranslator.CanTranslate(leftExpression, rightExpression, out var countExpression, out sizeExpression))
            {
                return CountComparisonExpressionToFilterTranslator.Translate(context, expression, countExpression, sizeExpression);
            }

            if (ModuloComparisonExpressionToFilterTranslator.CanTranslate(leftExpression, rightExpression, out var moduloExpression, out var remainderExpression))
            {
                return ModuloComparisonExpressionToFilterTranslator.Translate(context, expression, moduloExpression, remainderExpression);
            }

            if (StringExpressionToRegexFilterTranslator.CanTranslateComparisonExpression(leftExpression, comparisonOperator, rightExpression))
            {
                return StringExpressionToRegexFilterTranslator.TranslateComparisonExpression(context, expression, leftExpression, comparisonOperator, rightExpression);
            }

            var comparand = rightExpression.GetConstantValue<object>(containingExpression: expression);

            if (leftExpression.Type == typeof(bool) &&
                (comparisonOperator == AstComparisonFilterOperator.Eq || comparisonOperator == AstComparisonFilterOperator.Ne) &&
                rightExpression.Type == typeof(bool))
            {
                return TranslateComparisonToBooleanConstant(context, expression, leftExpression, comparisonOperator, (bool)comparand);
            }

            var field = ExpressionToFilterFieldTranslator.Translate(context, leftExpression);
            var serializedComparand = SerializationHelper.SerializeValue(field.Serializer, comparand);
            return AstFilter.Compare(field, comparisonOperator, serializedComparand);
        }

        private static AstFilter TranslateComparisonToBooleanConstant(TranslationContext context, Expression expression, Expression leftExpression, AstComparisonFilterOperator comparisonOperator, bool comparand)
        {
            var filter = ExpressionToFilterTranslator.Translate(context, leftExpression);

            if (filter is AstFieldOperationFilter fieldOperationFilter &&
                fieldOperationFilter.Operation is AstComparisonFilterOperation comparisonOperation &&
                comparisonOperation.Operator == AstComparisonFilterOperator.Eq &&
                comparisonOperation.Value == true)
            {
                var field = fieldOperationFilter.Field;

                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Eq:
                        return AstFilter.Eq(field, comparand);
                    case AstComparisonFilterOperator.Ne:
                        return AstFilter.Ne(field, comparand);
                    default:
                        throw new ExpressionNotSupportedException(expression);
                }
            }
            else
            {
                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Eq:
                        return comparand ? filter : AstFilter.Not(filter);
                    case AstComparisonFilterOperator.Ne:
                        return comparand ? AstFilter.Not(filter) : filter;
                    default:
                        throw new ExpressionNotSupportedException(expression);
                }
            }
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
                case ExpressionType.GreaterThan: return AstComparisonFilterOperator.Lt;
                case ExpressionType.GreaterThanOrEqual: return AstComparisonFilterOperator.Lte;
                case ExpressionType.LessThan: return AstComparisonFilterOperator.Gt;
                case ExpressionType.LessThanOrEqual: return AstComparisonFilterOperator.Gte;
                case ExpressionType.NotEqual: return AstComparisonFilterOperator.Ne;
                default: throw new ExpressionNotSupportedException(expression);
            }
        }
    }
}
