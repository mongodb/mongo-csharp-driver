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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitIndex(IndexExpression node)
    {
        base.VisitIndex(node);

        var collectionExpression = node.Object;
        var indexer = node.Indexer;
        var arguments = node.Arguments;

        if (IsBsonValueIndexer())
        {
            DeduceSerializer(node, BsonValueSerializer.Instance);
        }
        else if (IsDictionaryIndexer())
        {
            if (IsKnown(collectionExpression, out var collectionSerializer) &&
                collectionSerializer is IBsonDictionarySerializer dictionarySerializer)
            {
                var valueSerializer = dictionarySerializer.ValueSerializer;
                DeduceSerializer(node, valueSerializer);
            }
        }
        // check array indexer AFTER dictionary indexer
        else if (IsCollectionIndexer())
        {
            if (IsKnown(collectionExpression, out var collectionSerializer) &&
                collectionSerializer is IBsonArraySerializer arraySerializer)
            {
                var itemSerializer = arraySerializer.GetItemSerializer();
                DeduceSerializer(node, itemSerializer);
            }
        }
        // handle generic cases?

        return node;

        bool IsCollectionIndexer()
        {
            return
                arguments.Count == 1 &&
                arguments[0] is var index &&
                index.Type == typeof(int);
        }

        bool IsBsonValueIndexer()
        {
            var declaringType = indexer.DeclaringType;
            return
                (declaringType == typeof(BsonValue) || declaringType.IsSubclassOf(typeof(BsonValue))) &&
                arguments.Count == 1 &&
                arguments[0] is var index &&
                (index.Type == typeof(int) || index.Type == typeof(string));
        }

        bool IsDictionaryIndexer()
        {
            return
                collectionExpression.Type.ImplementsDictionaryInterface(out var keyType, out var valueType) &&
                arguments.Count == 1 &&
                arguments[0] is var indexExpression &&
                indexExpression.Type == keyType &&
                indexer.PropertyType == valueType;
        }
    }
}
