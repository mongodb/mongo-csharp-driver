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

namespace MongoDB.Driver.Linq3.Translators.ExpressionToFilterTranslators.ExpressionToFilterFieldTranslators
{
    public static class ExpressionToFilterFieldTranslator
    {
        // public static methods
        public static AstFilterField Translate(TranslationContext context, Expression expression)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess: return TranslateMemberExpression(context, (MemberExpression)expression);
                case ExpressionType.Call: return TranslateMethodCallExpression(context, (MethodCallExpression)expression);
                case ExpressionType.Parameter: return TranslateParameterExpression(context, (ParameterExpression)expression);
                case ExpressionType.Convert: return TranslateConvertExpression(context, (UnaryExpression)expression);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        public static AstFilterField TranslateEnumerable(TranslationContext context, Expression expression)
        {
            var resolvedFieldAst = Translate(context, expression);

            var serializer = resolvedFieldAst.Serializer;
            if (serializer is IWrappedEnumerableSerializer wrappedEnumerableSerializer)
            {
                var enumerableSerializer = IEnumerableSerializer.Create(wrappedEnumerableSerializer.EnumerableElementSerializer);
                resolvedFieldAst = resolvedFieldAst.CreateFilterSubField(wrappedEnumerableSerializer.EnumerableFieldName, enumerableSerializer);
            }

            return resolvedFieldAst;
        }

        // private static methods
        private static AstFilterField TranslateMemberExpression(TranslationContext context, MemberExpression memberExpression)
        {
            var containingFieldAst = Translate(context, memberExpression.Expression);
            var containingFieldSerializer = containingFieldAst.Serializer;
            var containingFieldSerializerType = containingFieldSerializer.GetType();

            if (containingFieldSerializer is IBsonDocumentSerializer documentSerializer &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var subFieldName = memberSerializationInfo.ElementName;
                var subFieldSerializer = memberSerializationInfo.Serializer;
                return containingFieldAst.CreateFilterSubField(subFieldName, subFieldSerializer);
            }

            if (memberExpression.Expression.Type.IsConstructedGenericType &&
                memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                memberExpression.Member.Name == "Value" &&
                containingFieldSerializerType.IsConstructedGenericType &&
                containingFieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
            {
                var valueSerializer = ((IChildSerializerConfigurable)containingFieldSerializer).ChildSerializer;
                return new AstFilterField(containingFieldAst.Path, valueSerializer);
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }

        private static AstFilterField TranslateMethodCallExpression(TranslationContext context, MethodCallExpression expression)
        {
            var method = expression.Method;
            var arguments = expression.Arguments;
            var parameters = method.GetParameters();

            if (method.IsStatic &&
                method.Name == "First" &&
                parameters.Length == 1)
            {
                var enumerableFieldAst = TranslateEnumerable(context, arguments[0]);
                if (enumerableFieldAst.Serializer is IBsonArraySerializer arraySerializer &&
                    arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
                {
                    var itemSerializer = itemSerializationInfo.Serializer;
                    if (method.ReturnType.IsAssignableFrom(itemSerializer.ValueType))
                    {
                        return enumerableFieldAst.CreateFilterSubField("0", itemSerializer);
                    }
                }
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateParameterExpression(TranslationContext context, ParameterExpression expression)
        {
            var symbolTable = context.SymbolTable;
            if (symbolTable.TryGetSymbol(expression, out Symbol symbol))
            {
                var fieldName = symbol == symbolTable.Current ? "$CURRENT" : symbol.Name;
                var fieldSerializer = symbol.Serializer;
                var fieldAst = new AstFilterField(fieldName, fieldSerializer);

                if (fieldSerializer is IWrappedValueSerializer wrappedValueSerializer)
                {
                    fieldAst = fieldAst.CreateFilterSubField("_v", wrappedValueSerializer.ValueSerializer);
                }

                return fieldAst; ;
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AstFilterField TranslateConvertExpression(TranslationContext context, UnaryExpression expression)
        {
            var fieldAst = Translate(context, expression.Operand);
            var fieldSerializer = fieldAst.Serializer;
            var fieldType = fieldSerializer.ValueType;
            var targetType = expression.Type;

            if (fieldType.IsEnum)
            {
                var enumType = fieldType;
                var enumUnderlyingType = enumType.GetEnumUnderlyingType();
                if (targetType == enumUnderlyingType)
                {
                    var enumAsUnderlyingTypeSerializer = EnumAsUnderlyingTypeSerializer.Create(fieldSerializer);
                    return new AstFilterField(fieldAst.Path, enumAsUnderlyingTypeSerializer);
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
                return new AstFilterField(fieldAst.Path, targetTypeSerializer);
            }

            if (targetType.IsConstructedGenericType &&
                targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var nullableValueType = targetType.GetGenericArguments()[0];
                if (nullableValueType == fieldType)
                {
                    var nullableSerializerType = typeof(NullableSerializer<>).MakeGenericType(nullableValueType);
                    var nullableSerializer = (IBsonSerializer)Activator.CreateInstance(nullableSerializerType, fieldSerializer);
                    return new AstFilterField(fieldAst.Path, nullableSerializer);
                }

                if (fieldType.IsConstructedGenericType &&
                    fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var fieldValueType = fieldType.GetGenericArguments()[0];
                    if (fieldValueType.IsEnum)
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
                                return new AstFilterField(fieldAst.Path, nullableSerializer);
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
