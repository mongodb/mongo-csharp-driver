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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class KeyValuePairWrappedValueSerializer
{
    public static IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
    {
        var keyType = keySerializer.ValueType;
        var valueType = valueSerializer.ValueType;
        var serializerType  = typeof(KeyValuePairWrappedValueSerializer<,>).MakeGenericType(keyType, valueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, [keySerializer, valueSerializer]);
    }
}

internal class KeyValuePairWrappedValueSerializer<TKey, TValue> : SerializerBase<TValue>, IWrappedValueSerializer
{
    private readonly IBsonSerializer<KeyValuePair<TKey, TValue>> _keyValuePairSerializer;
    private readonly IBsonSerializer<TValue> _valueSerializer;

    public KeyValuePairWrappedValueSerializer(IBsonSerializer<TKey> keySerializer,  IBsonSerializer<TValue> valueSerializer)
    {
        _keyValuePairSerializer = (IBsonSerializer<KeyValuePair<TKey, TValue>>)KeyValuePairSerializer.Create(BsonType.Document,  keySerializer, valueSerializer);
        _valueSerializer = valueSerializer;
    }

    public string FieldName => "v";
    public IBsonSerializer ValueSerializer => _valueSerializer;

    public override TValue Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var keyValuePair = _keyValuePairSerializer.Deserialize(context, args);
        return keyValuePair.Value;
    }
}
