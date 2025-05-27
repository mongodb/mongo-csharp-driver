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
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class IBsonSerializerExtensions
{
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
}
