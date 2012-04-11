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
using System.Linq;
using System.Text;
using System.IO;

using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class DictionarySerializer<TKey, TValue> : BsonBaseSerializer
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the DictionarySerializer class.
        /// </summary>
        public DictionarySerializer()
            : base(DictionarySerializationOptions.Defaults)
        {
        }

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
            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Document)
            {
                if (nominalType == typeof(object))
                {
                    bsonReader.ReadStartDocument();
                    bsonReader.ReadString("_t"); // skip over discriminator
                    bsonReader.ReadName("_v");
                    var value = Deserialize(bsonReader, actualType, options); // recursive call replacing nominalType with actualType
                    bsonReader.ReadEndDocument();
                    return value;
                }

                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartDocument();
                var valueDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TValue));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var key = (TKey)(object)bsonReader.ReadName();
                    var valueType = valueDiscriminatorConvention.GetActualType(bsonReader, typeof(TValue));
                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                    var value = (TValue)valueSerializer.Deserialize(bsonReader, typeof(TValue), valueType, null);
                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndDocument();
                return dictionary;
            }
            else if (bsonType == BsonType.Array)
            {
                var dictionary = CreateInstance(nominalType);
                bsonReader.ReadStartArray();
                var keyDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TKey));
                var valueDiscriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(typeof(TValue));
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var keyValuePairBsonType = bsonReader.GetCurrentBsonType();
                    if (keyValuePairBsonType == BsonType.Array)
                    {
                        bsonReader.ReadStartArray();
                        bsonReader.ReadBsonType();
                        var keyType = keyDiscriminatorConvention.GetActualType(bsonReader, typeof(TKey));
                        var keySerializer = BsonSerializer.LookupSerializer(keyType);
                        var key = (TKey)keySerializer.Deserialize(bsonReader, typeof(TKey), keyType, null);
                        bsonReader.ReadBsonType();
                        var valueType = valueDiscriminatorConvention.GetActualType(bsonReader, typeof(TValue));
                        var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                        var value = (TValue)valueSerializer.Deserialize(bsonReader, typeof(TValue), valueType, null);
                        bsonReader.ReadEndArray();
                        dictionary.Add(key, value);
                    }
                    else if (keyValuePairBsonType == BsonType.Document)
                    {
                        bsonReader.ReadStartDocument();
                        TKey key = default(TKey);
                        TValue value = default(TValue);
                        bool keyFound = false, valueFound = false;
                        while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                        {
                            var name = bsonReader.ReadName();
                            switch (name)
                            {
                                case "k":
                                    var keyType = keyDiscriminatorConvention.GetActualType(bsonReader, typeof(TKey));
                                    var keySerializer = BsonSerializer.LookupSerializer(keyType);
                                    key = (TKey)keySerializer.Deserialize(bsonReader, typeof(TKey), keyType, null);
                                    keyFound = true;
                                    break;
                                case "v":
                                    var valueType = valueDiscriminatorConvention.GetActualType(bsonReader, typeof(TValue));
                                    var valueSerializer = BsonSerializer.LookupSerializer(valueType);
                                    value = (TValue)valueSerializer.Deserialize(bsonReader, typeof(TValue), valueType, null);
                                    valueFound = true;
                                    break;
                                default:
                                    var message = string.Format("Element '{0}' is not valid for Dictionary items (expecting 'k' or 'v').", name);
                                    throw new FileFormatException(message);
                            }
                        }
                        bsonReader.ReadEndDocument();
                        if (!keyFound)
                        {
                            throw new FileFormatException("Dictionary item was missing the 'k' element.");
                        }
                        if (!valueFound)
                        {
                            throw new FileFormatException("Dictionary item was missing the 'v' element.");
                        }
                        dictionary.Add(key, value);
                    }
                    else
                    {
                        var message = string.Format("Expected document or array for Dictionary item, not {0}.", keyValuePairBsonType);
                        throw new FileFormatException(message);
                    }
                }
                bsonReader.ReadEndArray();
                return dictionary;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", nominalType.FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Gets the serialization info for individual items of an enumerable type.
        /// </summary>
        /// <returns>The serialization info for the items.</returns>
        public override BsonSerializationInfo GetItemSerializationInfo()
        {
            string elementName = null;
            var serializer = BsonSerializer.LookupSerializer(typeof(TValue));
            var nominalType = typeof(TValue);
            IBsonSerializationOptions serializationOptions = null;
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
                if (nominalType == typeof(object))
                {
                    var actualType = value.GetType();
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteString("_t", TypeNameDiscriminator.GetDiscriminator(actualType));
                    bsonWriter.WriteName("_v");
                    Serialize(bsonWriter, actualType, value, options); // recursive call replacing nominalType with actualType
                    bsonWriter.WriteEndDocument();
                    return;
                }

                // support RepresentationSerializationOptions for backward compatibility
                var representationSerializationOptions = options as RepresentationSerializationOptions;
                if (representationSerializationOptions != null)
                {
                    switch (representationSerializationOptions.Representation)
                    {
                        case BsonType.Array:
                            options = DictionarySerializationOptions.ArrayOfArrays;
                            break;
                        case BsonType.Document:
                            options = DictionarySerializationOptions.Document;
                            break;
                        default:
                            var message = string.Format("BsonType {0} is not a valid representation for a Dictionary.", representationSerializationOptions.Representation);
                            throw new BsonSerializationException(message);
                    }
                }

                var dictionary = (IDictionary<TKey, TValue>)value;
                var dictionarySerializationOptions = EnsureSerializationOptions<DictionarySerializationOptions>(options);
                var representation = dictionarySerializationOptions.Representation;
                var itemSerializationOptions = dictionarySerializationOptions.ItemSerializationOptions;

                if (representation == DictionaryRepresentation.Dynamic)
                {
                    if (typeof(TKey) == typeof(string) || typeof(TKey) == typeof(object))
                    {
                        representation = DictionaryRepresentation.Document;
                        foreach (object key in dictionary.Keys)
                        {
                            var name = key as string; // check for null and type string at the same time
                            if (name == null || name[0] == '$' || name.IndexOf('.') != -1)
                            {
                                representation = DictionaryRepresentation.ArrayOfArrays;
                                break;
                            }
                        }
                    }
                    else
                    {
                        representation = DictionaryRepresentation.ArrayOfArrays;
                    }
                }

                switch (representation)
                {
                    case DictionaryRepresentation.Document:
                        bsonWriter.WriteStartDocument();
                        foreach (KeyValuePair<TKey, TValue> entry in dictionary)
                        {
                            bsonWriter.WriteName((string)(object)entry.Key);
                            BsonSerializer.Serialize(bsonWriter, typeof(TValue), entry.Value, itemSerializationOptions);
                        }
                        bsonWriter.WriteEndDocument();
                        break;
                    case DictionaryRepresentation.ArrayOfArrays:
                        bsonWriter.WriteStartArray();
                        foreach (KeyValuePair<TKey, TValue> entry in dictionary)
                        {
                            bsonWriter.WriteStartArray();
                            BsonSerializer.Serialize(bsonWriter, typeof(TKey), entry.Key);
                            BsonSerializer.Serialize(bsonWriter, typeof(TValue), entry.Value, itemSerializationOptions);
                            bsonWriter.WriteEndArray();
                        }
                        bsonWriter.WriteEndArray();
                        break;
                    case DictionaryRepresentation.ArrayOfDocuments:
                        bsonWriter.WriteStartArray();
                        foreach (KeyValuePair<TKey, TValue> entry in dictionary)
                        {
                            bsonWriter.WriteStartDocument();
                            bsonWriter.WriteName("k");
                            BsonSerializer.Serialize(bsonWriter, typeof(TKey), entry.Key);
                            bsonWriter.WriteName("v");
                            BsonSerializer.Serialize(bsonWriter, typeof(TValue), entry.Value, itemSerializationOptions);
                            bsonWriter.WriteEndDocument();
                        }
                        bsonWriter.WriteEndArray();
                        break;
                    default:
                        var message = string.Format(
                            "'{0}' is not a valid {1} representation.",
                            representation,
                            BsonUtils.GetFriendlyTypeName(typeof(IDictionary<TKey, TValue>)));
                        throw new BsonSerializationException(message);
                }
            }
        }

        // private methods
        private IDictionary<TKey, TValue> CreateInstance(Type nominalType)
        {
            if (nominalType == typeof(Dictionary<TKey, TValue>))
            {
                return new Dictionary<TKey, TValue>();
            }
            else if (nominalType == typeof(IDictionary<TKey, TValue>))
            {
                return new Dictionary<TKey, TValue>();
            }
            else if (nominalType == typeof(SortedDictionary<TKey, TValue>))
            {
                return new SortedDictionary<TKey, TValue>();
            }
            else if (nominalType == typeof(SortedList<TKey, TValue>))
            {
                return new SortedList<TKey, TValue>();
            }
            else
            {
                var message = string.Format(
                    "Invalid nominalType {0} for DictionarySerializer<{1}, {2}>.",
                    nominalType.FullName, typeof(TKey).FullName, typeof(TValue).FullName);
                throw new BsonSerializationException(message);
            }
        }
    }
}
