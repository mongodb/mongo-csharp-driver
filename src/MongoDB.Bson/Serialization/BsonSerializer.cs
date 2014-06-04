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
using System.Threading;

// don't add using statement for MongoDB.Bson.Serialization.Serializers to minimize dependencies on DefaultSerializer
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

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
        private static Dictionary<Type, IDiscriminatorConvention> __discriminatorConventions = new Dictionary<Type, IDiscriminatorConvention>();
        private static Dictionary<BsonValue, HashSet<Type>> __discriminators = new Dictionary<BsonValue, HashSet<Type>>();
        private static HashSet<Type> __discriminatedTypes = new HashSet<Type>();
        private static HashSet<Type> __typesWithRegisteredKnownTypes = new HashSet<Type>();

        private static bool __useNullIdChecker = false;
        private static bool __useZeroIdChecker = false;

        // static constructor
        static BsonSerializer()
        {
            RegisterDefaultSerializationProviders();
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
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(BsonDocument document, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new BsonDocumentReader(document))
            {
                return Deserialize<TNominalType>(bsonReader, configurator);
            }
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(BsonReader bsonReader, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            var serializer = LookupSerializer<TNominalType>();
            var context = BsonDeserializationContext.CreateRoot<TNominalType>(bsonReader, configurator);
            return serializer.Deserialize(context);
        }

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bytes">The BSON byte array.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(byte[] bytes, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var memoryStream = new BsonMemoryStream(bytes))
            {
                return Deserialize<TNominalType>(memoryStream, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(Stream stream, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new BsonBinaryReader(stream))
            {
                return Deserialize<TNominalType>(bsonReader, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(string json, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new JsonReader(json))
            {
                return Deserialize<TNominalType>(bsonReader, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static TNominalType Deserialize<TNominalType>(TextReader textReader, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new JsonReader(textReader))
            {
                return Deserialize<TNominalType>(bsonReader, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a BsonDocument.
        /// </summary>
        /// <param name="document">The BsonDocument.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A TNominalType.</returns>
        public static object Deserialize(BsonDocument document, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new BsonDocumentReader(document))
            {
                return Deserialize(bsonReader, nominalType, configurator);
            }
        }

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(BsonReader bsonReader, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            var serializer = LookupSerializer(nominalType);
            var context = BsonDeserializationContext.CreateRoot(bsonReader, nominalType, configurator);
            return serializer.Deserialize(context);
        }

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <param name="bytes">The BSON byte array.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(byte[] bytes, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var memoryStream = new BsonMemoryStream(bytes))
            {
                return Deserialize(memoryStream, nominalType, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(Stream stream, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new BsonBinaryReader(stream))
            {
                return Deserialize(bsonReader, nominalType, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(string json, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new JsonReader(json))
            {
                return Deserialize(bsonReader, nominalType, configurator);
            }
        }

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>An object.</returns>
        public static object Deserialize(TextReader textReader, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
        {
            using (var bsonReader = new JsonReader(textReader))
            {
                return Deserialize(bsonReader, nominalType, configurator);
            }
        }

        /// <summary>
        /// Returns whether the given type has any discriminators registered for any of its subclasses.
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the type is discriminated.</returns>
        public static bool IsTypeDiscriminated(Type type)
        {
            return type.IsInterface || __discriminatedTypes.Contains(type);
        }

        /// <summary>
        /// Looks up the actual type of an object to be deserialized.
        /// </summary>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="discriminator">The discriminator.</param>
        /// <returns>The actual type of the object.</returns>
        public static Type LookupActualType(Type nominalType, BsonValue discriminator)
        {
            if (discriminator == null)
            {
                return nominalType;
            }

            // note: EnsureKnownTypesAreRegistered handles its own locking so call from outside any lock
            EnsureKnownTypesAreRegistered(nominalType);

            __configLock.EnterReadLock();
            try
            {
                Type actualType = null;

                HashSet<Type> hashSet;
                if (__discriminators.TryGetValue(discriminator, out hashSet))
                {
                    foreach (var type in hashSet)
                    {
                        if (nominalType.IsAssignableFrom(type))
                        {
                            if (actualType == null)
                            {
                                actualType = type;
                            }
                            else
                            {
                                string message = string.Format("Ambiguous discriminator '{0}'.", discriminator);
                                throw new BsonSerializationException(message);
                            }
                        }
                    }
                }

                if (actualType == null && discriminator.IsString)
                {
                    actualType = TypeNameDiscriminator.GetActualType(discriminator.AsString); // see if it's a Type name
                }

                if (actualType == null)
                {
                    string message = string.Format("Unknown discriminator value '{0}'.", discriminator);
                    throw new BsonSerializationException(message);
                }

                if (!nominalType.IsAssignableFrom(actualType))
                {
                    string message = string.Format(
                        "Actual type {0} is not assignable to expected type {1}.",
                        actualType.FullName, nominalType.FullName);
                    throw new BsonSerializationException(message);
                }

                return actualType;
            }
            finally
            {
                __configLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Looks up the discriminator convention for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A discriminator convention.</returns>
        public static IDiscriminatorConvention LookupDiscriminatorConvention(Type type)
        {
            __configLock.EnterReadLock();
            try
            {
                IDiscriminatorConvention convention;
                if (__discriminatorConventions.TryGetValue(type, out convention))
                {
                    return convention;
                }
            }
            finally
            {
                __configLock.ExitReadLock();
            }

            __configLock.EnterWriteLock();
            try
            {
                IDiscriminatorConvention convention;
                if (!__discriminatorConventions.TryGetValue(type, out convention))
                {
                    if (type == typeof(object))
                    {
                        // if there is no convention registered for object register the default one
                        convention = new ObjectDiscriminatorConvention("_t");
                        RegisterDiscriminatorConvention(typeof(object), convention);
                    }
                    else if (type.IsInterface)
                    {
                        // TODO: should convention for interfaces be inherited from parent interfaces?
                        convention = LookupDiscriminatorConvention(typeof(object));
                        RegisterDiscriminatorConvention(type, convention);
                    }
                    else
                    {
                        // inherit the discriminator convention from the closest parent (that isn't object) that has one
                        // otherwise default to the standard hierarchical convention
                        Type parentType = type.BaseType;
                        while (convention == null)
                        {
                            if (parentType == typeof(object))
                            {
                                convention = StandardDiscriminatorConvention.Hierarchical;
                                break;
                            }
                            if (__discriminatorConventions.TryGetValue(parentType, out convention))
                            {
                                break;
                            }
                            parentType = parentType.BaseType;
                        }

                        // register this convention for all types between this and the parent type where we found the convention
                        var unregisteredType = type;
                        while (unregisteredType != parentType)
                        {
                            RegisterDiscriminatorConvention(unregisteredType, convention);
                            unregisteredType = unregisteredType.BaseType;
                        }
                    }
                }

                return convention;
            }
            finally
            {
                __configLock.ExitWriteLock();
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
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>A serializer for type T.</returns>
        public static IBsonSerializer<T> LookupSerializer<T>()
        {
            return (IBsonSerializer<T>)LookupSerializer(typeof(T));
        }

        /// <summary>
        /// Looks up a serializer for a Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <returns>A serializer for the Type.</returns>
        public static IBsonSerializer LookupSerializer(Type type)
        {
            // since we don't allow registering serializers for BsonDocument no lookup is needed
            if (type == typeof(BsonDocument))
            {
                return BsonDocumentSerializer.Instance;
            }

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
                    if (serializer == null)
                    {
                        var serializerAttributes = type.GetCustomAttributes(typeof(BsonSerializerAttribute), false); // don't inherit
                        if (serializerAttributes.Length == 1)
                        {
                            var serializerAttribute = (BsonSerializerAttribute)serializerAttributes[0];
                            serializer = serializerAttribute.CreateSerializer(type);
                        }
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
        /// Registers the discriminator for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="discriminator">The discriminator.</param>
        public static void RegisterDiscriminator(Type type, BsonValue discriminator)
        {
            if (type.IsInterface)
            {
                var message = string.Format("Discriminators can only be registered for classes, not for interface {0}.", type.FullName);
                throw new BsonSerializationException(message);
            }

            __configLock.EnterWriteLock();
            try
            {
                HashSet<Type> hashSet;
                if (!__discriminators.TryGetValue(discriminator, out hashSet))
                {
                    hashSet = new HashSet<Type>();
                    __discriminators.Add(discriminator, hashSet);
                }

                if (!hashSet.Contains(type))
                {
                    hashSet.Add(type);

                    // mark all base types as discriminated (so we know that it's worth reading a discriminator)
                    for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
                    {
                        __discriminatedTypes.Add(baseType);
                    }
                }
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers the discriminator convention for a type.
        /// </summary>
        /// <param name="type">Type type.</param>
        /// <param name="convention">The discriminator convention.</param>
        public static void RegisterDiscriminatorConvention(Type type, IDiscriminatorConvention convention)
        {
            __configLock.EnterWriteLock();
            try
            {
                if (!__discriminatorConventions.ContainsKey(type))
                {
                    __discriminatorConventions.Add(type, convention);
                }
                else
                {
                    var message = string.Format("There is already a discriminator convention registered for type {0}.", type.FullName);
                    throw new BsonSerializationException(message);
                }
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
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="serializer">The serializer.</param>
        public static void RegisterSerializer<T>(IBsonSerializer<T> serializer)
        {
            RegisterSerializer(typeof(T), serializer);
        }

        /// <summary>
        /// Registers a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializer">The serializer.</param>
        public static void RegisterSerializer(Type type, IBsonSerializer serializer)
        {
            // don't allow a serializer to be registered for subclasses of BsonValue
            if (typeof(BsonValue).IsAssignableFrom(type))
            {
                var message = string.Format("A serializer cannot be registered for type {0} because it is a subclass of BsonValue.", BsonUtils.GetFriendlyTypeName(type));
                throw new BsonSerializationException(message);
            }

            __configLock.EnterWriteLock();
            try
            {
                if (__serializers.ContainsKey(type))
                {
                    var message = string.Format("There is already a serializer registered for type {0}.", type.FullName);
                    throw new BsonSerializationException(message);
                }
                __serializers.Add(type, serializer);
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        public static void Serialize<TNominalType>(
            BsonWriter bsonWriter,
            TNominalType value,
            Action<BsonSerializationContext.Builder> configurator = null)
        {
            var serializer = LookupSerializer<TNominalType>();
            var context = BsonSerializationContext.CreateRoot<TNominalType>(bsonWriter, configurator);
            serializer.Serialize(context, value);
        }

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="value">The object.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        public static void Serialize(
            BsonWriter bsonWriter,
            Type nominalType,
            object value,
            Action<BsonSerializationContext.Builder> configurator = null)
        {
            var serializer = LookupSerializer(nominalType);
            var context = BsonSerializationContext.CreateRoot(bsonWriter, nominalType, configurator);
            serializer.Serialize(context, value);
        }

        // internal static methods
        internal static void EnsureKnownTypesAreRegistered(Type nominalType)
        {
            __configLock.EnterReadLock();
            try
            {
                if (__typesWithRegisteredKnownTypes.Contains(nominalType))
                {
                    return;
                }
            }
            finally
            {
                __configLock.ExitReadLock();
            }

            __configLock.EnterWriteLock();
            try
            {
                if (!__typesWithRegisteredKnownTypes.Contains(nominalType))
                {
                    // only call LookupClassMap for classes with a BsonKnownTypesAttribute
                    var knownTypesAttribute = nominalType.GetCustomAttributes(typeof(BsonKnownTypesAttribute), false);
                    if (knownTypesAttribute != null && knownTypesAttribute.Length > 0)
                    {
                        // try and force a scan of the known types
                        LookupSerializer(nominalType);
                    }

                    __typesWithRegisteredKnownTypes.Add(nominalType);
                }
            }
            finally
            {
                __configLock.ExitWriteLock();
            }
        }

        // private static methods
        private static void RegisterDefaultSerializationProviders()
        {
            // last one registered gets first chance at providing the serializer
            RegisterSerializationProvider(new BsonClassMapSerializationProvider());
            RegisterSerializationProvider(new BsonDefaultSerializationProvider());
        }

        private static void RegisterIdGenerators()
        {
            RegisterIdGenerator(typeof(BsonObjectId), BsonObjectIdGenerator.Instance);
            RegisterIdGenerator(typeof(Guid), GuidGenerator.Instance);
            RegisterIdGenerator(typeof(ObjectId), ObjectIdGenerator.Instance);
        }
    }
}
