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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver.Builders {
    public static class SortBy {
        #region public static methods
        public static SortByBuilder Ascending(
            params string[] keys
        ) {
            return new SortByBuilder().Ascending(keys);
        }

        public static SortByBuilder Descending(
            params string[] keys
        ) {
            return new SortByBuilder().Descending(keys);
        }
        #endregion
    }

    [Serializable]
    public class SortByBuilder : BuilderBase, IConvertibleToBsonDocument, IBsonSerializable {
        #region private fields
        private BsonDocument document;
        #endregion

        #region constructors
        public SortByBuilder() {
            document = new BsonDocument();
        }
        #endregion

        #region public methods
        public SortByBuilder Ascending(
            params string[] keys
        ) {
            foreach (var key in keys) {
                document.Add(key, 1);
            }
            return this;
        }

        public SortByBuilder Descending(
            params string[] keys
        ) {
            foreach (var key in keys) {
                document.Add(key, -1);
            }
            return this;
        }

        public BsonDocument ToBsonDocument() {
            return document;
        }
        #endregion

        #region explicit interface implementations
        object IBsonSerializable.DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            throw new InvalidOperationException("Deserialize is not supported for SortByBuilder");
        }

        object IBsonSerializable.DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            throw new InvalidOperationException("Deserialize is not supported for SortByBuilder");
        }

        void IBsonSerializable.SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            bool serializeIdFirst
        ) {
            document.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
        }

        void IBsonSerializable.SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            bool useCompactRepresentation
        ) {
            document.SerializeElement(bsonWriter, nominalType, name, useCompactRepresentation);
        }
        #endregion
    }
}
