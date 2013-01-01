/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Wrappers
{
    /// <summary>
    /// Abstract base class for wrapper classes.
    /// </summary>
    public abstract class BaseWrapper : IBsonSerializable
    {
        // private fields
        private Type _nominalType;
        private object _obj;

        // constructors
        /// <summary>
        /// Initializes a new instance of the BaseWrapper class.
        /// </summary>
        /// <param name="obj">The wrapped object.</param>
        protected BaseWrapper(object obj)
        {
            _nominalType = obj.GetType();
            _obj = obj;
        }

        /// <summary>
        /// Initializes a new instance of the BaseWrapper class.
        /// </summary>
        /// <param name="nominalType">The nominal type of the wrapped object.</param>
        /// <param name="obj">The wrapped object.</param>
        protected BaseWrapper(Type nominalType, object obj)
        {
            _nominalType = nominalType;
            _obj = obj;
        }

        // public methods
        /// <summary>
        /// Deserialize is an invalid operation for wrapper classes.
        /// </summary>
        /// <param name="bsonReader">Not applicable.</param>
        /// <param name="nominalType">Not applicable.</param>
        /// <param name="options">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        [Obsolete("Deserialize was intended to be private and will become private in a future release.")]
        public object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            var message = string.Format("Deserialize method cannot be called on a {0}.", this.GetType().Name);
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// GetDocumentId is an invalid operation for wrapper classes.
        /// </summary>
        /// <param name="id">Not applicable.</param>
        /// <param name="idNominalType">Not applicable.</param>
        /// <param name="idGenerator">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        [Obsolete("GetDocumentId was intended to be private and will become private in a future release.")]
        public bool GetDocumentId(out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            var message = string.Format("GetDocumentId method cannot be called on a {0}.", this.GetType().Name);
            throw new NotSupportedException(message);
        }

        /// <summary>
        /// Serializes a wrapped object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type (ignored).</param>
        /// <param name="options">The serialization options.</param>
        [Obsolete("Serialize was intended to be private and will become private in a future release.")]
        public void Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            BsonSerializer.Serialize(bsonWriter, _nominalType, _obj, options); // use wrapped nominalType
        }

        /// <summary>
        /// SetDocumentId is an invalid operation for wrapper classes.
        /// </summary>
        /// <param name="id">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        [Obsolete("SetDocumentId was intended to be private and will become private in a future release.")]
        public void SetDocumentId(object id)
        {
            var message = string.Format("SetDocumentId method cannot be called on a {0}.", this.GetType().Name);
            throw new NotSupportedException(message);
        }
    }
}
