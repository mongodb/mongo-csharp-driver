/* Copyright 2010 10gen Inc.
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

// don't add using statement for MongoDB.Bson.DefaultSerializer to minimize dependencies on DefaultSerializer
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization {
    public static class BsonSerializer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, IBsonIdGenerator> idGenerators = new Dictionary<Type, IBsonIdGenerator>();
        private static Dictionary<SerializerKey, IBsonSerializer> serializers = new Dictionary<SerializerKey, IBsonSerializer>();
        private static Dictionary<Type, Type> genericSerializerDefinitions = new Dictionary<Type, Type>();
        private static IBsonSerializationProvider serializationProvider = null;
        #endregion

        #region public static properties
        public static IBsonSerializationProvider SerializationProvider {
            get { return serializationProvider; }
            set { serializationProvider = value; }
        }
        #endregion

        #region public static methods
        public static T DeserializeDocument<T>(
            BsonReader bsonReader
        ) {
            return (T) DeserializeDocument(bsonReader, typeof(T));
        }

        public static T DeserializeDocument<T>(
            byte[] bytes
        ) {
            return (T) DeserializeDocument(bytes, typeof(T));
        }

        public static T DeserializeDocument<T>(
            Stream stream
        ) {
            return (T) DeserializeDocument(stream, typeof(T));
        }

        public static object DeserializeDocument(
            BsonReader bsonReader,
            Type nominalType
        ) {
            if (nominalType == typeof(BsonDocument)) {
                return BsonDocument.ReadFrom(bsonReader);
            }

            var serializer = LookupSerializer(nominalType);
            return serializer.DeserializeDocument(bsonReader, nominalType);
        }

        public static object DeserializeDocument(
            byte[] bytes,
            Type nominalType
        ) {
            using (var memoryStream = new MemoryStream(bytes)) {
                return DeserializeDocument(memoryStream, nominalType);
            }
        }

        public static object DeserializeDocument(
            Stream stream,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(stream)) {
                return DeserializeDocument(bsonReader, nominalType);
            }
        }

        public static T DeserializeElement<T>(
            BsonReader bsonReader,
            out string name
        ) {
            return (T) DeserializeElement(bsonReader, typeof(T), out name);
        }

        public static object DeserializeElement(
            BsonReader bsonReader,
            Type nominalType,
            out string name
        ) {
            var serializer = LookupSerializer(nominalType);
            return serializer.DeserializeElement(bsonReader, nominalType, out name);
        }

        public static Type LookupGenericSerializerDefinition(
            Type genericTypeDefinition
        ) {
            lock (staticLock) {
                Type genericSerializerDefinition;
                genericSerializerDefinitions.TryGetValue(genericTypeDefinition, out genericSerializerDefinition);
                return genericSerializerDefinition;
            }
        }

        public static IBsonIdGenerator LookupIdGenerator(
            Type type
        ) {
            lock (staticLock) {
                IBsonIdGenerator idGenerator;
                if (!idGenerators.TryGetValue(type, out idGenerator)) {
                    if (idGenerator == null) {
                        if (serializationProvider == null) {
                            serializationProvider = GetDefaultSerializationProvider();
                        }
                        idGenerator = serializationProvider.GetIdGenerator(type);
                    }

                    if (idGenerator == null) {
                        var message = string.Format("No idGenerator found for type: {0}", type.FullName);
                        throw new BsonSerializationException(message);
                    }

                    idGenerators[type] = idGenerator;
                }

                return idGenerator;
            }
        }

        public static IBsonSerializer LookupSerializer(
            Type type
        ) {
            return LookupSerializer(type, null);
        }

        public static IBsonSerializer LookupSerializer(
            Type type,
            object serializationOptions
        ) {
            lock (staticLock) {
                var key = new SerializerKey(type, serializationOptions);
                IBsonSerializer serializer;
                if (!serializers.TryGetValue(key, out serializer)) {
                    // special case for IBsonSerializable
                    if (serializer == null && typeof(IBsonSerializable).IsAssignableFrom(type)) {
                        serializer = DefaultSerializer.BsonIBsonSerializableSerializer.Singleton;
                    }

                    if (serializer == null && type.IsGenericType) {
                        var genericTypeDefinition = type.GetGenericTypeDefinition();
                        var genericSerializerDefinition = LookupGenericSerializerDefinition(genericTypeDefinition);
                        if (genericSerializerDefinition != null) {
                            var genericSerializerType = genericSerializerDefinition.MakeGenericType(type.GetGenericArguments());
                            serializer = (IBsonSerializer) Activator.CreateInstance(genericSerializerType, serializationOptions);
                        }
                    }

                    if (serializer == null) {
                        if (serializationProvider == null) {
                            serializationProvider = GetDefaultSerializationProvider();
                        }
                        serializer = serializationProvider.GetSerializer(type, serializationOptions);
                    }

                    if (serializer == null) {
                        string message;
                        if (serializationOptions == null) {
                            message = string.Format("No serializer found for type: {0}", type.FullName);
                        } else {
                            message = string.Format("No serializer found for type: {0} (options: {1})", type.FullName, serializationOptions);
                        }
                        throw new BsonSerializationException(message);
                    }

                    serializers[key] = serializer;
                }

                return serializer;
            }
        }

        public static void RegisterGenericSerializerDefinition(
            Type genericTypeDefinition,
            Type genericSerializerDefinition
        ) {
            lock (staticLock) {
                genericSerializerDefinitions[genericTypeDefinition] = genericSerializerDefinition;
            }
        }

        public static void RegisterIdGenerator(
            Type type,
            IBsonIdGenerator idGenerator
        ) {
            lock (staticLock) {
                idGenerators[type] = idGenerator;
            }
        }

        public static void RegisterSerializer(
            Type type,
            IBsonSerializer serializer
        ) {
            RegisterSerializer(type, null, serializer);
        }

        public static void RegisterSerializer(
            Type type,
            object serializationOptions,
            IBsonSerializer serializer
        ) {
            lock (staticLock) {
                var key = new SerializerKey(type, serializationOptions);
                serializers[key] = serializer;
            }
        }

        public static void SerializeDocument<T>(
            BsonWriter bsonWriter,
            T document
        ) {
            SerializeDocument(bsonWriter, document, false);
        }

        public static void SerializeDocument<T>(
            BsonWriter bsonWriter,
            T document,
            bool serializeIdFirst
        ) {
            SerializeDocument(bsonWriter, typeof(T), document, serializeIdFirst);
        }

        public static void SerializeDocument(
            BsonWriter bsonWriter,
            Type nominalType,
            object document,
            bool serializeIdFirst
        ) {
            var bsonSerializable = document as IBsonSerializable;
            if (bsonSerializable != null) {
                bsonSerializable.SerializeDocument(bsonWriter, nominalType, serializeIdFirst);
                return;
            }

            var serializer = LookupSerializer(document.GetType());
            serializer.SerializeDocument(bsonWriter, nominalType, document, serializeIdFirst);
        }

        public static void SerializeElement<T>(
            BsonWriter bsonWriter,
            string name,
            T value
        ) {
            SerializeElement(bsonWriter, typeof(T), name, value);
        }

        public static void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value
        ) {
            var serializer = LookupSerializer(value == null ? nominalType : value.GetType());
            serializer.SerializeElement(bsonWriter, nominalType, name, value);
        }

        public static void UnregisterGenericSerializerDefinition(
            Type genericTypeDefinition
        ) {
            lock (staticLock) {
                genericSerializerDefinitions.Remove(genericTypeDefinition);
            }
        }

        public static void UnregisterIdGenerator(
            Type type
        ) {
            lock (staticLock) {
                idGenerators.Remove(type);
            }
        }

        public static void UnregisterSerializer(
            Type type
        ) {
            UnregisterSerializer(type, null);
        }

        public static void UnregisterSerializer(
            Type type,
            object serializationOptions
        ) {
            lock (staticLock) {
                var key = new SerializerKey(type, serializationOptions);
                serializers.Remove(key);
            }
        }
        #endregion

        #region private static methods
        private static IBsonSerializationProvider GetDefaultSerializationProvider() {
            DefaultSerializer.BsonDefaultSerializer.Initialize();
            return DefaultSerializer.BsonDefaultSerializer.Singleton;
        }
        #endregion

        #region nested classes
        private struct SerializerKey {
            private Type type;
            private object serializationOptions;

            public SerializerKey(
                Type type,
                object serializationOptions
            ) {
                this.type = type;
                this.serializationOptions = serializationOptions;
            }

            public override bool Equals(
                object obj
            ) {
                if (obj == null || obj.GetType() != typeof(SerializerKey)) {
                    return false;
                }
                var other = (SerializerKey) obj;
                return this.type == other.type && object.Equals(this.serializationOptions, other.serializationOptions);
            }

            public override int GetHashCode() {
                if (serializationOptions == null) {
                    return type.GetHashCode();
                } else {
                    // see Effective Java by Joshua Bloch
                    int hash = 17;
                    hash = 37 * hash + type.GetHashCode();
                    hash = 37 * hash + serializationOptions.GetHashCode();
                    return hash;
                }
            }
        }
        #endregion
    }
}
