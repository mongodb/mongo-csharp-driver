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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

// don't add using statement for MongoDB.Bson.Serialization.Serializers to minimize dependencies on DefaultSerializer
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// A static class that represents the BSON serialization functionality.
    /// </summary>
    public static class BsonSerializer
    {
        // private static fields
        private static ReaderWriterLockSlim __configLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static Dictionary<Type, IIdGenerator> __idGenerators = new Dictionary<Type, IIdGenerator>();
        private static Dictionary<Type, IBsonSerializer> __serializers = new Dictionary<Type, IBsonSerializer>();
        private static Dictionary<Type, Type> __genericSerializerDefinitions = new Dictionary<Type, Type>();
        private static List<IBsonSerializationProvider> __serializationProviders = new List<IBsonSerializationProvider>();
        private static bool __useNullIdChecker = false;
        private static bool __useZeroIdChecker = false;

        // static constructor
        static BsonSerializer()
        {
            RegisterDefaultSerializationProvider();
            RegisterIdGenerators();
        }

        // public static properties
        /// <summary>
        /// Gets or sets whether to use the NullIdChecker on reference Id types that don't have an IdGenerator registered.
        /// </summary>
        public static bool UseNullIdChecker
        {
            get { return __useNullIdChecker; }
            set { __useNullIdChecker = value; }
        }

        /// <summary>
        /// Gets or sets whether to use the ZeroIdChecker on value Id types that don't have an IdGenerator registered.
        /// </summary>
        public static bool UseZeroIdChecker
        {
            get { return __useZeroIdChecker; }
            set { __useZeroIdChecker = value; }
        }

        // internal static properties
        internal static ReaderWriterLockSlim ConfigLock
        {
            get { return __configLock; }
        }

        // public static methods
        /// <summary>
        /// Deserializes an object from a BsonDocument.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="document">The BsonDocument.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(BsonDocument document)
        {
            return (TNominalType)Deserialize(document, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a JsonBuffer.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="buffer">The JsonBuffer.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(JsonBuffer buffer)
        {
            return (TNominalType)Deserialize(buffer, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(BsonReader bsonReader)
        {
            return (TNominalType)Deserialize(bsonReader, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bytes">The BSON byte array.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(byte[] bytes)
        {
            return (TNominalType)Deserialize(bytes, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="stream">The BSON Stream.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(Stream stream)
        {
            return (TNominalType)Deserialize(stream, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(string json)
        {
            return (TNominalType)Deserialize(json, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(TextReader textReader)
        {
            return (TNominalType)Deserialize(textReader, typeof(TNominalType));
        }

        /// <summary>
        /// Deserializes an object from a BsonDocument.
        /// </summary>
        /// <param name="document">The BsonDocument.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>A TNominalType.</returns>
        public static object Deserialize(BsonDocument document, Type nominalType)
        {
            using (var bsonReader = BsonReader.Create(document))
            {
                return Deserialize(bsonReader, nominalType);
            }
        }

        /// <summary>
        /// Deserializes an object from a JsonBuffer.
        /// </summary>
        /// <param name="buffer">The JsonBuffer.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(JsonBuffer buffer, Type nominalType)
        {
            using (var bsonReader = BsonReader.Create(buffer))
            {
                return Deserialize(bsonReader, nominalType);
            }
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(BsonReader bsonReader, Type nominalType)
        {
            return Deserialize(bsonReader, nominalType, null);
        }

        /// <summary>
        /// Deserializes an object from a BsonReader.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="options">The serialization options.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (nominalType == typeof(BsonDocument))
            {
                var bsonDocument = new BsonDocument();
                return bsonDocument.Deserialize(bsonReader, nominalType, options);
            }

            // if nominalType is an interface find out the actualType and use it instead
            if (nominalType.IsInterface)
            {
                var discriminatorConvention = BsonDefaultSerializer.LookupDiscriminatorConvention(nominalType);
                var actualType = discriminatorConvention.GetActualType(bsonReader, nominalType);
                if (actualType == nominalType)
                {
                    var message = string.Format("Unable to determine actual type of object to deserialize. NominalType is the interface {0}.", nominalType);
                    throw new FileFormatException(message);
                }
                var serializer = LookupSerializer(actualType);
                return serializer.Deserialize(bsonReader, actualType, options);
            }
            else
            {
                var serializer = LookupSerializer(nominalType);
                return serializer.Deserialize(bsonReader, nominalType, options);
            }
        }

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <param name="bytes">The BSON byte array.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(byte[] bytes, Type nominalType)
        {
            using (var memoryStream = new MemoryStream(bytes))
            {
                return Deserialize(memoryStream, nominalType);
            }
        }

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(Stream stream, Type nominalType)
        {
            using (var bsonReader = BsonReader.Create(stream))
            {
                return Deserialize(bsonReader, nominalType);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(string json, Type nominalType)
        {
            using (var bsonReader = BsonReader.Create(json))
            {
                return Deserialize(bsonReader, nominalType);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(TextReader textReader, Type nominalType)
        {
            using (var bsonReader = BsonReader.Create(textReader))
            {
                return Deserialize(bsonReader, nominalType);
            }
        }

        /// <summary>
        /// Looks up a generic serializer definition.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type.</param>
        /// <returns>A generic serializer definition.</returns>
        public static Type LookupGenericSerializerDefinition(Type genericTypeDefinition)
        {
            __configLock.EnterReadLock();
            try
            {
                Type genericSerializerDefinition;
                __genericSerializerDefinitions.TryGetValue(genericTypeDefinition, out genericSerializerDefinition);
                return genericSerializerDefinition;
            }
            finally
            {
                __configLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Looks up an IdGenerator.
        /// </summary>
        /// <param name="type">The Id type.</param>
        /// <returns>An IdGenerator for the Id type.</returns>
        public static IIdGenerator LookupIdGenerator(Type type)
        {
            __configLock.EnterReadLock();
            try
            {
                IIdGenerator idGenerator;
                if (__idGenerators.TryGetValue(type, out idGenerator))
                {
                    return idGenerator;
                }
            }
            finally
            {
                __configLock.ExitReadLock();
            }

            __configLock.EnterWriteLock();
            try
            {
                IIdGenerator idGenerator;
                if (!__idGenerators.TryGetValue(type, out idGenerator))
                {
                    if (type.IsValueType && __useZeroIdChecker)
                    {
                        var iEquatableDefinition = typeof(IEquatable<>);
                        var iEquatableType = iEquatableDefinition.MakeGenericType(type);
                        if (iEquatableType.IsAssignableFrom(type))
                        {
                            var zeroIdCheckerDefinition = typeof(ZeroIdChecker<>);
                            var zeroIdCheckerType = zeroIdCheckerDefinition.MakeGenericType(type);
                            idGenerator = (IIdGenerator)Activator.CreateInstance(zeroIdCheckerType);
                        }
                    }
                    else if (__useNullIdChecker)
                    {
                        idGenerator = NullIdChecker.Instance;
                    }
                    else
                    {
                        idGenerator = null;
                    }

                    __idGenerators[type] = idGenerator; // remember it even if it's null
                }

                return idGenerator;
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Looks up a serializer for a Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <returns>A serializer for the Type.</returns>
        public static IBsonSerializer LookupSerializer(Type type)
        {
            __configLock.EnterReadLock();
            try
            {
                IBsonSerializer serializer;
                if (__serializers.TryGetValue(type, out serializer))
                {
                    return serializer;
                }
            }
            finally
            {
                __configLock.ExitReadLock();
            }

            __configLock.EnterWriteLock();
            try
            {
                IBsonSerializer serializer;
                if (!__serializers.TryGetValue(type, out serializer))
                {
                    // special case for IBsonSerializable
                    if (serializer == null && typeof(IBsonSerializable).IsAssignableFrom(type))
                    {
                        serializer = Serializers.BsonIBsonSerializableSerializer.Instance;
                    }

                    if (serializer == null && type.IsGenericType)
                    {
                        var genericTypeDefinition = type.GetGenericTypeDefinition();
                        var genericSerializerDefinition = LookupGenericSerializerDefinition(genericTypeDefinition);
                        if (genericSerializerDefinition != null)
                        {
                            var genericSerializerType = genericSerializerDefinition.MakeGenericType(type.GetGenericArguments());
                            serializer = (IBsonSerializer)Activator.CreateInstance(genericSerializerType);
                        }
                    }

                    if (serializer == null)
                    {
                        foreach (var serializationProvider in __serializationProviders)
                        {
                            serializer = serializationProvider.GetSerializer(type);
                            if (serializer != null)
                            {
                                break;
                            }
                        }
                    }

                    if (serializer == null)
                    {
                        var message = string.Format("No serializer found for type {0}.", type.FullName);
                        throw new BsonSerializationException(message);
                    }

                    __serializers[type] = serializer;
                }

                return serializer;
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a generic serializer definition for a generic type.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type.</param>
        /// <param name="genericSerializerDefinition">The generic serializer definition.</param>
        public static void RegisterGenericSerializerDefinition(
            Type genericTypeDefinition,
            Type genericSerializerDefinition)
        {
            __configLock.EnterWriteLock();
            try
            {
                __genericSerializerDefinitions[genericTypeDefinition] = genericSerializerDefinition;
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers an IdGenerator for an Id Type.
        /// </summary>
        /// <param name="type">The Id Type.</param>
        /// <param name="idGenerator">The IdGenerator for the Id Type.</param>
        public static void RegisterIdGenerator(Type type, IIdGenerator idGenerator)
        {
            __configLock.EnterWriteLock();
            try
            {
                __idGenerators[type] = idGenerator;
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a serialization provider.
        /// </summary>
        /// <param name="provider">The serialization provider.</param>
        public static void RegisterSerializationProvider(IBsonSerializationProvider provider)
        {
            __configLock.EnterWriteLock();
            try
            {
                // add new provider to the front of the list
                __serializationProviders.Insert(0, provider);
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializer">The serializer.</param>
        public static void RegisterSerializer(Type type, IBsonSerializer serializer)
        {
            __configLock.EnterWriteLock();
            try
            {
                __serializers[type] = serializer;
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        public static void Serialize<TNominalType>(BsonWriter bsonWriter, TNominalType value)
        {
            Serialize(bsonWriter, value, null);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public static void Serialize<TNominalType>(
            BsonWriter bsonWriter,
            TNominalType value,
            IBsonSerializationOptions options)
        {
            Serialize(bsonWriter, typeof(TNominalType), value, options);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="value">The object.</param>
        public static void Serialize(BsonWriter bsonWriter, Type nominalType, object value)
        {
            Serialize(bsonWriter, nominalType, value, null);
        }

        /// <summary>
        /// Serializes an object to a BsonWriter.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="value">The object.</param>
        /// <param name="options">The serialization options.</param>
        public static void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            IBsonSerializationOptions options)
        {
            var bsonSerializable = value as IBsonSerializable;
            if (bsonSerializable != null)
            {
                bsonSerializable.Serialize(bsonWriter, nominalType, options);
                return;
            }

            var actualType = (value == null) ? nominalType : value.GetType();
            var serializer = LookupSerializer(actualType);
            serializer.Serialize(bsonWriter, nominalType, value, options);
        }

        // private static methods
        private static void RegisterDefaultSerializationProvider()
        {
            RegisterSerializationProvider(BsonDefaultSerializer.Instance);
        }

        private static void RegisterIdGenerators()
        {
            BsonSerializer.RegisterIdGenerator(typeof(BsonObjectId), BsonObjectIdGenerator.Instance);
            BsonSerializer.RegisterIdGenerator(typeof(Guid), GuidGenerator.Instance);
            BsonSerializer.RegisterIdGenerator(typeof(ObjectId), ObjectIdGenerator.Instance);
        }
    }
}
