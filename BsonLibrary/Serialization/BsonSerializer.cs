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

// don't add using statement for MongoDB.BsonLibrary.DefaultSerializer to minimize dependencies on DefaultSerializer
using MongoDB.BsonLibrary.IO;

namespace MongoDB.BsonLibrary.Serialization {
    public static class BsonSerializer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, IBsonSerializer> registry = new Dictionary<Type, IBsonSerializer>();
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

        public static IBsonSerializer LookupSerializer(
            Type type
        ) {
            lock (staticLock) {
                IBsonSerializer serializer;
                if (!registry.TryGetValue(type, out serializer)) {
                    // special case for IBsonSerializable
                    if (serializer == null && typeof(IBsonSerializable).IsAssignableFrom(type)) {
                        serializer = DefaultSerializer.BsonIBsonSerializableSerializer.Singleton;
                    }

                    if (serializer == null && type.IsGenericType) {
                        var genericType = type.GetGenericTypeDefinition();
                        registry.TryGetValue(genericType, out serializer);
                    }

                    if (serializer == null) {
                        if (serializationProvider == null) {
                            // if no other serialization provider has been configured by now use the default one
                            DefaultSerializer.BsonDefaultSerializationProvider.Initialize();
                            serializationProvider = DefaultSerializer.BsonDefaultSerializationProvider.Singleton;
                        }
                        serializer = serializationProvider.GetSerializer(type);
                    }

                    registry[type] = serializer;
                }

                return serializer;
            }
        }

        public static void RegisterSerializer(
            Type type,
            IBsonSerializer serializer
        ) {
            lock (staticLock) {
                registry[type] = serializer;
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
            T value,
            bool useCompactRepresentation
        ) {
            SerializeElement(bsonWriter, typeof(T), name, value, useCompactRepresentation);
        }

        public static void SerializeElement(
            BsonWriter bsonWriter,
            Type nominalType,
            string name,
            object value,
            bool useCompactRepresentation
        ) {
            var serializer = LookupSerializer(value == null ? nominalType : value.GetType());
            serializer.SerializeElement(bsonWriter, nominalType, name, value, useCompactRepresentation);
        }

        public static void UnregisterSerializer(
            Type type
        ) {
            lock (staticLock) {
                registry.Remove(type);
            }
        }
        #endregion
    }
}
