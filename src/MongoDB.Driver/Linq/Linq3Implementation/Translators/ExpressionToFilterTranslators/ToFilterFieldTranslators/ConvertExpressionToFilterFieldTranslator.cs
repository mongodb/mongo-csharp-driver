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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class ConvertExpressionToFilterFieldTranslator
    {
        public static TranslatedFilterField Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.TypeAs)
            {
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, expression.Operand);
                var fieldType = fieldTranslation.Serializer.ValueType;
                var targetType = expression.Type;

                if (targetType == fieldType)
                {
                    return fieldTranslation;
                }

                if (IsConvertEnumToUnderlyingType(fieldType, targetType))
                {
                    return TranslateConvertEnumToUnderlyingType(fieldTranslation, targetType);
                }

                if (IsConvertUnderlyingTypeToEnum(fieldType, targetType))
                {
                    return TranslateConvertUnderlyingTypeToEnum(fieldTranslation, targetType);
                }

                if (IsNumericConversion(fieldType, targetType))
                {
                    return TranslateNumericConversion(fieldTranslation, targetType);
                }

                if (IsConvertToNullable(fieldType, targetType))
                {
                    return TranslateConvertToNullable(fieldTranslation);
                }

                if (IsConvertToBaseType(fieldType, targetType))
                {
                    return TranslateConvertToBaseType(fieldTranslation, targetType);
                }

                if (IsConvertToDerivedType(fieldType, targetType))
                {
                    return TranslateConvertToDerivedType(fieldTranslation, targetType, context.SerializationDomain);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static bool IsConvertEnumToUnderlyingType(Type fieldType, Type targetType)
        {
            return
                fieldType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                targetType.IsSameAsOrNullableOf(underlyingType);
        }

        private static bool IsConvertToBaseType(Type fieldType, Type targetType)
        {
            return fieldType.IsSubclassOfOrImplements(targetType);
        }

        private static bool IsConvertToDerivedType(Type fieldType, Type targetType)
        {
            return fieldType.IsAssignableFrom(targetType); // targetType either derives from fieldType or implements fieldType interface
        }

        private static bool IsConvertToNullable(Type fieldType, Type targetType)
        {
            return
                targetType.IsConstructedGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                targetType.GetGenericArguments()[0] == fieldType;
        }

        private static bool IsConvertUnderlyingTypeToEnum(Type fieldType, Type targetType)
        {
            return
                targetType.IsEnumOrNullableEnum(out _, out var underlyingType) &&
                fieldType.IsSameAsOrNullableOf(underlyingType);
        }

        private static bool IsNumericConversion(Type fieldType, Type targetType)
        {
            return IsNumericType(fieldType) && IsNumericType(targetType);
        }

        private static bool IsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        private static TranslatedFilterField TranslateConvertEnumToUnderlyingType(TranslatedFilterField fieldTranslation, Type targetType)
        {
            var fieldSerializer = fieldTranslation.Serializer;
            var fieldType = fieldSerializer.ValueType;

            IBsonSerializer enumSerializer;
            if (fieldType.IsNullable())
            {
                var nullableSerializer = (INullableSerializer)fieldSerializer;
                enumSerializer = nullableSerializer.ValueSerializer;
            }
            else
            {
                enumSerializer = fieldSerializer;
            }

            var targetSerializer = EnumUnderlyingTypeSerializer.Create(enumSerializer);
            if (targetType.IsNullable())
            {
                targetSerializer = NullableSerializer.Create(targetSerializer);
            }

            return new TranslatedFilterField(fieldTranslation.Ast, targetSerializer);
        }

        private static TranslatedFilterField TranslateConvertToBaseType(TranslatedFilterField fieldTranslation, Type baseType)
        {
            var derivedTypeSerializer = fieldTranslation.Serializer;
            var derivedType = derivedTypeSerializer.ValueType;
            var targetSerializer = DowncastingSerializer.Create(baseType, derivedType, derivedTypeSerializer);
            return new TranslatedFilterField(fieldTranslation.Ast, targetSerializer);
        }

        private static TranslatedFilterField TranslateConvertToDerivedType(TranslatedFilterField fieldTranslation, Type targetType, IBsonSerializationDomain serializationDomain)
        {
            var targetSerializer = serializationDomain.LookupSerializer(targetType);
            return new TranslatedFilterField(fieldTranslation.Ast, targetSerializer);
        }

        private static TranslatedFilterField TranslateConvertToNullable(TranslatedFilterField fieldTranslation)
        {
            var nullableSerializer = NullableSerializer.Create(fieldTranslation.Serializer);
            return new TranslatedFilterField(fieldTranslation.Ast, nullableSerializer);
        }

        private static TranslatedFilterField TranslateConvertUnderlyingTypeToEnum(TranslatedFilterField fieldTranslation, Type targetType)
        {
            var valueSerializer = fieldTranslation.Serializer;
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

            return new TranslatedFilterField(fieldTranslation.Ast, targetSerializer);
        }

        private static TranslatedFilterField TranslateNumericConversion(TranslatedFilterField fieldTranslation, Type targetType)
        {
            IBsonSerializer targetTypeSerializer = targetType switch
            {
                Type t when t == typeof(byte) => new ByteSerializer(),
                Type t when t == typeof(sbyte) => new SByteSerializer(),
                Type t when t == typeof(short) => new Int16Serializer(),
                Type t when t == typeof(ushort) => new UInt16Serializer(),
                Type t when t == typeof(int) => new Int32Serializer(),
                Type t when t == typeof(uint) => new UInt32Serializer(),
                Type t when t == typeof(long) => new Int64Serializer(),
                Type t when t == typeof(ulong) => new UInt64Serializer(),
                Type t when t == typeof(float) => new SingleSerializer(),
                Type t when t == typeof(double) => new DoubleSerializer(),
                Type t when t == typeof(decimal) => new DecimalSerializer(),
                _ => throw new Exception($"Unexpected target type: {targetType}.")
            };
            if (fieldTranslation.Serializer is IRepresentationConfigurable representationConfigurableFieldSerializer &&
                targetTypeSerializer is IRepresentationConfigurable representationConfigurableTargetTypeSerializer)
            {
                var fieldRepresentation = representationConfigurableFieldSerializer.Representation;
                if (fieldRepresentation == BsonType.String)
                {
                    targetTypeSerializer = representationConfigurableTargetTypeSerializer.WithRepresentation(fieldRepresentation);
                }
            }
            if (fieldTranslation.Serializer is IRepresentationConverterConfigurable converterConfigurableFieldSerializer &&
                targetTypeSerializer is IRepresentationConverterConfigurable converterConfigurableTargetTypeSerializer)
            {
                targetTypeSerializer = converterConfigurableTargetTypeSerializer.WithConverter(converterConfigurableFieldSerializer.Converter);
            }
            return new TranslatedFilterField(fieldTranslation.Ast, targetTypeSerializer);
        }
    }
}
