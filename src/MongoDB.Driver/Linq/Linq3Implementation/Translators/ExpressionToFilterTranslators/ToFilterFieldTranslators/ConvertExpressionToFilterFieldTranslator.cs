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
using MongoDB.Driver.Linq.Linq3Implementation.Ast.Filters;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class ConvertExpressionToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.TypeAs)
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, expression.Operand);
                var fieldType = field.Serializer.ValueType;
                var targetType = expression.Type;

                if (targetType == fieldType)
                {
                    return field;
                }

                // must check for enum conversions before numeric conversions
                if (IsConvertEnumToUnderlyingType(fieldType, targetType))
                {
                    return TranslateConvertEnumToUnderlyingType(field, targetType);
                }

                if (IsConvertUnderlyingTypeToEnum(fieldType, targetType))
                {
                    return TranslateConvertUnderlyingTypeToEnum(field, targetType);
                }

                if (IsNumericConversion(fieldType, targetType))
                {
                    return TranslateNumericConversion(field, targetType);
                }

                if (IsNullableNumericConversion(fieldType, targetType, out var underlyingFieldType, out var underlyingTargetType))
                {
                    return TranslateNullableNumericConversion(field, underlyingFieldType, underlyingTargetType);
                }

                if (IsConvertToNullable(fieldType, targetType))
                {
                    return TranslateConvertToNullable(field);
                }

                if (IsConvertToBaseType(fieldType, targetType))
                {
                    return TranslateConvertToBaseType(field, targetType);
                }

                if (IsConvertToDerivedType(fieldType, targetType))
                {
                    return TranslateConvertToDerivedType(field, targetType);
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
            return fieldType.IsSubclassOf(targetType);
        }

        private static bool IsConvertToDerivedType(Type fieldType, Type targetType)
        {
            return targetType.IsSubclassOf(fieldType);
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
            return NumericConversionHelper.IsNumericConversion(fieldType, targetType);
        }

        private static bool IsNullableNumericConversion(Type fieldType, Type targetType, out Type underlyingFieldType, out Type underlyingTargetType)
        {
            return NumericConversionHelper.IsNullableNumericConversion(fieldType, targetType, out underlyingFieldType, out underlyingTargetType);   
        }

        private static AstFilterField TranslateConvertEnumToUnderlyingType(AstFilterField field, Type targetType)
        {
            var fieldSerializer = field.Serializer;
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

            return AstFilter.Field(field.Path, targetSerializer);
        }

        private static AstFilterField TranslateConvertToBaseType(AstFilterField field, Type baseType)
        {
            var derivedTypeSerializer = field.Serializer;
            var derivedType = derivedTypeSerializer.ValueType;
            var targetSerializer = DowncastingSerializer.Create(baseType, derivedType, derivedTypeSerializer);
            return AstFilter.Field(field.Path, targetSerializer);
        }

        private static AstFilterField TranslateConvertToDerivedType(AstFilterField field, Type targetType)
        {
            var targetSerializer = BsonSerializer.LookupSerializer(targetType);
            return AstFilter.Field(field.Path, targetSerializer);
        }

        private static AstFilterField TranslateConvertToNullable(AstFilterField field)
        {
            var nullableSerializer = NullableSerializer.Create(field.Serializer);
            return AstFilter.Field(field.Path, nullableSerializer);
        }

        private static AstFilterField TranslateConvertUnderlyingTypeToEnum(AstFilterField field, Type targetType)
        {
            var valueSerializer = field.Serializer;
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

            return AstFilter.Field(field.Path, targetSerializer);
        }

        private static AstFilterField TranslateNumericConversion(AstFilterField field, Type targetType)
        {
            var fieldSerializer = field.Serializer;
            var fieldType = fieldSerializer.ValueType;
            var targetSerializer = NumericConversionHelper.CreateNumericConversionSerializer(fieldType, targetType, fieldSerializer);
            return AstFilter.Field(field.Path, targetSerializer);
        }

        private static AstFilterField TranslateNullableNumericConversion(AstFilterField field, Type underlyingFieldType, Type underlyingTargetType)
        {
            var fieldSerializer = (INullableSerializer)field.Serializer;
            var underlyingFieldSerializer = fieldSerializer.ValueSerializer;
            var underlyingTargetSerializer = NumericConversionHelper.CreateNumericConversionSerializer(underlyingFieldType, underlyingTargetType, underlyingFieldSerializer);
            var targetSerializer = NullableSerializer.Create(underlyingTargetSerializer);
            return AstFilter.Field(field.Path, targetSerializer);
        }
    }
}
