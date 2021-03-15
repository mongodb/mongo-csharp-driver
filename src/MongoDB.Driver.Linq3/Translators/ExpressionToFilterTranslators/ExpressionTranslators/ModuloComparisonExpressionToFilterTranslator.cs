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
using MongoDB.Bson;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public static class ModuloComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out BinaryExpression moduloExpression, out Expression remainderExpression)
        {
            if (leftExpression.NodeType == ExpressionType.Modulo)
            {
                moduloExpression = (BinaryExpression)leftExpression;
                remainderExpression = rightExpression;
                return true;
            }

            moduloExpression = null;
            remainderExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, BinaryExpression moduloExpression, Expression remainderExpression)
        {
            if (moduloExpression.Right is ConstantExpression divisorConstantExpression &&
                remainderExpression is ConstantExpression remainderConstantExpression)
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, moduloExpression.Left);

                BsonValue divisor;
                BsonValue remainder;
                if (divisorConstantExpression.Type == typeof(int) && remainderConstantExpression.Type == typeof(int))
                {
                    divisor = (int)divisorConstantExpression.Value;
                    remainder = (int)remainderConstantExpression.Value;
                }
                else if (divisorConstantExpression.Type == typeof(long) && remainderConstantExpression.Type == typeof(long))
                {
                    divisor = (long)divisorConstantExpression.Value;
                    remainder = (long)remainderConstantExpression.Value;
                }
                else
                {
                    throw new ExpressionNotSupportedException(expression);
                }

                var moduloComparisonAst = AstFilter.Mod(field, divisor, remainder);
                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        return moduloComparisonAst;

                    case ExpressionType.NotEqual:
                        return AstFilter.Not(moduloComparisonAst);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
