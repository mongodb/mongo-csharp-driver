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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver {
    public abstract class BaseWrapper : IBsonSerializable {
        #region private fields
        private Type nominalType;
        private object obj;
        #endregion

        #region constructors
        protected BaseWrapper(
            object obj
        ) {
            this.nominalType = obj.GetType();
            this.obj = obj;
        }

        protected BaseWrapper(
            Type nominalType,
            object obj
        ) {
            this.nominalType = nominalType;
            this.obj = obj;
        }
        #endregion

        #region public methods
        public object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var message = string.Format("Deserialize method cannot be called on a {0}", this.GetType().Name);
            throw new InvalidOperationException(message);
        }

        public bool GetDocumentId(
            out object id,
            out IIdGenerator idGenerator
        ) {
            var message = string.Format("GetDocumentId method cannot be called on a {0}", this.GetType().Name);
            throw new InvalidOperationException(message);
        }

        public void Serialize(
            BsonWriter bsonWriter,
            Type nominalType, // ignored
            IBsonSerializationOptions options
        ) {
            BsonSerializer.Serialize(bsonWriter, this.nominalType, obj, options); // use wrapped nominalType
        }

        public void SetDocumentId(
            object id
        ) {
            var message = string.Format("SetDocumentId method cannot be called on a {0}", this.GetType().Name);
            throw new InvalidOperationException(message);
        }
        #endregion
    }
}
