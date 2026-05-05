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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.GridFS;
using Xunit;

namespace MongoDB.Driver.Tests;

public class MultipleRegistriesProviderPathTests
{
    // Instantiation path: AttributedSerializationProvider → BsonSerializerAttribute.CreateSerializer with domain ctor
    [Fact]
    public void Attribute_path_domain_ctor_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<TypeWithDomainAwareAttributedSerializer>();

        serializer.Should().BeOfType<DomainAwareAttributeSerializer>();
        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // Instantiation path: AttributedSerializationProvider → BsonSerializerAttribute.CreateSerializer
    [Fact]
    public void Attribute_path_parameterless_serializer_resolves_from_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<TypeWithAttributedSerializer>();

        serializer.Should().BeOfType<CustomAttributeSerializer>();
    }

    // Instantiation path: BsonClassMapSerializationProvider (Activator.CreateInstance with domain)
    [Fact]
    public void ClassMap_provider_path_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<Person>();

        serializer.Should().BeOfType<BsonClassMapSerializer<Person>>();
        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    [Fact]
    public void GridFSFileInfoSerializer_attribute_lookup_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        customDomain.RegisterSerializer(new CustomStringSerializer("_suffix"));

        var serializer = customDomain.LookupSerializer<GridFSFileInfo<string>>();

        var docSerializer = (IBsonDocumentSerializer)serializer;
        docSerializer.TryGetMemberSerializationInfo("Id", out var memberInfo).Should().BeTrue();
        memberInfo.Serializer.Should().BeOfType<CustomStringSerializer>();
    }

    // Reconfiguration path: SerializerConfigurator.ReconfigureSerializerRecursively via ObjectSerializerAllowedTypesConvention
    [Fact]
    public void ObjectSerializerAllowedTypesConvention_preserves_domain_on_reconfigured_ObjectSerializer()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var pack = new ConventionPack(customDomain);
        pack.Add(ObjectSerializerAllowedTypesConvention.AllowAllTypes);
        customDomain.ConventionRegistry.Register("allow-all", pack, t => t == typeof(ClassWithObjectMember));

        // Force the class map to be created and frozen so the convention is applied.
        customDomain.SerializerRegistry.GetSerializer<ClassWithObjectMember>();

        var classMap = customDomain.ClassMapRegistry.LookupClassMap(typeof(ClassWithObjectMember));
        var memberSerializer = classMap.GetMemberMap("Data").GetSerializer();

        memberSerializer.Should().BeOfType<ObjectSerializer>();
        ((IHasSerializationDomain)memberSerializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    [Fact]
    public void Provider_created_dictionary_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var dictSerializer = (DictionaryInterfaceImplementerSerializer<Dictionary<string, string>, string, string>)
            customDomain.SerializerRegistry.GetSerializer<Dictionary<string, string>>();

        dictSerializer.ValueSerializer.Should().BeSameAs(customSerializer);
        dictSerializer.KeySerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void Provider_created_enumerable_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var listSerializer = (EnumerableInterfaceImplementerSerializer<List<string>, string>)
            customDomain.SerializerRegistry.GetSerializer<List<string>>();

        listSerializer.ItemSerializer.Should().BeSameAs(customSerializer);
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Provider_created_immutable_array_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var serializer = customDomain.SerializerRegistry.GetSerializer<System.Collections.Immutable.ImmutableArray<string>>();

        serializer.Should().BeOfType<ImmutableArraySerializer<string>>();
        ((EnumerableSerializerBase<System.Collections.Immutable.ImmutableArray<string>, string>)serializer)
            .ItemSerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void Provider_created_immutable_dictionary_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var serializer = customDomain.SerializerRegistry.GetSerializer<System.Collections.Immutable.ImmutableDictionary<string, string>>();

        serializer.Should().BeOfType<ImmutableDictionarySerializer<string, string>>();
        var dictSerializer = (DictionarySerializerBase<System.Collections.Immutable.ImmutableDictionary<string, string>, string, string>)serializer;
        dictSerializer.ValueSerializer.Should().BeSameAs(customSerializer);
        dictSerializer.KeySerializer.Should().BeSameAs(customSerializer);
    }
#endif

    [Fact]
    public void Provider_created_object_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var objSerializer = customDomain.SerializerRegistry.GetSerializer<object>();

        objSerializer.Should().BeOfType<ObjectSerializer>();
        ((IHasSerializationDomain)objSerializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    [Fact]
    public void Provider_created_readonly_dictionary_serializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var dictSerializer = (ReadOnlyDictionaryInterfaceImplementerSerializer<ReadOnlyDictionary<string, string>, string, string>)
            customDomain.SerializerRegistry.GetSerializer<ReadOnlyDictionary<string, string>>();

        dictSerializer.ValueSerializer.Should().BeSameAs(customSerializer);
        dictSerializer.KeySerializer.Should().BeSameAs(customSerializer);
    }

    // Instantiation path: DiscriminatedInterfaceSerializer internally chains
    // ObjectSerializer.WithDiscriminatorConvention().WithAllowedTypes() — verifies the With* fix
    [Fact]
    public void Provider_path_DiscriminatedInterfaceSerializer_internal_ObjectSerializer_preserves_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = (DiscriminatedInterfaceSerializer<IAnimal>)
            customDomain.SerializerRegistry.GetSerializer<IAnimal>();

        var objectSerializerField = typeof(DiscriminatedInterfaceSerializer<IAnimal>)
            .GetField("_objectSerializer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var objectSerializer = (ObjectSerializer)objectSerializerField.GetValue(serializer);

        ((IHasSerializationDomain)objectSerializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // Instantiation path: DiscriminatedInterfaceSerializationProvider via BsonSerializationProviderBase.CreateSerializer (reflection)
    [Fact]
    public void Provider_path_DiscriminatedInterfaceSerializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<IAnimal>();

        serializer.Should().BeOfType<DiscriminatedInterfaceSerializer<IAnimal>>();
        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // Instantiation path: BsonSerializationProviderBase.CreateSerializer (reflection) via CollectionsSerializationProvider
    [Fact]
    public void Provider_path_ExpandoObjectSerializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<ExpandoObject>();

        serializer.Should().BeOfType<ExpandoObjectSerializer>();
        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // Instantiation path: CollectionsSerializationProvider via BsonSerializationProviderBase.CreateSerializer (reflection)
    [Fact]
    public void Provider_path_ImpliedImplementationInterfaceSerializer_uses_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var serializer = customDomain.SerializerRegistry.GetSerializer<IDictionary<string, string>>();

        serializer.Should().BeOfType<ImpliedImplementationInterfaceSerializer<IDictionary<string, string>, Dictionary<string, string>>>();
        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // Instantiation path: BsonSerializationProviderBase.CreateSerializer (reflection) via PrimitiveSerializationProvider
    [Fact]
    public void Provider_path_TupleSerializer_resolves_items_from_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var serializer = customDomain.SerializerRegistry.GetSerializer<Tuple<string>>();

        serializer.Should().BeOfType<TupleSerializer<string>>();
        ((TupleSerializer<string>)serializer).Item1Serializer.Should().BeSameAs(customSerializer);
    }

    // Instantiation path: BsonSerializationProviderBase.CreateSerializer (reflection) via PrimitiveSerializationProvider
    [Fact]
    public void Provider_path_ValueTupleSerializer_resolves_items_from_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var customSerializer = new CustomStringSerializer("_X");
        customDomain.RegisterSerializer(customSerializer);

        var serializer = customDomain.SerializerRegistry.GetSerializer<ValueTuple<string>>();

        serializer.Should().BeOfType<ValueTupleSerializer<string>>();
        ((ValueTupleSerializer<string>)serializer).Item1Serializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void RegisterDiscriminatorConvention_positive_path_under_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var convention = new ScalarDiscriminatorConvention("_type");

        customDomain.RegisterDiscriminatorConvention(typeof(BasePerson), convention);

        customDomain.LookupDiscriminatorConvention(typeof(BasePerson)).Should().BeSameAs(convention);
    }

    [Fact]
    public void TryRegisterClassMap_with_initializer_binds_to_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");

        var registered = customDomain.ClassMapRegistry.TryRegisterClassMap<Person>(cm => cm.AutoMap());

        registered.Should().BeTrue();
        customDomain.ClassMapRegistry.LookupClassMap(typeof(Person)).SerializationDomain.Should().BeSameAs(customDomain);
    }

    [Fact]
    public void TryRegisterSerializer_positive_path_under_custom_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var serializer = new CustomStringSerializer("suffix");

        var result = customDomain.TryRegisterSerializer(typeof(string), serializer);

        result.Should().BeTrue();
        customDomain.SerializerRegistry.GetSerializer<string>().Should().BeSameAs(serializer);
    }

    [Fact]
    public void UseNullIdChecker_and_UseZeroIdChecker_are_per_domain()
    {
        var domain1 = BsonSerializationDomain.CreateWithDefaultConfiguration("Domain1");
        var domain2 = BsonSerializationDomain.CreateWithDefaultConfiguration("Domain2");

        domain1.UseNullIdChecker = false;
        domain2.UseNullIdChecker = true;
        domain1.UseZeroIdChecker = true;
        domain2.UseZeroIdChecker = false;

        // Behavior check: LookupIdGenerator routes through the per-domain flags.
        // domain1 (UseZero=true): value-typed id (int) gets ZeroIdChecker; ref-typed id (string) gets nothing.
        domain1.LookupIdGenerator(typeof(int)).Should().BeOfType<ZeroIdChecker<int>>();
        domain1.LookupIdGenerator(typeof(string)).Should().BeNull();
        // domain2 (UseNull=true, UseZero=false): both ref and value types fall through to NullIdChecker.
        domain2.LookupIdGenerator(typeof(int)).Should().BeSameAs(NullIdChecker.Instance);
        domain2.LookupIdGenerator(typeof(string)).Should().BeSameAs(NullIdChecker.Instance);
    }

    // With* path: ObjectSerializer.WithAllowedTypes preserves domain
    [Fact]
    public void WithAllowedTypes_preserves_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var objectSerializer = (ObjectSerializer)customDomain.SerializerRegistry.GetSerializer<object>();

        var reconfigured = objectSerializer.WithAllowedTypes(t => true, t => true);

        ((IHasSerializationDomain)reconfigured).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // With* path: ObjectSerializer.WithDiscriminatorConvention preserves domain
    [Fact]
    public void WithDiscriminatorConvention_preserves_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var objectSerializer = (ObjectSerializer)customDomain.SerializerRegistry.GetSerializer<object>();

        var reconfigured = objectSerializer.WithDiscriminatorConvention(new ScalarDiscriminatorConvention("_t"));

        ((IHasSerializationDomain)reconfigured).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // With* path: ImpliedImplementationInterfaceSerializer.WithImplementationSerializer preserves domain
    [Fact]
    public void WithImplementationSerializer_preserves_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var serializer = (ImpliedImplementationInterfaceSerializer<IDictionary<string, string>, Dictionary<string, string>>)
            customDomain.SerializerRegistry.GetSerializer<IDictionary<string, string>>();

        ((IHasSerializationDomain)serializer).SerializationDomain.Should().BeSameAs(customDomain);

        var newImplSerializer = (IBsonSerializer<Dictionary<string, string>>)
            customDomain.SerializerRegistry.GetSerializer<Dictionary<string, string>>();
        var reconfigured = serializer.WithImplementationSerializer(newImplSerializer);

        ((IHasSerializationDomain)reconfigured).SerializationDomain.Should().BeSameAs(customDomain);
    }

    // With* path: EnumerableInterfaceImplementerSerializer.WithItemSerializer preserves domain
    [Fact]
    public void WithItemSerializer_on_enumerable_preserves_domain()
    {
        var customDomain = BsonSerializationDomain.CreateWithDefaultConfiguration("Test");
        var listSerializer = (EnumerableInterfaceImplementerSerializer<List<string>, string>)
            customDomain.SerializerRegistry.GetSerializer<List<string>>();

        var domainField = typeof(EnumerableSerializerBase<List<string>, string>)
            .GetField("_serializationDomain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        domainField.GetValue(listSerializer).Should().BeSameAs(customDomain);

        var reconfigured = listSerializer.WithItemSerializer(new CustomStringSerializer("_Y"));
        domainField.GetValue(reconfigured).Should().BeSameAs(customDomain);
    }
}

internal interface IAnimal
{
    string Name { get; set; }
}

[BsonSerializer(typeof(CustomAttributeSerializer))]
internal class TypeWithAttributedSerializer
{
    public string Value { get; set; }
}

internal class CustomAttributeSerializer : SerializerBase<TypeWithAttributedSerializer>
{
    public override TypeWithAttributedSerializer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        context.Reader.ReadString();
        context.Reader.ReadEndDocument();
        return new TypeWithAttributedSerializer { Value = "deserialized" };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TypeWithAttributedSerializer value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("v");
        context.Writer.WriteString(value.Value ?? "");
        context.Writer.WriteEndDocument();
    }
}

[BsonSerializer(typeof(DomainAwareAttributeSerializer))]
internal class TypeWithDomainAwareAttributedSerializer
{
    public string Value { get; set; }
}

internal class DomainAwareAttributeSerializer : SerializerBase<TypeWithDomainAwareAttributedSerializer>, IHasSerializationDomain
{
    private readonly IBsonSerializationDomain _serializationDomain;

    public DomainAwareAttributeSerializer()
        : this(BsonSerializationDomain.Default)
    {
    }

    internal DomainAwareAttributeSerializer(IBsonSerializationDomain serializationDomain)
    {
        _serializationDomain = serializationDomain;
    }

    IBsonSerializationDomain IHasSerializationDomain.SerializationDomain => _serializationDomain;

    public override TypeWithDomainAwareAttributedSerializer Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        context.Reader.ReadStartDocument();
        context.Reader.ReadString();
        context.Reader.ReadEndDocument();
        return new TypeWithDomainAwareAttributedSerializer { Value = "deserialized" };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TypeWithDomainAwareAttributedSerializer value)
    {
        context.Writer.WriteStartDocument();
        context.Writer.WriteName("v");
        context.Writer.WriteString(value.Value ?? "");
        context.Writer.WriteEndDocument();
    }
}

internal class ClassWithObjectMember
{
    public ObjectId Id { get; set; }
    public object Data { get; set; }
}
