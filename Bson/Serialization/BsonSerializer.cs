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
using System.IO;
using System.Linq;
using System.Text;

// don't add using statement for MongoDB.Bson.DefaultSerializer to minimize dependencies on DefaultSerializer
using MongoDB.Bson.IO;

namespace MongoDB.Bson.Serialization {
    public static class BsonSerializer {
        #region private static fields
        private static object staticLock = new object();
        private static Dictionary<Type, IIdGenerator> idGenerators = new Dictionary<Type, IIdGenerator>();
        private static Dictionary<Type, IBsonSerializer> serializers = new Dictionary<Type, IBsonSerializer>();
        private static Dictionary<Type, Type> genericSerializerDefinitions = new Dictionary<Type, Type>();
        private static IBsonSerializationProvider serializationProvider = null;
        #endregion

        #region static constructor
        static BsonSerializer() {
            RegisterIdGenerators();
        }
        #endregion

        #region public static properties
        public static IBsonSerializationProvider SerializationProvider {
            get { return serializationProvider; }
            set {
                if (serializationProvider != null) {
                    throw new BsonSerializationException("SerializationProvider has already been set");
                }
                serializationProvider = value;
            }
        }
        #endregion

        #region public static methods
        public static T Deserialize<T>(
            BsonDocument document
        ) {
            return (T) Deserialize(document, typeof(T));
        }

        public static T Deserialize<T>(
            JsonBuffer buffer
        ) {
            return (T) Deserialize(buffer, typeof(T));
        }

        public static T Deserialize<T>(
            BsonReader bsonReader
        ) {
            return (T) Deserialize(bsonReader, typeof(T));
        }

        public static T Deserialize<T>(
            byte[] bytes
        ) {
            return (T) Deserialize(bytes, typeof(T));
        }

        public static T Deserialize<T>(
            Stream stream
        ) {
            return (T) Deserialize(stream, typeof(T));
        }

        public static T Deserialize<T>(
            string json
        ) {
            return (T) Deserialize(json, typeof(T));
        }

        public static T Deserialize<T>(
            TextReader textReader
        ) {
            return (T) Deserialize(textReader, typeof(T));
        }

        public static object Deserialize(
            BsonDocument document,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(document)) {
                return Deserialize(bsonReader, nominalType);
            }
        }

        public static object Deserialize(
            JsonBuffer buffer,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(buffer)) {
                return Deserialize(bsonReader, nominalType);
            }
        }

        public static object Deserialize(
            BsonReader bsonReader,
            Type nominalType
        ) {
            if (nominalType == typeof(BsonDocument)) {
                return BsonDocument.ReadFrom(bsonReader);
            }

            var serializer = LookupSerializer(nominalType);
            return serializer.Deserialize(bsonReader, nominalType, null);
        }

        public static object Deserialize(
            byte[] bytes,
            Type nominalType
        ) {
            using (var memoryStream = new MemoryStream(bytes)) {
                return Deserialize(memoryStream, nominalType);
            }
        }

        public static object Deserialize(
            Stream stream,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(stream)) {
                return Deserialize(bsonReader, nominalType);
            }
        }

        public static object Deserialize(
            string json,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(json)) {
                return Deserialize(bsonReader, nominalType);
            }
        }

        public static object Deserialize(
            TextReader textReader,
            Type nominalType
        ) {
            using (var bsonReader = BsonReader.Create(textReader)) {
                return Deserialize(bsonReader, nominalType);
            }
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

        public static IIdGenerator LookupIdGenerator(
            Type type
        ) {
            lock (staticLock) {
                IIdGenerator idGenerator;
                if (!idGenerators.TryGetValue(type, out idGenerator)) {
                    if (type.IsValueType) {
                        var iEquatableDefinition = typeof(IEquatable<>);
                        var iEquatableType = iEquatableDefinition.MakeGenericType(type);
                        if (iEquatableType.IsAssignableFrom(type)) {
                            var zeroIdCheckerDefinition = typeof(ZeroIdChecker<>);
                            var zeroIdCheckerType = zeroIdCheckerDefinition.MakeGenericType(type);
                            idGenerator = (IIdGenerator) Activator.CreateInstance(zeroIdCheckerType);
                        }
                    } else {
                        idGenerator = NullIdChecker.Instance;
                    }

                    idGenerators[type] = idGenerator; // remember it even if it's null
                }

                return idGenerator;
            }
        }

        public static IBsonSerializer LookupSerializer(
            Type type
        ) {
            lock (staticLock) {
                // initialize the serialization provider before calling serializers.TryGetValue
                // so the serialization provider gets a chance to register the serializers first
                if (serializationProvider == null) {
                    InitializeSerializationProvider();
                }

                IBsonSerializer serializer;
                if (!serializers.TryGetValue(type, out serializer)) {
                    // special case for IBsonSerializable
                    if (serializer == null && typeof(IBsonSerializable).IsAssignableFrom(type)) {
                        serializer = DefaultSerializer.BsonIBsonSerializableSerializer.Instance;
                    }

                    if (serializer == null && type.IsGenericType) {
                        var genericTypeDefinition = type.GetGenericTypeDefinition();
                        var genericSerializerDefinition = LookupGenericSerializerDefinition(genericTypeDefinition);
                        if (genericSerializerDefinition != null) {
                            var genericSerializerType = genericSerializerDefinition.MakeGenericType(type.GetGenericArguments());
                            serializer = (IBsonSerializer) Activator.CreateInstance(genericSerializerType);
                        }
                    }

                    if (serializer == null) {
                        serializer = serializationProvider.GetSerializer(type);
                    }

                    if (serializer == null) {
                        var message = string.Format("No serializer found for type: {0}", type.FullName);
                        throw new BsonSerializationException(message);
                    }

                    serializers[type] = serializer;
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
            IIdGenerator idGenerator
        ) {
            lock (staticLock) {
                idGenerators[type] = idGenerator;
            }
        }

        public static void RegisterSerializer(
            Type type,
            IBsonSerializer serializer
        ) {
            lock (staticLock) {
                serializers[type] = serializer;
            }
        }

        public static void Serialize<T>(
            BsonWriter bsonWriter,
            T value
        ) {
            Serialize(bsonWriter, value, null);
        }

        public static void Serialize<T>(
            BsonWriter bsonWriter,
            T value,
            IBsonSerializationOptions options
        ) {
            Serialize(bsonWriter, typeof(T), value, options);
        }

        public static void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value
        ) {
            Serialize(bsonWriter, nominalType, value, null);
        }

        public static void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options
        ) {
            var bsonSerializable = value as IBsonSerializable;
            if (bsonSerializable != null) {
                bsonSerializable.Serialize(bsonWriter, nominalType, options);
                return;
            }

            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = LookupSerializer(actualType);
            serializer.Serialize(bsonWriter, nominalType, value, options);
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
            lock (staticLock) {
                serializers.Remove(type);
            }
        }
        #endregion

        #region private static methods
        private static void InitializeSerializationProvider() {
            lock (staticLock) {
                // repeat the test for null but this time while holding the staticLock
                if (serializationProvider == null) {
                    DefaultSerializer.BsonDefaultSerializer.Initialize();
                    serializationProvider = DefaultSerializer.BsonDefaultSerializer.Instance;
                }
            }
        }

        private static void RegisterIdGenerators() {
            BsonSerializer.RegisterIdGenerator(typeof(Guid), GuidGenerator.Instance);
            BsonSerializer.RegisterIdGenerator(typeof(ObjectId), ObjectIdGenerator.Instance);
        }
        #endregion
    }
}
