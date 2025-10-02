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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers;

internal static class FixedSizeArraySerializer
{
    public static IBsonSerializer Create(Type itemType, IEnumerable<IBsonSerializer> itemSerializers)
    {
        var serializerType = typeof(FixedSizeArraySerializer<>).MakeGenericType(itemType);
        return (IBsonSerializer)Activator.CreateInstance(serializerType, itemSerializers);
    }
}

internal interface IFixedSizeArraySerializer
{
    IBsonSerializer GetItemSerializer(int index);
}

internal sealed class FixedSizeArraySerializer<TItem> : SerializerBase<TItem[]>, IFixedSizeArraySerializer
{
    private readonly IReadOnlyList<IBsonSerializer> _itemSerializers;

    public FixedSizeArraySerializer(IEnumerable<IBsonSerializer> itemSerializers)
    {
        var itemSerializersArray = itemSerializers.ToArray();
        foreach (var itemSerializer in itemSerializersArray)
        {
            if (!typeof(TItem).IsAssignableFrom(itemSerializer.ValueType))
            {
                throw new ArgumentException($"Serializer class {itemSerializer.ValueType} value type is not assignable to item type {typeof(TItem).Name}");
            }
        }

        _itemSerializers = itemSerializersArray;
    }

    public override TItem[] Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var reader = context.Reader;
        var array = new TItem[_itemSerializers.Count];

        reader.ReadStartArray();
        var i = 0;
        while (reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            if (i < array.Length)
            {
                array[i] = (TItem)_itemSerializers[i].Deserialize(context);
                i++;
            }
        }
        if (i != array.Length)
        {
            throw new BsonSerializationException($"Expected {array.Length} array items but found {i}.");
        }
        reader.ReadEndArray();

        return array;
    }

    IBsonSerializer IFixedSizeArraySerializer.GetItemSerializer(int index) => _itemSerializers[index];

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TItem[] value)
    {
        if (value.Length != _itemSerializers.Count)
        {
            throw new BsonSerializationException($"Expected array value to have {_itemSerializers.Count} items but found {value.Length}.");
        }

        var writer = context.Writer;
        writer.WriteStartArray();
        for (var i = 0; i < value.Length; i++)
        {
            _itemSerializers[i].Serialize(context, args, value[i]);
        }
        writer.WriteEndArray();
    }
}
