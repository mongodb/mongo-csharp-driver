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
    /// Represents a base implementation for the many implementations of IBsonSerializer.
    /// </summary>
    public abstract class BsonBaseSerializer : IBsonSerializer {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the BsonBaseSerializer class.
        /// </summary>
        protected BsonBaseSerializer() {
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
        public virtual object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            // override this method to determine actualType if your serializer handles polymorphic data types
            return Deserialize(bsonReader, nominalType, nominalType, options);
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public virtual object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options
        ) {
            throw new NotSupportedException("Subclass must implement Deserialize.");
        }

        /// <summary>
        /// Gets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        /// <param name="idNominalType">The nominal type of the Id.</param>
        /// <param name="idGenerator">The IdGenerator for the Id type.</param>
        /// <returns>True if the document has an Id.</returns>
        public virtual bool GetDocumentId(
            object document,
            out object id,
            out Type idNominalType,
            out IIdGenerator idGenerator
        ) {
            throw new NotSupportedException("Subclass must implement GetDocumentId.");
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public virtual void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            throw new NotSupportedException("Subclass must implement Serialize.");
        }

        /// <summary>
        /// Sets the document Id.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="id">The Id.</param>
        public virtual void SetDocumentId(
            object document,
            object id
        ) {
            throw new NotSupportedException("Subclass must implement SetDocumentId.");
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Verifies the nominal and actual types against the expected type.
        /// </summary>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="actualType">The actual type.</param>
        /// <param name="expectedType">The expected type.</param>
        protected void VerifyTypes(
            Type nominalType,
            Type actualType,
            Type expectedType
        ) {
            if (actualType != expectedType) {
                var message = string.Format("{0} can only be used with type {1}, not with type {2}.", this.GetType().FullName, expectedType.FullName, actualType.FullName);
                throw new BsonSerializationException(message);
            }
            if (!nominalType.IsAssignableFrom(actualType)) {
                var message = string.Format("{0} can only be used with a nominal type that is assignable from the actual type {1}, but nominal type {2} is not.", this.GetType().FullName, actualType.FullName, nominalType.FullName);
            }
        }
        #endregion
    }
}
