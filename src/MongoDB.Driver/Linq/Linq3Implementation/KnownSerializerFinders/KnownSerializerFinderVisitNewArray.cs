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
using System.Linq;
using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.KnownSerializerFinders;

internal partial class KnownSerializerFinderVisitor
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
            if (node.Expressions.Any(IsNotKnown) && IsKnown(node, out var arraySerializer))
            {
                var itemSerializer = arraySerializer.GetItemSerializer();

                foreach (var valueExpression in node.Expressions)
                {
                    DeduceSerializer(valueExpression, itemSerializer);
                }
            }

            if (IsNotKnown(node))
            {
                var itemType = node.Type.GetElementType();
                IBsonSerializer itemSerializer = null;

                if (node.Expressions.Count == 0)
                {
                    itemSerializer = BsonSerializer.LookupSerializer(itemType); // TODO: don't use static registry
                }
                else if (node.Expressions.Any(e => IsKnown(e, out itemSerializer)))
                {
                    // itemSerializer has been assigned a value by IsKnown
                }

                if (itemSerializer != null)
                {
                    var arraySerializerType = typeof(ArraySerializer<>).MakeGenericType(itemType);
                    arraySerializer = (IBsonSerializer)Activator.CreateInstance(arraySerializerType, itemSerializer);
                    AddKnownSerializer(node, arraySerializer);
                }
            }
        }
    }
}
