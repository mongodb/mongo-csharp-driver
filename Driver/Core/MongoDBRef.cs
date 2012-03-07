/* Copyright 2010-2012 10gen Inc.
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
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a DBRef (a convenient way to refer to a document).
    /// </summary>
    public class MongoDBRef : IEquatable<MongoDBRef>
    {
        // private fields
        private string _databaseName;
        private string _collectionName;
        private BsonValue _id;

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
            _collectionName = collectionName;
            _id = id;
        }

        /// <summary>
        /// Creates a MongoDBRef.
        /// </summary>
        /// <param name="databaseName">The name of the database that contains the document.</param>
        /// <param name="collectionName">The name of the collection that contains the document.</param>
        /// <param name="id">The Id of the document.</param>
        public MongoDBRef(string databaseName, string collectionName, BsonValue id)
        {
            _databaseName = databaseName;
            _collectionName = collectionName;
            _id = id;
        }

        // public properties
        /// <summary>
        /// Gets the name of the database that contains the document.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
        }

        /// <summary>
        /// Gets the name of the collection that contains the document.
        /// </summary>
        public string CollectionName
        {
            get { return _collectionName; }
        }

        /// <summary>
        /// Gets the Id of the document.
        /// </summary>
        public BsonValue Id
        {
            get { return _id; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoDBRefs.
        /// </summary>
        /// <param name="lhs">The first MongoDBRef.</param>
        /// <param name="rhs">The other MongoDBRef.</param>
        /// <returns>True if the two MongoDBRefs are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoDBRef lhs, MongoDBRef rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Compares two MongoDBRefs.
        /// </summary>
        /// <param name="lhs">The first MongoDBRef.</param>
        /// <param name="rhs">The other MongoDBRef.</param>
        /// <returns>True if the two MongoDBRefs are equal (or both null).</returns>
        public static bool operator ==(MongoDBRef lhs, MongoDBRef rhs)
        {
            return object.Equals(lhs, rhs);
        }

        // public methods
        /// <summary>
        /// Compares this MongoDBRef to another one.
        /// </summary>
        /// <param name="rhs">The other MongoDBRef.</param>
        /// <returns>True if the two MongoDBRefs are equal.</returns>
        public bool Equals(MongoDBRef rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType())
            {
                return false;
            }
            // note: _databaseName can be null
            return object.Equals(_databaseName, rhs._databaseName) && _collectionName.Equals(rhs._collectionName) && _id.Equals(rhs._id);
        }

        /// <summary>
        /// Compares this MongoDBRef to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if the other objects is a MongoDBRef and is equal to this one.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoDBRef); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + ((_databaseName == null) ? 0 :_databaseName.GetHashCode());
            hash = 37 * hash + _collectionName.GetHashCode();
            hash = 37 * hash + _id.GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Represents a serializer for MongoDBRefs.
    /// </summary>
    public class MongoDBRefSerializer : BsonBaseSerializer
    {
        // public methods
        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="actualType">The actual type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            Type actualType,
            IBsonSerializationOptions options)
        {
            VerifyTypes(nominalType, actualType, typeof(MongoDBRef));

            if (bsonReader.GetCurrentBsonType() == Bson.BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else
            {
                string databaseName = null;
                string collectionName = null;
                BsonValue id = null;

                bsonReader.ReadStartDocument();
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
                            var message = string.Format("Element '{0}' is not valid for MongoDBRef.", name);
                            throw new FileFormatException(message);
                    }
                }
                bsonReader.ReadEndDocument();

                return new MongoDBRef(databaseName, collectionName, id);
            }
        }

        /// <summary>
        /// Gets the serialization info for a member.
        /// </summary>
        /// <param name="memberName">The member name.</param>
        /// <returns>The serialization info for the member.</returns>
        public override BsonSerializationInfo GetMemberSerializationInfo(string memberName)
        {
            string elementName;
            IBsonSerializer serializer;
            Type nominalType;
            IBsonSerializationOptions serializationOptions = null;

            switch (memberName)
            {
                case "DatabaseName":
                    elementName = "$db";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "CollectionName":
                    elementName = "$ref";
                    serializer = StringSerializer.Instance;
                    nominalType = typeof(string);
                    break;
                case "Id":
                    elementName = "$id";
                    serializer = BsonValueSerializer.Instance;
                    nominalType = typeof(BsonValue);
                    break;
                default:
                    var message = string.Format("{0} is not a member of MongoDBRef.", memberName);
                    throw new ArgumentOutOfRangeException("memberName", message);
            }

            return new BsonSerializationInfo(elementName, serializer, nominalType, serializationOptions);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var dbRef = (MongoDBRef)value;

                bsonWriter.WriteStartDocument();
                bsonWriter.WriteString("$ref", dbRef.CollectionName);
                bsonWriter.WriteName("$id");
                dbRef.Id.WriteTo(bsonWriter);
                if (dbRef.DatabaseName != null)
                {
                    bsonWriter.WriteString("$db", dbRef.DatabaseName);
                }
                bsonWriter.WriteEndDocument();
            }
        }
    }
}
