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
    public class KeyValuePairSerializer<TKey, TValue> : StructSerializerBase<KeyValuePair<TKey, TValue>>
    {
        // private constants
        private static class Flags
        {
            public const long Key = 1;
            public const long Value = 2;
        }

        // private fields
        private readonly SerializerHelper _helper;
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly BsonType _representation;
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

            _helper = new SerializerHelper
            (
                new SerializerHelper.Member("k", Flags.Key),
                new SerializerHelper.Member("v", Flags.Value)
            );
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
            var bsonType = bsonReader.GetCurrentBsonType();
            switch (bsonType)
            {
                case BsonType.Array:
                    return DeserializeArrayRepresentation(context);
                case BsonType.Document:
                    return DeserializeDocumentRepresentation(context);
                default:
                    throw CreateCannotDeserializeFromBsonTypeException(bsonType);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, KeyValuePair<TKey, TValue> value)
        {
            switch (_representation)
            {
                case BsonType.Array:
                    SerializeArrayRepresentation(context, value);
                    break;

                case BsonType.Document:
                    SerializeDocumentRepresentation(context, value);
                    break;

                default:
                    var message = string.Format(
                        "'{0}' is not a valid {1} representation.",
                        _representation,
                        BsonUtils.GetFriendlyTypeName(typeof(KeyValuePair<TKey, TValue>)));
                    throw new BsonSerializationException(message);
            }
        }

        // private methods
        private KeyValuePair<TKey, TValue> DeserializeArrayRepresentation(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            var key = context.DeserializeWithChildContext(_keySerializer);
            var value = context.DeserializeWithChildContext(_valueSerializer);
            bsonReader.ReadEndArray();
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private KeyValuePair<TKey, TValue> DeserializeDocumentRepresentation(BsonDeserializationContext context)
        {
            var key = default(TKey);
            var value = default(TValue);
            _helper.DeserializeMembers(context, (elementName, flag) =>
            {
                switch (flag)
                {
                    case Flags.Key: key = context.DeserializeWithChildContext(_keySerializer); break;
                    case Flags.Value: value = context.DeserializeWithChildContext(_valueSerializer); break;
                }
            });
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private void SerializeArrayRepresentation(BsonSerializationContext context, KeyValuePair<TKey, TValue> value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            context.SerializeWithChildContext(_keySerializer, value.Key);
            context.SerializeWithChildContext(_valueSerializer, value.Value);
            bsonWriter.WriteEndArray();
        }

        private void SerializeDocumentRepresentation(BsonSerializationContext context, KeyValuePair<TKey, TValue> value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartDocument();
            bsonWriter.WriteName("k");
            context.SerializeWithChildContext(_keySerializer, value.Key);
            bsonWriter.WriteName("v");
            context.SerializeWithChildContext(_valueSerializer, value.Value);
            bsonWriter.WriteEndDocument();
        }
    }
}
