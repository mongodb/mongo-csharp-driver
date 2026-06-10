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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Bson.Serialization;
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
        if (serializer is IPolymorphicArraySerializer polymorphicArraySerializer)
        {
            return polymorphicArraySerializer.GetItemSerializer(index);
        }
        else
        {
            return serializer.GetItemSerializer();
        }
    }

    public static IBsonSerializer GetItemSerializer(this IBsonSerializer serializer, Expression indexExpression, Expression containingExpression)
    {
        if (serializer is IPolymorphicArraySerializer polymorphicArraySerializer)
        {
            var index = indexExpression.GetConstantValue<int>(containingExpression);
            return polymorphicArraySerializer.GetItemSerializer(index);
        }
        else
        {
            return serializer.GetItemSerializer();
        }
    }

    public static IReadOnlyList<BsonSerializationInfo> GetMatchingMemberSerializationInfosForConstructorParameters(
        this IBsonSerializer serializer,
        Expression expression,
        ConstructorInfo constructorInfo)
    {
        if (serializer is not IBsonDocumentSerializer documentSerializer)
        {
            throw new ExpressionNotSupportedException(expression, because: $"serializer type {serializer.GetType().Name} does not implement IBsonDocumentSerializer");
        }

        var matchingMemberSerializationInfos = new List<BsonSerializationInfo>();
        foreach (var constructorParameter in constructorInfo.GetParameters())
        {
            var matchingMemberSerializationInfo = GetMatchingMemberSerializationInfo(expression, documentSerializer, constructorParameter.Name);
            matchingMemberSerializationInfos.Add(matchingMemberSerializationInfo);
        }

        return matchingMemberSerializationInfos;

        static BsonSerializationInfo GetMatchingMemberSerializationInfo(
            Expression expression,
            IBsonDocumentSerializer documentSerializer,
            string constructorParameterName)
        {
            var possibleMatchingMembers = documentSerializer.ValueType.GetMembers().Where(m => m.Name.Equals(constructorParameterName, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (possibleMatchingMembers.Length == 0)
            {
                throw new ExpressionNotSupportedException(expression, because: $"no matching member found for constructor parameter: {constructorParameterName}");
            }
            if (possibleMatchingMembers.Length > 1)
            {
                throw new ExpressionNotSupportedException(expression, because: $"multiple possible matching members found for constructor parameter: {constructorParameterName}");
            }
            var matchingMemberName = possibleMatchingMembers[0].Name;

            if (!documentSerializer.TryGetMemberSerializationInfo(matchingMemberName, out var matchingMemberSerializationInfo))
            {
                throw new ExpressionNotSupportedException(expression, because: $"serializer of type {documentSerializer.GetType().Name} did not provide serialization info for member {matchingMemberName}");
            }

            return matchingMemberSerializationInfo;
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
        // TODO: add properties to IKeyValuePairSerializer to let us extract the needed information
        // note: we can only verify the existence of "Key" and "Value" properties, but can't verify there are no others
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

    public static IBsonSerializer GetValueSerializerIfWrapped(this IBsonSerializer serializer)
    {
        return serializer is IWrappedValueSerializer wrappedValueSerializer ? wrappedValueSerializer.ValueSerializer.GetValueSerializerIfWrapped() :  serializer;
    }
}
