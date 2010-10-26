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
using System.IO;
using System.Linq;
using System.Text;

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace MongoDB.Driver {
    public class MongoDBRef : IBsonSerializable {
        #region private fields
        private string databaseName;
        private string collectionName;
        private object id;
        #endregion

        #region constructors
        public MongoDBRef(
            string collectionName,
            object id
        ) {
            this.collectionName = collectionName;
            this.id = id;
        }

        public MongoDBRef(
            string databaseName,
            string collectionName,
            object id
        ) {
            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.id = id;
        }
        #endregion

        #region public operators
        public static implicit operator MongoDBRef(
            BsonDocument document
        ) {
            if (
                (document.Count != 2 && document.Count != 3) ||
                document.GetElement(0).Name != "$ref" ||
                document.GetElement(1).Name != "$id" ||
                (document.Count == 3 && document.GetElement(2).Name != "$db")
            ) {
                throw new MongoException("BsonDocument is not a valid MongoDBRef");
            }

            var databaseName = document.Contains("$db") ? document["$db"].AsString : null;
            var collectionName = document["$ref"].AsString;
            var id = document["$id"].RawValue;
            return new MongoDBRef(databaseName, collectionName, id);
        }
        #endregion

        #region public properties
        public string DatabaseName {
            get { return databaseName; }
        }

        public string CollectionName {
            get { return collectionName; }
        }
        public object Id {
           get { return id; }
        }
        #endregion

        #region explicit interface implementations
        object IBsonSerializable.DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            bsonReader.ReadStartDocument();
            collectionName = bsonReader.ReadString("$ref");
            var bsonType = bsonReader.PeekBsonType();
            switch (bsonType) {
                case BsonType.Binary:
                    byte[] bytes;
                    BsonBinarySubType subType;
                    bsonReader.ReadBinaryData("$id", out bytes, out subType);
                    if (bytes.Length == 16 && subType == BsonBinarySubType.Uuid) {
                        id = new Guid(bytes);
                    } else {
                        throw new FileFormatException("Binary data is not valid for Guid");
                    }
                    break;
                case BsonType.Int32:
                    id = bsonReader.ReadInt32("$id");
                    break;
                case BsonType.Int64:
                    id = bsonReader.ReadInt64("$id");
                    break;
                case BsonType.ObjectId:
                    int timestamp;
                    long machinePidIncrement;
                    bsonReader.ReadObjectId("$id", out timestamp, out machinePidIncrement);
                    id = new ObjectId(timestamp, machinePidIncrement);
                    break;
                case BsonType.String:
                    id = bsonReader.ReadString("$id");
                    break;
            }
            if (bsonReader.HasElement()) {
                databaseName = bsonReader.ReadString("$db");
            }
            bsonReader.ReadEndDocument();
            return this;
        }

        object IBsonSerializable.DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var bsonType = bsonReader.PeekBsonType();
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull(out name);
                return null;
            } else {
                bsonReader.ReadDocumentName(out name);
                return ((IBsonSerializable) this).DeserializeDocument(bsonReader, nominalType);
            }
        }

        bool IBsonSerializable.DocumentHasIdMember() {
            return false;
        }

        bool IBsonSerializable.DocumentHasIdValue(
            out object existingId
        ) {
            throw new InvalidOperationException();
        }

        void IBsonSerializable.GenerateDocumentId() {
            throw new InvalidOperationException();
        }

        void IBsonSerializable.SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            bool serializeIdFirst
        ) {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("$ref", collectionName);
            if (id is ObjectId) {
                var objectId = (ObjectId) id;
                bsonWriter.WriteObjectId("$id", objectId.Timestamp, objectId.MachinePidIncrement);
            } else if (id is Guid) {
                var guid = (Guid) id;
                bsonWriter.WriteBinaryData("$id", guid.ToByteArray(), BsonBinarySubType.Uuid);
            } else if (id is int) {
                bsonWriter.WriteInt32("$id", (int) id);
            } else if (id is long) {
                bsonWriter.WriteInt64("$id", (long) id);
            } else if (id is string) {
                bsonWriter.WriteString("$id", (string) id);
            } else {
                var message = string.Format("Unexpected Id type: {0}", id.GetType().FullName);
                throw new BsonInternalException(message); 
            }
            if (databaseName != null) {
                bsonWriter.WriteString("$db", databaseName);
            }
            bsonWriter.WriteEndDocument();
        }

        void IBsonSerializable.SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            bool useCompactRepresentation
        ) {
            bsonWriter.WriteDocumentName(name);
            ((IBsonSerializable) this).SerializeDocument(bsonWriter, nominalType, false);
        }
        #endregion
    }
}
