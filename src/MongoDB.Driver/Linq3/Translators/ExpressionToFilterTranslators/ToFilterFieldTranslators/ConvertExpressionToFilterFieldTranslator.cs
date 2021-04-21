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
using MongoDB.Driver.Linq3.Ast.Filters;
using MongoDB.Driver.Linq3.Misc;
using MongoDB.Driver.Linq3.Serializers;

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class ConvertExpressionToFilterFieldTranslator
    {
        public static AstFilterField Translate(TranslationContext context, UnaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.Convert)
            {
                var field = ExpressionToFilterFieldTranslator.Translate(context, expression.Operand);
                var fieldSerializer = field.Serializer;
                var fieldType = fieldSerializer.ValueType;
                var targetType = expression.Type;

                if (fieldType.IsEnum())
                {
                    var enumType = fieldType;
                    var enumUnderlyingType = enumType.GetEnumUnderlyingType();
                    if (targetType == enumUnderlyingType)
                    {
                        var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(fieldSerializer);
                        return AstFilter.Field(field.Path, enumAsUnderlyingTypeSerializer);
                    }
                }

                if (IsNumericType(targetType))
                {
                    var targetTypeSerializer = BsonSerializer.LookupSerializer(targetType); // TODO: use known serializer
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
                    return AstFilter.Field(field.Path, targetTypeSerializer);
                }

                if (targetType.IsConstructedGenericType &&
                    targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var nullableValueType = targetType.GetGenericArguments()[0];
                    if (nullableValueType == fieldType)
                    {
                        var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(nullableValueType);
                        var nullableSerializer = (IBsonSerializer)Activator.CreateInstance(nullableSerializerType, fieldSerializer);
                        return AstFilter.Field(field.Path, nullableSerializer);
                    }

                    if (fieldType.IsConstructedGenericType &&
                        fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        var fieldValueType = fieldType.GetGenericArguments()[0];
                        if (fieldValueType.IsEnum())
                        {
                            var enumUnderlyingType = fieldValueType.GetEnumUnderlyingType();
                            if (nullableValueType == enumUnderlyingType)
                            {
                                var fieldSerializerType = fieldSerializer.GetType();
                                if (fieldSerializerType.IsConstructedGenericType &&
                                    fieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
                                {
                                    var enumSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                                    var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(enumSerializer);
                                    var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(nullableValueType);
                                    var nullableSerializer = (IBsonSerializer)Activator.CreateInstance(nullableSerializerType, enumAsUnderlyingTypeSerializer);
                                    return AstFilter.Field(field.Path, nullableSerializer);
                                }
                            }
                        }
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
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
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }
    }
}
