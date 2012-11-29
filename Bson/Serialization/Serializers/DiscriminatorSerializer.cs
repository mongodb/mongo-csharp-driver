/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a wrapper for serializers with discriminators.
    /// </summary>
    public class DiscriminatorSerializer : IBsonSerializer
    {
        // private fields
        private readonly Type _nominalType;
        private readonly IDiscriminatorConvention _discriminatorConvention;
        private readonly IBsonSerializer _serializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the KeyValuePairSerializer class.
        /// </summary>
        private DiscriminatorSerializer(
            Type nominalType,
            IBsonSerializer serializer)
        {
            _nominalType = nominalType;
            _discriminatorConvention = BsonSerializer.LookupDiscriminatorConvention(nominalType);
            _serializer = serializer;
        }

        // public static methods
        /// <summary>
        /// Creates a DiscriminatorSerializer for a Type if the type is discriminated.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <returns>A serializer for the Type.</returns>
        public static IBsonSerializer Create(Type type)
        {
            var serializer = BsonSerializer.LookupSerializer(type);
            // no discriminator required if the type cannot be inherrited
            if ((!type.IsClass && !type.IsInterface) || type.IsSealed)
            {
                return serializer;
            }
            return new DiscriminatorSerializer(type, serializer);
        }

        /// <summary>
        /// Unwraps the inner serializer if a serializer is a DiscriminatorSerializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The inner serializer.</returns>
        public static IBsonSerializer Unwrap(IBsonSerializer serializer)
        {
            var discriminatorSerializer = serializer as DiscriminatorSerializer;
            return discriminatorSerializer != null ? discriminatorSerializer._serializer : serializer;
        }

        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            VerifyType(nominalType);

            var actualType = _discriminatorConvention.GetActualType(bsonReader, nominalType);
            var serializer = actualType == _nominalType ?
                _serializer : BsonSerializer.LookupSerializer(actualType);
            return serializer.Deserialize(bsonReader, _nominalType, actualType, options);
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            return _serializer.Deserialize(bsonReader, nominalType, actualType, options);
        }

        /// <summary>
        /// Gets the default serialization options for this serializer.
        /// </summary>
        /// <returns>The default serialization options for this serializer.</returns>
        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            return _serializer.GetDefaultSerializationOptions();
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            VerifyNominalType(nominalType);

            var actualType = value != null ? value.GetType() : nominalType;
            var serializer = actualType == _nominalType ?
                _serializer : BsonSerializer.LookupSerializer(actualType);
            serializer.Serialize(bsonWriter, _nominalType, value, options);
        }

        // private methods
        private void VerifyType(Type type)
        {
            if (type != _nominalType)
            {
                var message = string.Format(
                    "DiscriminatorSerializer for type {0} cannot be used with type {1}.",
                    _nominalType,
                    type.FullName);
                throw new BsonSerializationException(message);
            }
        }

        private void VerifyNominalType(Type nominalType)
        {
            if (nominalType != _nominalType && !_nominalType.IsAssignableFrom(nominalType))
            {
                var message = string.Format(
                    "DiscriminatorSerializer for type {0} cannot be used with type {1}.",
                    _nominalType,
                    nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
    }
}
