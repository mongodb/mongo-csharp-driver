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
using System.Linq;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for IOrderedEnumerable<typeparamref name="TItem"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of the items.</typeparam>
    public sealed class IOrderedEnumerableSerializer<TItem> : SerializerBase<IOrderedEnumerable<TItem>>, IBsonArraySerializer
    {
        // private fields
        private readonly IBsonSerializer<TItem> _itemSerializer;
        private readonly string _thenByExceptionMessage;

        // public constructors
        /// <summary>
        /// Initializes a new instance of IOrderedEnumerableSerializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <param name="thenByExceptionMessage">The message to use when throwing an exception because ThenBy is not supported.</param>
        public IOrderedEnumerableSerializer(IBsonSerializer<TItem> itemSerializer, string thenByExceptionMessage)
        {
            _itemSerializer = itemSerializer ?? throw new ArgumentNullException(nameof(itemSerializer));
            _thenByExceptionMessage = thenByExceptionMessage ?? throw new ArgumentNullException(nameof(thenByExceptionMessage));
        }

        // public methods
        /// <inheritdoc/>
        public override IOrderedEnumerable<TItem> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            reader.ReadStartArray();
            var list = new List<TItem>();
            while (reader.ReadBsonType() != 0)
            {
                var item = _itemSerializer.Deserialize(context);
                list.Add(item);
            }
            reader.ReadEndArray();
            return new OrderedEnumerableListWrapper<TItem>(list, _thenByExceptionMessage);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is IOrderedEnumerableSerializer<TItem> other &&
                object.Equals(_itemSerializer, other._itemSerializer) &&
                object.Equals(_thenByExceptionMessage, other._thenByExceptionMessage);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IOrderedEnumerable<TItem> value)
        {
            var writer = context.Writer;
            writer.WriteStartArray();
            foreach (var item in value)
            {
                _itemSerializer.Serialize(context, item);
            }
            writer.WriteEndArray();
        }

        /// <inheritdoc/>
        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            serializationInfo = new BsonSerializationInfo(null, _itemSerializer, typeof(TItem));
            return true;
        }
    }

    /// <summary>
    /// A factory class for instances of IOrderedEnumerableSerializer&lt;TItem>.
    /// </summary>
    public static class IOrderedEnumerableSerializer
    {
        /// <summary>
        /// Creates an instance IOrderedEnumerableSerializer&lt;TItem>.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <param name="thenByExceptionMessage">The message to use when throwing an exception because ThenBy is not supported.</param>
        /// <returns>An IOrderedEnumerableSerializer&lt;TItem>.</returns>
        public static IBsonSerializer Create(IBsonSerializer itemSerializer, string thenByExceptionMessage)
        {
            if (itemSerializer == null) { throw new ArgumentNullException(nameof(itemSerializer)); }
            var itemType = itemSerializer.ValueType;
            var serializerType = typeof(IOrderedEnumerableSerializer<>).MakeGenericType(itemType);
            return (IBsonSerializer)Activator.CreateInstance(serializerType, itemSerializer, thenByExceptionMessage);
        }
    }

    internal class OrderedEnumerableListWrapper<T> : IOrderedEnumerable<T>
    {
        private readonly List<T> _list;
        private readonly string _thenByExceptionMessage;

        public OrderedEnumerableListWrapper(List<T> list, string thenByExceptionMessage)
        {
            _list = list;
            _thenByExceptionMessage = thenByExceptionMessage;
        }

        public IOrderedEnumerable<T> CreateOrderedEnumerable<TKey>(Func<T, TKey> keySelector, IComparer<TKey> comparer, bool descending)
        {
            throw new InvalidOperationException(_thenByExceptionMessage);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
