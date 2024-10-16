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

using System.Collections;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Translators.ExpressionToAggregationExpressionTranslators;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class SerializationHelper
    {
        public static void EnsureRepresentationIsArray(Expression expression, IBsonSerializer serializer)
        {
            var representation = GetRepresentation(serializer);
            if (representation != BsonType.Array)
            {
                throw new ExpressionNotSupportedException(expression, because: "the expression is not represented as an array in the database");
            }
        }

        public static void EnsureRepresentationIsNumeric(Expression expression, Expression argumentExpression, AggregationExpression argumentTranslation)
        {
            EnsureRepresentationIsNumeric(expression, argumentExpression, argumentTranslation.Serializer);
        }

        public static void EnsureRepresentationIsNumeric(Expression expression, Expression argumentExpression, IBsonSerializer argumentSerializer)
        {
            var argumentRepresentation = GetRepresentation(argumentSerializer);
            if (!IsNumericRepresentation(argumentRepresentation))
            {
                throw new ExpressionNotSupportedException(expression, because: $"{argumentExpression} uses a non-numeric representation: {argumentRepresentation}");
            }
        }

        public static BsonType GetRepresentation(IBsonSerializer serializer)
        {
            if (serializer is IDiscriminatedInterfaceSerializer discriminatedInterfaceSerializer)
            {
                return GetRepresentation(discriminatedInterfaceSerializer.InterfaceSerializer);
            }

            if (serializer is IDowncastingSerializer downcastingSerializer)
            {
                return GetRepresentation(downcastingSerializer.DerivedSerializer);
            }

            if (serializer is IEnumUnderlyingTypeSerializer enumUnderlyingTypeSerializer)
            {
                return GetRepresentation(enumUnderlyingTypeSerializer.EnumSerializer);
            }

            if (serializer is IImpliedImplementationInterfaceSerializer impliedImplementationSerializer)
            {
                return GetRepresentation(impliedImplementationSerializer.ImplementationSerializer);
            }

            if (serializer is IHasRepresentationSerializer hasRepresentationSerializer)
            {
                return hasRepresentationSerializer.Representation;
            }

            if (serializer is IBsonDictionarySerializer dictionarySerializer)
            {
                return dictionarySerializer.DictionaryRepresentation switch
                {
                    DictionaryRepresentation.ArrayOfArrays => BsonType.Array,
                    DictionaryRepresentation.ArrayOfDocuments => BsonType.Array,
                    DictionaryRepresentation.Document => BsonType.Document,
                    _ => BsonType.Undefined
                };
            }

            if (serializer is IKeyValuePairSerializer keyValuePairSerializer)
            {
                return keyValuePairSerializer.Representation;
            }

            if (serializer is INullableSerializer nullableSerializer)
            {
                return GetRepresentation(nullableSerializer.ValueSerializer);
            }

            // for backward compatibility assume that any remaining implementers of IBsonDocumentSerializer are represented as documents
            if (serializer is IBsonDocumentSerializer)
            {
                return BsonType.Document;
            }

            // for backward compatibility assume that any remaining implementers of IBsonArraySerializer are represented as documents
            if (serializer is IBsonArraySerializer)
            {
                return BsonType.Array;
            }

            return BsonType.Undefined;
        }

        public static bool IsIntegerRepresentation(BsonType representation)
        {
            return representation switch
            {
                BsonType.Int32 or BsonType.Int64 => true,
                _ => false
            };
        }

        public static bool IsNumericRepresentation(BsonType representation)
        {
            return representation switch
            {
                BsonType.Decimal128 or BsonType.Double or BsonType.Int32 or BsonType.Int64 => true,
                _ => false
            };
        }

        public static bool IsRepresentedAsDocument(IBsonSerializer serializer)
        {
            return SerializationHelper.GetRepresentation(serializer) == BsonType.Document;
        }

        public static bool IsRepresentedAsInteger(IBsonSerializer serializer)
        {
            var representation = GetRepresentation(serializer);
            return IsIntegerRepresentation(representation);
        }

        public static bool IsRepresentedAsIntegerOrNullableInteger(AggregationExpression translation)
        {
            return IsRepresentedAsIntegerOrNullableInteger(translation.Serializer);
        }

        public static bool IsRepresentedAsIntegerOrNullableInteger(IBsonSerializer serializer)
        {
            if (serializer is INullableSerializer nullableSerializer)
            {
                return IsRepresentedAsInteger(nullableSerializer.ValueSerializer);
            }
            else
            {
                return IsRepresentedAsInteger(serializer);
            }
        }

        public static BsonValue SerializeValue(IBsonSerializer serializer, ConstantExpression constantExpression, Expression containingExpression)
        {
            var value = constantExpression.Value;
            if (value == null || serializer.ValueType.IsAssignableFrom(value.GetType()))
            {
                return SerializeValue(serializer, value);
            }

            if (value.GetType().ImplementsIEnumerable(out var itemType) &&
                serializer is IBsonArraySerializer arraySerializer &&
                arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo) &&
                itemSerializationInfo.Serializer is var itemSerializer &&
                itemSerializer.ValueType.IsAssignableFrom(itemType))
            {
                var ienumerableSerializer = IEnumerableSerializer.Create(itemSerializer);
                return SerializeValue(ienumerableSerializer, value);
            }

            throw new ExpressionNotSupportedException(constantExpression, containingExpression, because: "it was not possible to determine how to serialize the constant");
        }

        public static BsonValue SerializeValue(IBsonSerializer serializer, object value)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteName("_v");
                var context = BsonSerializationContext.CreateRoot(writer);
                serializer.Serialize(context, value);
                writer.WriteEndDocument();
            }
            return document["_v"];
        }

        public static BsonArray SerializeValues(IBsonSerializer itemSerializer, IEnumerable values)
        {
            var document = new BsonDocument();
            using (var writer = new BsonDocumentWriter(document))
            {
                writer.WriteStartDocument();
                writer.WriteName("_v");
                writer.WriteStartArray();
                var context = BsonSerializationContext.CreateRoot(writer);
                foreach(var value in values)
                {
                    itemSerializer.Serialize(context, value);
                }
                writer.WriteEndArray();
                writer.WriteEndDocument();
            }
            return document["_v"].AsBsonArray;
        }
    }
}
