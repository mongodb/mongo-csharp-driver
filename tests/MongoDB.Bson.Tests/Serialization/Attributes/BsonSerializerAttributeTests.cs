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

using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Attributes
{
    public class BsonSerializerAttributeTests
    {
        [BsonSerializer(typeof(ParameterlessOnlySerializer))]
        internal class ParameterlessOnlyType { }

        internal class ParameterlessOnlySerializer : SerializerBase<ParameterlessOnlyType> { }

        [Fact]
        public void Parameterless_ctor_is_used_when_no_other_ctor_is_present()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-parameterless");

            var serializer = domain.LookupSerializer<ParameterlessOnlyType>();

            serializer.Should().BeOfType<ParameterlessOnlySerializer>();
        }

        [BsonSerializer(typeof(RegistryCtorSerializer))]
        internal class RegistryCtorType { }

        internal class RegistryCtorSerializer : SerializerBase<RegistryCtorType>
        {
            public IBsonSerializerRegistry CapturedRegistry { get; }

            public RegistryCtorSerializer() { }

            internal RegistryCtorSerializer(IBsonSerializerRegistry registry)
            {
                CapturedRegistry = registry;
            }
        }

        [Fact]
        public void Registry_ctor_is_preferred_over_parameterless_and_captures_lookup_registry()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-registry");

            var serializer = (RegistryCtorSerializer)domain.LookupSerializer<RegistryCtorType>();

            serializer.CapturedRegistry.Should().BeSameAs(domain.SerializerRegistry);
        }

        [BsonSerializer(typeof(DomainCtorSerializer))]
        internal class DomainCtorType { }

        internal class DomainCtorSerializer : SerializerBase<DomainCtorType>, IHasSerializationDomain
        {
            public IBsonSerializationDomain SerializationDomain { get; }
            public IBsonSerializerRegistry CapturedRegistry { get; }

            public DomainCtorSerializer() { }

            internal DomainCtorSerializer(IBsonSerializerRegistry registry)
            {
                CapturedRegistry = registry;
            }

            internal DomainCtorSerializer(IBsonSerializationDomain domain)
            {
                SerializationDomain = domain;
            }
        }

        [Fact]
        public void Domain_ctor_is_preferred_over_registry_ctor_and_captures_lookup_domain()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-domain");

            var serializer = (DomainCtorSerializer)domain.LookupSerializer<DomainCtorType>();

            serializer.SerializationDomain.Should().BeSameAs(domain);
            serializer.CapturedRegistry.Should().BeNull();
        }

        [BsonSerializer(typeof(InternalOnlyRegistryCtorSerializer))]
        internal class InternalOnlyRegistryCtorType { }

        internal class InternalOnlyRegistryCtorSerializer : SerializerBase<InternalOnlyRegistryCtorType>
        {
            public IBsonSerializerRegistry CapturedRegistry { get; }
            public InternalOnlyRegistryCtorSerializer() { }
            internal InternalOnlyRegistryCtorSerializer(IBsonSerializerRegistry registry)
            {
                CapturedRegistry = registry;
            }
        }

        [Fact]
        public void Internal_registry_ctor_is_discoverable()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-internal-ctor");

            var serializer = (InternalOnlyRegistryCtorSerializer)domain.LookupSerializer<InternalOnlyRegistryCtorType>();

            serializer.CapturedRegistry.Should().BeSameAs(domain.SerializerRegistry);
        }

        [BsonSerializer(typeof(GenericRegistryCtorSerializer<>))]
        internal class GenericType<T> { }

        internal class GenericRegistryCtorSerializer<T> : SerializerBase<GenericType<T>>
        {
            public IBsonSerializerRegistry CapturedRegistry { get; }
            public GenericRegistryCtorSerializer() { }
            internal GenericRegistryCtorSerializer(IBsonSerializerRegistry registry)
            {
                CapturedRegistry = registry;
            }
        }

        [Fact]
        public void Generic_serializer_closed_type_is_constructed_via_registry_ctor()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-generic");

            var serializer = (GenericRegistryCtorSerializer<int>)domain.LookupSerializer<GenericType<int>>();

            serializer.CapturedRegistry.Should().BeSameAs(domain.SerializerRegistry);
        }

        [Fact]
        public void Default_domain_lookup_supplies_default_domains_registry_to_registry_ctor()
        {
            var serializer = (RegistryCtorSerializer)BsonSerializationDomain.Default.LookupSerializer<RegistryCtorType>();

            serializer.CapturedRegistry.Should().BeSameAs(BsonSerializationDomain.Default.SerializerRegistry);
        }

        internal class HolderType
        {
            [BsonSerializer(typeof(MemberRegistryCtorSerializer))]
            public string Foo { get; set; }
        }

        internal class MemberRegistryCtorSerializer : SerializerBase<string>
        {
            public IBsonSerializerRegistry CapturedRegistry { get; }
            public MemberRegistryCtorSerializer() { }
            internal MemberRegistryCtorSerializer(IBsonSerializerRegistry registry)
            {
                CapturedRegistry = registry;
            }
        }

        [Fact]
        public void Apply_to_member_map_threads_member_maps_domain_through_to_registry_ctor()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-member-map");

            domain.ClassMapRegistry.RegisterClassMap<HolderType>(cm => cm.AutoMap());

            var classMap = domain.ClassMapRegistry.LookupClassMap(typeof(HolderType));
            var memberMap = classMap.GetMemberMap(nameof(HolderType.Foo));
            var serializer = (MemberRegistryCtorSerializer)memberMap.GetSerializer();

            serializer.CapturedRegistry.Should().BeSameAs(domain.SerializerRegistry);
        }
    }
}
