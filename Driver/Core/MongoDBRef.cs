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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a DBRef (a convenient way to refer to a document).
    /// </summary>
    public class MongoDBRef : IBsonSerializable
    {
        // private fields
        private string databaseName;
        private string collectionName;
        private BsonValue id;

        // constructors
        // default constructor is private and only used for deserialization
        private MongoDBRef()
        {
        }

        /// <summary>
        /// Creates a MongoDBRef.
        /// </summary>
        /// <param name="collectionName">The name of the collection that contains the document.</param>
        /// <param name="id">The Id of the document.</param>
        public MongoDBRef(string collectionName, BsonValue id)
        {
            this.collectionName = collectionName;
            this.id = id;
        }

        /// <summary>
        /// Creates a MongoDBRef.
        /// </summary>
        /// <param name="databaseName">The name of the database that contains the document.</param>
        /// <param name="collectionName">The name of the collection that contains the document.</param>
        /// <param name="id">The Id of the document.</param>
        public MongoDBRef(string databaseName, string collectionName, BsonValue id)
        {
            this.databaseName = databaseName;
            this.collectionName = collectionName;
            this.id = id;
        }

        // public properties
        /// <summary>
        /// Gets the name of the database that contains the document.
        /// </summary>
        public string DatabaseName
        {
            get { return databaseName; }
        }

        /// <summary>
        /// Gets the name of the collection that contains the document.
        /// </summary>
        public string CollectionName
        {
            get { return collectionName; }
        }

        /// <summary>
        /// Gets the Id of the document.
        /// </summary>
        public BsonValue Id
        {
            get { return id; }
        }

        // explicit interface implementations
        object IBsonSerializable.Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonReader.CurrentBsonType == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                bsonReader.ReadStartDocument();
                string message;
                BsonType bsonType;
                while ((bsonType = bsonReader.ReadBsonType()) != BsonType.EndOfDocument)
                {
                    var name = bsonReader.ReadName();
                    switch (name)
                    {
                        case "$ref":
                            collectionName = bsonReader.ReadString();
                            break;
                        case "$id":
                            id = BsonValue.ReadFrom(bsonReader); ;
                            break;
                        case "$db":
                            databaseName = bsonReader.ReadString();
                            break;
                        default:
                            message = string.Format("Element '{0}' is not valid for DBRef.", name);
                            throw new FileFormatException(message);
                    }
                }
                bsonReader.ReadEndDocument();
                return this;
            }
        }

        bool IBsonSerializable.GetDocumentId(out object id, out Type idNominalType, out IIdGenerator idGenerator)
        {
            throw new NotSupportedException();
        }

        void IBsonSerializable.Serialize(BsonWriter bsonWriter, Type nominalType, IBsonSerializationOptions options)
        {
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteString("$ref", collectionName);
            bsonWriter.WriteName("$id");
            id.WriteTo(bsonWriter);
            if (databaseName != null)
            {
                bsonWriter.WriteString("$db", databaseName);
            }
            bsonWriter.WriteEndDocument();
        }

        void IBsonSerializable.SetDocumentId(object id)
        {
            throw new NotSupportedException();
        }
    }
}
