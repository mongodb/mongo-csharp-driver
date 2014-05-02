/* Copyright 2010-2014 MongoDB Inc.
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
using System.IO;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for two-dimensional arrays.
    /// </summary>
    /// <typeparam name="TItem">The type of the elements.</typeparam>
    public class TwoDimensionalArraySerializer<TItem> :
        SealedClassSerializerBase<TItem[,]>,
        IChildSerializerConfigurable
    {
        // private fields
        private readonly IBsonSerializer<TItem> _itemSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="TwoDimensionalArraySerializer{TItem}"/> class.
        /// </summary>
        public TwoDimensionalArraySerializer()
            : this(BsonSerializer.LookupSerializer<TItem>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoDimensionalArraySerializer{TItem}"/> class.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        public TwoDimensionalArraySerializer(IBsonSerializer<TItem> itemSerializer)
        {
            _itemSerializer = itemSerializer;
        }

        // public properties
        /// <summary>
        /// Gets the item serializer.
        /// </summary>
        /// <value>
        /// The item serializer.
        /// </value>
        public IBsonSerializer<TItem> ItemSerializer
        {
            get { return _itemSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override TItem[,] DeserializeValue(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            EnsureBsonTypeEquals(bsonReader, BsonType.Array);

            bsonReader.ReadStartArray();
            var outerList = new List<List<TItem>>();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                bsonReader.ReadStartArray();
                var innerList = new List<TItem>();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    innerList.Add(context.DeserializeWithChildContext(_itemSerializer));
                }
                bsonReader.ReadEndArray();
                outerList.Add(innerList);
            }
            bsonReader.ReadEndArray();

            var length1 = outerList.Count;
            var length2 = (length1 == 0) ? 0 : outerList[0].Count;
            var array = new TItem[length1, length2];

            for (int i = 0; i < length1; i++)
            {
                var innerList = outerList[i];
                if (innerList.Count != length2)
                {
                    var message = string.Format("Inner list {0} is of length {1} but should be of length {2}.", i, innerList.Count, length2);
                    throw new FileFormatException(message);
                }
                for (int j = 0; j < length2; j++)
                {
                    array[i, j] = innerList[j];
                }
            }

            return array;
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        protected override void SerializeValue(BsonSerializationContext context, TItem[,] value)
        {
            var bsonWriter = context.Writer;

            var length1 = value.GetLength(0);
            var length2 = value.GetLength(1);

            bsonWriter.WriteStartArray();
            for (int i = 0; i < length1; i++)
            {
                bsonWriter.WriteStartArray();
                for (int j = 0; j < length2; j++)
                {
                    context.SerializeWithChildContext(_itemSerializer, value[i, j]);
                }
                bsonWriter.WriteEndArray();
            }
            bsonWriter.WriteEndArray();
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified item serializer.
        /// </summary>
        /// <param name="itemSerializer">The item serializer.</param>
        /// <returns>The reconfigured serializer.</returns>
        public TwoDimensionalArraySerializer<TItem> WithItemSerializer(IBsonSerializer<TItem> itemSerializer)
        {
            if (itemSerializer == _itemSerializer)
            {
                return this;
            }
            else
            {
                return new TwoDimensionalArraySerializer<TItem>(itemSerializer);
            }
        }

        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer
        {
            get { return _itemSerializer; }
        }

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
        {
            return WithItemSerializer((IBsonSerializer<TItem>)childSerializer);
        }
    }
}
