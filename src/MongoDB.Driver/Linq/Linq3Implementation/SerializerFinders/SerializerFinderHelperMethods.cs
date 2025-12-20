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

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    private void AddNodeSerializer(Expression node, IBsonSerializer serializer) => _nodeSerializers.AddSerializer(node, serializer);

    private bool AreAllKnown(IEnumerable<Expression> nodes, out IReadOnlyList<IBsonSerializer> nodeSerializers)
    {
        var nodeSerializersList = new List<IBsonSerializer>();
        foreach (var node in nodes)
        {
            if (IsKnown(node, out var nodeSerializer))
            {
                nodeSerializersList.Add(nodeSerializer);
            }
            else
            {
                nodeSerializers = null;
                return false;
            }
        }

        nodeSerializers = nodeSerializersList;
        return true;
    }

    private bool IsAnyKnown(IEnumerable<Expression> nodes, out IBsonSerializer nodeSerializer)
    {
        foreach (var node in nodes)
        {
            if (IsKnown(node, out var outSerializer))
            {
                nodeSerializer = outSerializer;
                return true;
            }
        }

        nodeSerializer = null;
        return false;
    }

    private bool IsAnyNotKnown(IEnumerable<Expression> nodes)
    {
        return nodes.Any(IsNotKnown);
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
        IBsonSerializer baseTypeSerializer;
        IBsonSerializer derivedTypeSerializer;

        if (IsNotKnown(baseTypeExpression) && IsKnown(derivedTypeExpression, out derivedTypeSerializer))
        {
            baseTypeSerializer = derivedTypeSerializer.GetBaseTypeSerializer(baseTypeExpression.Type);
            AddNodeSerializer(baseTypeExpression, baseTypeSerializer);
        }

        if (IsNotKnown(derivedTypeExpression) && IsKnown(baseTypeExpression, out baseTypeSerializer))
        {
            derivedTypeSerializer = baseTypeSerializer.GetDerivedTypeSerializer(baseTypeExpression.Type);
            AddNodeSerializer(derivedTypeExpression, derivedTypeSerializer);
        }
    }

    private void DeduceBooleanSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddNodeSerializer(node, BooleanSerializer.Instance);
        }
    }

    private void DeduceCharSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddNodeSerializer(node, CharSerializer.Instance);
        }
    }

    private void DeduceCollectionAndCollectionSerializers(Expression collectionExpression1, Expression collectionExpression2)
    {
        IBsonSerializer collectionSerializer1;
        IBsonSerializer collectionSerializer2;

        if (IsNotKnown(collectionExpression1) && IsKnown(collectionExpression2, out collectionSerializer2))
        {
            collectionSerializer1 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression1.Type, collectionSerializer2);
            AddNodeSerializer(collectionExpression1, collectionSerializer1);
        }

        if (IsNotKnown(collectionExpression2) && IsKnown(collectionExpression1, out collectionSerializer1))
        {
             collectionSerializer2 = CreateCollectionSerializerFromCollectionSerializer(collectionExpression2.Type, collectionSerializer1);
            AddNodeSerializer(collectionExpression2, collectionSerializer2);
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
            AddNodeSerializer(itemExpression, itemSerializer);
        }

        if (IsNotKnown(collectionExpression) && IsKnown(itemExpression, out itemSerializer))
        {
            var collectionSerializer = CreateCollectionSerializerFromItemSerializer(collectionExpression.Type, itemSerializer);
            if (collectionSerializer != null)
            {
                AddNodeSerializer(collectionExpression, collectionSerializer);
            }
        }
    }

    private void DeduceSerializer(Expression node, IBsonSerializer serializer)
    {
        if (IsNotKnown(node) && serializer != null)
        {
            AddNodeSerializer(node, serializer);
        }
    }

    private void DeduceSerializers(Expression expression1, Expression expression2)
    {
        if (IsNotKnown(expression1) && IsKnown(expression2, out var expression2Serializer) && expression2Serializer.ValueType == expression1.Type)
        {
            AddNodeSerializer(expression1, expression2Serializer);
        }

        if (IsNotKnown(expression2) && IsKnown(expression1, out var expression1Serializer)&&  expression1Serializer.ValueType == expression2.Type)
        {
            AddNodeSerializer(expression2, expression1Serializer);
        }
    }

    private void DeduceStringSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            AddNodeSerializer(node, StringSerializer.Instance);
        }
    }

    private void DeduceUnknowableSerializer(Expression node)
    {
        if (IsNotKnown(node))
        {
            var unknowableSerializer = UnknowableSerializer.Create(node.Type);
            AddNodeSerializer(node, unknowableSerializer);
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

    private bool IsKnown(Expression node) => _nodeSerializers.IsKnown(node);

    private bool IsKnown(Expression node, out IBsonSerializer nodeSerializer) => _nodeSerializers.IsKnown(node, out nodeSerializer);

    private bool IsNotKnown(Expression node) => _nodeSerializers.IsNotKnown(node);
}
