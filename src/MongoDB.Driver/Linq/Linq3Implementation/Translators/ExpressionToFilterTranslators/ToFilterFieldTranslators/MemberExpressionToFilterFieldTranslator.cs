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

        public static TranslatedFilterField Translate(TranslationContext context, MemberExpression memberExpression)
        {
            var fieldExpression = ConvertHelper.RemoveConvertToInterface(memberExpression.Expression);
            var fieldTranslation = ExpressionToFilterFieldTranslator.Translate(context, fieldExpression);
            var fieldSerializer = fieldTranslation.Serializer;
            var fieldSerializerType = fieldSerializer.GetType();

            if (fieldSerializer.GetType() == typeof(BsonValueSerializer))
            {
                var field = fieldTranslation.AstField;
                switch (memberExpression.Member.Name)
                {
                    case "AsBoolean": return new TranslatedFilterField(field, BooleanSerializer.Instance);
                    case "AsBsonArray": return new TranslatedFilterField(field, BsonArraySerializer.Instance);
                    case "AsBsonBinaryData": return new TranslatedFilterField(field, BsonBinaryDataSerializer.Instance);
                    case "AsBsonDateTime": return new TranslatedFilterField(field, BsonDateTimeSerializer.Instance);
                    case "AsBsonDocument": return new TranslatedFilterField(field, BsonDocumentSerializer.Instance);
                    case "AsBsonJavaScript": return new TranslatedFilterField(field, BsonJavaScriptSerializer.Instance);
                    case "AsBsonJavaScriptWithScope": return new TranslatedFilterField(field, BsonJavaScriptWithScopeSerializer.Instance);
                    case "AsBsonMaxKey": return new TranslatedFilterField(field, BsonMaxKeySerializer.Instance);
                    case "AsBsonMinKey": return new TranslatedFilterField(field, BsonMinKeySerializer.Instance);
                    case "AsBsonNull": return new TranslatedFilterField(field, BsonNullSerializer.Instance);
                    case "AsBsonRegularExpression": return new TranslatedFilterField(field, BsonRegularExpressionSerializer.Instance);
                    case "AsBsonSymbol": return new TranslatedFilterField(field, BsonSymbolSerializer.Instance);
                    case "AsBsonTimestamp": return new TranslatedFilterField(field, BsonTimestampSerializer.Instance);
                    case "AsBsonUndefined": return new TranslatedFilterField(field, BsonUndefinedSerializer.Instance);
                    case "AsBsonValue": return new TranslatedFilterField(field, fieldSerializer);
                    case "AsByteArray": return new TranslatedFilterField(field, ByteArraySerializer.Instance);
                    case "AsDecimal": return new TranslatedFilterField(field, DecimalSerializer.Instance);
                    case "AsDecimal128": return new TranslatedFilterField(field, Decimal128Serializer.Instance);
                    case "AsDouble": return new TranslatedFilterField(field, DoubleSerializer.Instance);
                    case "AsGuid": return new TranslatedFilterField(field, GuidSerializer.StandardInstance);
                    case "AsInt32": return new TranslatedFilterField(field, Int32Serializer.Instance);
                    case "AsInt64": return new TranslatedFilterField(field, Int64Serializer.Instance);
                    case "AsLocalTime": return new TranslatedFilterField(field, DateTimeSerializer.LocalInstance);
                    case "AsNullableBoolean": return new TranslatedFilterField(field, __nullableBooleanSerializer);
                    case "AsNullableDecimal": return new TranslatedFilterField(field, __nullableDecimalSerializer);
                    case "AsNullableDecimal128": return new TranslatedFilterField(field, __nullableDecimal128Serializer);
                    case "AsNullableDouble": return new TranslatedFilterField(field, __nullableDoubleSerializer);
                    case "AsNullableGuid": return new TranslatedFilterField(field, __nullableGuidSerializer);
                    case "AsNullableInt32": return new TranslatedFilterField(field, __nullableInt32Serializer);
                    case "AsNullableInt64": return new TranslatedFilterField(field, __nullableInt64Serializer);
                    case "AsNullableObjectId": return new TranslatedFilterField(field, __nullableObjectIdSerializer);
                    case "AsNullableUniversalTime": return new TranslatedFilterField(field, __nullableDateTimeSerializer);
                    case "AsObjectId": return new TranslatedFilterField(field, ObjectIdSerializer.Instance);
                    case "AsRegex": return new TranslatedFilterField(field, RegexSerializer.RegularExpressionInstance);
                    case "AsString": return new TranslatedFilterField(field, StringSerializer.Instance);
                    case "AsUniversalTime": return new TranslatedFilterField(field, DateTimeSerializer.UtcInstance);
                }
            }

            if (DocumentSerializerHelper.AreMembersRepresentedAsFields(fieldSerializer , out var documentSerializer) &&
                documentSerializer.TryGetMemberSerializationInfo(memberExpression.Member.Name, out BsonSerializationInfo memberSerializationInfo))
            {
                var subFieldSerializer = memberSerializationInfo.Serializer;
                if (memberSerializationInfo.ElementPath == null)
                {
                    var subFieldName = memberSerializationInfo.ElementName;
                    return fieldTranslation.SubField(subFieldName, subFieldSerializer);
                }
                else
                {
                    var subField = fieldTranslation.AstField;
                    foreach (var subFieldName in memberSerializationInfo.ElementPath)
                    {
                        subField = subField.SubField(subFieldName);
                    }
                    return new TranslatedFilterField(subField, subFieldSerializer);
                }
            }

            if (memberExpression.Expression.Type.IsConstructedGenericType &&
                memberExpression.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                memberExpression.Member.Name == "Value" &&
                fieldSerializerType.IsConstructedGenericType &&
                fieldSerializerType.GetGenericTypeDefinition() == typeof(NullableSerializer<>))
            {
                var valueSerializer = ((IChildSerializerConfigurable)fieldSerializer).ChildSerializer;
                return new TranslatedFilterField(fieldTranslation.AstField, valueSerializer);
            }

            if (fieldExpression.Type.IsTupleOrValueTuple())
            {
                if (fieldTranslation.Serializer is IBsonTupleSerializer tupleSerializer)
                {
                    var itemName = memberExpression.Member.Name;
                    if (TupleSerializer.TryParseItemName(itemName, out var itemNumber))
                    {
                        var itemFieldName = (itemNumber - 1).ToString();
                        var itemSerializer = tupleSerializer.GetItemSerializer(itemNumber);
                        return fieldTranslation.SubField(itemFieldName, itemSerializer);
                    }

                    throw new ExpressionNotSupportedException(memberExpression, because: $"Item name is not valid: {itemName}");
                }

                throw new ExpressionNotSupportedException(memberExpression, because: $"serializer {fieldTranslation.Serializer.GetType().FullName} does not implement IBsonTupleSerializer");
            }

            throw new ExpressionNotSupportedException(memberExpression);
        }
    }
}
