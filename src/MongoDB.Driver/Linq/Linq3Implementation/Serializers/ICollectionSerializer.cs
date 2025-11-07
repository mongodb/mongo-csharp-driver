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

internal static class ICollectionSerializer
{
    public static IBsonSerializer Create(IBsonSerializer itemSerializer)
    {
        var itemType = itemSerializer.ValueType;
        var serializerType  = typeof(ICollectionSerializer<>).MakeGenericType(itemType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, [itemSerializer]);
    }
}

internal class ICollectionSerializer<TItem> : EnumerableSerializerBase<ICollection<TItem>>
{
    public ICollectionSerializer(IBsonSerializer<TItem> itemSerializer)
        : base(itemSerializer)
    {
    }

    protected override void AddItem(object accumulator, object item) => ((List<TItem>)accumulator).Add((TItem)item);

    protected override object CreateAccumulator() => new List<TItem>();

    protected override IEnumerable EnumerateItemsInSerializationOrder(ICollection<TItem> value) => value;

    protected override ICollection<TItem> FinalizeResult(object accumulator) => (ICollection<TItem>)accumulator;
}
