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
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked || expression.NodeType == ExpressionType.TypeAs)
            {
                var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, expression.Operand);
                var fieldType = fieldTranslation.Serializer.ValueType;
                var targetType = expression.Type;

                if (targetType == fieldType)
                {
                    return fieldTranslation;
                }

                // must check for enum conversions before numeric conversions
                if (IsConvertEnumToIntegralType(fieldType, targetType))
                {
                    return TranslateConvertEnumToIntegralType(fieldTranslation, targetType);
                }

                if (IsConvertIntegralTypeToEnum(fieldType, targetType))
                {
                    return TranslateConvertIntegralTypeToEnum(fieldTranslation, targetType);
                }

                if (IsNumericOrCharConversion(fieldType, targetType))
                {
                    return TranslateNumericOrCharConversion(expression, fieldTranslation, targetType);
                }

                if (IsNullableNumericOrCharConversion(fieldType, targetType, out _, out var underlyingTargetType))
                {
                    return TranslateNullableNumericOrCharConversion(expression, fieldTranslation, underlyingTargetType);
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
                    return TranslateConvertToDerivedType(fieldTranslation, targetType);
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static IBsonSerializer GetConfiguredNumericTargetTypeSerializer(IBsonSerializer fieldSerializer, Type targetType)
        {
            var targetTypeSerializer = StandardSerializers.GetSerializer(targetType);

            if (fieldSerializer is IRepresentationConfigurable representationConfigurableFieldSerializer &&
                targetTypeSerializer is IRepresentationConfigurable representationConfigurableTargetTypeSerializer)
            {
                var fieldRepresentation = representationConfigurableFieldSerializer.Representation;
                if (fieldRepresentation == BsonType.String)
                {
                    targetTypeSerializer = representationConfigurableTargetTypeSerializer.WithRepresentation(fieldRepresentation);
                }
            }
            if (fieldSerializer is IRepresentationConverterConfigurable converterConfigurableFieldSerializer &&
                targetTypeSerializer is IRepresentationConverterConfigurable converterConfigurableTargetTypeSerializer)
            {
                targetTypeSerializer = converterConfigurableTargetTypeSerializer.WithConverter(converterConfigurableFieldSerializer.Converter);
            }

            return targetTypeSerializer;
        }

        private static bool IsConvertEnumToIntegralType(Type fieldType, Type targetType)
        {
            return
                fieldType.IsEnumOrNullableEnum(out _, out _) &&
                targetType.IsIntegralOrNullableIntegral();
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

        private static bool IsConvertIntegralTypeToEnum(Type fieldType, Type targetType)
        {
            return
                fieldType.IsIntegralOrNullableIntegral() &&
                targetType.IsEnumOrNullableEnum(out _, out _);
        }

        private static bool IsNullableNumericOrCharConversion(Type fieldType, Type targetType, out Type underlyingFieldType, out Type underlyingTargetType)
        {
            return ConvertHelper.IsNullableNumericOrCharConversion(fieldType, targetType, out underlyingFieldType, out underlyingTargetType);
        }

        private static bool IsNumericOrCharConversion(Type fieldType, Type targetType)
        {
            return ConvertHelper.IsNumericOrCharConversion(fieldType, targetType);
        }

        private static bool IsStandardNumericSerializer(IBsonSerializer serializer)
        {
            return
                serializer is ByteSerializer ||
                serializer is SByteSerializer ||
                serializer is Int16Serializer ||
                serializer is UInt16Serializer ||
                serializer is Int32Serializer ||
                serializer is UInt32Serializer ||
                serializer is Int64Serializer ||
                serializer is UInt64Serializer ||
                serializer is DoubleSerializer ||
                serializer is SingleSerializer ||
                serializer is DecimalSerializer ||
                serializer is Decimal128Serializer;
        }

        private static bool IsStandardNumericOrCharSerializer(IBsonSerializer serializer)
        {
            return IsStandardNumericSerializer(serializer) || serializer is CharSerializer;
        }

        private static TranslatedFilterField TranslateConvertEnumToIntegralType(TranslatedFilterField fieldTranslation, Type targetType)
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

            var integralType = targetType.IsNullable() ? Nullable.GetUnderlyingType(targetType) : targetType;

            // the serializer converts in the opposite direction as the C# expression
            var targetSerializer = ConvertIntegralTypeToEnumSerializer.Create(integralType, enumSerializer);
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

        private static TranslatedFilterField TranslateConvertToDerivedType(TranslatedFilterField fieldTranslation, Type targetType)
        {
            var targetSerializer = BsonSerializer.LookupSerializer(targetType);
            return new TranslatedFilterField(fieldTranslation.Ast, targetSerializer);
        }

        private static TranslatedFilterField TranslateConvertToNullable(TranslatedFilterField fieldTranslation)
        {
            var nullableSerializer = NullableSerializer.Create(fieldTranslation.Serializer);
            return new TranslatedFilterField(fieldTranslation.Ast, nullableSerializer);
        }

        private static TranslatedFilterField TranslateConvertIntegralTypeToEnum(TranslatedFilterField fieldTranslation, Type targetType)
        {
            var valueSerializer = fieldTranslation.Serializer;
            if (valueSerializer is INullableSerializer nullableSerializer)
            {
                valueSerializer = nullableSerializer.ValueSerializer;
            }

            IBsonSerializer targetSerializer;
            if (valueSerializer is IConvertIntegralTypeToEnumSerializer enumUnderlyingTypeSerializer)
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

        private static TranslatedFilterField TranslateNullableNumericOrCharConversion(Expression expression, TranslatedFilterField fieldTranslation, Type targetUnderlyingType)
        {
            var fieldNullableSerializer = (INullableSerializer)fieldTranslation.Serializer;
            var fieldUnderlyingTypeSerializer = fieldNullableSerializer.ValueSerializer;

            if (IsStandardNumericOrCharSerializer(fieldUnderlyingTypeSerializer))
            {
                var targetUnderlyingTypeSerializer = GetConfiguredNumericTargetTypeSerializer(fieldUnderlyingTypeSerializer, targetUnderlyingType);
                var nullableTargetUnderlyingTypeSerializer = NullableSerializer.Create(targetUnderlyingTypeSerializer);
                return new TranslatedFilterField(fieldTranslation.Ast, nullableTargetUnderlyingTypeSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static TranslatedFilterField TranslateNumericOrCharConversion(Expression expression, TranslatedFilterField fieldTranslation, Type targetType)
        {
            var fieldSerializer = fieldTranslation.Serializer;

            if (IsStandardNumericOrCharSerializer(fieldSerializer))
            {
                var targetTypeSerializer = GetConfiguredNumericTargetTypeSerializer(fieldSerializer, targetType);
                return new TranslatedFilterField(fieldTranslation.Ast, targetTypeSerializer);
            }

            throw new ExpressionNotSupportedException(expression);
        }
    }
}
