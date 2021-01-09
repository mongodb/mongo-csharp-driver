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
using MongoDB.Driver.Linq3.Ast.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators
{
    public static class ComparisonExpressionToFilterTranslator
    {
        public static AstFilter Translate(TranslationContext context, BinaryExpression expression)
        {
            AstComparisonFilterOperator comparisonOperator;
            switch (expression.NodeType)
            {
                case ExpressionType.Equal: comparisonOperator = AstComparisonFilterOperator.Eq; break;
                case ExpressionType.GreaterThan: comparisonOperator = AstComparisonFilterOperator.Gt; break;
                case ExpressionType.GreaterThanOrEqual: comparisonOperator = AstComparisonFilterOperator.Gte; break;
                case ExpressionType.LessThan: comparisonOperator = AstComparisonFilterOperator.Lt; break;
                case ExpressionType.LessThanOrEqual: comparisonOperator = AstComparisonFilterOperator.Lte; break;
                case ExpressionType.NotEqual: comparisonOperator = AstComparisonFilterOperator.Ne; break;
                default: throw new ExpressionNotSupportedException(expression);
            }

            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            if (leftExpression.NodeType == ExpressionType.Constant && rightExpression.NodeType != ExpressionType.Constant)
            {
                var leftConstantExpression = leftExpression;
                leftExpression = rightExpression;
                rightExpression = leftConstantExpression;

                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Gte: comparisonOperator = AstComparisonFilterOperator.Lt; break;
                    case AstComparisonFilterOperator.Gt: comparisonOperator = AstComparisonFilterOperator.Lte; break;
                    case AstComparisonFilterOperator.Lte: comparisonOperator = AstComparisonFilterOperator.Gt; break;
                    case AstComparisonFilterOperator.Lt: comparisonOperator = AstComparisonFilterOperator.Gte; break;
                }
            }

            var field = ExpressionToFilterFieldTranslator.Translate(context, leftExpression);
            if (rightExpression is ConstantExpression constantExpression)
            {
                var value = constantExpression.Value;
                var serializedValue = SerializationHelper.SerializeValue(field.Serializer, value);

                return new AstComparisonFilter(comparisonOperator, field, serializedValue);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
