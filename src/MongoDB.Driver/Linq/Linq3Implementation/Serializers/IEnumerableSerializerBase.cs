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

using System.Collections.Generic;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Linq.Linq3Implementation.Serializers
{
    internal abstract class IEnumerableSerializerBase<TEnumerable, TItem> : SerializerBase<TEnumerable>, IBsonArraySerializer
        where TEnumerable : IEnumerable<TItem>
    {
        // private fields
        private readonly IBsonSerializer<TItem> _itemSerializer;

        // constructors
        public IEnumerableSerializerBase(IBsonSerializer<TItem> itemSerializer)
        {
            _itemSerializer = Ensure.IsNotNull(itemSerializer, nameof(itemSerializer));
        }

        // public methods
        public override TEnumerable Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartArray();
            var items = new List<TItem>();
            while (reader.ReadBsonType() != 0)
            {
                var item = _itemSerializer.Deserialize(context);
                items.Add(item);
            }
            reader.ReadEndArray();
            return CreateDeserializedValue(items);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is IEnumerableSerializerBase<TEnumerable, TItem> other &&
                object.Equals(_itemSerializer, other._itemSerializer);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TEnumerable value)
        {
            var writer = context.Writer;
            writer.WriteStartArray();
            foreach (var item in value)
            {
                _itemSerializer.Serialize(context, item);
            }
            writer.WriteEndArray();
        }

        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = new BsonSerializationInfo(null, _itemSerializer, typeof(TItem));
            return true;
        }

        protected abstract TEnumerable CreateDeserializedValue(List<TItem> items);
    }
}
