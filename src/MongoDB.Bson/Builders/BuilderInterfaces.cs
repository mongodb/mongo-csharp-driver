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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace MongoDB.Bson.Builders;

/* All the builders here contain the methods from the respective domain/classes that can be used for configurations.
 */

internal interface ISerializationDomainBuilder
{
    ISerializationDomainBuilder RegisterDiscriminator(Type type, BsonValue discriminator);

    ISerializationDomainBuilder RegisterDiscriminatorConvention(Type type, IDiscriminatorConvention convention);

    ISerializationDomainBuilder RegisterGenericSerializerDefinition(
        Type genericTypeDefinition,
        Type genericSerializerDefinition);

    ISerializationDomainBuilder RegisterIdGenerator(Type type, IIdGenerator idGenerator);

    ISerializationDomainBuilder RegisterSerializationProvider(IBsonSerializationProvider provider);

    ISerializationDomainBuilder RegisterSerializer<T>(IBsonSerializer<T> serializer);

    ISerializationDomainBuilder RegisterSerializer(Type type, IBsonSerializer serializer);

    ISerializationDomainBuilder TryRegisterSerializer(Type type, IBsonSerializer serializer);

    ISerializationDomainBuilder TryRegisterSerializer<T>(IBsonSerializer<T> serializer);

    ISerializationDomainBuilder UseNullIdChecker(bool useNullIdChecker);

    ISerializationDomainBuilder UseZeroIdChecker(bool useZeroIdChecker);

    ISerializationDomainBuilder ConfigureClassMap(Action<IBsonClassMapDomainBuilder> configure);

    ISerializationDomainBuilder ConfigureConventionRegistry(Action<IConventionRegistryDomainBuilder> configure);

    ISerializationDomainBuilder ConfigureBsonDefaults(Action<IBsonDefaultsBuilder> configure);

    IBsonSerializationDomain1 Build();
}

internal interface IBsonClassMapDomainBuilder
{
    IBsonClassMapDomainBuilder RegisterClassMap<TClass>();

    IBsonClassMapDomainBuilder RegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer);

    IBsonClassMapDomainBuilder RegisterClassMap(BsonClassMap classMap);

    IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>();

    IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(BsonClassMap<TClass> classMap);

    IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer);

    IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(Func<BsonClassMap<TClass>> classMapFactory);
}

internal interface IConventionRegistryDomainBuilder
{
    IConventionRegistryDomainBuilder Register(string name, IConventionPack conventions, Func<Type, bool> filter);
}

internal interface IBsonDefaultsBuilder
{
    IBsonDefaultsBuilder SetDynamicArraySerializer(IBsonSerializer serializer);
    IBsonDefaultsBuilder SetDynamicDocumentSerializer(IBsonSerializer serializer);
    IBsonDefaultsBuilder SetMaxDocumentSize(int size);
    IBsonDefaultsBuilder SetMaxSerializationDepth(int depth);
}