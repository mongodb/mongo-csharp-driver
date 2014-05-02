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

using System;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for nullable values.
    /// </summary>
    /// <typeparam name="T">The underlying type.</typeparam>
    public class NullableSerializer<T> :
        SerializerBase<Nullable<T>>,
        IChildSerializerConfigurable
            where T : struct
    {
        // private fields
        private IBsonSerializer<T> _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="NullableSerializer{T}"/> class.
        /// </summary>
        public NullableSerializer()
            : this(BsonSerializer.LookupSerializer<T>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullableSerializer{T}"/> class.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        public NullableSerializer(IBsonSerializer<T> serializer)
        {
            _serializer = serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override T? Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                return context.DeserializeWithChildContext(_serializer);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, T? value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                context.SerializeWithChildContext(_serializer, value.Value);
            }
        }

        /// <summary>
        /// Returns a serializer that has been reconfigured with the specified serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>
        /// The reconfigured serializer.
        /// </returns>
        public NullableSerializer<T> WithSerializer(IBsonSerializer<T> serializer)
        {
            if (serializer == _serializer)
            {
                return this;
            }
            else
            {
                return new NullableSerializer<T>(serializer);
            }
        }

        // explicit interface implementations
        IBsonSerializer IChildSerializerConfigurable.ChildSerializer
        {
            get { return _serializer; }
        }

        IBsonSerializer IChildSerializerConfigurable.WithChildSerializer(IBsonSerializer childSerializer)
        {
            return WithSerializer((IBsonSerializer<T>)childSerializer);
        }
    }
}
