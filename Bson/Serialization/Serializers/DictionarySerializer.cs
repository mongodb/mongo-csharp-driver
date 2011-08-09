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
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers {
    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    public class DictionarySerializer : BsonBaseSerializer {
        #region private static fields
        private static DictionarySerializer instance = new DictionarySerializer();
        #endregion

        #region constructors
        /// <summary>
        /// Initializes a new instance of the DictionarySerializer class.
        /// </summary>
        public DictionarySerializer() {
        }
        #endregion

        #region public static properties
        /// <summary>
        /// Gets an instance of the DictionarySerializer class.
        /// </summary>
        public static DictionarySerializer Instance {
            get { return instance; }
        }
        #endregion

        #region public methods
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
            Type actualType, // ignored
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartDocument();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var key = bsonReader.ReadName();
                    var valueType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                    var value = valueSerializer.Deserialize(bsonReader, typeof(object), valueType, null);
                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndDocument();
                return dictionary;
            } else if (bsonType == BsonType.Array) {
                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartArray();
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(object));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.ReadStartArray();
                    bsonReader.ReadBsonType();
                    var keyType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var keySerializer = BsonSerializer.LookupSerializer(keyType);
                    var key = keySerializer.Deserialize(bsonReader, typeof(object), keyType, null);
                    bsonReader.ReadBsonType();
                    var valueType = discriminatorConvention.GetActualType(bsonReader, typeof(object));
                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                    var value = valueSerializer.Deserialize(bsonReader, typeof(object), valueType, null);
                    bsonReader.ReadEndArray();
                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndArray();
                return dictionary;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
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
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var dictionary = (IDictionary) value;

                var representationOptions = options as RepresentationSerializationOptions;
                BsonType representation;
                if (representationOptions == null) {
                    representation = BsonType.Document;
                    foreach (object key in dictionary.Keys) {
                        var name = key as string; // check for null and type string at the same time
                        if (name == null || name.StartsWith("$") || name.Contains(".")) {
                            representation = BsonType.Array;
                            break;
                        }
                    }
                } else {
                    representation = representationOptions.Representation;
                }

                switch (representation) {
                    case BsonType.Document:
                        bsonWriter.WriteStartDocument();
                        foreach (DictionaryEntry entry in dictionary) {
                            bsonWriter.WriteName((string) entry.Key);
                            BsonSerializer.Serialize(bsonWriter, typeof(object), entry.Value);
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case BsonType.Array:
                        bsonWriter.WriteStartArray();
                        foreach (DictionaryEntry entry in dictionary) {
                            bsonWriter.WriteStartArray();
                            BsonSerializer.Serialize(bsonWriter, typeof(object), entry.Key);
                            BsonSerializer.Serialize(bsonWriter, typeof(object), entry.Value);
                            bsonWriter.WriteEndArray();
                        }
                        bsonWriter.WriteEndArray();
                        break;
                    default:
                        var message = string.Format("'{0}' is not a valid representation for type IDictionary.", representation);
                        throw new BsonSerializationException(message);
                }
            }
        }
        #endregion

        #region private methods
        private IDictionary CreateInstance(
            Type nominalType
        ) {
            if (nominalType == typeof(Hashtable)) {
                return new Hashtable();
            } else if (nominalType == typeof(ListDictionary)) {
                return new ListDictionary();
            } else if (nominalType == typeof(IDictionary)) {
                return new Hashtable();
            } else if (nominalType == typeof(OrderedDictionary)) {
                return new OrderedDictionary();
            } else if (nominalType == typeof(SortedList)) {
                return new SortedList();
            } else {
                var message = string.Format("Invalid nominalType {0} for DictionarySerializer.", nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
