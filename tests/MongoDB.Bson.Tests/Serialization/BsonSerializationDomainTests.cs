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
using System.Collections.Concurrent;
using System.Threading.Tasks;
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

        // negative isolation: a registration in one domain must not leak into Default or an independent domain

        [Fact]
        public void Serializer_registered_in_custom_domain_should_not_leak_to_Default_or_other_domains()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var serializer = new MarkerSerializer<Foo>();

            domainA.RegisterSerializer(typeof(Foo), serializer);

            domainA.LookupSerializer(typeof(Foo)).Should().BeSameAs(serializer);
            domainB.LookupSerializer(typeof(Foo)).Should().NotBeSameAs(serializer);
            BsonSerializationDomain.Default.LookupSerializer(typeof(Foo)).Should().NotBeSameAs(serializer);
        }

        [Fact]
        public void DiscriminatorConvention_registered_in_custom_domain_should_not_leak_to_Default_or_other_domains()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var convention = new ScalarDiscriminatorConvention("_type");

            domainA.RegisterDiscriminatorConvention(typeof(Animal), convention);

            domainA.LookupDiscriminatorConvention(typeof(Animal)).Should().BeSameAs(convention);
            domainB.LookupDiscriminatorConvention(typeof(Animal)).Should().NotBeSameAs(convention);
            BsonSerializationDomain.Default.LookupDiscriminatorConvention(typeof(Animal)).Should().NotBeSameAs(convention);
        }

        [Fact]
        public void IdGenerator_registered_in_custom_domain_should_not_leak_to_Default_or_other_domains()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var idGenerator = new DomainTaggedIdGenerator(domainA);

            domainA.RegisterIdGenerator(typeof(string), idGenerator);

            domainA.LookupIdGenerator(typeof(string)).Should().BeSameAs(idGenerator);
            domainB.LookupIdGenerator(typeof(string)).Should().NotBeSameAs(idGenerator);
            BsonSerializationDomain.Default.LookupIdGenerator(typeof(string)).Should().NotBeSameAs(idGenerator);
        }

        [Fact]
        public void Discriminator_registered_in_custom_domain_should_not_leak_to_Default_or_other_domains()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");

            domainA.RegisterDiscriminator(typeof(Cat), "Cat");

            domainA.IsTypeDiscriminated(typeof(Animal)).Should().BeTrue();
            domainA.LookupActualType(typeof(Animal), "Cat").Should().Be(typeof(Cat));
            domainB.IsTypeDiscriminated(typeof(Animal)).Should().BeFalse();
            BsonSerializationDomain.Default.IsTypeDiscriminated(typeof(Animal)).Should().BeFalse();
        }

        [Fact]
        public void Convention_registered_in_custom_domain_should_not_leak_to_Default_or_other_domains()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var pack = new ConventionPack(domainA) { new IgnoreExtraElementsConvention(true) };

            domainA.ConventionRegistry.Register("isolationPack", pack, t => t == typeof(ConventionPackTarget));

            domainA.ClassMapRegistry.LookupClassMap(typeof(ConventionPackTarget)).IgnoreExtraElements.Should().BeTrue();
            domainB.ClassMapRegistry.LookupClassMap(typeof(ConventionPackTarget)).IgnoreExtraElements.Should().BeFalse();
            BsonSerializationDomain.Default.ClassMapRegistry.LookupClassMap(typeof(ConventionPackTarget)).IgnoreExtraElements.Should().BeFalse();
        }

        [Fact]
        public void Concurrent_registration_and_lookup_on_two_domains_should_not_cross_contaminate()
        {
            var domainA = BsonSerializationDomain.CreateWithDefaultConfiguration("A");
            var domainB = BsonSerializationDomain.CreateWithDefaultConfiguration("B");
            var serializerA = new MarkerSerializer<Foo>();
            var serializerB = new MarkerSerializer<Foo>();
            domainA.RegisterSerializer(typeof(Foo), serializerA);
            domainB.RegisterSerializer(typeof(Foo), serializerB);

            var exceptions = new ConcurrentQueue<Exception>();
            Parallel.For(0, 1000, _ =>
            {
                try
                {
                    // reads of the per-domain registration must never cross over
                    domainA.LookupSerializer(typeof(Foo)).Should().BeSameAs(serializerA);
                    domainB.LookupSerializer(typeof(Foo)).Should().BeSameAs(serializerB);
                    // lookups that mutate the per-domain cache / take the config write lock, run concurrently
                    domainA.LookupSerializer(typeof(int)).Should().NotBeNull();
                    domainB.LookupSerializer(typeof(Guid)).Should().NotBeNull();
                    domainA.LookupDiscriminatorConvention(typeof(Animal)).Should().NotBeNull();
                    domainB.LookupDiscriminatorConvention(typeof(Cat)).Should().NotBeNull();
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });

            exceptions.Should().BeEmpty();
            domainA.LookupSerializer(typeof(Foo)).Should().BeSameAs(serializerA);
            domainB.LookupSerializer(typeof(Foo)).Should().BeSameAs(serializerB);
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

        private sealed class MarkerSerializer<T> : SerializerBase<T>
        {
        }

        private class Animal { }

        private class Cat : Animal { }

        private class ConventionPackTarget
        {
            public int A { get; set; }
        }
    }
}
