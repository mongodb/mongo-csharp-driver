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

using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.ExtensionMethods;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Misc;

internal static class IBsonSerializerExtensions
{
    public static bool CanBeAssignedTo(this IBsonSerializer sourceSerializer, IBsonSerializer targetSerializer)
    {
        if (sourceSerializer.Equals(targetSerializer))
        {
            return true;
        }

        if (sourceSerializer.ValueType.IsNumeric() &&
            targetSerializer.ValueType.IsNumeric() &&
            sourceSerializer.HasNumericRepresentation() &&
            targetSerializer.HasNumericRepresentation())
        {
            return true;
        }

        if (targetSerializer.ValueType.IsAssignableFrom(sourceSerializer.ValueType))
        {
            return true;
        }

        return false;
    }

    public static IBsonSerializer GetItemSerializer(this IBsonSerializer serializer)
        => ArraySerializerHelper.GetItemSerializer(serializer);

    public static IBsonSerializer GetItemSerializer(this IBsonSerializer serializer, int index)
    {
        if (serializer is IFixedSizeArraySerializer fixedSizeArraySerializer)
        {
            return fixedSizeArraySerializer.GetItemSerializer(index);
        }
        else
        {
            return serializer.GetItemSerializer();
        }
    }

    public static IBsonSerializer GetItemSerializer(this IBsonSerializer serializer, Expression indexExpression, Expression containingExpression)
    {
        if (serializer is IFixedSizeArraySerializer fixedSizeArraySerializer)
        {
            var index = indexExpression.GetConstantValue<int>(containingExpression);
            return fixedSizeArraySerializer.GetItemSerializer(index);
        }
        else
        {
            return serializer.GetItemSerializer();
        }
    }

    public static bool HasNumericRepresentation(this IBsonSerializer serializer)
    {
        return
            serializer is IHasRepresentationSerializer hasRepresentationSerializer &&
            hasRepresentationSerializer.Representation.IsNumeric();
    }

    public static bool IsKeyValuePairSerializer(
        this IBsonSerializer serializer,
        out string keyElementName,
        out string valueElementName,
        out IBsonSerializer keySerializer,
        out IBsonSerializer valueSerializer)
    {
        if (serializer is IKeyValuePairSerializer keyValuePairSerializer)
        {
            keyElementName = "k";
            valueElementName = "v";
            keySerializer = keyValuePairSerializer.KeySerializer;
            valueSerializer = keyValuePairSerializer.ValueSerializer;
            return true;
        }

        // for backward compatibility not all KeyValuePair serializers implement IKeyValuePairSerializer
        if (serializer.ValueType is var valueType &&
            valueType.IsConstructedGenericType &&
            valueType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) &&
            serializer is IBsonDocumentSerializer documentSerializer &&
            documentSerializer.TryGetMemberSerializationInfo("Key", out var keySerializationInfo) &&
            documentSerializer.TryGetMemberSerializationInfo("Value", out var valueSerializationInfo))
        {
            keyElementName = keySerializationInfo.ElementName;
            valueElementName = valueSerializationInfo.ElementName;
            keySerializer = keySerializationInfo.Serializer;
            valueSerializer = valueSerializationInfo.Serializer;
            return true;
        }

        keyElementName = null;
        valueElementName = null;
        keySerializer = null;
        valueSerializer = null;
        return false;
    }
}
