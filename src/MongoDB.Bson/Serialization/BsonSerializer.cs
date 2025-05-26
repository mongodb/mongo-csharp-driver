/* Copyright 2010-present MongoDB Inc.
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
using System.IO;
using System.Threading;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Serialization
{
    /// <summary>
    /// A static class that represents the BSON serialization functionality.
    /// </summary>
    public static class BsonSerializer
    {
        private static readonly IBsonSerializationDomainInternal _serializationDomain;

        // static constructor
        static BsonSerializer()
        {
            _serializationDomain = new BsonSerializationDomain("MAIN");
        }

        /// <summary>
        /// //TODO
        /// </summary>
        public static IBsonSerializationDomain DefaultSerializationDomain => _serializationDomain;

        // public static properties
        /// <summary>
        /// Gets the serializer registry.
        /// </summary>
        public static IBsonSerializerRegistry SerializerRegistry => _serializationDomain.SerializerRegistry;

        /// <summary>
        /// Gets or sets whether to use the NullIdChecker on reference Id types that don't have an IdGenerator registered.
        /// </summary>
        public static bool UseNullIdChecker
        {
            get => _serializationDomain.UseNullIdChecker;
            set => _serializationDomain.UseNullIdChecker = value;
        }

        /// <summary>
        /// Gets or sets whether to use the ZeroIdChecker on value Id types that don't have an IdGenerator registered.
        /// </summary>
        public static bool UseZeroIdChecker
        {
            get => _serializationDomain.UseZeroIdChecker;
            set => _serializationDomain.UseZeroIdChecker = value;
        }

        // internal static properties
        internal static ReaderWriterLockSlim ConfigLock => _serializationDomain.ConfigLock;

        // public static methods

        /// <summary>
        /// //TODO
        /// </summary>
        /// <returns></returns>
        public static IBsonSerializationDomain CreateSerializationDomain() => new BsonSerializationDomain();

        /// <summary>
        /// Deserializes an object from a BsonDocument.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="document">The BsonDocument.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(BsonDocument document, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(document, configurator);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(IBsonReader bsonReader, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(bsonReader, configurator);

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bytes">The BSON byte array.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(byte[] bytes, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(bytes, configurator);

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(Stream stream, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(stream, configurator);

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(string json, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(json, configurator);

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static TNominalType Deserialize<TNominalType>(TextReader textReader, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize<TNominalType>(textReader, configurator);

        /// <summary>
        /// Deserializes an object from a BsonDocument.
        /// </summary>
        /// <param name="document">The BsonDocument.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(BsonDocument document, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(document, nominalType, configurator);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="bsonReader">The BsonReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(IBsonReader bsonReader, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(bsonReader, nominalType, configurator);

        /// <summary>
        /// Deserializes an object from a BSON byte array.
        /// </summary>
        /// <param name="bytes">The BSON byte array.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(byte[] bytes, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(bytes, nominalType, configurator);

        /// <summary>
        /// Deserializes an object from a BSON Stream.
        /// </summary>
        /// <param name="stream">The BSON Stream.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(Stream stream, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(stream, nominalType, configurator);

        /// <summary>
        /// Deserializes an object from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(string json, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(json, nominalType, configurator);

        /// <summary>
        /// Deserializes an object from a JSON TextReader.
        /// </summary>
        /// <param name="textReader">The JSON TextReader.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="configurator">The configurator.</param>
        /// <returns>A deserialized value.</returns>
        public static object Deserialize(TextReader textReader, Type nominalType, Action<BsonDeserializationContext.Builder> configurator = null)
            => _serializationDomain.Deserialize(textReader, nominalType, configurator);

        internal static IDiscriminatorConvention GetOrRegisterDiscriminatorConvention(Type type, IDiscriminatorConvention discriminatorConvention)
            => _serializationDomain.GetOrRegisterDiscriminatorConvention(type, discriminatorConvention);

        internal static bool IsDiscriminatorConventionRegisteredAtThisLevel(Type type)
            => _serializationDomain.IsDiscriminatorConventionRegisteredAtThisLevel(type);

        /// <summary>
        /// Returns whether the given type has any discriminators registered for any of its subclasses.
        /// </summary>
        /// <param name="type">A Type.</param>
        /// <returns>True if the type is discriminated.</returns>
        public static bool IsTypeDiscriminated(Type type)
            => _serializationDomain.IsTypeDiscriminated(type);

        /// <summary>
        /// Looks up the actual type of an object to be deserialized.
        /// </summary>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="discriminator">The discriminator.</param>
        /// <returns>The actual type of the object.</returns>
        public static Type LookupActualType(Type nominalType, BsonValue discriminator)
            => _serializationDomain.LookupActualType(nominalType, discriminator);

        /// <summary>
        /// Looks up the discriminator convention for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A discriminator convention.</returns>
        public static IDiscriminatorConvention LookupDiscriminatorConvention(Type type)
            => _serializationDomain.LookupDiscriminatorConvention(type);

        /// <summary>
        /// Looks up an IdGenerator.
        /// </summary>
        /// <param name="type">The Id type.</param>
        /// <returns>An IdGenerator for the Id type.</returns>
        public static IIdGenerator LookupIdGenerator(Type type)
            => _serializationDomain.LookupIdGenerator(type);

        /// <summary>
        /// Looks up a serializer for a Type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>A serializer for type T.</returns>
        public static IBsonSerializer<T> LookupSerializer<T>() => _serializationDomain.LookupSerializer<T>();

        /// <summary>
        /// Looks up a serializer for a Type.
        /// </summary>
        /// <param name="type">The Type.</param>
        /// <returns>A serializer for the Type.</returns>
        public static IBsonSerializer LookupSerializer(Type type) => _serializationDomain.LookupSerializer(type);

        /// <summary>
        /// Registers the discriminator for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="discriminator">The discriminator.</param>
        public static void RegisterDiscriminator(Type type, BsonValue discriminator)
            => _serializationDomain.RegisterDiscriminator(type, discriminator);

        /// <summary>
        /// Registers the discriminator convention for a type.
        /// </summary>
        /// <param name="type">Type type.</param>
        /// <param name="convention">The discriminator convention.</param>
        public static void RegisterDiscriminatorConvention(Type type, IDiscriminatorConvention convention)
            => _serializationDomain.RegisterDiscriminatorConvention(type, convention);

        /// <summary>
        /// Registers a generic serializer definition for a generic type.
        /// </summary>
        /// <param name="genericTypeDefinition">The generic type.</param>
        /// <param name="genericSerializerDefinition">The generic serializer definition.</param>
        public static void RegisterGenericSerializerDefinition(Type genericTypeDefinition, Type genericSerializerDefinition)
            => _serializationDomain.RegisterGenericSerializerDefinition(genericTypeDefinition, genericSerializerDefinition);

        /// <summary>
        /// Registers an IdGenerator for an Id Type.
        /// </summary>
        /// <param name="type">The Id Type.</param>
        /// <param name="idGenerator">The IdGenerator for the Id Type.</param>
        public static void RegisterIdGenerator(Type type, IIdGenerator idGenerator)
            => _serializationDomain.RegisterIdGenerator(type, idGenerator);

        /// <summary>
        /// Registers a serialization provider.
        /// </summary>
        /// <param name="provider">The serialization provider.</param>
        public static void RegisterSerializationProvider(IBsonSerializationProvider provider)
            => _serializationDomain.RegisterSerializationProvider(provider);

        /// <summary>
        /// Registers a serializer for a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="serializer">The serializer.</param>
        public static void RegisterSerializer<T>(IBsonSerializer<T> serializer)
            => _serializationDomain.RegisterSerializer<T>(serializer);

        /// <summary>
        /// Registers a serializer for a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="serializer">The serializer.</param>
        public static void RegisterSerializer(Type type, IBsonSerializer serializer)
            => _serializationDomain.RegisterSerializer(type, serializer);

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <typeparam name="TNominalType">The nominal type of the object.</typeparam>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="value">The object.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        public static void Serialize<TNominalType>(
            IBsonWriter bsonWriter,
            TNominalType value,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default)
            => _serializationDomain.Serialize<TNominalType>(bsonWriter, value, configurator, args);

        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="bsonWriter">The BsonWriter.</param>
        /// <param name="nominalType">The nominal type of the object.</param>
        /// <param name="value">The object.</param>
        /// <param name="configurator">The serialization context configurator.</param>
        /// <param name="args">The serialization args.</param>
        public static void Serialize(
            IBsonWriter bsonWriter,
            Type nominalType,
            object value,
            Action<BsonSerializationContext.Builder> configurator = null,
            BsonSerializationArgs args = default(BsonSerializationArgs))
            => _serializationDomain.Serialize(bsonWriter, nominalType, value, configurator, args);

        /// <summary>
        /// Tries to register a serializer for a type.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="type">The type.</param>
        /// <returns>True if the serializer was registered on this call, false if the same serializer was already registered on a previous call, throws an exception if a different serializer was already registered.</returns>
        public static bool TryRegisterSerializer(Type type, IBsonSerializer serializer)
            => _serializationDomain.TryRegisterSerializer(type, serializer);

        /// <summary>
        /// Tries to register a serializer for a type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="serializer">The serializer.</param>
        /// <returns>True if the serializer was registered on this call, false if the same serializer was already registered on a previous call, throws an exception if a different serializer was already registered.</returns>
        public static bool TryRegisterSerializer<T>(IBsonSerializer<T> serializer)
            => _serializationDomain.TryRegisterSerializer<T>(serializer);

        // internal static methods
        internal static void EnsureKnownTypesAreRegistered(Type nominalType)
            => _serializationDomain.EnsureKnownTypesAreRegistered(nominalType);

        internal static BsonValue[] GetDiscriminatorsForTypeAndSubTypes(Type type)
            => _serializationDomain.GetDiscriminatorsForTypeAndSubTypes(type);
    }
}
