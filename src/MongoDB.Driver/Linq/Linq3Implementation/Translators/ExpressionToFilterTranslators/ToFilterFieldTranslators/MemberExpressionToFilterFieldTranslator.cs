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

namespace MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToFilterTranslators.ToFilterFieldTranslators
{
    internal static class MemberExpressionToFilterFieldTranslator
    {
        private static readonly IBsonSerializer<Nullable<bool>> __nullableBooleanSerializer = new NullableSerializer<bool>(BooleanSerializer.Instance);
        private static readonly IBsonSerializer<Nullable<DateTime>> __nullableDateTimeSerializer = new NullableSerializer<DateTime>(DateTimeSerializer.UtcInstance);
        private static readonly IBsonSerializer<Nullable<Decimal>> __nullableDecimalSerializer = new NullableSerializer<Decimal>(DecimalSerializer.Instance);
        private static readonly IBsonSerializer<Nullable<Decimal128>> __nullableDecimal128Serializer = new NullableSerializer<Decimal128>(Decimal128Serializer.Instance);
        private static readonly IBsonSerializer<Nullable<double>> __nullableDoubleSerializer = new NullableSerializer<Double>(DoubleSerializer.Instance);
        private static readonly IBsonSerializer<Nullable<Guid>> __nullableGuidSerializer = new NullableSerializer<Guid>(GuidSerializer.StandardInstance);
        private static readonly IBsonSerializer<Nullable<int>> __nullableInt32Serializer = new NullableSerializer<int>(Int32Serializer.Instance);
        private static readonly IBsonSerializer<Nullable<long>> __nullableInt64Serializer = new NullableSerializer<long>(Int64Serializer.Instance);
        private static readonly IBsonSerializer<Nullable<ObjectId>> __nullableObjectIdSerializer = new NullableSerializer<ObjectId>(ObjectIdSerializer.Instance);

        public static AstFilterField Translate(TranslationContext context, MemberExpression memberExpression)
        {
            var fieldExpression = ConvertHelper.RemoveConvertToInterface(memberExpression.Expression);
            var field = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var fieldSerializer = field.Serializer;
            var fieldSerializerType = fieldSerializer.GetType();

            if (fieldSerializer.GetType() == typeof(BsonValueSerializer))
            {
                switch (memberExpression.Member.Name)
                {
                    case "AsBoolean": return AstFilter.Field(field.Path, BooleanSerializer.Instance);
                    case "AsBsonArray": return AstFilter.Field(field.Path, BsonArraySerializer.Instance);
                    case "AsBsonBinaryData": return AstFilter.Field(field.Path, BsonBinaryDataSerializer.Instance);
                    case "AsBsonDateTime": return AstFilter.Field(field.Path, BsonDateTimeSerializer.Instance);
                    case "AsBsonDocument": return AstFilter.Field(field.Path, BsonDocumentSerializer.Instance);
                    case "AsBsonJavaScript": return AstFilter.Field(field.Path, BsonJavaScriptSerializer.Instance);
                    case "AsBsonJavaScriptWithScope": return AstFilter.Field(field.Path, BsonJavaScriptWithScopeSerializer.Instance);
                    case "AsBsonMaxKey": return AstFilter.Field(field.Path, BsonMaxKeySerializer.Instance);
                    case "AsBsonMinKey": return AstFilter.Field(field.Path, BsonMinKeySerializer.Instance);
                    case "AsBsonNull": return AstFilter.Field(field.Path, BsonNullSerializer.Instance);
                    case "AsBsonRegularExpression": return AstFilter.Field(field.Path, BsonRegularExpressionSerializer.Instance);
                    case "AsBsonSymbol": return AstFilter.Field(field.Path, BsonSymbolSerializer.Instance);
                    case "AsBsonTimestamp": return AstFilter.Field(field.Path, BsonTimestampSerializer.Instance);
                    case "AsBsonUndefined": return AstFilter.Field(field.Path, BsonUndefinedSerializer.Instance);
                    case "AsBsonValue": return AstFilter.Field(field.Path, fieldSerializer);
                    case "AsByteArray": return AstFilter.Field(field.Path, ByteArraySerializer.Instance);
                    case "AsDecimal": return AstFilter.Field(field.Path, DecimalSerializer.Instance);
                    case "AsDecimal128": return AstFilter.Field(field.Path, Decimal128Serializer.Instance);
                    case "AsDouble": return AstFilter.Field(field.Path, DoubleSerializer.Instance);
                    case "AsGuid": return AstFilter.Field(field.Path, GuidSerializer.StandardInstance);
                    case "AsInt32": return AstFilter.Field(field.Path, Int32Serializer.Instance);
                    case "AsInt64": return AstFilter.Field(field.Path, Int64Serializer.Instance);
                    case "AsLocalTime": return AstFilter.Field(field.Path, DateTimeSerializer.LocalInstance);
                    case "AsNullableBoolean": return AstFilter.Field(field.Path, __nullableBooleanSerializer);
                    case "AsNullableDecimal": return AstFilter.Field(field.Path, __nullableDecimalSerializer);
                    case "AsNullableDecimal128": return AstFilter.Field(field.Path, __nullableDecimal128Serializer);
                    case "AsNullableDouble": return AstFilter.Field(field.Path, __nullableDoubleSerializer);
                    case "AsNullableGuid": return AstFilter.Field(field.Path, __nullableGuidSerializer);
                    case "AsNullableInt32": return AstFilter.Field(field.Path, __nullableInt32Serializer);
                    case "AsNullableInt64": return AstFilter.Field(field.Path, __nullableInt64Serializer);
                    case "AsNullableObjectId": return AstFilter.Field(field.Path, __nullableObjectIdSerializer);
                    case "AsNullableUniversalTime": return AstFilter.Field(field.Path, __nullableDateTimeSerializer);
                    case "AsObjectId": return AstFilter.Field(field.Path, ObjectIdSerializer.Instance);
                    case "AsRegex": return AstFilter.Field(field.Path, RegexSerializer.RegularExpressionInstance);
                    case "AsString": return AstFilter.Field(field.Path, StringSerializer.Instance);
                    case "AsUniversalTime": return AstFilter.Field(field.Path, DateTimeSerializer.UtcInstance);
                }
            }

            if (DocumentSerializerHelper.AreMembersRepresentedAsFields(fieldSerializer , out var documentSerializer) &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var subFieldSerializer = memberSerializationInfo.Serializer;
                if (memberSerializationInfo.ElementPath == null)
                {
                    var subFieldName = memberSerializationInfo.ElementName;
                    return field.SubField(subFieldName, subFieldSerializer);
                }
                else
                {
                    var subField = field;
                    foreach (var subFieldName in memberSerializationInfo.ElementPath)
                    {
                        subField = subField.SubField(subFieldName, subFieldSerializer);
                    }
                    return subField;
                }
            }

            if (memberExpression.Expression.Type.IsConstructedGenericType &&
                memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                memberExpression.Member.Name == "Value" &&
                fieldSerializerType.IsConstructedGenericType &&
                fieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
            {
                var valueSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                return AstFilter.Field(field.Path, valueSerializer);
            }

            if (fieldExpression.Type.IsTupleOrValueTuple())
            {
                if (field.Serializer is IBsonTupleSerializer tupleSerializer)
                {
                    var itemName = memberExpression.Member.Name;
                    if (TupleSerializer.TryParseItemName(itemName, out var itemNumber))
                    {
                        var itemPath = $"{field.Path}.{itemNumber - 1}";
                        var itemSerializer = tupleSerializer.GetItemSerializer(itemNumber);
                        return AstFilter.Field(itemPath, itemSerializer);
                    }

                    throw new ExpressionNotSupportedException(memberExpression, because: $"Item name is not valid: {itemName}");
                }

                throw new ExpressionNotSupportedException(memberExpression, because: $"serializer {field.Serializer.GetType().FullName} does not implement IBsonTupleSerializer");
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }
    }
}
