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
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace MongoDB.Bson.DefaultSerializer {
    public static class DictionarySerializerRegistration {
        #region public static methods
        public static void RegisterGenericSerializerDefinitions() {
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(Dictionary<,>), typeof(DictionarySerializer<,>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(IDictionary<,>), typeof(DictionarySerializer<,>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(SortedDictionary<,>), typeof(DictionarySerializer<,>));
            BsonSerializer.RegisterGenericSerializerDefinition(typeof(SortedList<,>), typeof(DictionarySerializer<,>));
        }
        #endregion
    }

    public class DictionarySerializer<TKey, TValue> : BsonBaseSerializer {
        #region constructors
        public DictionarySerializer() {
        }
        #endregion

        #region public methods
        public override object Deserialize(
            BsonReader bsonReader,
            Type nominalType,
            IBsonSerializationOptions options
        ) {
            var bsonType = bsonReader.CurrentBsonType;
            if (bsonType == BsonType.Null) {
                bsonReader.ReadNull();
                return null;
            } else if (bsonType == BsonType.Document) {
                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartDocument();
                var valueDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TValue));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    var key = (TKey) (object) bsonReader.ReadName();
                    var valueType = valueDiscriminatorConvention.GetActualType(bsonReader, typeof(TValue));
                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                    var value = (TValue) valueSerializer.Deserialize(bsonReader, typeof(TValue), valueType, null);
                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndDocument();
                return dictionary;
            } else if (bsonType == BsonType.Array) {
                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartArray();
                var keyDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TKey));
                var valueDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TValue));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument) {
                    bsonReader.ReadStartArray();
                    bsonReader.ReadBsonType();
                    var keyType = keyDiscriminatorConvention.GetActualType(bsonReader, typeof(TKey));
                    var keySerializer = BsonSerializer.LookupSerializer(keyType);
                    var key = (TKey) keySerializer.Deserialize(bsonReader, typeof(TKey), keyType, null);
                    bsonReader.ReadBsonType();
                    var valueType = valueDiscriminatorConvention.GetActualType(bsonReader, typeof(TValue));
                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                    var value = (TValue) valueSerializer.Deserialize(bsonReader, typeof(TValue), valueType, null);
                    bsonReader.ReadEndArray();
                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndArray();
                return dictionary;
            } else {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        public override void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            if (value == null) {
                bsonWriter.WriteNull();
            } else {
                var dictionary = (IDictionary<TKey, TValue>) value;
                if (
                    typeof(TKey) == typeof(string) ||
                    (typeof(TKey) == typeof(object) && dictionary.Keys.All(o => o.GetType() == typeof(string)))
                ) {
                    bsonWriter.WriteStartDocument();
                    int index = 0;
                    foreach (KeyValuePair<TKey, TValue> entry in dictionary) {
                        bsonWriter.WriteName((string) (object) entry.Key);
                        BsonSerializer.Serialize(bsonWriter, typeof(TValue), entry.Value);
                        index++;
                    }
                    bsonWriter.WriteEndDocument();
                } else {
                    bsonWriter.WriteStartArray();
                    foreach (KeyValuePair<TKey, TValue> entry in dictionary) {
                        bsonWriter.WriteStartArray();
                        BsonSerializer.Serialize(bsonWriter, typeof(object), entry.Key);
                        BsonSerializer.Serialize(bsonWriter, typeof(object), entry.Value);
                        bsonWriter.WriteEndArray();
                    }
                    bsonWriter.WriteEndArray();
                }
            }
        }
        #endregion

        #region private methods
        private IDictionary<TKey, TValue> CreateInstance(
            Type nominalType
        ) {
            if (nominalType == typeof(Dictionary<TKey, TValue>)) {
                return new Dictionary<TKey, TValue>();
            } else if (nominalType == typeof(IDictionary<TKey, TValue>)) {
                return new Dictionary<TKey, TValue>();
            } else if (nominalType == typeof(SortedDictionary<TKey, TValue>)) {
                return new SortedDictionary<TKey, TValue>();
            } else if (nominalType == typeof(SortedList<TKey, TValue>)) {
                return new SortedList<TKey, TValue>();
            } else {
                var message = string.Format("Invalid nominalType for DictionarySerializer<{0}, {1}>: {2}", typeof(TKey).FullName, typeof(TValue).FullName, nominalType.FullName);
                throw new BsonSerializationException(message);
            }
        }
        #endregion
    }
}
