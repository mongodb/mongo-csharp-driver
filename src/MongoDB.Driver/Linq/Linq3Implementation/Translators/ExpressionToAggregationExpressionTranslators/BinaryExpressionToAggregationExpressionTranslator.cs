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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Expressions;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class BinaryExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, BinaryExpression expression)
        {
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

            if (IsArithmeticExpression(expression))
            {
                leftExpression = ConvertHelper.RemoveWideningConvert(leftExpression);
                rightExpression = ConvertHelper.RemoveWideningConvert(rightExpression);
            }

            if (IsEnumExpression(expression))
            {
                return TranslateEnumExpression(context, expression);
            }

            AggregationExpression leftTranslation, rightTranslation;
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

            var ast = expression.NodeType switch
            {
                ExpressionType.Add => IsStringConcatenationExpression(expression) ?
                    AstExpression.Concat(leftTranslation.Ast, rightTranslation.Ast) :
                    AstExpression.Add(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.And => expression.Type == typeof(bool) ?
                    AstExpression.And(leftTranslation.Ast, rightTranslation.Ast) :
                    AstExpression.BitAnd(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.AndAlso => AstExpression.And(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Coalesce => AstExpression.IfNull(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Divide => AstExpression.Divide(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Equal => AstExpression.Eq(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.ExclusiveOr => expression.Type == typeof(bool) ?
                    throw new ExpressionNotSupportedException(expression, because: "MongoDB does not have an $xor operator") :
                    AstExpression.BitXor(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.GreaterThan => AstExpression.Gt(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.GreaterThanOrEqual => AstExpression.Gte(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.LessThan => AstExpression.Lt(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.LessThanOrEqual => AstExpression.Lte(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Modulo => AstExpression.Mod(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Multiply => AstExpression.Multiply(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.NotEqual => AstExpression.Ne(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Or => expression.Type == typeof(bool) ?
                    AstExpression.Or(leftTranslation.Ast, rightTranslation.Ast) :
                    AstExpression.BitOr(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.OrElse => AstExpression.Or(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Power => AstExpression.Pow(leftTranslation.Ast, rightTranslation.Ast),
                ExpressionType.Subtract => AstExpression.Subtract(leftTranslation.Ast, rightTranslation.Ast),
                _ => throw new ExpressionNotSupportedException(expression)
            };
            var serializer = expression.Type switch
            {
                Type t when t == typeof(bool) => new BooleanSerializer(),
                Type t when t == typeof(string) => new StringSerializer(),
                Type t when t == typeof(byte) => new ByteSerializer(),
                Type t when t == typeof(short) => new Int16Serializer(),
                Type t when t == typeof(ushort) => new UInt16Serializer(),
                Type t when t == typeof(int) => new Int32Serializer(),
                Type t when t == typeof(uint) => new UInt32Serializer(),
                Type t when t == typeof(long) => new Int64Serializer(),
                Type t when t == typeof(ulong) => new UInt64Serializer(),
                Type t when t == typeof(float) => new SingleSerializer(),
                Type t when t == typeof(double) => new DoubleSerializer(),
                Type t when t == typeof(decimal) => new DecimalSerializer(),
                Type { IsConstructedGenericType: true } t when t.GetGenericTypeDefinition() == typeof(Nullable<>) => (IBsonSerializer)Activator.CreateInstance(typeof(NullableSerializer<>).MakeGenericType(t.GenericTypeArguments[0])),
                Type { IsArray: true } t => (IBsonSerializer)Activator.CreateInstance(typeof(ArraySerializer<>).MakeGenericType(t.GetElementType())),
                _ => context.KnownSerializersRegistry.GetSerializer(expression) // Required for Coalesce
            };

            return new AggregationExpression(expression, ast, serializer);
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
            return expression.Type.IsNumeric() && IsArithmeticOperator(expression.NodeType);
        }

        private static bool IsArithmeticOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => true,
                ExpressionType.Divide => true,
                ExpressionType.Modulo => true,
                ExpressionType.Multiply => true,
                ExpressionType.Power => true,
                ExpressionType.Subtract => true,
                _ => false
            };
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

        internal static bool IsEnumExpression(BinaryExpression expression)
        {
            return IsEnumOrConvertEnumToUnderlyingType(expression.Left) || IsEnumOrConvertEnumToUnderlyingType(expression.Right);

        }

        static bool IsEnumOrConvertEnumToUnderlyingType(Expression expression)
        {
            return expression.Type.IsEnum || IsConvertEnumToUnderlyingType(expression);
        }

        private static bool IsStringConcatenationExpression(BinaryExpression expression)
        {
            return
                expression.NodeType == ExpressionType.Add &&
                expression.Type == typeof(string) &&
                expression.Left.Type == typeof(string) &&
                expression.Right.Type == typeof(string);
        }

        private static AstBinaryOperator ToBinaryOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Equal => AstBinaryOperator.Eq,
                ExpressionType.NotEqual => AstBinaryOperator.Ne,
                ExpressionType.LessThan => AstBinaryOperator.Lt,
                ExpressionType.LessThanOrEqual => AstBinaryOperator.Lte,
                ExpressionType.GreaterThan => AstBinaryOperator.Gt,
                ExpressionType.GreaterThanOrEqual => AstBinaryOperator.Gte,
                ExpressionType.Subtract => AstBinaryOperator.Subtract,
                _ => throw new Exception($"Unexpected expression type: {nodeType}.")
            };
        }

        private static AggregationExpression TranslateConstant(BinaryExpression containingExpression, ConstantExpression constantExpression, IBsonSerializer otherSerializer)
        {
            var serializedValue = SerializationHelper.SerializeValue(otherSerializer, constantExpression, containingExpression);
            var ast = AstExpression.Constant(serializedValue);
            return new AggregationExpression(constantExpression, ast, otherSerializer);
        }

        private static AggregationExpression TranslateEnumExpression(TranslationContext context, BinaryExpression expression)
        {
            var leftExpression = expression.Left;
            var rightExpression = expression.Right;

            AggregationExpression leftTranslation;
            AggregationExpression rightTranslation;
            IBsonSerializer serializer;

            if (IsComparisonExpression(expression))
            {
                if (leftExpression.NodeType == ExpressionType.Constant)
                {
                    rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
                    leftTranslation = TranslateEnumConstant(expression, leftExpression, rightTranslation.Serializer);
                }
                else if (rightExpression.NodeType == ExpressionType.Constant)
                {
                    leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                    rightTranslation = TranslateEnumConstant(expression, rightExpression, leftTranslation.Serializer);
                }
                else
                {
                    leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                    rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);
                }

                if (!leftTranslation.Serializer.Equals(rightTranslation.Serializer))
                {
                    throw new ExpressionNotSupportedException(expression, because: "the two enums being compared are serialized using different serializers");
                }

                serializer = BooleanSerializer.Instance;
            }
            else if (IsAddOrSubtractExpression(expression))
            {
                leftTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, leftExpression);
                rightTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, rightExpression);

                if (IsEnumOrConvertEnumToUnderlyingType(leftExpression))
                {
                    serializer = leftTranslation.Serializer;
                }
                else
                {
                    serializer = rightTranslation.Serializer;
                }

                var representation = BsonType.Int32; // assume an integer representation unless we can determine otherwise
                var valueSerializer = serializer;
                if (valueSerializer is INullableSerializer nullableSerializer)
                {
                    valueSerializer = nullableSerializer.ValueSerializer;
                }
                if (valueSerializer is IEnumUnderlyingTypeSerializer enumUnderlyingTypeSerializer &&
                    enumUnderlyingTypeSerializer.EnumSerializer is IHasRepresentationSerializer withRepresentationSerializer)
                {
                    representation = withRepresentationSerializer.Representation;
                }

                if (representation != BsonType.Int32 && representation != BsonType.Int64)
                {
                    throw new ExpressionNotSupportedException(expression, because: "arithmetic on enums is only allowed when the enum is represented as an integer");
                }
            }
            else
            {
                throw new ExpressionNotSupportedException(expression);
            }

            AstExpression ast;
            if (expression.NodeType == ExpressionType.Add)
            {
                ast = AstExpression.Add(leftTranslation.Ast, rightTranslation.Ast);
            }
            else
            {
                var binaryOperator = ToBinaryOperator(expression.NodeType);
                ast = AstExpression.Binary(binaryOperator, leftTranslation.Ast, rightTranslation.Ast);
            }

            return new AggregationExpression(expression, ast, serializer);

            static AggregationExpression TranslateEnumConstant(Expression expression, Expression constantExpression, IBsonSerializer serializer)
            {
                var value = constantExpression.GetConstantValue<object>(expression);
                var serializedValue = SerializationHelper.SerializeValue(serializer, value);
                var ast = AstExpression.Constant(serializedValue);
                return new AggregationExpression(constantExpression, ast, serializer);
            }
        }
    }
}
