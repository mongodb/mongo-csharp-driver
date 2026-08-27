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

        [BsonSerializer(typeof(DomainCtorSerializer))]
        internal class DomainCtorType { }

        internal class DomainCtorSerializer : SerializerBase<DomainCtorType>, IHasSerializationDomain
        {
            public IBsonSerializationDomain SerializationDomain { get; }

            public DomainCtorSerializer() { }

            internal DomainCtorSerializer(IBsonSerializationDomain domain)
            {
                SerializationDomain = domain;
            }
        }

        [Fact]
        public void Domain_ctor_is_preferred_over_parameterless_and_captures_lookup_domain()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-domain");

            var serializer = (DomainCtorSerializer)domain.LookupSerializer<DomainCtorType>();

            serializer.SerializationDomain.Should().BeSameAs(domain);
        }

        [BsonSerializer(typeof(InternalOnlyDomainCtorSerializer))]
        internal class InternalOnlyDomainCtorType { }

        internal class InternalOnlyDomainCtorSerializer : SerializerBase<InternalOnlyDomainCtorType>, IHasSerializationDomain
        {
            public IBsonSerializationDomain SerializationDomain { get; }
            public InternalOnlyDomainCtorSerializer() { }
            internal InternalOnlyDomainCtorSerializer(IBsonSerializationDomain domain)
            {
                SerializationDomain = domain;
            }
        }

        [Fact]
        public void Internal_domain_ctor_is_discoverable()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-internal-ctor");

            var serializer = (InternalOnlyDomainCtorSerializer)domain.LookupSerializer<InternalOnlyDomainCtorType>();

            serializer.SerializationDomain.Should().BeSameAs(domain);
        }

        [BsonSerializer(typeof(GenericDomainCtorSerializer<>))]
        internal class GenericType<T> { }

        internal class GenericDomainCtorSerializer<T> : SerializerBase<GenericType<T>>, IHasSerializationDomain
        {
            public IBsonSerializationDomain SerializationDomain { get; }
            public GenericDomainCtorSerializer() { }
            internal GenericDomainCtorSerializer(IBsonSerializationDomain domain)
            {
                SerializationDomain = domain;
            }
        }

        [Fact]
        public void Generic_serializer_closed_type_is_constructed_via_domain_ctor()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-generic");

            var serializer = (GenericDomainCtorSerializer<int>)domain.LookupSerializer<GenericType<int>>();

            serializer.SerializationDomain.Should().BeSameAs(domain);
        }

        [Fact]
        public void Default_domain_lookup_supplies_default_domain_to_domain_ctor()
        {
            var serializer = (DomainCtorSerializer)BsonSerializationDomain.Default.LookupSerializer<DomainCtorType>();

            serializer.SerializationDomain.Should().BeSameAs(BsonSerializationDomain.Default);
        }

        internal class HolderType
        {
            [BsonSerializer(typeof(MemberDomainCtorSerializer))]
            public string Foo { get; set; }
        }

        internal class MemberDomainCtorSerializer : SerializerBase<string>, IHasSerializationDomain
        {
            public IBsonSerializationDomain SerializationDomain { get; }
            public MemberDomainCtorSerializer() { }
            internal MemberDomainCtorSerializer(IBsonSerializationDomain domain)
            {
                SerializationDomain = domain;
            }
        }

        [Fact]
        public void Apply_to_member_map_threads_member_maps_domain_through_to_domain_ctor()
        {
            var domain = BsonSerializationDomain.CreateWithDefaultConfiguration("BsonSerializerAttributeTests-member-map");

            domain.ClassMapRegistry.RegisterClassMap<HolderType>(cm => cm.AutoMap());

            var classMap = domain.ClassMapRegistry.LookupClassMap(typeof(HolderType));
            var memberMap = classMap.GetMemberMap(nameof(HolderType.Foo));
            var serializer = (MemberDomainCtorSerializer)memberMap.GetSerializer();

            serializer.SerializationDomain.Should().BeSameAs(domain);
        }
    }
}
