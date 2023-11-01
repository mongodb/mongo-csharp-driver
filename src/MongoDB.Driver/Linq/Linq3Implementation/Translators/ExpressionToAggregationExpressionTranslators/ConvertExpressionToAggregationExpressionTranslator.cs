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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators
{
    internal static class ConvertExpressionToAggregationExpressionTranslator
    {
        public static AggregationExpression Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.TypeAs)
            {
                var expressionType = expression.Type;
                if (expressionType == typeof(BsonValue))
                {
                    return TranslateConvertToBsonValue(context, expression, expression.Operand);
                }

                var operandExpression = expression.Operand;
                var operandTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, operandExpression);

                if (expressionType == operandExpression.Type)
                {
                    return operandTranslation;
                }

                if (IsConvertEnumToUnderlyingType(expression))
                {
                    return TranslateConvertEnumToUnderlyingType(expression, operandTranslation);
                }

                if (IsConvertUnderlyingTypeToEnum(expression))
                {
                    return TranslateConvertUnderlyingTypeToEnum(expression, operandTranslation);
                }

                if (IsConvertToBaseType(sourceType: operandExpression.Type, targetType: expressionType))
                {
                    return TranslateConvertToBaseType(expression, operandTranslation);
                }

                if (IsConvertToDerivedType(sourceType: operandExpression.Type, targetType: expressionType))
                {
                    return TranslateConvertToDerivedType(expression, operandTranslation);
                }

                if (expressionType.IsConstructedGenericType && expressionType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var valueType = expressionType.GetGenericArguments()[0];
                    if (operandExpression.Type == valueType)
                    {
                        // use the same AST but with a new nullable serializer
                        var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(valueType);
                        var valueSerializerType = typeof(IBsonSerializer<>).MakeGenericType(valueType);
                        var constructorInfo = nullableSerializerType.GetConstructor(new[] { valueSerializerType });
                        var nullableSerializer = (IBsonSerializer)constructorInfo.Invoke(new[] { operandTranslation.Serializer });
                        return new AggregationExpression(expression, operandTranslation.Ast, nullableSerializer);
                    }
                }

                var ast = operandTranslation.Ast;
                IBsonSerializer serializer;
                if (expressionType.IsInterface)
                {
                    // when an expression is cast to an interface it's a no-op as far as we're concerned
                    // and we can just use the serializer for the concrete type and members not defined in the interface will just be ignored
                    serializer = operandTranslation.Serializer;
                }
                else
                {
                    AstExpression to;
                    switch (expressionType.FullName)
                    {
                        case "MongoDB.Bson.ObjectId": to = "objectId"; serializer = ObjectIdSerializer.Instance; break;
                        case "System.Boolean": to = "bool"; serializer = BooleanSerializer.Instance; break;
                        case "System.DateTime": to = "date"; serializer = DateTimeSerializer.Instance; break;
                        case "System.Decimal": to = "decimal"; serializer = DecimalSerializer.Decimal128Instance; break; // not the default representation
                        case "System.Double": to = "double"; serializer = DoubleSerializer.Instance; break;
                        case "System.Int32": to = "int"; serializer = Int32Serializer.Instance; break;
                        case "System.Int64": to = "long"; serializer = Int64Serializer.Instance; break;
                        case "System.String": to = "string"; serializer = StringSerializer.Instance; break;
                        default: throw new ExpressionNotSupportedException(expression, because: $"conversion to {expressionType} is not supported");
                    }

                    ast = AstExpression.Convert(ast, to);
                }

                return new AggregationExpression(expression, ast, serializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsConvertEnumToUnderlyingType(UnaryExpression expression)
        {
            var sourceType = expression.Operand.Type;
            var targetType = expression.Type;

            return
                sourceType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                targetType.IsSameAsOrNullableOf(underlyingType);
        }

        private static bool IsConvertToBaseType(Type sourceType, Type targetType)
        {
            return sourceType.IsSubclassOf(targetType);
        }

        private static bool IsConvertToDerivedType(Type sourceType, Type targetType)
        {
            return targetType.IsSubclassOf(sourceType);
        }

        private static bool IsConvertUnderlyingTypeToEnum(UnaryExpression expression)
        {
            var sourceType = expression.Operand.Type;
            var targetType = expression.Type;

            return
                targetType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                sourceType.IsSameAsOrNullableOf(underlyingType);
        }

        private static AggregationExpression TranslateConvertToBaseType(UnaryExpression expression, AggregationExpression operandTranslation)
        {
            var baseType = expression.Type;
            var derivedType = expression.Operand.Type;
            var derivedTypeSerializer = operandTranslation.Serializer;
            var downcastingSerializer = DowncastingSerializer.Create(baseType, derivedType, derivedTypeSerializer);

            return new AggregationExpression(expression, operandTranslation.Ast, downcastingSerializer);
        }

        private static AggregationExpression TranslateConvertToDerivedType(UnaryExpression expression, AggregationExpression operandTranslation)
        {
            var serializer = BsonSerializer.LookupSerializer(expression.Type);

            return new AggregationExpression(expression, operandTranslation.Ast, serializer);
        }

        private static AggregationExpression TranslateConvertToBsonValue(TranslationContext context, UnaryExpression expression, Expression operand)
        {
            // handle double conversions like `(BsonValue)(object)x.Anything`
            if (operand is UnaryExpression unaryExpression &&
                unaryExpression.NodeType == ExpressionType.Convert &&
                unaryExpression.Type == typeof(object))
            {
                operand = unaryExpression.Operand;
            }

            var operandTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, operand);

            return new AggregationExpression(expression, operandTranslation.Ast, BsonValueSerializer.Instance);
        }

        private static AggregationExpression TranslateConvertEnumToUnderlyingType(UnaryExpression expression, AggregationExpression operandTranslation)
        {
            var sourceType = expression.Operand.Type;
            var targetType = expression.Type;

            IBsonSerializer enumSerializer;
            if (sourceType.IsNullable())
            {
                var nullableSerializer = (INullableSerializer)operandTranslation.Serializer;
                enumSerializer = nullableSerializer.ValueSerializer;
            }
            else
            {
                enumSerializer = operandTranslation.Serializer;
            }

            IBsonSerializer targetSerializer;
            var enumUnderlyingTypeSerializer = EnumUnderlyingTypeSerializer.Create(enumSerializer);
            if (targetType.IsNullable())
            {
                targetSerializer = NullableSerializer.Create(enumUnderlyingTypeSerializer);
            }
            else
            {
                targetSerializer = enumUnderlyingTypeSerializer;
            }

            return new AggregationExpression(expression, operandTranslation.Ast, targetSerializer);
        }

        private static AggregationExpression TranslateConvertUnderlyingTypeToEnum(UnaryExpression expression, AggregationExpression operandTranslation)
        {
            var targetType = expression.Type;

            var valueSerializer = operandTranslation.Serializer;
            if (valueSerializer is INullableSerializer nullableSerializer)
            {
                valueSerializer = nullableSerializer.ValueSerializer;
            }

            IBsonSerializer targetSerializer;
            if (valueSerializer is IEnumUnderlyingTypeSerializer enumUnderlyingTypeSerializer)
            {
                targetSerializer = enumUnderlyingTypeSerializer.EnumSerializer;
            }
            else
            {
                var enumType = targetType;
                if (targetType.IsNullable(out var wrappedType))
                {
                    enumType = wrappedType;
                }

                targetSerializer = EnumSerializer.Create(enumType);
            }

            if (targetType.IsNullableEnum())
            {
                targetSerializer = NullableSerializer.Create(targetSerializer);
            }

            return new AggregationExpression(expression, operandTranslation.Ast, targetSerializer);
        }
    }
}
