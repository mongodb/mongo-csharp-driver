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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
{
    private void AddKnownSerializer(Expression node, IBsonSerializer serializer) => _knownSerializers.AddSerializer(node, serializer);

    private bool AllAreKnown(IEnumerable<Expression> nodes, out IReadOnlyList<IBsonSerializer> serializers)
    {
        var serializersList = new List<IBsonSerializer>();
        foreach (var node in nodes)
        {
            if (IsKnown(node, out var knownSerializer))
            {
                serializersList.Add(knownSerializer);
            }
            else
            {
                serializers = null;
                return false;
            }
        }

        serializers = serializersList;
        return true;
    }

    private bool AnyAreNotKnown(IEnumerable<Expression> nodes)
    {
        return nodes.Any(IsNotKnown);
    }

    private bool CanDeduceSerializer(Expression node1, Expression node2, out Expression unknownNode, out IBsonSerializer knownSerializer)
    {
        if (IsKnown(node1, out var node1Serializer) &&
            IsNotKnown(node2))
        {
            unknownNode = node2;
            knownSerializer = node1Serializer;
            return true;
        }

        if (IsNotKnown(node1) &&
            IsKnown(node2, out var node2Serializer))
        {
            unknownNode = node1;
            knownSerializer = node2Serializer;
            return true;
        }

        unknownNode = null;
        knownSerializer = null;
        return false;
    }

    IBsonSerializer CreateCollectionSerializerFromCollectionSerializer(Type type, IBsonSerializer collectionSerializer)
    {
        if (collectionSerializer.ValueType == type)
        {
            return collectionSerializer;
        }

        var itemSerializer = collectionSerializer.GetItemSerializer();
        return CreateCollectionSerializerFromItemSerializer(type, itemSerializer);
    }

    IBsonSerializer CreateCollectionSerializerFromItemSerializer(Type type, IBsonSerializer itemSerializer)
    {
        return type switch
        {
            _ when type.IsArray => ArraySerializer.Create(itemSerializer),
            _ when type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) => IEnumerableSerializer.Create(itemSerializer),
            _ when type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>) => IQueryableSerializer.Create(itemSerializer),
            _ => (BsonSerializer.LookupSerializer(type) as IChildSerializerConfigurable)?.WithChildSerializer(itemSerializer)
        };
    }

    private void DeduceBooleanSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddKnownSerializer(node, BooleanSerializer.Instance);
        }
    }

    private void DeduceCollectionAndItemSerializers(Expression collectionExpression, Expression itemExpression)
    {
        DeduceItemAndCollectionSerializers(itemExpression, collectionExpression);
    }

    private void DeduceCollectionAndCollectionSerializers(Expression collectionExpression1, Expression collectionExpression2)
    {
        IBsonSerializer collectionSerializer1;
        IBsonSerializer collectionSerializer2;

        if (IsNotKnown(collectionExpression1) && IsKnown(collectionExpression2, out collectionSerializer2))
        {
            collectionSerializer1 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression1.Type, collectionSerializer2);
            AddKnownSerializer(collectionExpression1, collectionSerializer1);
        }

        if (IsNotKnown(collectionExpression2) && IsKnown(collectionExpression1, out collectionSerializer1))
        {
            collectionSerializer2 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression2.Type, collectionSerializer1);
            AddKnownSerializer(collectionExpression2, collectionSerializer2);
        }

    }

    private void DeduceItemAndCollectionSerializers(Expression itemExpression, Expression collectionExpression)
    {
        if (IsNotKnown(itemExpression) && IsItemSerializerKnown(collectionExpression, out var itemSerializer))
        {
            AddKnownSerializer(itemExpression, itemSerializer);
        }

        if (IsNotKnown(collectionExpression) && IsKnown(itemExpression, out itemSerializer))
        {
            var collectionSerializer = CreateCollectionSerializerFromItemSerializer(collectionExpression.Type, itemSerializer);
            if (collectionSerializer != null)
            {
                AddKnownSerializer(collectionExpression, collectionSerializer);
            }
        }
    }

    private void DeduceSerializer(Expression node, IBsonSerializer serializer)
    {
        if (IsNotKnown(node) && serializer != null)
        {
            AddKnownSerializer(node, serializer);
        }
    }

    private void DeduceSerializers(Expression expression1, Expression expression2)
    {
        if (IsNotKnown(expression1) && IsKnown(expression2, out var expression2Serializer))
        {
            AddKnownSerializer(expression1, expression2Serializer);
        }

        if (IsNotKnown(expression2) && IsKnown(expression1, out var expression1Serializer))
        {
            AddKnownSerializer(expression2, expression1Serializer);
        }
    }

    private void DeduceStringSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddKnownSerializer(node, StringSerializer.Instance);
        }
    }

    private bool IsItemSerializerKnown(Expression node, out IBsonSerializer itemSerializer)
    {
        if (IsKnown(node, out var serializer) &&
            serializer is IBsonArraySerializer arraySerializer &&
            arraySerializer.TryGetItemSerializationInfo(out var itemSerializationInfo))
        {
            itemSerializer = itemSerializationInfo.Serializer;
            return true;
        }

        itemSerializer = null;
        return false;
    }

    private bool IsKnown(Expression node) => _knownSerializers.IsKnown(node);

    private bool IsKnown(Expression node, out IBsonSerializer knownSerializer) => _knownSerializers.IsKnown(node, out knownSerializer);

    private bool IsNotKnown(Expression node) => _knownSerializers.IsNotKnown(node);
}
