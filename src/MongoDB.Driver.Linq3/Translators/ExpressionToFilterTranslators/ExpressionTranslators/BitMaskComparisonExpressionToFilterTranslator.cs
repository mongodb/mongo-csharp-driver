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

using System;
using System.Linq.Expressions;
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.ExtensionMethods;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class BitMaskComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression)
        {
            return
                leftExpression is BinaryExpression leftBinaryExpression &&
                leftBinaryExpression.NodeType == ExpressionType.And;
        }

        public static AstFilter Translate(
            TranslationContext context,
            Expression expression,
            Expression leftExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression rightExpression)
        {
            if (leftExpression is BinaryExpression leftBinaryExpression &&
                leftBinaryExpression.NodeType == ExpressionType.And)
            {
                var fieldExpression = leftBinaryExpression.Left;
                var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);

                var bitMaskExpression = leftBinaryExpression.Right;
                var bitMask = bitMaskExpression.GetConstantValue<object>(containingExpression: expression);
                var serializedBitMask = SerializationHelper.SerializeValue(field.Serializer, bitMask);

                var rightValue = rightExpression.GetConstantValue<object>(containingExpression: expression);
                var zeroValue = Activator.CreateInstance(bitMask.GetType());

                switch (comparisonOperator)
                {
                    case AstComparisonFilterOperator.Eq:
                        if (rightValue.Equals(zeroValue))
                        {
                            return AstFilter.BitsAllClear(field, serializedBitMask);
                        }
                        else if (rightValue.Equals(bitMask))
                        {
                            return AstFilter.BitsAllSet(field, serializedBitMask);
                        }
                        break;

                    case AstComparisonFilterOperator.Ne:
                        if (rightValue.Equals(zeroValue))
                        {
                            return AstFilter.BitsAnySet(field, serializedBitMask);
                        }
                        else if (rightValue.Equals(bitMask))
                        {
                            return AstFilter.BitsAnyClear(field, serializedBitMask);
                        }
                        break;
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
