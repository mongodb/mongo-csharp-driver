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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators.MethodTranslators;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class BinaryExpressionToAggregationExpressionTranslator
    {
        public static TranslatedExpression Translate(TranslationContext context, BinaryExpression expression)
        {
            if (StringConcatMethodToAggregationExpressionTranslator.CanTranslate(expression, out var method, out var arguments))
            {
                return StringConcatMethodToAggregationExpressionTranslator.Translate(context, expression, method, arguments);
            }

            if (GetTypeComparisonExpressionToAggregationExpressionTranslator.CanTranslate(expression))
            {
                return GetTypeComparisonExpressionToAggregationExpressionTranslator.Translate(context, expression);
            }

            if (StringGetCharsComparisonExpressionToAggregationExpressionTranslator.CanTranslate(expression, out var getCharsExpression))
            {
                return StringGetCharsComparisonExpressionToAggregationExpressionTranslator.Translate(context, expression, getCharsExpression);
            }

            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            if (!AreOperandTypesCompatible(expression, leftExpression, rightExpression))
            {
                throw new ExpressionNotSupportedException(expression, because: "operand types are not compatible with each other");
            }

            if (IsEnumArithmeticExpression(expression))
            {
                return TranslateEnumArithmeticExpression(context, expression, leftExpression, rightExpression);
            }

            if (IsCoalesceExpression(expression))
            {
                return TranslateCoalesceExpression(context, expression, leftExpression, rightExpression);
            }

            TranslatedExpression leftTranslation, rightTranslation;
            if (leftExpression is ConstantExpression leftConstantExpresion)
            {
                rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
                leftTranslation = TranslateConstant(expression, leftConstantExpresion, rightTranslation.Serializer);
            }
            else if (rightExpression is ConstantExpression rightConstantExpression)
            {
                leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                rightTranslation = TranslateConstant(expression, rightConstantExpression, leftTranslation.Serializer);
            }
            else
            {
                leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
            }

            if (IsArithmeticExpression(expression))
            {
                return TranslateArithmeticExpression(expression, leftExpression, rightExpression, leftTranslation, rightTranslation);
            }

            if (IsBooleanExpression(expression))
            {
                return TranslateBooleanExpression(expression, leftExpression, rightExpression, leftTranslation, rightTranslation);
            }

            if (IsComparisonExpression(expression))
            {
                return TranslateComparisonExpression(expression, leftTranslation, rightTranslation);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static bool AreOperandTypesCompatible(Expression expression, Expression leftExpression, Expression rightExpression)
        {
            if (leftExpression is ConstantExpression leftConstantExpression &&
                leftConstantExpression.Value == null)
            {
                return true;
            }

            if (rightExpression is ConstantExpression rightConstantExpression &&
                rightConstantExpression.Value == null)
            {
                return true;
            }

            if (leftExpression.Type.IsAssignableFrom(rightExpression.Type) ||
                rightExpression.Type.IsAssignableFrom(leftExpression.Type))
            {
                return true;
            }

            return false;
        }

        private static void EnsureCoalesceArgumentSerializersAreCompatible(Expression expression, IBsonSerializer leftSerializer, IBsonSerializer rightSerializer)
        {
            if (leftSerializer.Equals(rightSerializer) ||
                leftSerializer is INullableSerializer nullableLeftSerializer && nullableLeftSerializer.ValueSerializer.Equals(rightSerializer))
            {
                return;
            }

            throw new ExpressionNotSupportedException(expression, because: "argument serializers are not compatible");
        }

        private static IBsonSerializer GetConstantSerializer(BinaryExpression containingExpression, IBsonSerializer otherSerializer, Type constantType)
        {
            if (
                IsArithmeticExpression(containingExpression) &&
                otherSerializer.ValueType != constantType &&
                ConvertHelper.IsWideningConvert(otherSerializer.ValueType, constantType) &&
                otherSerializer is IRepresentationConfigurable otherRepresentationConfigurableSerializer &&
                SerializationHelper.IsNumericRepresentation(otherRepresentationConfigurableSerializer.Representation))
            {
                return ConvertHelper.CreateWiderSerializer(otherSerializer.ValueType, constantType);
            }
            else
            {
                return otherSerializer;
            }
        }

        private static bool IsAddOrSubtractExpression(Expression expression)
        {
            return expression.NodeType switch
            {
                ExpressionType.Add => true,
                ExpressionType.Subtract => true,
                _ => false
            };
        }

        private static bool IsArithmeticExpression(BinaryExpression expression)
        {
            return expression.Type.IsNumericOrNullableNumeric() && IsArithmeticOperator(expression.NodeType);
        }

        private static bool IsArithmeticOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => true,
                ExpressionType.And => true, // bitwise and
                ExpressionType.Divide => true,
                ExpressionType.ExclusiveOr => true, // bitwise xor
                ExpressionType.Modulo => true,
                ExpressionType.Multiply => true,
                ExpressionType.Or => true, // bitwise or
                ExpressionType.Power => true,
                ExpressionType.Subtract => true,
                _ => false
            };
        }

        private static bool IsBooleanExpression(BinaryExpression expression)
        {
            return expression.Type.IsBooleanOrNullableBoolean() && IsBooleanOperator(expression.NodeType);
        }

        private static bool IsBooleanOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.And => true,
                ExpressionType.AndAlso => true,
                ExpressionType.ExclusiveOr => true,
                ExpressionType.Or => true,
                ExpressionType.OrElse => true,
                _ => false
            };
        }

        private static bool IsCoalesceExpression(BinaryExpression expression)
        {
            return expression.NodeType == ExpressionType.Coalesce;
        }

        private static bool IsComparisonExpression(Expression expression)
        {
            return expression.NodeType switch
            {
                ExpressionType.Equal => true,
                ExpressionType.GreaterThan => true,
                ExpressionType.GreaterThanOrEqual => true,
                ExpressionType.LessThan => true,
                ExpressionType.LessThanOrEqual => true,
                ExpressionType.NotEqual => true,
                _ => false
            };
        }

        static bool IsConvertEnumToUnderlyingType(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var convertExpression = (UnaryExpression)expression;
                var sourceType = convertExpression.Operand.Type;
                var targetType = convertExpression.Type;

                return
                    sourceType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                    targetType.IsSameAsOrNullableOf(underlyingType);
            }

            return false;
        }

        internal static bool IsEnumArithmeticExpression(BinaryExpression expression)
        {
            return
                (IsEnumOrConvertEnumToUnderlyingType(expression.Left) || IsEnumOrConvertEnumToUnderlyingType(expression.Right)) &&
                IsAddOrSubtractExpression(expression);
        }

        static bool IsEnumOrConvertEnumToUnderlyingType(Expression expression)
        {
            return expression.Type.IsEnum || IsConvertEnumToUnderlyingType(expression);
        }

        private static TranslatedExpression TranslateArithmeticExpression(
            BinaryExpression expression,
            Expression leftExpression,
            Expression rightExpression,
            TranslatedExpression leftTranslation,
            TranslatedExpression rightTranslation)
        {
            SerializationHelper.EnsureRepresentationIsNumeric(expression, leftExpression, leftTranslation);
            SerializationHelper.EnsureRepresentationIsNumeric(expression, rightExpression, rightTranslation);

            var leftAst = ConvertHelper.RemoveWideningConvert(leftTranslation);
            var rightAst = ConvertHelper.RemoveWideningConvert(rightTranslation);
            var ast = expression.NodeType switch
            {
                ExpressionType.Add => AstExpression.Add(leftAst, rightAst),
                ExpressionType.And => AstExpression.BitAnd(leftAst, rightAst),
                ExpressionType.Divide => AstExpression.Divide(leftAst, rightAst),
                ExpressionType.ExclusiveOr => AstExpression.BitXor(leftAst, rightAst),
                ExpressionType.Modulo => AstExpression.Mod(leftAst, rightAst),
                ExpressionType.Multiply => AstExpression.Multiply(leftAst, rightAst),
                ExpressionType.Or => AstExpression.BitOr(leftAst, rightAst),
                ExpressionType.Power => AstExpression.Pow(leftAst, rightAst),
                ExpressionType.Subtract => AstExpression.Subtract(leftAst, rightAst),
                _ => throw new ExpressionNotSupportedException(expression)
            };
            var serializer = StandardSerializers.GetSerializer(expression.Type);

            return new TranslatedExpression(expression, ast, serializer);
        }

        private static TranslatedExpression TranslateBooleanExpression(
            BinaryExpression expression,
            Expression leftExpression,
            Expression rightExpression,
            TranslatedExpression leftTranslation,
            TranslatedExpression rightTranslation)
        {
            SerializationHelper.EnsureRepresentationIsBoolean(expression, leftExpression, leftTranslation);
            SerializationHelper.EnsureRepresentationIsBoolean(expression, rightExpression, rightTranslation);

            var leftAst = leftTranslation.Ast;
            var rightAst = rightTranslation.Ast;
            var ast = expression.NodeType switch
            {
                ExpressionType.And => AstExpression.And(leftAst, rightAst),
                ExpressionType.AndAlso => AstExpression.And(leftAst, rightAst),
                ExpressionType.ExclusiveOr => throw new ExpressionNotSupportedException(expression, because: "MongoDB does not have a boolean $xor operator"),
                ExpressionType.Or => AstExpression.Or(leftAst, rightAst),
                ExpressionType.OrElse => AstExpression.Or(leftAst, rightAst),
                _ => throw new ExpressionNotSupportedException(expression)
            };

            return new TranslatedExpression(expression, ast, StandardSerializers.BooleanSerializer);
        }

        private static TranslatedExpression TranslateCoalesceExpression(
            TranslationContext context,
            BinaryExpression expression,
            Expression leftExpression,
            Expression rightExpression)
        {
            TranslatedExpression leftTranslation, rightTranslation;
            if (leftExpression is ConstantExpression leftConstantExpression)
            {
                rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
                var constantType = leftConstantExpression.Type;
                var constantSerializer = rightTranslation.Serializer;
                if (constantType.IsNullable(out var valueType) && constantSerializer.ValueType == valueType)
                {
                    constantSerializer = NullableSerializer.Create(constantSerializer);
                }
                leftTranslation = ConstantExpressionToAggregationExpressionTranslator.Translate(leftConstantExpression, constantSerializer);
            }
            else if (rightExpression is ConstantExpression rightConstantExpression)
            {
                leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                var constantType = rightConstantExpression.Type;
                var constantSerializer = leftTranslation.Serializer;
                if (constantSerializer is INullableSerializer nullableSerializer && nullableSerializer.ValueSerializer.ValueType == constantType)
                {
                    constantSerializer = nullableSerializer.ValueSerializer;
                }
                rightTranslation = ConstantExpressionToAggregationExpressionTranslator.Translate(rightConstantExpression, constantSerializer);
            }
            else
            {
                leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
            }

            EnsureCoalesceArgumentSerializersAreCompatible(expression, leftTranslation.Serializer, rightTranslation.Serializer);
            var ast = AstExpression.IfNull(leftTranslation.Ast, rightTranslation.Ast);

            return new TranslatedExpression(expression, ast, rightTranslation.Serializer);
        }

        private static TranslatedExpression TranslateComparisonExpression(
            BinaryExpression expression,
            TranslatedExpression leftTranslation,
            TranslatedExpression rightTranslation)
        {
            SerializationHelper.EnsureArgumentSerializersAreEqual(expression, leftTranslation, rightTranslation);

            var leftAst = leftTranslation.Ast;
            var rightAst = rightTranslation.Ast;
            var ast = expression.NodeType switch
            {
                ExpressionType.Equal => AstExpression.Eq(leftAst, rightAst),
                ExpressionType.GreaterThan => AstExpression.Gt(leftAst, rightAst),
                ExpressionType.GreaterThanOrEqual => AstExpression.Gte(leftAst, rightAst),
                ExpressionType.LessThan => AstExpression.Lt(leftAst, rightAst),
                ExpressionType.LessThanOrEqual => AstExpression.Lte(leftAst, rightAst),
                ExpressionType.NotEqual => AstExpression.Ne(leftAst, rightAst),
                _ => throw new ExpressionNotSupportedException(expression)
            };

            return new TranslatedExpression(expression, ast, StandardSerializers.BooleanSerializer);
        }

        private static TranslatedExpression TranslateConstant(BinaryExpression containingExpression, ConstantExpression constantExpression, IBsonSerializer otherSerializer)
        {
            var constantSerializer = GetConstantSerializer(containingExpression, otherSerializer, constantExpression.Type);
            return ConstantExpressionToAggregationExpressionTranslator.Translate(constantExpression, constantSerializer);
        }

        private static TranslatedExpression TranslateEnumArithmeticExpression(
            TranslationContext context,
            BinaryExpression expression,
            Expression leftExpression,
            Expression rightExpression)
        {
            var leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
            var rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);

            TranslatedExpression enumTranslation, operandTranslation;
            if (IsEnumOrConvertEnumToUnderlyingType(leftExpression))
            {
                enumTranslation = leftTranslation;
                operandTranslation = rightTranslation;
            }
            else
            {
                enumTranslation = rightTranslation;
                operandTranslation = leftTranslation;
            }

            if (!SerializationHelper.IsRepresentedAsIntegerOrNullableInteger(enumTranslation))
            {
                throw new ExpressionNotSupportedException(expression, because: "arithmetic on enums is only allowed when the enum is represented as an integer");
            }

            if (!SerializationHelper.IsRepresentedAsIntegerOrNullableInteger(operandTranslation))
            {
                throw new ExpressionNotSupportedException(expression, because: "the value being added to or subtracted from an enum must be represented as an integer");
            }

            var leftAst = leftTranslation.Ast;
            var rightAst = rightTranslation.Ast;
            var ast = expression.NodeType switch
            {
                ExpressionType.Add => AstExpression.Add(leftAst, rightAst),
                ExpressionType.Subtract => AstExpression.Subtract(leftAst, rightAst),
                _ => throw new ExpressionNotSupportedException(expression)
            };
            var serializer = enumTranslation.Serializer;

            return new TranslatedExpression(expression, ast, serializer);
        }
    }
}
