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
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitUnary(UnaryExpression node)
    {
        var unaryOperator = node.NodeType;
        var operand = node.Operand;

        base.VisitUnary(node);

        switch (unaryOperator)
        {
            case ExpressionType.Negate:
                DeduceNegateSerializers(); // TODO: fold into general case?
                break;

            default:
                DeduceUnaryOperatorSerializers();
                break;
        }

        return node;

        void DeduceNegateSerializers()
        {
            DeduceSerializers(node, operand);
        }

        void DeduceUnaryOperatorSerializers()
        {
            if (IsNotKnown(node))
            {
                var resultSerializer = unaryOperator switch
                {
                    ExpressionType.ArrayLength => Int32Serializer.Instance,
                    ExpressionType.Convert or ExpressionType.TypeAs => GetConvertSerializer(),
                    ExpressionType.Not => StandardSerializers.GetSerializer(node.Type),
                    ExpressionType.Quote => IgnoreNodeSerializer.Create(node.Type),
                    _ => null
                };

                if (resultSerializer != null)
                {
                    AddNodeSerializer(node, resultSerializer);
                }
            }
        }

        IBsonSerializer GetConvertSerializer()
        {
            var sourceType = operand.Type;
            var targetType = node.Type;

            // handle double conversion (BsonValue)(object)x
            if (targetType == typeof(BsonValue) &&
                operand is UnaryExpression unarySourceExpression &&
                unarySourceExpression.NodeType == ExpressionType.Convert &&
                unarySourceExpression.Type == typeof(object))
            {
                operand = unarySourceExpression.Operand;
            }

            if (IsKnown(operand, out var sourceSerializer))
            {
                return GetTargetSerializer(node, sourceType, targetType, sourceSerializer);
            }

            return null;

            static IBsonSerializer GetTargetSerializer(UnaryExpression node, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                if (targetType == sourceType)
                {
                    return sourceSerializer;
                }

                // handle conversion to BsonValue before any others
                if (targetType == typeof(BsonValue))
                {
                    return GetConvertToBsonValueSerializer(node, sourceSerializer);
                }

                // from Nullable<T> must be handled before to Nullable<T>
                if (IsConvertFromNullableType(sourceType))
                {
                    return GetConvertFromNullableTypeSerializer(node, sourceType, targetType, sourceSerializer);
                }

                if (IsConvertToNullableType(targetType, out var valueType))
                {
                    var valueSerializer = valueType == targetType ? sourceSerializer : GetTargetSerializer(node, sourceType, valueType, sourceSerializer);
                    return valueSerializer != null ? GetConvertToNullableTypeSerializer(node, sourceType, targetType, valueSerializer) : null;
                }

                // from here on we know there are no longer any Nullable<T> types involved

                if (sourceType == typeof(BsonValue))
                {
                    return GetConvertFromBsonValueSerializer(node, targetType);
                }

                if (IsConvertEnumToUnderlyingType(sourceType, targetType))
                {
                    return GetConvertEnumToUnderlyingTypeSerializer(node, sourceType, targetType, sourceSerializer);
                }

                if (IsConvertUnderlyingTypeToEnum(sourceType, targetType))
                {
                    return GetConvertUnderlyingTypeToEnumSerializer(node, sourceType, targetType, sourceSerializer);
                }

                if (IsConvertEnumToEnum(sourceType, targetType))
                {
                    return GetConvertEnumToEnumSerializer(node, sourceType, targetType, sourceSerializer);
                }

                if (IsConvertToBaseType(sourceType, targetType))
                {
                    return GetConvertToBaseTypeSerializer(node, sourceType, targetType, sourceSerializer);
                }

                if (IsConvertToDerivedType(sourceType, targetType))
                {
                    return GetConvertToDerivedTypeSerializer(node, targetType, sourceSerializer);
                }

                if (IsNumericConversion(sourceType, targetType))
                {
                    return GetNumericConversionSerializer(node, sourceType, targetType, sourceSerializer);
                }

                return null;
            }

            static IBsonSerializer GetConvertFromBsonValueSerializer(UnaryExpression expression, Type targetType)
            {
                return targetType switch
                {
                    _ when targetType == typeof(string) => StringSerializer.Instance,
                    _ => throw new ExpressionNotSupportedException(expression, because: $"conversion from BsonValue to {targetType} is not supported")
                };
            }

            static IBsonSerializer GetConvertToBaseTypeSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                var derivedTypeSerializer = sourceSerializer;
                return DowncastingSerializer.Create(targetType, sourceType, derivedTypeSerializer);
            }

            static IBsonSerializer GetConvertToDerivedTypeSerializer(UnaryExpression expression, Type targetType, IBsonSerializer sourceSerializer)
            {
                var derivedTypeSerializer = sourceSerializer.GetDerivedTypeSerializer(targetType);
                return derivedTypeSerializer;
            }

            static IBsonSerializer GetConvertToBsonValueSerializer(UnaryExpression expression, IBsonSerializer sourceSerializer)
            {
                return BsonValueSerializer.Instance;
            }

            static IBsonSerializer GetConvertEnumToEnumSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                if (!sourceType.IsEnum)
                {
                    throw new ExpressionNotSupportedException(expression, because: "source type is not an enum");
                }
                if (!targetType.IsEnum)
                {
                    throw new ExpressionNotSupportedException(expression, because: "target type is not an enum");
                }

                return EnumSerializer.Create(targetType);
            }

            static IBsonSerializer GetConvertEnumToUnderlyingTypeSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                var enumSerializer = sourceSerializer;
                return AsEnumUnderlyingTypeSerializer.Create(enumSerializer);
            }

            static IBsonSerializer GetConvertFromNullableTypeSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                if (sourceSerializer is not INullableSerializer nullableSourceSerializer)
                {
                    throw new ExpressionNotSupportedException(expression, because: $"sourceSerializer type {sourceSerializer.GetType()} does not implement nameof(INullableSerializer)");
                }

                var sourceValueSerializer = nullableSourceSerializer.ValueSerializer;
                var sourceValueType = sourceValueSerializer.ValueType;

                if (targetType.IsNullable(out var targetValueType))
                {
                    var targetValueSerializer = GetTargetSerializer(expression, sourceValueType, targetValueType, sourceValueSerializer);
                    return NullableSerializer.Create(targetValueSerializer);
                }
                else
                {
                    return GetTargetSerializer(expression, sourceValueType, targetType, sourceValueSerializer);
                }
            }

            static IBsonSerializer GetConvertToNullableTypeSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                if (sourceType.IsNullable())
                {
                    throw new ExpressionNotSupportedException(expression, because: "sourceType is already nullable");
                }

                if (targetType.IsNullable())
                {
                    return NullableSerializer.Create(sourceSerializer);
                }

                throw new ExpressionNotSupportedException(expression, because: "targetType is not nullable");
            }

            static IBsonSerializer GetConvertUnderlyingTypeToEnumSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                IBsonSerializer targetSerializer;
                if (sourceSerializer is IAsEnumUnderlyingTypeSerializer enumUnderlyingTypeSerializer)
                {
                    targetSerializer = enumUnderlyingTypeSerializer.EnumSerializer;
                }
                else
                {
                    targetSerializer = EnumSerializer.Create(targetType);
                }

                return targetSerializer;
            }

            static IBsonSerializer GetNumericConversionSerializer(UnaryExpression expression, Type sourceType, Type targetType, IBsonSerializer sourceSerializer)
            {
                return NumericConversionSerializer.Create(sourceType, targetType, sourceSerializer);
            }

            static bool IsConvertEnumToEnum(Type sourceType, Type targetType)
            {
                return sourceType.IsEnum && targetType.IsEnum;
            }

            static bool IsConvertEnumToUnderlyingType(Type sourceType, Type targetType)
            {
                return
                    sourceType.IsEnum(out var underlyingType) &&
                    targetType == underlyingType;
            }

            static bool IsConvertFromNullableType(Type sourceType)
            {
                return sourceType.IsNullable();
            }

            static bool IsConvertToBaseType(Type sourceType, Type targetType)
            {
                return sourceType.IsSubclassOf(targetType) || sourceType.ImplementsInterface(targetType);
            }

            static bool IsConvertToDerivedType(Type sourceType, Type targetType)
            {
                return sourceType.IsAssignableFrom(targetType); // targetType either derives from sourceType or implements sourceType interface
            }

            static bool IsConvertToNullableType(Type targetType, out Type valueType)
            {
                return targetType.IsNullable(out  valueType);
            }

            static bool IsConvertUnderlyingTypeToEnum(Type sourceType, Type targetType)
            {
                return
                    targetType.IsEnum(out var underlyingType) &&
                    sourceType == underlyingType;
            }

            static bool IsNumericConversion(Type sourceType, Type targetType)
            {
                return sourceType.IsNumeric() && targetType.IsNumeric();
            }
        }
    }
}
