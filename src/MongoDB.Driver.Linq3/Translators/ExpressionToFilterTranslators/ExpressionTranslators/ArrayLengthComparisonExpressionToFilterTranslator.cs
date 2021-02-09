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
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    public static class ArrayLengthComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out UnaryExpression arrayLengthExpression, out ConstantExpression sizeExpression)
        {
            if (leftExpression.NodeType == ExpressionType.ArrayLength &&
                rightExpression.NodeType == ExpressionType.Constant)
            {
                arrayLengthExpression = (UnaryExpression)leftExpression;
                sizeExpression = (ConstantExpression)rightExpression;
                return true;
            }

            arrayLengthExpression = null;
            sizeExpression = null;
            return false;
        }

        public static AstFilter Translate(TranslationContext context, BinaryExpression expression, UnaryExpression arrayLengthExpression, ConstantExpression sizeExpression)
        {
            if (arrayLengthExpression.NodeType == ExpressionType.ArrayLength)
            {
                var arrayExpression = arrayLengthExpression.Operand;
                var arrayField = ExpressionToFilterFieldTranslator.Translate(context, arrayExpression);
                var size = (int)sizeExpression.Value;

                switch (expression.NodeType)
                {
                    case ExpressionType.Equal:
                        return new AstSizeFilter(arrayField, size);

                    case ExpressionType.NotEqual:
                        return new AstNotFilter(new AstSizeFilter(arrayField, size));
                }

            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
