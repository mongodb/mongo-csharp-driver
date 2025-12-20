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
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal interface IReadOnlySerializerMap
{
    IBsonSerializer GetSerializer(Expression node);
}

internal class SerializerMap : IReadOnlySerializerMap
{
    private readonly Dictionary<Expression, IBsonSerializer> _map = new();

    public int Count => _map.Count;

    public void AddSerializer(Expression node, IBsonSerializer serializer)
    {
        if (serializer.ValueType != node.Type &&
            node.Type.IsNullable(out var nodeNonNullableType) &&
            serializer.ValueType.IsNullable(out var serializerNonNullableType) &&
            serializer is INullableSerializer nullableSerializer)
        {
            if (nodeNonNullableType.IsEnum(out var targetEnumUnderlyingType) && targetEnumUnderlyingType == serializerNonNullableType)
            {
                var enumType = nodeNonNullableType;
                var underlyingTypeSerializer = nullableSerializer.ValueSerializer;
                var enumSerializer = AsUnderlyingTypeEnumSerializer.Create(enumType, underlyingTypeSerializer);
                serializer = NullableSerializer.Create(enumSerializer);
            }
            else if (serializerNonNullableType.IsEnum(out var serializerUnderlyingType) && serializerUnderlyingType == nodeNonNullableType)
            {
                var enumSerializer = nullableSerializer.ValueSerializer;
                var underlyingTypeSerializer = AsEnumUnderlyingTypeSerializer.Create(enumSerializer);
                serializer = NullableSerializer.Create(underlyingTypeSerializer);
            }
        }

        if (serializer.ValueType != node.Type)
        {
            if (node.Type.IsAssignableFrom(serializer.ValueType))
            {
                serializer = DowncastingSerializer.Create(baseType: node.Type, derivedType: serializer.ValueType, derivedTypeSerializer: serializer);
            }
            else if (serializer.ValueType.IsAssignableFrom(node.Type))
            {
                serializer = UpcastingSerializer.Create(baseType: serializer.ValueType, derivedType: node.Type, baseTypeSerializer: serializer);
            }
            else
            {
                throw new ArgumentException($"Serializer value type {serializer.ValueType} does not match expression value type {node.Type}", nameof(serializer));
            }
        }

        if (_map.TryGetValue(node, out var existingSerializer))
        {
            throw new ExpressionNotSupportedException(
                node,
                because: $"there are duplicate known serializers for expression '{node}': {serializer.GetType()} and {existingSerializer.GetType()}");
        }

        _map.Add(node, serializer);
    }

    public IBsonSerializer GetSerializer(Expression node)
    {
        if (_map.TryGetValue(node, out var nodeSerializer))
        {
            return nodeSerializer;
        }

        throw new ExpressionNotSupportedException(node, because: "unable to determine which serializer to use");
    }

    public bool IsNotKnown(Expression node)
    {
        return !IsKnown(node);
    }

    public bool IsKnown(Expression node)
    {
        return _map.ContainsKey(node);
    }

    public bool IsKnown(Expression node, out IBsonSerializer serializer)
    {
        serializer = null;
        return node != null && _map.TryGetValue(node, out serializer);
    }
}
