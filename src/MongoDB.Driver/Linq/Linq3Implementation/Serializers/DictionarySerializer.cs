﻿/* Copyright 2010-present MongoDB Inc.
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

internal static class DictionarySerializer
{
    public static IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
    {
        var serializerType = typeof(DictionarySerializer<,>).MakeGenericType(keySerializer.ValueType, valueSerializer.ValueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, keySerializer, valueSerializer);
    }
}

internal class DictionarySerializer<TKey, TValue> : DictionaryInterfaceImplementerSerializer<Dictionary<TKey, TValue>, TKey, TValue>
{
    public DictionarySerializer(IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
        : base(DictionaryRepresentation.Document, keySerializer, valueSerializer)
    {
    }

    protected override ICollection<KeyValuePair<TKey, TValue>> CreateAccumulator() => new Dictionary<TKey, TValue>();

    protected override Dictionary<TKey, TValue>FinalizeAccumulator(ICollection<KeyValuePair<TKey, TValue>> accumulator) => (Dictionary<TKey, TValue>)accumulator;
}
