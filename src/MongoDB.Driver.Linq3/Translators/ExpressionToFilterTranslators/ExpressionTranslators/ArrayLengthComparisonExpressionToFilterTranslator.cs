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
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class ArrayLengthComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out UnaryExpression arrayLengthExpression, out Expression sizeExpression)
        {
            if (leftExpression.NodeType == ExpressionType.ArrayLength)
            {
                arrayLengthExpression = (UnaryExpression)leftExpression;
                sizeExpression = rightExpression;
                return true;
            }

            arrayLengthExpression = null;
            sizeExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, UnaryExpression arrayLengthExpression, Expression sizeExpression)
        {
            if (arrayLengthExpression.NodeType == ExpressionType.ArrayLength)
            {
                var arrayExpression = arrayLengthExpression.Operand;
                var arrayField = ExpressionToFilterFieldTranslator.Translate(context, arrayExpression);
                var size = sizeExpression.GetConstantValue<int>(containingExpression: expression);

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        return AstFilter.Size(arrayField, size);

                    case ExpressionType.NotEqual:
                        return AstFilter.Not(AstFilter.Size(arrayField, size));
                }

            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
