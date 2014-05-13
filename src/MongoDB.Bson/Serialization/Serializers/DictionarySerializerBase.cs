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
using System.Collections;
using System.Collections.Generic;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    public abstract class DictionarySerializerBase<TDictionary> : ClassSerializerBase<TDictionary>, IBsonDictionarySerializer where TDictionary : class, IDictionary
    {
        // private constants
        private static class Flags
        {
            public const long Key = 1;
            public const long Value = 2;
        }

        // private fields
        private readonly DictionaryRepresentation _dictionaryRepresentation;
        private readonly SerializerHelper _helper;
        private readonly IBsonSerializer _keySerializer;
        private readonly IBsonSerializer _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary}"/> class.
        /// </summary>
        public DictionarySerializerBase()
            : this(DictionaryRepresentation.Document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary}"/> class.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        public DictionarySerializerBase(DictionaryRepresentation dictionaryRepresentation)
            : this(dictionaryRepresentation, new ObjectSerializer(), new ObjectSerializer())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary}"/> class.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        public DictionarySerializerBase(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer keySerializer, IBsonSerializer valueSerializer)
        {
            _dictionaryRepresentation = dictionaryRepresentation;
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
        /// Gets the dictionary representation.
        /// </summary>
        /// <value>
        /// The dictionary representation.
        /// </value>
        public DictionaryRepresentation DictionaryRepresentation
        {
            get { return _dictionaryRepresentation; }
        }

        /// <summary>
        /// Gets the key serializer.
        /// </summary>
        /// <value>
        /// The key serializer.
        /// </value>
        public IBsonSerializer KeySerializer
        {
            get { return _keySerializer; }
        }

        /// <summary>
        /// Gets the value serializer.
        /// </summary>
        /// <value>
        /// The value serializer.
        /// </value>
        public IBsonSerializer ValueSerializer
        {
            get { return _valueSerializer; }
        }

        // public methods
        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="context">The deserialization context.</param>
        /// <returns>An object.</returns>
        protected override TDictionary DeserializeValue(BsonDeserializationContext context)
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
        protected override void SerializeValue(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;

            switch (_dictionaryRepresentation)
            {
                case DictionaryRepresentation.Document:
                    SerializeDocumentRepresentation(context, value);
                    break;

                case DictionaryRepresentation.ArrayOfArrays:
                    SerializeArrayOfArraysRepresentation(context, value);
                    break;

                case DictionaryRepresentation.ArrayOfDocuments:
                    SerializeArrayOfDocumentsRepresentation(context, value);
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid IDictionary representation.", _dictionaryRepresentation);
                    throw new BsonSerializationException(message);
            }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        protected abstract TDictionary CreateInstance();

        // private methods
        private TDictionary DeserializeArrayRepresentation(BsonDeserializationContext context)
        {
            var dictionary = CreateInstance();

            var bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                object key;
                object value;

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
                        key = null;
                        value = null;
                        _helper.DeserializeMembers(context, (elementName, flag) =>
                        {
                            switch (flag)
                            {
                                case Flags.Key: key = context.DeserializeWithChildContext(_keySerializer); break;
                                case Flags.Value: value = context.DeserializeWithChildContext(_valueSerializer); break;
                            }
                        });
                        break;

                    default:
                        throw CreateCannotDeserializeFromBsonTypeException(bsonType);
                }

                dictionary.Add(key, value);
            }
            bsonReader.ReadEndArray();

            return dictionary;
        }

        private TDictionary DeserializeDocumentRepresentation(BsonDeserializationContext context)
        {
            var dictionary = CreateInstance();
            var bsonReader = context.Reader;
            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var key = DeserializeKeyString(bsonReader.ReadName());
                var value = context.DeserializeWithChildContext(_valueSerializer);
                dictionary.Add(key, value);
            }
            bsonReader.ReadEndDocument();
            return dictionary;
        }

        private object DeserializeKeyString(string keyString)
        {
            var keyDocument = new BsonDocument("k", keyString);
            using (var keyReader = new BsonDocumentReader(keyDocument))
            {
                var context = BsonDeserializationContext.CreateRoot<BsonDocument>(keyReader);
                keyReader.ReadStartDocument();
                keyReader.ReadName("k");
                var key = context.DeserializeWithChildContext(_keySerializer);
                keyReader.ReadEndDocument();
                return key;
            }
        }

        private void SerializeArrayOfArraysRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            foreach (DictionaryEntry dictionaryEntry in value)
            {
                bsonWriter.WriteStartArray();
                context.SerializeWithChildContext(_keySerializer, dictionaryEntry.Key);
                context.SerializeWithChildContext(_valueSerializer, dictionaryEntry.Value);
                bsonWriter.WriteEndArray();
            }
            bsonWriter.WriteEndArray();
        }

        private void SerializeArrayOfDocumentsRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            foreach (DictionaryEntry dictionaryEntry in value)
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, dictionaryEntry.Key);
                bsonWriter.WriteName("v");
                context.SerializeWithChildContext(_valueSerializer, dictionaryEntry.Value);
                bsonWriter.WriteEndDocument();
            }
            bsonWriter.WriteEndArray();
        }

        private void SerializeDocumentRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartDocument();
            foreach (DictionaryEntry dictionaryEntry in value)
            {
                bsonWriter.WriteName(SerializeKeyString(dictionaryEntry.Key));
                context.SerializeWithChildContext(_valueSerializer, dictionaryEntry.Value);
            }
            bsonWriter.WriteEndDocument();
        }

        private string SerializeKeyString(object key)
        {
            var keyDocument = new BsonDocument();
            using (var keyWriter = new BsonDocumentWriter(keyDocument))
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(keyWriter);
                keyWriter.WriteStartDocument();
                keyWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, key);
                keyWriter.WriteEndDocument();
            }

            var keyValue = keyDocument["k"];
            if (keyValue.BsonType != BsonType.String)
            {
                throw new BsonSerializationException("When using DictionaryRepresentation.Document key values must serialize as strings.");
            }

            return (string)keyValue;
        }
    }

    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    /// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public abstract class DictionarySerializerBase<TDictionary, TKey, TValue> : ClassSerializerBase<TDictionary>, IBsonDictionarySerializer where TDictionary : class, IDictionary<TKey, TValue>
    {
        // private constants
        private static class Flags
        {
            public const long Key = 1;
            public const long Value = 2;
        }

        // private fields
        private readonly DictionaryRepresentation _dictionaryRepresentation;
        private readonly SerializerHelper _helper;
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary, TKey, TValue}"/> class.
        /// </summary>
        public DictionarySerializerBase()
            : this(DictionaryRepresentation.Document)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary, TKey, TValue}" /> class.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        public DictionarySerializerBase(DictionaryRepresentation dictionaryRepresentation)
            : this(dictionaryRepresentation, BsonSerializer.LookupSerializer<TKey>(), BsonSerializer.LookupSerializer<TValue>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary, TKey, TValue}" /> class.
        /// </summary>
        /// <param name="dictionaryRepresentation">The dictionary representation.</param>
        /// <param name="keySerializer">The key serializer.</param>
        /// <param name="valueSerializer">The value serializer.</param>
        public DictionarySerializerBase(DictionaryRepresentation dictionaryRepresentation, IBsonSerializer<TKey> keySerializer, IBsonSerializer<TValue> valueSerializer)
        {
            _dictionaryRepresentation = dictionaryRepresentation;
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
        /// Gets the dictionary representation.
        /// </summary>
        /// <value>
        /// The dictionary representation.
        /// </value>
        public DictionaryRepresentation DictionaryRepresentation
        {
            get { return _dictionaryRepresentation; }
        }

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
        protected override TDictionary DeserializeValue(BsonDeserializationContext context)
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
        protected override void SerializeValue(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;

            switch (_dictionaryRepresentation)
            {
                case DictionaryRepresentation.Document:
                    SerializeDocumentRepresentation(context, value);
                    break;

                case DictionaryRepresentation.ArrayOfArrays:
                    SerializeArrayOfArraysRepresentation(context, value);
                    break;

                case DictionaryRepresentation.ArrayOfDocuments:
                    SerializeArrayOfDocumentsRepresentation(context, value);
                    break;

                default:
                    var message = string.Format("'{0}' is not a valid IDictionary<{1}, {2}> representation.",
                        _dictionaryRepresentation,
                        BsonUtils.GetFriendlyTypeName(typeof(TKey)),
                        BsonUtils.GetFriendlyTypeName(typeof(TValue)));
                    throw new BsonSerializationException(message);
            }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        protected abstract TDictionary CreateInstance();

        // private methods
        private TDictionary DeserializeArrayRepresentation(BsonDeserializationContext context)
        {
            var dictionary = CreateInstance();

            var bsonReader = context.Reader;
            bsonReader.ReadStartArray();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
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
                        _helper.DeserializeMembers(context, (elementName, flag) =>
                        {
                            switch (flag)
                            {
                                case Flags.Key: key = context.DeserializeWithChildContext(_keySerializer); break;
                                case Flags.Value: value = context.DeserializeWithChildContext(_valueSerializer); break;
                            }
                        });
                        break;

                    default:
                        throw CreateCannotDeserializeFromBsonTypeException(bsonType);
                }

                dictionary.Add(key, value);
            }
            bsonReader.ReadEndArray();

            return dictionary;
        }

        private TDictionary DeserializeDocumentRepresentation(BsonDeserializationContext context)
        {
            var dictionary = CreateInstance();

            var bsonReader = context.Reader;
            bsonReader.ReadStartDocument();
            while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var key = DeserializeKeyString(bsonReader.ReadName());
                var value = context.DeserializeWithChildContext(_valueSerializer);
                dictionary.Add(key, value);
            }
            bsonReader.ReadEndDocument();

            return dictionary;
        }

        private TKey DeserializeKeyString(string keyString)
        {
            var keyDocument = new BsonDocument("k", keyString);
            using (var keyReader = new BsonDocumentReader(keyDocument))
            {
                var context = BsonDeserializationContext.CreateRoot<BsonDocument>(keyReader);
                keyReader.ReadStartDocument();
                keyReader.ReadName("k");
                var key = context.DeserializeWithChildContext(_keySerializer);
                keyReader.ReadEndDocument();
                return key;
            }
        }

        private void SerializeArrayOfArraysRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            foreach (var keyValuePair in value)
            {
                bsonWriter.WriteStartArray();
                context.SerializeWithChildContext(_keySerializer, keyValuePair.Key);
                context.SerializeWithChildContext(_valueSerializer, keyValuePair.Value);
                bsonWriter.WriteEndArray();
            }
            bsonWriter.WriteEndArray();
        }

        private void SerializeArrayOfDocumentsRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartArray();
            foreach (var keyValuePair in value)
            {
                bsonWriter.WriteStartDocument();
                bsonWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, keyValuePair.Key);
                bsonWriter.WriteName("v");
                context.SerializeWithChildContext(_valueSerializer, keyValuePair.Value);
                bsonWriter.WriteEndDocument();
            }
            bsonWriter.WriteEndArray();
        }

        private void SerializeDocumentRepresentation(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;
            bsonWriter.WriteStartDocument();
            foreach (var keyValuePair in value)
            {
                bsonWriter.WriteName(SerializeKeyString(keyValuePair.Key));
                context.SerializeWithChildContext(_valueSerializer, keyValuePair.Value);
            }
            bsonWriter.WriteEndDocument();
        }

        private string SerializeKeyString(TKey key)
        {
            var keyDocument = new BsonDocument();
            using (var keyWriter = new BsonDocumentWriter(keyDocument))
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(keyWriter);
                keyWriter.WriteStartDocument();
                keyWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, key);
                keyWriter.WriteEndDocument();
            }

            var keyValue = keyDocument["k"];
            if (keyValue.BsonType != BsonType.String)
            {
                throw new BsonSerializationException("When using DictionaryRepresentation.Document key values must serialize as strings.");
            }

            return (string)keyValue;
        }

        // explicit interface implementations
        IBsonSerializer IBsonDictionarySerializer.KeySerializer
        {
            get { return _keySerializer; }
        }

        IBsonSerializer IBsonDictionarySerializer.ValueSerializer
        {
            get { return _valueSerializer; }
        }
    }
}
