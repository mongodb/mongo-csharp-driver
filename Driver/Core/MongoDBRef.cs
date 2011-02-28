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
        private BsonValue id;
        #endregion

        #region constructors
        // default constructor is private and only used for deserialization
        private MongoDBRef() {
        }

        public MongoDBRef(
            string collectionName,
            BsonValue id
        ) {
            this.collectionName = collectionName;
            this.id = id;
        }

        public MongoDBRef(
            string databaseName,
            string collectionName,
            BsonValue id
        ) {
            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.id = id;
        }
        #endregion

        #region public properties
        public string DatabaseName {
            get { return databaseName; }
        }

        public string CollectionName {
            get { return collectionName; }
        }

        public BsonValue Id {
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
                            id = BsonValue.ReadFrom(bsonReader);;
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
            bsonWriter.WriteName("$id");
            id.WriteTo(bsonWriter);
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
