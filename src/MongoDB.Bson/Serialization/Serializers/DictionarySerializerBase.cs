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
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Bson.Serialization.Serializers
{
    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    public abstract class DictionarySerializerBase<TDictionary> : BsonBaseSerializer<TDictionary>, IBsonDictionarySerializer where TDictionary : class, IDictionary
    {
        // private fields
        private readonly DictionaryRepresentation _dictionaryRepresentation;
        private readonly IBsonSerializer _keySerializer;
        private readonly IBsonSerializer _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary}"/> class.
        /// </summary>
        public DictionarySerializerBase()
            : this(DictionaryRepresentation.Dynamic)
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
        public override TDictionary Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Document)
            {
                var dictionary = CreateInstance();

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
            else if (bsonType == BsonType.Array)
            {
                var dictionary = CreateInstance();

                bsonReader.ReadStartArray();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    object key;
                    object value;

                    switch (bsonReader.GetCurrentBsonType())
                    {
                        case BsonType.Array:
                            bsonReader.ReadStartArray();
                            key = context.DeserializeWithChildContext(_keySerializer);
                            value = context.DeserializeWithChildContext(_valueSerializer);
                            bsonReader.ReadEndArray();
                            break;

                        case BsonType.Document:
                            bsonReader.ReadStartDocument();
                            bsonReader.ReadName("k");
                            key = context.DeserializeWithChildContext(_keySerializer);
                            bsonReader.ReadName("v");
                            value = context.DeserializeWithChildContext(_valueSerializer);
                            bsonReader.ReadEndDocument();
                            break;

                        default:
                            throw new FormatException(string.Format("Cannot deserialize dictionary key/value pair from BSON type: {0}.", bsonReader.GetCurrentBsonType()));
                    }

                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndArray();

                return dictionary;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", typeof(TDictionary).FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var dictionaryRepresentation = _dictionaryRepresentation;
                if (dictionaryRepresentation == DictionaryRepresentation.Dynamic)
                {
                    dictionaryRepresentation = DetermineDictionaryRepresentation(value);
                }

                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.Document:
                        bsonWriter.WriteStartDocument();
                        foreach (DictionaryEntry dictionaryEntry in value)
                        {
                            bsonWriter.WriteName(SerializeKey(dictionaryEntry.Key).AsString);
                            context.SerializeWithChildContext(_valueSerializer, dictionaryEntry.Value);
                        }
                        bsonWriter.WriteEndDocument();
                        break;

                    case DictionaryRepresentation.ArrayOfArrays:
                        bsonWriter.WriteStartArray();
                        foreach (DictionaryEntry dictionaryEntry in value)
                        {
                            bsonWriter.WriteStartArray();
                            context.SerializeWithChildContext(_keySerializer, dictionaryEntry.Key);
                            context.SerializeWithChildContext(_valueSerializer, dictionaryEntry.Value);
                            bsonWriter.WriteEndArray();
                        }
                        bsonWriter.WriteEndArray();
                        break;

                    case DictionaryRepresentation.ArrayOfDocuments:
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
                        break;

                    default:
                        var message = string.Format("'{0}' is not a valid IDictionary representation.", dictionaryRepresentation);
                        throw new BsonSerializationException(message);
                }
            }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        protected abstract TDictionary CreateInstance();

        // private methods
        private object DeserializeKeyString(string keyString)
        {
            var keyDocument = new BsonDocument("k", keyString);
            using (var keyReader = BsonReader.Create(keyDocument))
            {
                var context = BsonDeserializationContext.CreateRoot<BsonDocument>(keyReader);
                keyReader.ReadStartDocument();
                keyReader.ReadName("k");
                var key = context.DeserializeWithChildContext(_keySerializer);
                keyReader.ReadEndDocument();
                return key;
            }
        }

        private DictionaryRepresentation DetermineDictionaryRepresentation(TDictionary value)
        {
            foreach (object key in value.Keys)
            {
                var serializedKey = SerializeKey(key);
                if (serializedKey == null || !serializedKey.IsString)
                {
                    return DictionaryRepresentation.ArrayOfArrays;
                }

                var name = serializedKey.AsString;
                if (name == "" || (name.Length > 0 && name[0] == '$') || name.IndexOf('.') != -1 || name.IndexOf('\0') != -1)
                {
                    return DictionaryRepresentation.ArrayOfArrays;
                }
            }

            return DictionaryRepresentation.Document;
        }

        private BsonValue SerializeKey(object key)
        {
            var keyDocument = new BsonDocument();
            using (var keyWriter = BsonWriter.Create(keyDocument))
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(keyWriter);
                keyWriter.WriteStartDocument();
                keyWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, key);
                keyWriter.WriteEndDocument();
            }

            return keyDocument["k"];
        }
    }

    /// <summary>
    /// Represents a serializer for dictionaries.
    /// </summary>
    /// <typeparam name="TDictionary">The type of the dictionary.</typeparam>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public abstract class DictionarySerializerBase<TDictionary, TKey, TValue> : BsonBaseSerializer<TDictionary>, IBsonDictionarySerializer where TDictionary : class, IDictionary<TKey, TValue>
    {
        // private fields
        private readonly DictionaryRepresentation _dictionaryRepresentation;
        private readonly IBsonSerializer<TKey> _keySerializer;
        private readonly IBsonSerializer<TValue> _valueSerializer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionarySerializerBase{TDictionary, TKey, TValue}"/> class.
        /// </summary>
        public DictionarySerializerBase()
            : this(DictionaryRepresentation.Dynamic)
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
        public override TDictionary Deserialize(BsonDeserializationContext context)
        {
            var bsonReader = context.Reader;

            var bsonType = bsonReader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return null;
            }
            else if (bsonType == BsonType.Document)
            {
                var dictionary = CreateInstance();

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
            else if (bsonType == BsonType.Array)
            {
                var dictionary = CreateInstance();

                bsonReader.ReadStartArray();
                while (bsonReader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    TKey key;
                    TValue value;

                    switch (bsonReader.GetCurrentBsonType())
                    {
                        case BsonType.Array:
                            bsonReader.ReadStartArray();
                            key = context.DeserializeWithChildContext(_keySerializer);
                            value = context.DeserializeWithChildContext(_valueSerializer);
                            bsonReader.ReadEndArray();
                            break;

                        case BsonType.Document:
                            bsonReader.ReadStartDocument();
                            bsonReader.ReadName("k");
                            key = context.DeserializeWithChildContext(_keySerializer);
                            bsonReader.ReadName("v");
                            value = context.DeserializeWithChildContext(_valueSerializer);
                            bsonReader.ReadEndDocument();
                            break;

                        default:
                            throw new FormatException(string.Format("Cannot deserialize dictionary key/value pair from BSON type: {0}.", bsonReader.GetCurrentBsonType()));
                    }

                    dictionary.Add(key, value);
                }
                bsonReader.ReadEndArray();

                return dictionary;
            }
            else
            {
                var message = string.Format("Can't deserialize a {0} from BsonType {1}.", this.GetType().FullName, bsonType);
                throw new FileFormatException(message);
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, TDictionary value)
        {
            var bsonWriter = context.Writer;

            if (value == null)
            {
                bsonWriter.WriteNull();
            }
            else
            {
                var dictionaryRepresentation = _dictionaryRepresentation;
                if (dictionaryRepresentation == DictionaryRepresentation.Dynamic)
                {
                    dictionaryRepresentation = DetermineDictionaryRepresentation(value);
                }

                switch (dictionaryRepresentation)
                {
                    case DictionaryRepresentation.Document:
                        bsonWriter.WriteStartDocument();
                        foreach (var keyValuePair in value)
                        {
                            bsonWriter.WriteName(SerializeKey(keyValuePair.Key).AsString);
                            context.SerializeWithChildContext(_valueSerializer, keyValuePair.Value);
                        }
                        bsonWriter.WriteEndDocument();
                        break;

                    case DictionaryRepresentation.ArrayOfArrays:
                        bsonWriter.WriteStartArray();
                        foreach (var keyValuePair in value)
                        {
                            bsonWriter.WriteStartArray();
                            context.SerializeWithChildContext(_keySerializer, keyValuePair.Key);
                            context.SerializeWithChildContext(_valueSerializer, keyValuePair.Value);
                            bsonWriter.WriteEndArray();
                        }
                        bsonWriter.WriteEndArray();
                        break;

                    case DictionaryRepresentation.ArrayOfDocuments:
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
                        break;

                    default:
                        var message = string.Format("'{0}' is not a valid IDictionary<{1}, {2}> representation.",
                            dictionaryRepresentation,
                            BsonUtils.GetFriendlyTypeName(typeof(TKey)),
                            BsonUtils.GetFriendlyTypeName(typeof(TValue)));
                        throw new BsonSerializationException(message);
                }
            }
        }

        // protected methods
        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <returns>The instance.</returns>
        protected abstract TDictionary CreateInstance();

        // private methods
        private TKey DeserializeKeyString(string keyString)
        {
            var keyDocument = new BsonDocument("k", keyString);
            using (var keyReader = BsonReader.Create(keyDocument))
            {
                var context = BsonDeserializationContext.CreateRoot<BsonDocument>(keyReader);
                keyReader.ReadStartDocument();
                keyReader.ReadName("k");
                var key = context.DeserializeWithChildContext(_keySerializer);
                keyReader.ReadEndDocument();
                return key;
            }
        }

        private DictionaryRepresentation DetermineDictionaryRepresentation(TDictionary value)
        {
            foreach (var key in value.Keys)
            {
                var serializedKey = SerializeKey(key);
                if (serializedKey == null || !serializedKey.IsString)
                {
                    return DictionaryRepresentation.ArrayOfArrays;
                }

                var name = serializedKey.AsString;
                if (name == "" || (name.Length > 0 && name[0] == '$') || name.IndexOf('.') != -1 || name.IndexOf('\0') != -1)
                {
                    return DictionaryRepresentation.ArrayOfArrays;
                }
            }

            return DictionaryRepresentation.Document;
        }

        private BsonValue SerializeKey(TKey key)
        {
            var keyDocument = new BsonDocument();
            using (var keyWriter = BsonWriter.Create(keyDocument))
            {
                var context = BsonSerializationContext.CreateRoot<BsonDocument>(keyWriter);
                keyWriter.WriteStartDocument();
                keyWriter.WriteName("k");
                context.SerializeWithChildContext(_keySerializer, key);
                keyWriter.WriteEndDocument();
            }

            return keyDocument["k"];
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
