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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq3.Serializers
{
    internal class IEnumerableSerializer<TItem> : SerializerBase<IEnumerable<TItem>>, IBsonArraySerializer
    {
        // private fields
        private readonly IBsonSerializer<TItem> _itemSerializer;

        // constructors
        public IEnumerableSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            _itemSerializer = Ensure.IsNotNull(itemSerializer, nameof(itemSerializer));
        }

        // public methods
        public override IEnumerable<TItem> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartArray();
            var value = new List<TItem>();
            while (reader.ReadBsonType() != 0)
            {
                var item = _itemSerializer.Deserialize(context);
                value.Add(item);
            }
            reader.ReadEndArray();
            return value;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IEnumerable<TItem> value)
        {
            var writer = context.Writer;
            writer.WriteStartArray();
            foreach (var item in value)
            {
                _itemSerializer.Serialize(context, value);
            }
            writer.WriteEndArray();
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = new BsonSerializationInfo(null, _itemSerializer, typeof(TItem));
            return true;
        }
    }

    internal static class IEnumerableSerializer
    {
        public static IBsonSerializer Create(IBsonSerializer itemSerializer)
        {
            var itemType = itemSerializer.ValueType;
            var factoryType = typeof(IEnumerableSerializerFactory<>).MakeGenericType(itemType);
            var factory = (IEnumerableSerializerFactory)Activator.CreateInstance(factoryType);
            return factory.Create(itemSerializer);
        }
    }

    internal abstract class IEnumerableSerializerFactory
    {
        public abstract IBsonSerializer Create(IBsonSerializer itemSerializer);
    }

    internal class IEnumerableSerializerFactory<TItem> : IEnumerableSerializerFactory
    {
        public override IBsonSerializer Create(IBsonSerializer itemSerializer)
        {
            return new IEnumerableSerializer<TItem>((IBsonSerializer<TItem>) itemSerializer);
        }
    }
}
