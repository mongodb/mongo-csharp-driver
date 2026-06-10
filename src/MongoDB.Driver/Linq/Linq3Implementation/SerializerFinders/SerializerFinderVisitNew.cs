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
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using MongoDB.Driver.Linq.Linq3Implementation.Reflection;
using MongoDB.Driver.Linq.Linq3Implementation.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.SerializerFinders;

internal partial class SerializerFinderVisitor
{
    protected override Expression VisitNew(NewExpression node)
    {
        var constructor = node.Constructor;
        var arguments = node.Arguments;
        IBsonSerializer nodeSerializer;

        if (IsKnown(node, out nodeSerializer) &&
            arguments.Any(IsNotKnown))
        {
            if (!typeof(BsonValue).IsAssignableFrom(node.Type) &&
                nodeSerializer is IBsonDocumentSerializer)
            {
                var matchingMemberSerializationInfos = nodeSerializer.GetMatchingMemberSerializationInfosForConstructorParameters(node, node.Constructor);
                for (var i = 0; i < matchingMemberSerializationInfos.Count; i++)
                {
                    var argument = arguments[i];
                    var matchingMemberSerializationInfo = matchingMemberSerializationInfos[i];

                    if (IsNotKnown(argument))
                    {
                        // arg => arg: matchingMemberSerializer
                        AddNodeSerializer(argument, matchingMemberSerializationInfo.Serializer);
                    }
                }
            }
        }

        base.VisitNew(node);

        if (IsNotKnown(node))
        {
            nodeSerializer = CreateSerializer(constructor);
            if (nodeSerializer != null)
            {
                AddNodeSerializer(node, nodeSerializer);
            }
        }

        return node;

        IBsonSerializer CreateSerializer(ConstructorInfo constructor)
        {
            if (constructor == null)
            {
                return CreateNewExpressionSerializer(node, node, bindings: null);
            }
            else if (constructor.DeclaringType == typeof(BsonDocument))
            {
                return BsonDocumentSerializer.Instance;
            }
            else if (constructor.DeclaringType == typeof(BsonValue))
            {
                return BsonValueSerializer.Instance;
            }
            else if (constructor.DeclaringType == typeof(DateTime))
            {
                return DateTimeSerializer.Instance;
            }
            else if (DictionaryConstructor.IsWithIEnumerableKeyValuePairConstructor(constructor))
            {
                var collectionExpression = arguments[0];
                if (IsItemSerializerKnown(collectionExpression, out var itemSerializer) &&
                    itemSerializer.IsKeyValuePairSerializer(out _, out _, out var keySerializer, out var valueSerializer))
                {
                    return DictionarySerializer.Create(DictionaryRepresentation.Document, keySerializer, valueSerializer);
                }
            }
            else if (HashSetConstructor.IsWithCollectionConstructor(constructor))
            {
                var collectionExpression = arguments[0];
                if (IsItemSerializerKnown(collectionExpression, out var itemSerializer))
                {
                    return HashSetSerializer.Create(itemSerializer);
                }
            }
            else if (ListConstructor.IsWithCollectionConstructor(constructor))
            {
                var collectionExpression = arguments[0];
                if (IsItemSerializerKnown(collectionExpression, out var itemSerializer))
                {
                    return ListSerializer.Create(itemSerializer);
                }
            }
            else if (KeyValuePairConstructor.IsWithKeyAndValueConstructor(constructor))
            {
                var key = arguments[0];
                var value = arguments[1];
                if (IsKnown(key, out var keySerializer) &&
                    IsKnown(value, out var valueSerializer))
                {
                    return KeyValuePairSerializer.Create(BsonType.Document, keySerializer, valueSerializer);
                }
            }
            else if (TupleOrValueTupleConstructor.IsTupleOrValueTupleConstructor(constructor))
            {
                if (AreAllKnown(arguments, out var argumentSerializers))
                {
                    return TupleOrValueTupleSerializer.Create(constructor.DeclaringType, argumentSerializers);
                }
            }
            else
            {
                return CreateNewExpressionSerializer(node, node, bindings: null);
            }

            return null;
        }
    }
}
