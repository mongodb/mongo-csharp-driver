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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        IBsonSerializer containingSerializer;
        var member = node.Member;
        var declaringType = member.DeclaringType;
        var memberName = member.Name;

        base.VisitMember(node);

        if (IsNotKnown(node))
        {
            var containingExpression = node.Expression;
            if (IsKnown(containingExpression, out containingSerializer))
            {
                // TODO: are there are other cases that still need to be handled?

                var resultSerializer = node.Member switch
                {
                    _ when declaringType == typeof(BsonValue) => GetBsonValuePropertySerializer(),
                    _ when IsCollectionCountOrLengthProperty() => GetCollectionCountOrLengthPropertySerializer(),
                    _ when declaringType == typeof(DateTime) => GetDateTimePropertySerializer(),
                    _ when declaringType.IsConstructedGenericType && declaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>) => GetDictionaryPropertySerializer(),
                    _ when declaringType.IsConstructedGenericType && declaringType.GetGenericTypeDefinition() == typeof(IDictionary<,>) => GetIDictionaryPropertySerializer(),
                    _ when declaringType.IsNullable() => GetNullablePropertySerializer(),
                    _ when declaringType.IsTupleOrValueTuple() => GetTupleOrValueTuplePropertySerializer(),
                    _ => GetPropertySerializer()
                };

                AddNodeSerializer(node, resultSerializer);
            }
        }

        return node;

        IBsonSerializer GetBsonValuePropertySerializer()
        {
            return memberName switch
            {
                "AsBoolean" => BooleanSerializer.Instance,
                "AsBsonArray" => BsonArraySerializer.Instance,
                "AsBsonBinaryData" => BsonBinaryDataSerializer.Instance,
                "AsBsonDateTime" => BsonDateTimeSerializer.Instance,
                "AsBsonDocument" => BsonDocumentSerializer.Instance,
                "AsBsonJavaScript" => BsonJavaScriptSerializer.Instance,
                "AsBsonJavaScriptWithScope" => BsonJavaScriptWithScopeSerializer.Instance,
                "AsBsonMaxKey" => BsonMaxKeySerializer.Instance,
                "AsBsonMinKey" => BsonMinKeySerializer.Instance,
                "AsBsonNull" => BsonNullSerializer.Instance,
                "AsBsonRegularExpression" => BsonRegularExpressionSerializer.Instance,
                "AsBsonSymbol" => BsonSymbolSerializer.Instance,
                "AsBsonTimestamp" => BsonTimestampSerializer.Instance,
                "AsBsonUndefined" => BsonUndefinedSerializer.Instance,
                "AsBsonValue" => BsonValueSerializer.Instance,
                "AsByteArray" => ByteArraySerializer.Instance,
                "AsDecimal128" => Decimal128Serializer.Instance,
                "AsDecimal" => DecimalSerializer.Instance,
                "AsDouble" => DoubleSerializer.Instance,
                "AsGuid" => GuidSerializer.StandardInstance,
                "AsInt32" => Int32Serializer.Instance,
                "AsInt64" => Int64Serializer.Instance,
                "AsLocalTime" => DateTimeSerializer.LocalInstance,
                "AsNullableBoolean" => NullableSerializer.NullableBooleanInstance,
                "AsNullableDecimal128" => NullableSerializer.NullableDecimal128Instance,
                "AsNullableDecimal" => NullableSerializer.NullableDecimalInstance,
                "AsNullableDouble" => NullableSerializer.NullableDoubleInstance,
                "AsNullableGuid" => NullableSerializer.NullableStandardGuidInstance,
                "AsNullableInt32" => NullableSerializer.NullableInt32Instance,
                "AsNullableInt64" => NullableSerializer.NullableInt64Instance,
                "AsNullableLocalTime" => NullableSerializer.NullableLocalDateTimeInstance,
                "AsNullableObjectId" => NullableSerializer.NullableObjectIdInstance,
                "AsNullableUniversalTime" => NullableSerializer.NullableUtcDateTimeInstance,
                "AsObjectId" => ObjectIdSerializer.Instance,
                "AsRegex" => RegexSerializer.RegularExpressionInstance,
                "AsString" => StringSerializer.Instance,
                "AsUniversalTime" => DateTimeSerializer.UtcInstance,
                // TODO: return UnknowableSerializer???
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        IBsonSerializer GetCollectionCountOrLengthPropertySerializer()
        {
            return Int32Serializer.Instance;
        }

        IBsonSerializer GetDateTimePropertySerializer()
        {
            return memberName switch
            {
                "Date" => DateTimeSerializer.Instance,
                "Day" => Int32Serializer.Instance,
                "DayOfWeek" => new EnumSerializer<DayOfWeek>(BsonType.Int32),
                "DayOfYear" => Int32Serializer.Instance,
                "Hour" => Int32Serializer.Instance,
                "Millisecond" => Int32Serializer.Instance,
                "Minute" => Int32Serializer.Instance,
                "Month" => Int32Serializer.Instance,
                "Now" => DateTimeSerializer.Instance,
                "Second" => Int32Serializer.Instance,
                "Ticks" => Int64Serializer.Instance,
                "TimeOfDay" => new TimeSpanSerializer(BsonType.Int64, TimeSpanUnits.Milliseconds),
                "Today" => DateTimeSerializer.Instance,
                "UtcNow" => DateTimeSerializer.Instance,
                "Year" => Int32Serializer.Instance,
                // TODO: return UnknowableSerializer???
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        IBsonSerializer GetDictionaryPropertySerializer()
        {
            if (containingSerializer.GetValueSerializerIfWrapped() is not IBsonDictionarySerializer dictionarySerializer)
            {
                throw new ExpressionNotSupportedException(node, because: "dictionary serializer does not implement IBsonDictionarySerializer");
            }

            var keySerializer =  dictionarySerializer.KeySerializer;
            var valueSerializer = dictionarySerializer.ValueSerializer;

            return memberName switch
            {
                "Keys" => DictionaryKeyCollectionSerializer.Create(keySerializer, valueSerializer),
                "Values" => DictionaryValueCollectionSerializer.Create(keySerializer, valueSerializer),
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        IBsonSerializer GetIDictionaryPropertySerializer()
        {
            if (containingSerializer is not IBsonDictionarySerializer dictionarySerializer)
            {
                throw new ExpressionNotSupportedException(node, because: "IDictionarySerializer does not implement IBsonDictionarySerializer");
            }

            var keySerializer =  dictionarySerializer.KeySerializer;
            var valueSerializer = dictionarySerializer.ValueSerializer;

            return memberName switch
            {
                "Keys" => ICollectionSerializer.Create(keySerializer),
                "Values" => ICollectionSerializer.Create(valueSerializer),
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        IBsonSerializer GetNullablePropertySerializer()
        {
            return memberName switch
            {
                "HasValue" => BooleanSerializer.Instance,
                "Value" => (containingSerializer as INullableSerializer)?.ValueSerializer,
                // TODO: return UnknowableSerializer???
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        IBsonSerializer GetPropertySerializer()
        {
            if (containingSerializer is not IBsonDocumentSerializer documentSerializer)
            {
                // TODO: return UnknowableSerializer???
                throw new ExpressionNotSupportedException(node, because: $"serializer type {containingSerializer.GetType()} does not implement the {nameof(IBsonDocumentSerializer)} interface");
            }

            if (!documentSerializer.TryGetMemberSerializationInfo(memberName, out var memberSerializationInfo))
            {
                // TODO: return UnknowableSerializer???
                throw new ExpressionNotSupportedException(node, because: $"serializer type {containingSerializer.GetType()} does not support a member named: {memberName}");
            }

            return memberSerializationInfo.Serializer;
        }

        IBsonSerializer GetTupleOrValueTuplePropertySerializer()
        {
            if (containingSerializer is not IBsonTupleSerializer tupleSerializer)
            {
                throw new ExpressionNotSupportedException(node, because: $"serializer type {containingSerializer.GetType()} does not implement the {nameof(IBsonTupleSerializer)} interface");
            }

            return memberName switch
            {
                "Item1" => tupleSerializer.GetItemSerializer(1),
                "Item2" => tupleSerializer.GetItemSerializer(2),
                "Item3" => tupleSerializer.GetItemSerializer(3),
                "Item4" => tupleSerializer.GetItemSerializer(4),
                "Item5" => tupleSerializer.GetItemSerializer(5),
                "Item6" => tupleSerializer.GetItemSerializer(6),
                "Item7" => tupleSerializer.GetItemSerializer(7),
                "Rest" => tupleSerializer.GetItemSerializer(8),
                // TODO: return UnknowableSerializer???
                _ => throw new ExpressionNotSupportedException(node, because: $"Unexpected member name: {memberName}")
            };
        }

        bool IsCollectionCountOrLengthProperty()
        {
            return
                (declaringType.ImplementsInterface(typeof(IEnumerable)) || declaringType == typeof(BitArray)) &&
                node.Type == typeof(int) &&
                (member.Name == "Count" || member.Name == "Length");
        }
    }
}
