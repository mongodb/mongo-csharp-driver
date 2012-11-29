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

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a placeholder serializer for null values.
    /// </summary>
    public class NullSerializer : IBsonSerializer
    {
        // private fields
        private readonly Type _nominalType;

        // constructors
        /// <summary>
        /// Initializes a new instance of the KeyValuePairSerializer class.
        /// </summary>
        /// <param name="nominalType">The Type.</param>
        public NullSerializer(
            Type nominalType)
        {
            _nominalType = nominalType;
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
            return this.Deserialize(bsonReader, nominalType, nominalType, options);
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
            VerifyType(nominalType);

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType != BsonType.Null)
            {
                var message = string.Format("BsonType {0} is unsupported.", bsonType);
                throw new BsonSerializationException(message);
            }
            bsonReader.ReadNull();
            return null;
        }

        /// <summary>
        /// Gets the default serialization options for this serializer.
        /// </summary>
        /// <returns>The default serialization options for this serializer.</returns>
        public IBsonSerializationOptions GetDefaultSerializationOptions()
        {
            return null;
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

            if (value != null)
            {
                throw new BsonSerializationException("value is not null.");
            }
            bsonWriter.WriteNull();
        }

        // private methods
        private void VerifyType(Type type)
        {
            if (type != _nominalType)
            {
                var message = string.Format(
                    "NullSerializer for type {0} cannot be used with type {1}.",
                    _nominalType.FullName,
                    type.FullName);
                throw new BsonSerializationException(message);
            }
        }

        private void VerifyNominalType(Type nominalType)
        {
            if (nominalType != _nominalType && !_nominalType.IsAssignableFrom(nominalType))
            {
                var message = string.Format(
                    "NullSerializer for type {0} cannot be used with type {1}.",
                    _nominalType.FullName,
                    nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
    }
}
