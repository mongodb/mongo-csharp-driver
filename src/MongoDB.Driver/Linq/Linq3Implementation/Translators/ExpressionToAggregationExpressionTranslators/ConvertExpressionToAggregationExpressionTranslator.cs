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
                var sourceExpression = expression.Operand;
                var sourceType = sourceExpression.Type;
                var targetType = expression.Type;

                // handle double conversions like `(BsonValue)(object)x`
                if (targetType == typeof(BsonValue) &&
                    sourceExpression is UnaryExpression unarySourceExpression &&
                    unarySourceExpression.NodeType == ExpressionType.Convert &&
                    unarySourceExpression.Type == typeof(object))
                {
                    sourceExpression = unarySourceExpression.Operand;
                }

                var sourceTranslation = ExpressionToAggregationExpressionTranslator.Translate(context, sourceExpression);
                return Translate(expression, sourceType, targetType, sourceTranslation);
            }

            throw new ExpressionNotSupportedException(expression);
        }

        private static AggregationExpression Translate(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            if (targetType == sourceType)
            {
                return sourceTranslation;
            }

            // from Nullable<T> must be handled before to Nullable<T>
            if (IsConvertFromNullableType(sourceType))
            {
                return TranslateConvertFromNullableType(expression, sourceType, targetType, sourceTranslation);
            }

            if (IsConvertToNullableType(targetType))
            {
                return TranslateConvertToNullableType(expression, sourceType, targetType, sourceTranslation);
            }

            // from here on we know there are no longer any Nullable<T> types involved

            if (targetType == typeof(BsonValue))
            {
                return TranslateConvertToBsonValue(expression, sourceTranslation);
            }

            if (IsConvertEnumToUnderlyingType(sourceType, targetType))
            {
                return TranslateConvertEnumToUnderlyingType(expression, sourceType, targetType, sourceTranslation);
            }

            if (IsConvertUnderlyingTypeToEnum(sourceType, targetType))
            {
                return TranslateConvertUnderlyingTypeToEnum(expression, sourceType, targetType, sourceTranslation);
            }

            if (IsConvertEnumToEnum(sourceType, targetType))
            {
                return TranslateConvertEnumToEnum(expression, sourceType, targetType, sourceTranslation);
            }

            if (IsConvertToBaseType(sourceType, targetType))
            {
                return TranslateConvertToBaseType(expression, sourceType, targetType, sourceTranslation);
            }

            if (IsConvertToDerivedType(sourceType, targetType))
            {
                return TranslateConvertToDerivedType(expression, targetType, sourceTranslation);
            }

            var ast = sourceTranslation.Ast;
            IBsonSerializer serializer;
            if (targetType.IsInterface)
            {
                // when an expression is cast to an interface it's a no-op as far as we're concerned
                // and we can just use the serializer for the concrete type and members not defined in the interface will just be ignored
                serializer = sourceTranslation.Serializer;
            }
            else
            {
                AstExpression to;
                switch (targetType.FullName)
                {
                    case "MongoDB.Bson.ObjectId": to = "objectId"; serializer = ObjectIdSerializer.Instance; break;
                    case "System.Boolean": to = "bool"; serializer = BooleanSerializer.Instance; break;
                    case "System.DateTime": to = "date"; serializer = DateTimeSerializer.Instance; break;
                    case "System.Decimal": to = "decimal"; serializer = DecimalSerializer.Instance; break; // not the default representation
                    case "System.Double": to = "double"; serializer = DoubleSerializer.Instance; break;
                    case "System.Int32": to = "int"; serializer = Int32Serializer.Instance; break;
                    case "System.Int64": to = "long"; serializer = Int64Serializer.Instance; break;
                    case "System.String": to = "string"; serializer = StringSerializer.Instance; break;
                    default: throw new ExpressionNotSupportedException(expression, because: $"conversion to {targetType} is not supported");
                }

                ast = AstExpression.Convert(ast, to);
            }

            return new AggregationExpression(expression, ast, serializer);
        }

        private static bool IsConvertEnumToEnum(Type sourceType, Type targetType)
        {
            return sourceType.IsEnum && targetType.IsEnum;
        }

        private static bool IsConvertEnumToUnderlyingType(Type sourceType, Type targetType)
        {
            return
                sourceType.IsEnum(out var underlyingType) &&
                targetType == underlyingType;
        }

        private static bool IsConvertFromNullableType(Type sourceType)
        {
            return sourceType.IsNullable();
        }

        private static bool IsConvertToBaseType(Type sourceType, Type targetType)
        {
            return sourceType.IsSubclassOf(targetType);
        }

        private static bool IsConvertToDerivedType(Type sourceType, Type targetType)
        {
            return targetType.IsSubclassOf(sourceType);
        }

        private static bool IsConvertToNullableType(Type targetType)
        {
            return targetType.IsNullable();
        }

        private static bool IsConvertUnderlyingTypeToEnum(Type sourceType, Type targetType)
        {
            return
                targetType.IsEnum(out var underlyingType) &&
                sourceType == underlyingType;
        }

        private static AggregationExpression TranslateConvertToBaseType(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            var derivedTypeSerializer = sourceTranslation.Serializer;
            var downcastingSerializer = DowncastingSerializer.Create(targetType, sourceType, derivedTypeSerializer);

            return new AggregationExpression(expression, sourceTranslation.Ast, downcastingSerializer);
        }

        private static AggregationExpression TranslateConvertToDerivedType(UnaryExpression expression, Type targetType, AggregationExpression sourceTranslation)
        {
            var serializer = BsonSerializer.LookupSerializer(targetType);

            return new AggregationExpression(expression, sourceTranslation.Ast, serializer);
        }

        private static AggregationExpression TranslateConvertToBsonValue(UnaryExpression expression, AggregationExpression sourceTranslation)
        {
            return new AggregationExpression(expression, sourceTranslation.Ast, BsonValueSerializer.Instance);
        }

        private static AggregationExpression TranslateConvertEnumToEnum(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            if (!sourceType.IsEnum)
            {
                throw new ExpressionNotSupportedException(expression, because: "source type is not an enum");
            }
            if (!targetType.IsEnum)
            {
                throw new ExpressionNotSupportedException(expression, because: "target type is not an enum");
            }

            var sourceSerializer = sourceTranslation.Serializer;
            if (sourceSerializer is IHasRepresentationSerializer sourceHasRepresentationSerializer &&
                !SerializationHelper.IsNumericRepresentation(sourceHasRepresentationSerializer.Representation))
            {
                throw new ExpressionNotSupportedException(expression, because: "source enum is not represented as a number");
            }

            var targetSerializer = EnumSerializer.Create(targetType);
            return new AggregationExpression(expression, sourceTranslation.Ast, targetSerializer);
        }

        private static AggregationExpression TranslateConvertEnumToUnderlyingType(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            var enumSerializer = sourceTranslation.Serializer;
            var targetSerializer = EnumUnderlyingTypeSerializer.Create(enumSerializer);
            return new AggregationExpression(expression, sourceTranslation.Ast, targetSerializer);
        }

        private static AggregationExpression TranslateConvertFromNullableType(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            if (sourceType.IsNullable(out var sourceValueType))
            {
                var (sourceVarBinding, sourceAst) = AstExpression.UseVarIfNotSimple("source", sourceTranslation.Ast);
                var sourceNullableSerializer = (INullableSerializer)sourceTranslation.Serializer;
                var sourceValueSerializer = sourceNullableSerializer.ValueSerializer;
                var sourceValueAggregationExpression = new AggregationExpression(expression.Operand, sourceAst, sourceValueSerializer);
                var convertTranslation = Translate(expression, sourceValueType, targetType, sourceValueAggregationExpression);

                // note: we would have liked to throw a query execution error here if the value is null and the target type is not nullable but there is no way to do that in MQL
                // so we just return null instead and the user must check for null themselves if they want to define what happens when the value is null
                // but see SERVER-78092 and the proposed $error operator

                var ast = AstExpression.Let(
                    sourceVarBinding,
                    AstExpression.Cond(AstExpression.Eq(sourceAst, BsonNull.Value), BsonNull.Value, convertTranslation.Ast));

                return new AggregationExpression(expression, ast, convertTranslation.Serializer);
            }

            throw new ExpressionNotSupportedException(expression, because: "sourceType is not nullable");
        }

        private static AggregationExpression TranslateConvertToNullableType(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            if (sourceType.IsNullable())
            {
                // ConvertFromNullableType should have been called first
                throw new ExpressionNotSupportedException(expression, because: "sourceType is nullable");
            }

            if (targetType.IsNullable(out var targetValueType))
            {
                var convertTranslation = Translate(expression, sourceType, targetValueType, sourceTranslation);
                var nullableSerializer = NullableSerializer.Create(convertTranslation.Serializer);
                return new AggregationExpression(expression, convertTranslation.Ast, nullableSerializer);
            }

            throw new ExpressionNotSupportedException(expression, because: "targetType is not nullable");
        }

        private static AggregationExpression TranslateConvertUnderlyingTypeToEnum(UnaryExpression expression, Type sourceType, Type targetType, AggregationExpression sourceTranslation)
        {
            var valueSerializer = sourceTranslation.Serializer;

            IBsonSerializer targetSerializer;
            if (valueSerializer is IEnumUnderlyingTypeSerializer enumUnderlyingTypeSerializer)
            {
                targetSerializer = enumUnderlyingTypeSerializer.EnumSerializer;
            }
            else
            {
                targetSerializer = EnumSerializer.Create(targetType);
            }

            return new AggregationExpression(expression, sourceTranslation.Ast, targetSerializer);
        }
    }
}
