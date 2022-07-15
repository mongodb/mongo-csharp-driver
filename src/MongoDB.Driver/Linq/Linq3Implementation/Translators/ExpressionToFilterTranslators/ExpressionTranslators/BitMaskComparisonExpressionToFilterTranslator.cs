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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ExpressionTranslators
{
    internal static class BitMaskComparisonExpressionToFilterTranslator
    {
        public static bool CanTranslate(Expression leftExpression, Expression rightExpression)
        {
            return CanTranslate(leftExpression, rightExpression, out _);
        }

        public static bool CanTranslate(Expression leftExpression, Expression rightExpression, out BinaryExpression leftBinaryExpression)
        {
            if (rightExpression.NodeType == ExpressionType.Constant)
            {
                // a leftExpression with an & operation with an enum looks like:
                // Convert(Convert((Convert(x.E, Int32) & mask), E), Int32)
                if (leftExpression is UnaryExpression outerToUnderlyingTypeConvertExpression &&
                    outerToUnderlyingTypeConvertExpression.NodeType == ExpressionType.Convert &&
                    outerToUnderlyingTypeConvertExpression.Operand is UnaryExpression innerToEnumConvertExpression &&
                    innerToEnumConvertExpression.NodeType == ExpressionType.Convert &&
                    innerToEnumConvertExpression.Operand is BinaryExpression innerBinaryExpression &&
                    innerBinaryExpression.NodeType == ExpressionType.And &&
                    innerBinaryExpression.Left is UnaryExpression innerToUnderlyingTypeConvertExpression &&
                    innerToUnderlyingTypeConvertExpression.NodeType == ExpressionType.Convert)
                {
                    var enumType = innerToEnumConvertExpression.Type;
                    if (enumType.IsEnum)
                    {
                        var underlyingType = enumType.GetEnumUnderlyingType();
                        if (outerToUnderlyingTypeConvertExpression.Type == underlyingType &&
                            innerToEnumConvertExpression.Type == enumType &&
                            innerToUnderlyingTypeConvertExpression.Type == underlyingType)
                        {
                            leftExpression = innerBinaryExpression; // Convert(x.E, Int32) & mask
                        }
                    }
                }

                leftBinaryExpression = leftExpression as BinaryExpression;
                if (leftBinaryExpression != null &&
                    leftBinaryExpression.NodeType == ExpressionType.And)
                {
                    return true;
                }
            }

            leftBinaryExpression = null;
            return false;
        }

        // caller is responsible for ensuring constant is on the right
        public static AstFilter Translate(
            TranslationContext context,
            Expression expression,
            Expression leftExpression,
            AstComparisonFilterOperator comparisonOperator,
            Expression rightExpression)
        {
            if (CanTranslate(leftExpression, rightExpression, out var leftBinaryExpression))
            {
                var fieldExpression = ConvertHelper.RemoveConvertToEnumUnderlyingType(leftBinaryExpression.Left);
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
