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
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class BinaryExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            if (StringGetCharsComparisonExpressionToAggregationExpressionTranslator.CanTranslate(expression, out var getCharsExpression))
            {
                return StringGetCharsComparisonExpressionToAggregationExpressionTranslator.Translate(context, expression, getCharsExpression);
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return AddExpressionToAggregationExpressionTranslator.Translate(context, expression);

                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return AndExpressionToAggregationExpressionTranslator.Translate(context, expression);

                case ExpressionType.Divide:
                    return DivideExpressionToAggregationExpressionTranslator.Translate(context, expression);

                case ExpressionType.Multiply:
                    return MultiplyExpressionToAggregationExpressionTranslator.Translate(context, expression);

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return OrExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            var leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, expression.Left);
            var rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, expression.Right);

            var ast = expression.NodeType switch
            {
                ExpressionType.Coalesce => AstExpression.IfNull(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Divide => AstExpression.Divide(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Equal => AstExpression.Eq(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.GreaterThan => AstExpression.Gt(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.GreaterThanOrEqual => AstExpression.Gte(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.LessThan => AstExpression.Lt(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.LessThanOrEqual => AstExpression.Lte(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Modulo => AstExpression.Mod(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Multiply => AstExpression.Multiply(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.NotEqual => AstExpression.Ne(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Power => AstExpression.Pow(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Subtract => AstExpression.Subtract(leftTranslation.Ast, rightTranslation.Ast),
                _ => throw new ExpressionNotSupportedException(expression)
            };
            var serializer = BsonSerializer.LookupSerializer(expression.Type); // TODO: get correct serializer

            return new AggregationExpression(expression, ast, serializer);
        }
    }
}
