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

internal class SerializationDomainBuilder : ISerializationDomainBuilder
{
    public ISerializationDomainBuilder RegisterDiscriminator(Type type, BsonValue discriminator) => this;
    public ISerializationDomainBuilder RegisterDiscriminatorConvention(Type type, IDiscriminatorConvention convention) => this;
    public ISerializationDomainBuilder RegisterGenericSerializerDefinition(Type genericTypeDefinition, Type genericSerializerDefinition) => this;
    public ISerializationDomainBuilder RegisterIdGenerator(Type type, IIdGenerator idGenerator) => this;
    public ISerializationDomainBuilder RegisterSerializationProvider(IBsonSerializationProvider provider) => this;
    public ISerializationDomainBuilder RegisterSerializer<T>(IBsonSerializer<T> serializer) => this;
    public ISerializationDomainBuilder RegisterSerializer(Type type, IBsonSerializer serializer) => this;
    public ISerializationDomainBuilder TryRegisterSerializer(Type type, IBsonSerializer serializer) => this;
    public ISerializationDomainBuilder TryRegisterSerializer<T>(IBsonSerializer<T> serializer) => this;
    public ISerializationDomainBuilder UseNullIdChecker(bool useNullIdChecker) => this;
    public ISerializationDomainBuilder UseZeroIdChecker(bool useZeroIdChecker) => this;

    public ISerializationDomainBuilder ConfigureClassMap(Action<IBsonClassMapDomainBuilder> configure)
    {
        configure(new BsonClassMapDomainBuilder());
        return this;
    }

    public ISerializationDomainBuilder ConfigureConventionRegistry(Action<IConventionRegistryDomainBuilder> configure)
    {
        configure(new ConventionRegistryDomainBuilder());
        return this;
    }

    public ISerializationDomainBuilder ConfigureBsonDefaults(Action<IBsonDefaultsBuilder> configure)
    {
        configure(new BsonDefaultsBuilder());
        return this;
    }

    public IBsonSerializationDomain1 Build() => null!;
}

internal class BsonClassMapDomainBuilder : IBsonClassMapDomainBuilder
{
    public IBsonClassMapDomainBuilder RegisterClassMap<TClass>() => this;
    public IBsonClassMapDomainBuilder RegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer) => this;
    public IBsonClassMapDomainBuilder RegisterClassMap(BsonClassMap classMap) => this;
    public IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>() => this;
    public IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(BsonClassMap<TClass> classMap) => this;
    public IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(Action<BsonClassMap<TClass>> classMapInitializer) => this;
    public IBsonClassMapDomainBuilder TryRegisterClassMap<TClass>(Func<BsonClassMap<TClass>> classMapFactory) => this;
}

internal class ConventionRegistryDomainBuilder : IConventionRegistryDomainBuilder
{
    public IConventionRegistryDomainBuilder Register(string name, IConventionPack conventions, Func<Type, bool> filter) => this;
}

internal class BsonDefaultsBuilder : IBsonDefaultsBuilder
{
    public IBsonDefaultsBuilder SetDynamicArraySerializer(IBsonSerializer serializer) => this;
    public IBsonDefaultsBuilder SetDynamicDocumentSerializer(IBsonSerializer serializer) => this;
    public IBsonDefaultsBuilder SetMaxDocumentSize(int size) => this;
    public IBsonDefaultsBuilder SetMaxSerializationDepth(int depth) => this;
}