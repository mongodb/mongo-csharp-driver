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
using IOrderedEnumerableSerializer=MongoDB.Driver.Linq.Linq3Implementation.Serializers.IOrderedEnumerableSerializer;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
{
    private void AddKnownSerializer(Expression node, IBsonSerializer serializer) => _knownSerializers.AddSerializer(node, serializer);

    private bool AllAreKnown(IEnumerable<Expression> nodes, out IReadOnlyList<IBsonSerializer> knownSerializers)
    {
        var knownSerializersList = new List<IBsonSerializer>();
        foreach (var node in nodes)
        {
            if (IsKnown(node, out var nodeSerializer))
            {
                knownSerializersList.Add(nodeSerializer);
            }
            else
            {
                knownSerializers = null;
                return false;
            }
        }

        knownSerializers = knownSerializersList;
        return true;
    }

    private bool AnyIsKnown(IEnumerable<Expression> nodes, out IBsonSerializer knownSerializer)
    {
        foreach (var node in nodes)
        {
            if (IsKnown(node, out var nodeSerializer))
            {
                knownSerializer = nodeSerializer;
                return true;
            }
        }

        knownSerializer = null;
        return false;
    }

    private bool AnyIsNotKnown(IEnumerable<Expression> nodes)
    {
        return nodes.Any(IsNotKnown);
    }

    private bool CanDeduceSerializer(Expression node1, Expression node2, out Expression unknownNode, out IBsonSerializer knownSerializer)
    {
        if (IsNotKnown(node1) && IsKnown(node2, out var node2Serializer))
        {
            unknownNode = node1;
            knownSerializer = node2Serializer;
            return true;
        }

        if (IsNotKnown(node2) && IsKnown(node1, out var node1Serializer))
        {
            unknownNode = node2;
            knownSerializer = node1Serializer;
            return true;
        }

        unknownNode = null;
        knownSerializer = null;
        return false;
    }

    IBsonSerializer CreateCollectionSerializerFromCollectionSerializer(Type collectionType, IBsonSerializer collectionSerializer)
    {
        if (collectionSerializer.ValueType == collectionType)
        {
            return collectionSerializer;
        }

        if (collectionSerializer is IUnknowableSerializer)
        {
            return UnknowableSerializer.Create(collectionType);
        }

        var itemSerializer = collectionSerializer.GetItemSerializer();
        return CreateCollectionSerializerFromItemSerializer(collectionType, itemSerializer);
    }

    IBsonSerializer CreateCollectionSerializerFromItemSerializer(Type collectionType, IBsonSerializer itemSerializer)
    {
        if (itemSerializer is IUnknowableSerializer)
        {
            return UnknowableSerializer.Create(collectionType);
        }

        return collectionType switch
        {
            _ when collectionType.IsArray => ArraySerializer.Create(itemSerializer),
            _ when collectionType.IsConstructedGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>) => IEnumerableSerializer.Create(itemSerializer),
            _ when collectionType.IsConstructedGenericType && collectionType.GetGenericTypeDefinition() == typeof(IOrderedEnumerable<>) => IOrderedEnumerableSerializer.Create(itemSerializer),
            _ when collectionType.IsConstructedGenericType && collectionType.GetGenericTypeDefinition() == typeof(IQueryable<>) => IQueryableSerializer.Create(itemSerializer),
            _ => (BsonSerializer.LookupSerializer(collectionType) as IChildSerializerConfigurable)?.WithChildSerializer(itemSerializer)
        };
    }

    private void DeduceBaseTypeAndDerivedTypeSerializers(Expression baseTypeExpression, Expression derivedTypeExpression)
    {
        if (IsNotKnown(baseTypeExpression) && IsKnown(derivedTypeExpression, out var knownDerivedTypeSerializer))
        {
            var baseTypeSerializer = knownDerivedTypeSerializer.GetBaseTypeSerializer(baseTypeExpression.Type);
            AddKnownSerializer(baseTypeExpression, baseTypeSerializer);
        }

        if (IsNotKnown(derivedTypeExpression) && IsKnown(baseTypeExpression, out var knownBaseTypeSerializer))
        {
            var derivedTypeSerializer = knownBaseTypeSerializer.GetDerivedTypeSerializer(baseTypeExpression.Type);
            AddKnownSerializer(derivedTypeExpression, derivedTypeSerializer);
        }
    }

    private void DeduceBooleanSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddKnownSerializer(node, BooleanSerializer.Instance);
        }
    }

    private void DeduceCollectionAndCollectionSerializers(Expression collectionExpression1, Expression collectionExpression2)
    {
        if (IsNotKnown(collectionExpression1) && IsKnown(collectionExpression2, out var knownCollectionSerializer2))
        {
            var collectionSerializer1 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression1.Type, knownCollectionSerializer2);
            AddKnownSerializer(collectionExpression1, collectionSerializer1);
        }

        if (IsNotKnown(collectionExpression2) && IsKnown(collectionExpression1, out var knownCollectionSerializer1))
        {
            var collectionSerializer2 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression2.Type, knownCollectionSerializer1);
            AddKnownSerializer(collectionExpression2, collectionSerializer2);
        }
    }

    private void DeduceCollectionAndItemSerializers(Expression collectionExpression, Expression itemExpression)
    {
        DeduceItemAndCollectionSerializers(itemExpression, collectionExpression);
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
        if (IsNotKnown(expression1) && IsKnown(expression2, out var expression2Serializer) && expression2Serializer.ValueType == expression1.Type)
        {
            AddKnownSerializer(expression1, expression2Serializer);
        }

        if (IsNotKnown(expression2) && IsKnown(expression1, out var expression1Serializer)&&  expression1Serializer.ValueType == expression2.Type)
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

    private void DeduceUnknowableSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            var unknowableSerializer = UnknowableSerializer.Create(node.Type);
            AddKnownSerializer(node, unknowableSerializer);
        }
    }

    private bool IsItemSerializerKnown(Expression node, out IBsonSerializer itemSerializer)
    {
        if (IsKnown(node, out var nodeSerializer) &&
            nodeSerializer is IBsonArraySerializer arraySerializer &&
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
