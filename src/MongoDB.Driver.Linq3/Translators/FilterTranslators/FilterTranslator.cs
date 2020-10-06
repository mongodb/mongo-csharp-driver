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

namespace MongoDB.Driver.Linq3.Translators.FilterTranslators
{
    public static class FilterTranslator
    {
        // public methods
        public static AstFilter Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return AndExpressionTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return ComparisonExpressionTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.Not:
                    return NotExpressionTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return OrExpressionTranslator.Translate(context, (BinaryExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
