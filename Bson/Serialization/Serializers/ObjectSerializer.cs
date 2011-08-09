/* Copyright 2010-2011 10gen Inc.
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
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.Serialization.Serializers {
    /// <summary>
    /// Represents a serializer for objects.
    /// </summary>
    public class ObjectSerializer : IBsonSerializer {
        #region private static fields
        private static ObjectSerializer instance = new ObjectSerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the ObjectSerializer class.
        /// </summary>
        public ObjectSerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the ObjectSerializer class.
        /// </summary>
        public static ObjectSerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            if (nominalType != typeof(object)) {
                var message = string.Format("ObjectSerializer can only be used with nominal type System.Object, not type {1}.", nominalType.FullName);
                throw new InvalidOperationException(message);
            }

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                var bookmark = bsonReader.GetBookmark();
                bsonReader.ReadStartDocument();
                if (bsonReader.ReadBsonType() == BsonType.EndOfDocument) {
                    bsonReader.ReadEndDocument();
                    return new object();
                } else {
                    bsonReader.ReturnToBookmark(bookmark);
                }
            }

            var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
            var actualType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
            if (actualType == typeof(object)) {
                var message = string.Format("Unable to determine actual type of object to deserialize. NominalType is System.Object and BsonType is {0}.", bsonType);
                throw new FileFormatException(message);
            }

            var serializer = BsonSerializer.LookupSerializer(actualType);
            return serializer.Deserialize(bsonReader, nominalType, actualType, options);
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
            IBsonSerializationOptions options
        ) {
            if (actualType != typeof(object)) {
                var message = string.Format("ObjectSerializer can only be used with actual type System.Object, not type {1}.", actualType.FullName);
                throw new ArgumentException(message, "actualType");
            }

            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                bsonReader.ReadStartDocument();
                if (bsonReader.ReadBsonType() == BsonType.EndOfDocument) {
                    bsonReader.ReadEndDocument();
                    return new object();
                } else {
                    var message = string.Format("A document being deserialized to System.Object must be empty.");
                    throw new FileFormatException(message);
                }
            } else {
                var message = string.Format("Cannot deserialize System.Object from BsonType {0}.", bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id type.</param>
        /// <returns>True if the document has an Id.</returns>
        public bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator
        ) {
            throw new NotSupportedException();
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var actualType = value.GetType();
                if (actualType != typeof(object)) {
                    var message = string.Format("ObjectSerializer can only be used with type System.Object, not type {1}.", actualType.FullName);
                    throw new InvalidOperationException(message);
                }

                bsonWriter.WriteStartDocument();
                bsonWriter.WriteEndDocument();
            }
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public void SetDocumentId(
            object document,
            object id
        ) {
            throw new NotSupportedException();
        }
        #endregion
    }
}
