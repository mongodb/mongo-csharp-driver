/* Copyright 2010 10gen Inc.
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

    public class BsonDocumentWrapper : BsonValue, IBsonSerializable {
        #region private fields
        private Type wrappedNominalType;
        private object wrappedObject;
        #endregion

        #region constructors
        public BsonDocumentWrapper(
            object wrappedObject
        )
            : base(BsonType.Document) {
            this.wrappedNominalType = (wrappedObject == null) ? typeof(object) : wrappedObject.GetType();
            this.wrappedObject = wrappedObject;
        }

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
        public static BsonDocumentWrapper Create<T>(
            T value
        ) {
            if (value != null) {
                return new BsonDocumentWrapper(typeof(T), value);
            } else {
                return null;
            }
        }

        public static IEnumerable<BsonDocumentWrapper> CreateMultiple<T>(
            IEnumerable<T> values
        ) {
            if (values != null) {
                return values.Where(v => v != null).Select(v => new BsonDocumentWrapper(typeof(T), v));
            } else {
                return null;
            }
        }
        #endregion

        #region public methods
        public override int CompareTo(
            BsonValue other
        ) {
            throw new InvalidOperationException("CompareTo not supported for BsonDocumentWrapper");
        }

        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            throw new InvalidOperationException("Deserialize not valid for BsonDocumentWrapper");
        }

        public bool GetDocumentId(
            out object id,
            out IIdGenerator idGenerator
        ) {
            throw new InvalidOperationException();
        }

        public override bool Equals(
            object obj
        ) {
            throw new InvalidOperationException("Equals not supported for BsonDocumentWrapper");
        }

        public override int GetHashCode() {
            throw new InvalidOperationException("GetHashCode not supported for BsonDocumentWrapper");
        }

        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            BsonSerializer.Serialize(bsonWriter, wrappedNominalType, wrappedObject, options);
        }

        public void SetDocumentId(
            object Id
        ) {
            throw new InvalidOperationException();
        }

        public override string ToString() {
            return this.ToJson();
        }
        #endregion
    }
}
