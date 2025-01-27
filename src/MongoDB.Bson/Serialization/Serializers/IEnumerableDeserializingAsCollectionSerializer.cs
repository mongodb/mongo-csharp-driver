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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for IEnumerable and any other derived interface implemented by TCollection.
    /// </summary>
    /// <typeparam name="TIEnumerable">The type of an IEnumerable interface.</typeparam>
    /// <typeparam name="TItem">The type of the items.</typeparam>
    /// <typeparam name="TCollection">The type of the collection used when deserializing.</typeparam>
    public sealed class IEnumerableDeserializingAsCollectionSerializer<TIEnumerable, TItem, TCollection> :
        SerializerBase<TIEnumerable>,
        IBsonArraySerializer,
        IChildSerializerConfigurable
        where TIEnumerable : class, IEnumerable<TItem> // TIEnumerable must be an interface
        where TCollection : class, ICollection<TItem>, new()
    {
        #region static
        private static void EnsureTIEnumerableIsAnInterface()
        {
            if (!typeof(TIEnumerable).IsInterface)
            {
                // this constraint cannot be specified at compile time
                throw new ArgumentException($"The {nameof(TIEnumerable)} type argument is not an interface: {typeof(TIEnumerable)}.", nameof(TIEnumerable));
            }
        }
        #endregion

        // private fields
        private readonly Lazy<IBsonSerializer<TItem>> _lazyItemSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the IEnumerableDeserializingAsCollectionSerializer class.
        /// </summary>
        public IEnumerableDeserializingAsCollectionSerializer()
            : this(BsonSerializer.SerializerRegistry) //TODO We can keep this as is
        {
        }

        /// <summary>
        /// Initializes a new instance of the IEnumerableDeserializingAsCollectionSerializer class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public IEnumerableDeserializingAsCollectionSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            EnsureTIEnumerableIsAnInterface();
            if (itemSerializer == null)
            {
                throw new ArgumentNullException(nameof(itemSerializer));
            }

            _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(() => itemSerializer);
        }

        /// <summary>
        /// Initializes a new instance of the IEnumerableDeserializingAsCollectionSerializer class.
        /// </summary>
        /// <param name="serializerRegistry">The serializer registry.</param>
        public IEnumerableDeserializingAsCollectionSerializer(IBsonSerializerRegistry serializerRegistry)
        {
            EnsureTIEnumerableIsAnInterface();
            if (serializerRegistry == null)
            {
                throw new ArgumentNullException(nameof(serializerRegistry));
            }

            _lazyItemSerializer = new Lazy<IBsonSerializer<TItem>>(serializerRegistry.GetSerializer<TItem>);
        }

        // public properties
        /// <summary>
        /// Gets the item serializer.
        /// </summary>
        /// <value>
        /// The item serializer.
        /// </value>
        public IBsonSerializer<TItem> ItemSerializer => _lazyItemSerializer.Value;

        // public methods
        /// <inheritdoc/>
        public override TIEnumerable Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;

            if (reader.GetCurrentBsonType() == BsonType.Null)
            {
                reader.ReadNull();
                return null;
            }
            else
            {
                reader.ReadStartArray();
                var collection = new TCollection();
                var itemSerializer = _lazyItemSerializer.Value;
                while (reader.ReadBsonType() != 0)
                {
                    var item = itemSerializer.Deserialize(context);
                    collection.Add(item);
                }
                reader.ReadEndArray();
                return (TIEnumerable)(object)collection;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null)) { return false; }
            if (object.ReferenceEquals(this, obj)) { return true; }
            return
                base.Equals(obj) &&
                obj is IEnumerableDeserializingAsCollectionSerializer<TIEnumerable, TItem, TCollection> other &&
                object.Equals(_lazyItemSerializer.Value, other._lazyItemSerializer.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => 0;

        /// <inheritdoc/>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TIEnumerable value)
        {
            var writer = context.Writer;

            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteStartArray();
                var itemSerializer = _lazyItemSerializer.Value;
                foreach (var item in value)
                {
                    itemSerializer.Serialize(context, item);
                }
                writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Tries to get the serialization info for the individual items of the array.
        /// </summary>
        /// <param name="serializationInfo">The serialization information.</param>
        /// <returns>
        /// The serialization info for the items.
        /// </returns>
        public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
        {
            var itemSerializer = _lazyItemSerializer.Value;
            serializationInfo = new BsonSerializationInfo(null, itemSerializer, itemSerializer.ValueType);
            return true;
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public IEnumerableDeserializingAsCollectionSerializer<TIEnumerable, TItem, TCollection> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            return new IEnumerableDeserializingAsCollectionSerializer<TIEnumerable, TItem, TCollection>(itemSerializer);
        }

        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer => ItemSerializer; 

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
            => WithItemSerializer((IBsonSerializer<TItem>)childSerializer);
    }
}
