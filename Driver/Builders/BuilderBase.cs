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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Driver.Builders {
    /// <summary>
    /// Abstract base class for the builders.
    /// </summary>
    [Serializable]
    public abstract class BuilderBase : IBsonSerializable, IConvertibleToBsonDocument {
        #region constructors
        /// <summary>
        /// Initializes a new instance of the BuilderBase class.
        /// </summary>
        protected BuilderBase() {
        }
        #endregion

        #region public methods
        /// <summary>
        /// Returns the result of the builder as a BsonDocument.
        /// </summary>
        /// <returns>A BsonDocument.</returns>
        public abstract BsonDocument ToBsonDocument();

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString() {
            return this.ToJson(); // "this." required to access extension method
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Serializes the result of the builder to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The writer.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="options">The serialization options.</param>
        protected abstract void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        );
        #endregion

        #region explicit interface implementations
        object IBsonSerializable.Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            throw new InvalidOperationException();
        }

        bool IBsonSerializable.GetDocumentId(
            out object id,
            out IIdGenerator idGenerator
        ) {
            throw new InvalidOperationException();
        }

        void IBsonSerializable.Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            Serialize(bsonWriter, nominalType, options);
        }

        void IBsonSerializable.SetDocumentId(
            object id
        ) {
            throw new InvalidOperationException();
        }

        BsonDocument IConvertibleToBsonDocument.ToBsonDocument() {
            return ToBsonDocument();
        }
        #endregion
    }
}
