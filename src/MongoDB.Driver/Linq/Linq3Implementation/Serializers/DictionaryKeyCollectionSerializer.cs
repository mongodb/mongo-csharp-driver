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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class DictionaryKeyCollectionSerializer
{
    public static IBsonSerializer Create(IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
    {
        var keyType = keySerializer.ValueType;
        var valueType = valueSerializer.ValueType;
        var serializerType = typeof(DictionaryKeyCollectionSerializer<,>).MakeGenericType(keyType, valueType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, [keySerializer]);
    }
}

internal class DictionaryKeyCollectionSerializer<TKey, TValue> : EnumerableSerializerBase<Dictionary<TKey, TValue>.KeyCollection>
{
    public DictionaryKeyCollectionSerializer(IBsonSerializer<TKey> keySerializer)
        : base(itemSerializer: keySerializer)
    {
    }

    protected override void AddItem(object accumulator, object item) => ((Dictionary<TKey, TValue>)accumulator).Add((TKey)item, default(TValue));

    protected override object CreateAccumulator() => new Dictionary<TKey, TValue>();

    protected override IEnumerable EnumerateItemsInSerializationOrder(Dictionary<TKey, TValue>.KeyCollection value) => value;

    protected override Dictionary<TKey, TValue>.KeyCollection FinalizeResult(object accumulator) => ((Dictionary<TKey, TValue>)accumulator).Keys;
}
