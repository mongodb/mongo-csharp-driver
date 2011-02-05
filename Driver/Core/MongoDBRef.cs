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
        // default constructor is private and only used for deserialization
        private MongoDBRef() {
        }

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
                (document.ElementCount != 2 && document.ElementCount != 3) ||
                document.GetElement(0).Name != "$ref" ||
                document.GetElement(1).Name != "$id" ||
                (document.ElementCount == 3 && document.GetElement(2).Name != "$db")
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
        object IBsonSerializable.Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else {
                bsonReader.ReadStartDocument();
                string message;
                BsonType bsonType;
                while ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument) {
                    var name = bsonReader.ReadName();
                    switch (name) {
                        case "$ref":
                            collectionName = bsonReader.ReadString();
                            break;
                        case "$id":
                            switch (bsonType) {
                                case BsonType.Binary:
                                    byte[] bytes;
                                    BsonBinarySubType subType;
                                    bsonReader.ReadBinaryData(out bytes, out subType);
                                    if (bytes.Length == 16 && subType == BsonBinarySubType.Uuid) {
                                        id = new Guid(bytes);
                                    } else {
                                        throw new FileFormatException("Binary data is not valid for Guid");
                                    }
                                    break;
                                case BsonType.Int32:
                                    id = bsonReader.ReadInt32();
                                    break;
                                case BsonType.Int64:
                                    id = bsonReader.ReadInt64();
                                    break;
                                case BsonType.ObjectId:
                                    int timestamp;
                                    int machine;
                                    short pid;
                                    int increment;
                                    bsonReader.ReadObjectId(out timestamp, out machine, out pid, out increment);
                                    id = new ObjectId(timestamp, machine, pid, increment);
                                    break;
                                case BsonType.String:
                                    id = bsonReader.ReadString();
                                    break;
                                default:
                                    message = string.Format("Unsupported BsonType for $id element of a DBRef: {0}", bsonType);
                                    throw new MongoException(message);
                            }
                            break;
                        case "$db":
                            databaseName = bsonReader.ReadString();
                            break;
                        default:
                            message = string.Format("Invalid element for DBRef: {0}", name);
                            throw new FileFormatException(message);
                    }
                }
                bsonReader.ReadEndDocument();
                return this;
            }
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
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("$ref", collectionName);
            if (id is ObjectId) {
                var objectId = (ObjectId) id;
                bsonWriter.WriteObjectId("$id", objectId.Timestamp, objectId.Machine, objectId.Pid, objectId.Increment);
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

        void IBsonSerializable.SetDocumentId(
            object id
        ) {
            throw new InvalidOperationException();
        }
        #endregion
    }
}
