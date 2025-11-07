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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class DictionaryValueCollectionSerializer
{
    public static IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
    {
        var keyType = keySerializer.ValueType;
        var valueType = valueSerializer.ValueType;
        var serializerType  = typeof(DictionaryValueCollectionSerializer<,>).MakeGenericType(keyType, valueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, [keySerializer, valueSerializer]);
    }
}

internal class DictionaryValueCollectionSerializer<TKey, TValue> : SerializerBase<Dictionary<TKey, TValue>.ValueCollection>, IBsonArraySerializer
{
    private readonly IBsonSerializer<Dictionary<TKey, TValue>> _dictionarySerializer;
    private readonly IBsonSerializer<TValue> _wrappedValueSerializer;

    public DictionaryValueCollectionSerializer(IBsonSerializer<TKey> keySerializer,  IBsonSerializer<TValue> valueSerializer)
    {
        _dictionarySerializer = (IBsonSerializer<Dictionary<TKey, TValue>>)DictionarySerializer.Create(DictionaryRepresentation.ArrayOfDocuments,  keySerializer, valueSerializer);
        _wrappedValueSerializer = (IBsonSerializer<TValue>)KeyValuePairWrappedValueSerializer.Create(keySerializer, valueSerializer);
    }

    public override Dictionary<TKey, TValue>.ValueCollection Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var dictionary = _dictionarySerializer.Deserialize(context, args);
        return dictionary.Values;
    }

    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = new BsonSerializationInfo(null, _wrappedValueSerializer, typeof(TValue));
        return true;
    }
}
