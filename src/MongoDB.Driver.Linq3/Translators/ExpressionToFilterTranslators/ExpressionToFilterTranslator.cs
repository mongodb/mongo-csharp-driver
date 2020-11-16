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

using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToExecutableQueryTranslators
{
    public static class ExpressionToFilterTranslator
    {
        // public methods
        public static AstFilter Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return AndExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.Equal:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                    return ComparisonExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);

                case ExpressionType.MemberAccess:
                    return MemberExpressionToFilterTranslator.Translate(context, (MemberExpression)expression);

                case ExpressionType.Not:
                    return NotExpressionToFilterTranslator.Translate(context, (UnaryExpression)expression);

                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return OrExpressionToFilterTranslator.Translate(context, (BinaryExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AstFilter Translate(TranslationContext context, LambdaExpression lambdaExpression, IBsonSerializer parameterSerializer)
        {
            var parameterExpression = lambdaExpression.Parameters.Single();
            var parameterSymbol = new Symbol(parameterExpression.Name, parameterSerializer);
            var lambdaContext = context.WithSymbolAsCurrent(parameterExpression, parameterSymbol);
            return Translate(lambdaContext, lambdaExpression.Body);
        }
    }
}
