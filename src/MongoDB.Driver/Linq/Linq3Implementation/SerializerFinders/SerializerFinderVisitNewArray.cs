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

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        DeduceNewArraySerializers();
        base.VisitNewArray(node);
        DeduceNewArraySerializers();

        return node;

        void DeduceNewArraySerializers()
        {
            switch (node.NodeType)
            {
                case ExpressionType.NewArrayBounds:
                    DeduceNewArrayBoundsSerializers();
                    break;

                case ExpressionType.NewArrayInit:
                    DeduceNewArrayInitSerializers();
                    break;
            }
        }

        void DeduceNewArrayBoundsSerializers()
        {
            throw new NotImplementedException();
        }

        void DeduceNewArrayInitSerializers()
        {
            var itemExpressions = node.Expressions;
            IBsonSerializer itemSerializer;

            if (IsAnyNotKnown(itemExpressions) && IsKnown(node, out var arraySerializer))
            {
                if (arraySerializer is IPolymorphicArraySerializer polymorphicArraySerializer)
                {
                    for (var i = 0; i < itemExpressions.Count; i++)
                    {
                        var itemExpression = itemExpressions[i];
                        if (IsNotKnown(itemExpression))
                        {
                            itemSerializer = polymorphicArraySerializer.GetItemSerializer(i);
                            AddNodeSerializer(itemExpression, itemSerializer);
                        }
                    }
                }
                else
                {
                    itemSerializer = arraySerializer.GetItemSerializer();
                    foreach (var itemExpression in itemExpressions)
                    {
                        if (IsNotKnown(itemExpression))
                        {
                            AddNodeSerializer(itemExpression, itemSerializer);
                        }
                    }
                }
            }

            if (IsAnyNotKnown(itemExpressions) && IsAnyKnown(itemExpressions, out itemSerializer))
            {
                var firstItemType = itemExpressions.First().Type;
                if (itemExpressions.All(e => e.Type == firstItemType))
                {
                    foreach (var itemExpression in itemExpressions)
                    {
                        if (IsNotKnown(itemExpression))
                        {
                            AddNodeSerializer(itemExpression, itemSerializer);
                        }
                    }
                }
            }

            if (IsNotKnown(node))
            {
                if (AreAllKnown(itemExpressions, out var itemSerializers))
                {
                    if (AllItemSerializersAreEqual(itemSerializers, out itemSerializer))
                    {
                        arraySerializer = ArraySerializer.Create(itemSerializer);
                    }
                    else
                    {
                        var itemType = node.Type.GetElementType();
                        arraySerializer = PolymorphicArraySerializer.Create(itemType, itemSerializers);
                    }
                    AddNodeSerializer(node, arraySerializer);
                }
            }

            static bool AllItemSerializersAreEqual(IReadOnlyList<IBsonSerializer> itemSerializers, out IBsonSerializer itemSerializer)
            {
                switch (itemSerializers.Count)
                {
                    case 0:
                        itemSerializer = null;
                        return false;
                    case 1:
                        itemSerializer = itemSerializers[0];
                        return true;
                    default:
                        var firstItemSerializer = itemSerializers[0];
                        if (itemSerializers.Skip(1).All(s => s.Equals(firstItemSerializer)))
                        {
                            itemSerializer = firstItemSerializer;
                            return true;
                        }
                        else
                        {
                            itemSerializer = null;
                            return false;
                        }
                }
            }
        }
    }
}
