﻿/* Copyright 2010-present MongoDB Inc.
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
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc
{
    internal static class SerializationHelper
    {
        public static void EnsureRepresentationIsNumeric(Expression expression, IBsonSerializer serializer)
        {
            if (serializer is IRepresentationConfigurable representationConfigurableSerializer)
            {
                EnsureRepresentationIsNumeric(expression, serializer.ValueType, representationConfigurableSerializer.Representation);
            }

            static void EnsureRepresentationIsNumeric(Expression expression, Type valueType, BsonType representation)
            {
                if (!IsNumericRepresentation(representation))
                {
                    throw new ExpressionNotSupportedException(expression, because: $"serializer for type {valueType} uses a non-numeric representation: {representation}");
                }
            }

            static bool IsNumericRepresentation(BsonType representation)
            {
                return representation switch
                {
                    BsonType.Decimal128 or BsonType.Double or BsonType.Int32 or BsonType.Int64 => true,
                    _ => false
                };
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
