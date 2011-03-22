﻿/* Copyright 2010-2011 10gen Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson {
    // this class is a wrapper for an object that we intend to serialize as a BSON document
    // it is a subclass of BsonValue so that it may be used where a BsonValue is expected
    // this class is mostly used by MongoCollection and MongoCursor when supporting generic query objects

    /// <summary>
    /// Represents a BsonDocument wrapper.
    /// </summary>
    public class BsonDocumentWrapper : BsonValue, IBsonSerializable {
        #region private fields
        private Type wrappedNominalType;
        private object wrappedObject;
        #endregion

        #region constructors
        // needed for Deserialize
        // (even though we're going to end up throwing an InvalidOperationException)
        private BsonDocumentWrapper()
            : base(BsonType.Document) {
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="wrappedObject">The wrapped object.</param>
        public BsonDocumentWrapper(
            object wrappedObject
        )
            : base(BsonType.Document) {
            this.wrappedNominalType = (wrappedObject == null) ? typeof(object) : wrappedObject.GetType();
            this.wrappedObject = wrappedObject;
        }

        /// <summary>
        /// Initializes a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <param name="wrappedNominalType">The nominal type of the wrapped object.</param>
        /// <param name="wrappedObject">The wrapped object.</param>
        public BsonDocumentWrapper(
            Type wrappedNominalType,
            object wrappedObject
        )
            : base(BsonType.Document) {
            this.wrappedNominalType = wrappedNominalType;
            this.wrappedObject = wrappedObject;
        }
        #endregion

        #region public static methods
        /// <summary>
        /// Creates a new instance of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped object.</typeparam>
        /// <param name="value">The wrapped object.</param>
        /// <returns>A BsonDocumentWrapper or null.</returns>
        public static BsonDocumentWrapper Create<TNominalType>(
            TNominalType value
        ) {
            if (value != null) {
                return new BsonDocumentWrapper(typeof(TNominalType), value);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Creates a list of new instances of the BsonDocumentWrapper class.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the wrapped objects.</typeparam>
        /// <param name="values">A list of wrapped objects.</param>
        /// <returns>A list of BsonDocumentWrappers or null.</returns>
        public static IEnumerable<BsonDocumentWrapper> CreateMultiple<TNominalType>(
            IEnumerable<TNominalType> values
        ) {
            if (values != null) {
                return values.Where(v => v != null).Select(v => new BsonDocumentWrapper(typeof(TNominalType), v));
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// CompareTo is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="other">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public override int CompareTo(
            BsonValue other
        ) {
            throw new InvalidOperationException("CompareTo not supported for BsonDocumentWrapper");
        }

        /// <summary>
        /// Deserialize is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="bsonReader">Not applicable.</param>
        /// <param name="nominalType">Not applicable.</param>
        /// <param name="options">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            throw new InvalidOperationException("Deserialize not valid for BsonDocumentWrapper");
        }

        /// <summary>
        /// GetDocumentId is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="id">Not applicable.</param>
        /// <param name="idGenerator">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public bool GetDocumentId(
            out object id,
            out IIdGenerator idGenerator
        ) {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Equals is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="obj">Not applicable.</param>
        /// <returns>Not applicable.</returns>
        public override bool Equals(
            object obj
        ) {
            throw new InvalidOperationException("Equals not supported for BsonDocumentWrapper");
        }

        /// <summary>
        /// GetHashCode is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <returns>Not applicable.</returns>
        public override int GetHashCode() {
            throw new InvalidOperationException("GetHashCode not supported for BsonDocumentWrapper");
        }

        /// <summary>
        /// Serializes the wrapped object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type (overridded by the wrapped nominal type).</param>
        /// <param name="options">The serialization options.</param>
        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonSerializer.Serialize(bsonWriter, wrappedNominalType, wrappedObject, options);
        }

        /// <summary>
        /// SetDocumentId is an invalid operation for BsonDocumentWrapper.
        /// </summary>
        /// <param name="Id">Not applicable.</param>
        public void SetDocumentId(
            object Id
        ) {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns a string representation of the wrapped document.
        /// </summary>
        /// <returns>A string representation of the wrapped document.</returns>
        public override string ToString() {
            return this.ToJson();
        }
        #endregion
    }
}
