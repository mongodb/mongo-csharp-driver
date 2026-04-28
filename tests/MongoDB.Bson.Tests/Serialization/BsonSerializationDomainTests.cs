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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class BsonSerializationDomainTests
    {
        private class Foo { }

        [Fact]
        public void RegisterSerializer_should_throw_when_serializer_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignSerializer = new DomainTaggedSerializer<Foo>(domainB);

            var exception = Record.Exception(() => domainA.RegisterSerializer(typeof(Foo), foreignSerializer));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void TryRegisterSerializer_should_throw_when_serializer_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignSerializer = new DomainTaggedSerializer<Foo>(domainB);

            var exception = Record.Exception(() => domainA.TryRegisterSerializer(typeof(Foo), foreignSerializer));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void RegisterClassMap_should_throw_when_class_map_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignClassMap = new BsonClassMap<Foo>(domainB);
            foreignClassMap.AutoMap();

            var exception = Record.Exception(() => domainA.ClassMapRegistry.RegisterClassMap(foreignClassMap));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void RegisterDiscriminatorConvention_should_throw_when_convention_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignConvention = new DomainTaggedDiscriminatorConvention(domainB);

            var exception = Record.Exception(() => domainA.RegisterDiscriminatorConvention(typeof(Foo), foreignConvention));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void RegisterIdGenerator_should_throw_when_id_generator_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignIdGenerator = new DomainTaggedIdGenerator(domainB);

            var exception = Record.Exception(() => domainA.RegisterIdGenerator(typeof(ObjectId), foreignIdGenerator));

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void RegisterSerializationProvider_should_throw_when_provider_belongs_to_different_domain()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var foreignProvider = new DomainTaggedSerializationProvider(domainB);

            var exception = Record.Exception(() => domainA.RegisterSerializationProvider(foreignProvider));

            exception.Should().BeOfType<ArgumentException>();
        }

        // private test types
        private sealed class DomainTaggedSerializer<T> : SerializerBase<T>, IHasSerializationDomain
        {
            public DomainTaggedSerializer(IBsonSerializationDomain domain) { SerializationDomain = domain; }
            public IBsonSerializationDomain SerializationDomain { get; }
        }

        private sealed class DomainTaggedDiscriminatorConvention : IDiscriminatorConvention, IHasSerializationDomain
        {
            public DomainTaggedDiscriminatorConvention(IBsonSerializationDomain domain) { SerializationDomain = domain; }
            public IBsonSerializationDomain SerializationDomain { get; }
            public string ElementName => "_t";
            public Type GetActualType(IBsonReader bsonReader, Type nominalType) => nominalType;
            public BsonValue GetDiscriminator(Type nominalType, Type actualType) => actualType.Name;
        }

        private sealed class DomainTaggedIdGenerator : IIdGenerator, IHasSerializationDomain
        {
            public DomainTaggedIdGenerator(IBsonSerializationDomain domain) { SerializationDomain = domain; }
            public IBsonSerializationDomain SerializationDomain { get; }
            public object GenerateId(object container, object document) => ObjectId.GenerateNewId();
            public bool IsEmpty(object id) => id == null || ((ObjectId)id).Equals(ObjectId.Empty);
        }

        private sealed class DomainTaggedSerializationProvider : IBsonSerializationProvider, IHasSerializationDomain
        {
            public DomainTaggedSerializationProvider(IBsonSerializationDomain domain) { SerializationDomain = domain; }
            public IBsonSerializationDomain SerializationDomain { get; }
            public IBsonSerializer GetSerializer(Type type) => null;
        }
    }
}
