/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for KeyValuePairs.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class KeyValuePairSerializer<TKey, TValue> : BsonBaseSerializer<KeyValuePair<TKey, TValue>>
    {
        // private fields
        private readonly BsonType _representation;
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValuePairSerializer{TKey, TValue}"/> class.
        /// </summary>
        public KeyValuePairSerializer()
            : this(BsonType.Document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValuePairSerializer{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        public KeyValuePairSerializer(BsonType representation)
            : this(representation, BsonSerializer.LookupSerializer<TKey>(), BsonSerializer.LookupSerializer<TValue>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyValuePairSerializer{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="representation">The representation.</param>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        public KeyValuePairSerializer(BsonType representation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
        {
            switch (representation)
            {
                case BsonType.Array:
                case BsonType.Document:
                    break;

                default:
                    var message = string.Format("{0} is not a valid representation for a KeyValuePairSerializer.", representation);
                    throw new ArgumentException(message);
            }

            _representation = representation;
            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
        }

        // public properties
        /// <summary>
        /// Gets the key serializer.
        /// </summary>
        /// <value>
        /// The key serializer.
        /// </value>
        public IBsonSerializer<TKey> KeySerializer
        {
            get { return _keySerializer; }
        }

        /// <summary>
        /// Gets the representation.
        /// </summary>
        /// <value>
        /// The representation.
        /// </value>
        public BsonType Representation
        {
            get { return _representation; }
        }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        public IBsonSerializer<TValue> ValueSerializer
        {
            get { return _valueSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        public override KeyValuePair<TKey, TValue> Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            string message;
            TKey key;
            TValue value;            

            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    bsonReader.ReadStartArray();
                    key = context.DeserializeWithChildContext(_keySerializer);
                    value = context.DeserializeWithChildContext(_valueSerializer);
                    bsonReader.ReadEndArray();
                    break;

                case BsonType.Document:
                    key = default(TKey);
                    value = default(TValue);
                    bool keyFound = false, valueFound = false;

                    bsonReader.ReadStartDocument();
                    while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                    {
                        var name = bsonReader.ReadName();
                        switch (name)
                        {
                            case "k":
                                key = context.DeserializeWithChildContext(_keySerializer);
                                keyFound = true;
                                break;

                            case "v":
                                value = context.DeserializeWithChildContext(_valueSerializer);
                                valueFound = true;
                                break;
            
                            default:
                                message = string.Format("Element '{0}' is not valid for KeyValuePairs (expecting 'k' or 'v').", name);
                                throw new BsonSerializationException(message);
                        }
                    }
                    bsonReader.ReadEndDocument();

                    if (!keyFound)
                    {
                        throw new FileFormatException("KeyValuePair item was missing the 'k' element.");
                    }
                    if (!valueFound)
                    {
                        throw new FileFormatException("KeyValuePair item was missing the 'v' element.");
                    }
                    break;

                default:
                    message = string.Format(
                        "Cannot deserialize '{0}' from BsonType {1}.",
                        BsonUtils.GetFriendlyTypeName(typeof(KeyValuePair<TKey, TValue>)),
                        bsonType);
                    throw new FileFormatException(message);
            }

            return new KeyValuePair<TKey, TValue>(key, value);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, KeyValuePair<TKey, TValue> value)
        {
            var bsonWriter = context.Writer;

            switch (_representation)
            {
                case BsonType.Array:
                    bsonWriter.WriteStartArray();
                    context.SerializeWithChildContext(_keySerializer, value.Key);
                    context.SerializeWithChildContext(_valueSerializer, value.Value);
                    bsonWriter.WriteEndArray();
                    break;

                case BsonType.Document:
                    bsonWriter.WriteStartDocument();
                    bsonWriter.WriteName("k");
                    context.SerializeWithChildContext(_keySerializer, value.Key);
                    bsonWriter.WriteName("v");
                    context.SerializeWithChildContext(_valueSerializer, value.Value);
                    bsonWriter.WriteEndDocument();
                    break;

                default:
                    var message = string.Format(
                        "'{0}' is not a valid {1} representation.",
                        _representation,
                        BsonUtils.GetFriendlyTypeName(typeof(KeyValuePair<TKey, TValue>)));
                    throw new BsonSerializationException(message);
            }
        }
    }
}
