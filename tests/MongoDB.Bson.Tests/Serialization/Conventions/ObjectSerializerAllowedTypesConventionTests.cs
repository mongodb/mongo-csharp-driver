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
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    [Collection(RegisterObjectSerializerFixture.CollectionName)]
    public class ObjectSerializerAllowedTypesConventionTests
    {
        private class TestClass
        {
            public object ObjectProp { get; set; }
            public object[] ArrayOfObjectProp { get; set; }
            public object[][] ArrayOfArrayOfObjectProp { get; set; }
            public Dictionary<string, object> DictionaryStringObjectProp { get; set; }
            public Dictionary<object, string> DictionaryObjectStringProp { get; set; }
            public TestClass RecursiveProp { get; set; }
            public TestClass[] RecursivePropArray { get; set; }
            public TestClass[][] RecursivePropArrayOfArray { get; set; }
            public Dictionary<string, TestClass> DictionaryRecursiveProp { get; set; }
            public Dictionary<string, TestClass>[] DictionaryRecursivePropArray { get; set; }
            public IC IndirectlyRecursiveProp { get; set; }
            public IC[] IndirectlyRecursivePropArray { get; set; }
            public Dictionary<string, IC> IndirectlyRecursivePropDictionary { get; set; }
            public IEnumerable<object> IEnumerableObjectProp { get; set; }
            public IDictionary<string, object> IDictionaryStringObjectProp { get; set; }
            public IDictionary<object, string> IDictionaryObjectStringProp { get; set; }
            public ArrayList ArrayListProp { get; set; }
            public SelfEnumerable SelfEnumerableProp { get; set; }
            // public Dictionary<TestClass, object> RecursivePropDictionary { get; set; } - this is not supported.
        }

        private class IC
        {
            public TestClass TestClass { get; set; }
        }

        private class SelfEnumerable : IEnumerable<SelfEnumerable>
        {
            public IEnumerator<SelfEnumerable> GetEnumerator() => throw new NotImplementedException();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        // A generic type declared in the "application" assembly, standing in for any generic wrapper an
        // allowed assembly happens to expose.
        private class GenericWrapper<T>
        {
            public T Value { get; set; }
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_single_delegate()
        {
            var allowedDelegate = (Type t) => t.Name.Contains("t");
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDelegate);
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Allowed type
            allowedDelegate(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Not allowed type
            allowedDelegate(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_double_delegate()
        {
            var allowedDeserializationDelegate = (Type t) => t.Name.Contains("t");
            var allowedSerializationDelegate = (Type t) => t.Name.Contains("n");
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDeserializationDelegate, allowedSerializationDelegate);
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Deserialization
            allowedDeserializationDelegate(typeof(TestClass)).Should().BeTrue();
            allowedDeserializationDelegate(typeof(EnumSerializer)).Should().BeFalse();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();

            // Serialization
            allowedSerializationDelegate(typeof(TestClass)).Should().BeFalse();
            allowedSerializationDelegate(typeof(EnumSerializer)).Should().BeTrue();

            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_single_enumerable()
        {
            var allowedTypes = new[] { typeof(TestClass) };
            var subject = new ObjectSerializerAllowedTypesConvention(allowedTypes);
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Allowed type
            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Not allowed type
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_double_enumerable()
        {
            var allowedDeserializableTypes = new[] { typeof(TestClass) };
            var allowedSerializableTypes = new[] { typeof(EnumSerializer) };
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDeserializableTypes, allowedSerializableTypes);
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Deserialization
            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();

            // Serialization
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_no_arguments()
        {
            var subject = new ObjectSerializerAllowedTypesConvention();
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Type in default framework types
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeTrue();

            // Type not in default framework types
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_assemblies()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetAssembly(typeof(TestClass)),
                Assembly.GetAssembly(typeof(System.Linq.Enumerable)));
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Types in input assemblies
            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(System.Linq.Enumerable)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(System.Linq.Enumerable)).Should().BeTrue();

            // Type not in input assemblies
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_without_default_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention { AllowDefaultFrameworkTypes = false };
            subject.AllowDefaultFrameworkTypes.Should().BeFalse();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Default framework type
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_default_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention { AllowDefaultFrameworkTypes = true };
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Default framework type
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_a_collection()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ArrayOfObjectProp);
            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (ObjectSerializer)serializer.ChildSerializer;

            // Type in assembly
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_a_nested_collection()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ArrayOfArrayOfObjectProp);
            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (ObjectSerializer)((IChildSerializerConfigurable)serializer.ChildSerializer).ChildSerializer;

            // Type in assembly
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_a_dictionary_with_object_value()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.DictionaryStringObjectProp);
            subject.Apply(memberMap);

            var serializer = (IMultipleChildSerializersConfigurable)memberMap.GetSerializer();
            var childrenSerializers = serializer.ChildSerializers;

            var valueSerializer = (ObjectSerializer)childrenSerializers[1];

            // Type in assembly
            valueSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            valueSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            valueSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            valueSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }


        [Fact]
        public void Apply_should_configure_serializer_when_member_is_a_dictionary_with_object_key()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.DictionaryObjectStringProp);
            subject.Apply(memberMap);

            var serializer = (IMultipleChildSerializersConfigurable)memberMap.GetSerializer();
            var childrenSerializers = serializer.ChildSerializers;

            var keySerializer = (ObjectSerializer)childrenSerializers[0];

            // Type in assembly
            keySerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            keySerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            keySerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            keySerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_an_IEnumerable_of_object()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.IEnumerableObjectProp);
            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (ObjectSerializer)serializer.ChildSerializer;

            // Type in assembly
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_an_IDictionary_interface_with_object_value()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.IDictionaryStringObjectProp);
            subject.Apply(memberMap);

            // IDictionary<K,V> members use ImpliedImplementationInterfaceSerializer wrapping the actual DictionarySerializer
            var outerSerializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var innerSerializer = (IMultipleChildSerializersConfigurable)outerSerializer.ChildSerializer;
            var valueSerializer = (ObjectSerializer)innerSerializer.ChildSerializers[1];

            // Type in assembly
            valueSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            valueSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            valueSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            valueSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_an_IDictionary_interface_with_object_key()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.IDictionaryObjectStringProp);
            subject.Apply(memberMap);

            // IDictionary<K,V> members use ImpliedImplementationInterfaceSerializer wrapping the actual DictionarySerializer
            var outerSerializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var innerSerializer = (IMultipleChildSerializersConfigurable)outerSerializer.ChildSerializer;
            var keySerializer = (ObjectSerializer)innerSerializer.ChildSerializers[0];

            // Type in assembly
            keySerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            keySerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            keySerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            keySerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_member_is_a_non_generic_collection()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ArrayListProp);
            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (ObjectSerializer)serializer.ChildSerializer;

            // Type in assembly
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowAllTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowAllTypes;
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowNoTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowNoTypes;
            subject.AllowDefaultFrameworkTypes.Should().BeFalse();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeFalse();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowOnlyDefaultFrameworkTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowOnlyDefaultFrameworkTypes;
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeFalse();
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowAllCallingAssemblyAndDefaultFrameworkTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.GetAllowAllCallingAssemblyAndDefaultFrameworkTypesConvention();
            subject.AllowDefaultFrameworkTypes.Should().BeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).Should().BeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();
        }

        [Fact]
        public void Convention_should_be_applied_during_automapping()
        {
            var conventionName = Guid.NewGuid().ToString();

            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());
            ConventionRegistry.Register(conventionName, new ConventionPack { subject }, t => t == typeof(TestClass));

            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("ObjectProp");

            var serializer = memberMap.GetSerializer();
            serializer.Should().BeOfType<ObjectSerializer>();
            var typedSerializer = (ObjectSerializer)serializer;

            // Type in assembly
            typedSerializer.AllowedDeserializationTypes(typeof(TestClass)).Should().BeTrue();
            typedSerializer.AllowedSerializationTypes(typeof(TestClass)).Should().BeTrue();

            // Type not in assembly
            typedSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).Should().BeFalse();
            typedSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).Should().BeFalse();

            ConventionRegistry.Remove(conventionName);
        }

        [Fact]
        public void Convention_should_work_with_recursive_type()
        {
            var pack = new ConventionPack { new ObjectSerializerAllowedTypesConvention() };
            ConventionRegistry.Register("objectRecursive", pack, t => t == typeof(TestClass));

            _ = new BsonClassMap<TestClass>(cm => cm.AutoMap()).Freeze();

            ConventionRegistry.Remove("objectRecursive");
        }

        [Fact]
        public void Convention_should_not_stack_overflow_with_self_referencing_enumerable_type()
        {
            var subject = new ObjectSerializerAllowedTypesConvention();
            var memberMap = CreateMemberMap(c => c.SelfEnumerableProp);

            subject.Apply(memberMap);
        }

        [Fact]
        public void Apply_should_not_allow_generic_type_from_allowed_assembly_with_generic_argument_from_disallowed_assembly()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // JsonWriterSettings is in MongoDB.Bson, which is not an allowed assembly
            serializer.AllowedDeserializationTypes(typeof(JsonWriterSettings)).Should().BeFalse();

            // the generic type definition is in an allowed assembly, but the generic argument is not
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<JsonWriterSettings>)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<JsonWriterSettings>)).Should().BeFalse();

            // and the same via an array of the constructed generic type
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<JsonWriterSettings>[])).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<JsonWriterSettings>[])).Should().BeFalse();
        }

        [Fact]
        public void Apply_should_allow_generic_type_from_allowed_assembly_with_allowed_generic_arguments()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // generic argument is a default framework type
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<int>)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<int>)).Should().BeTrue();

            // generic argument is from an allowed assembly
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();

            // nested generic arguments are all allowed
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<GenericWrapper<TestClass>>)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<GenericWrapper<TestClass>>)).Should().BeTrue();

            // arrays of allowed types are still allowed
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<TestClass>[])).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<TestClass>[])).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_not_allow_nested_array_or_multiple_argument_generics_with_disallowed_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var disallowedTypes = new[]
            {
                typeof(GenericWrapper<JsonWriterSettings[]>),
                typeof(GenericWrapper<JsonWriterSettings>[][]),
                typeof(GenericWrapper<JsonWriterSettings>[,]),
                typeof(GenericWrapper<GenericWrapper<JsonWriterSettings>[]>),
                typeof(Dictionary<int, GenericWrapper<JsonWriterSettings>>),
                typeof(GenericWrapper<List<JsonWriterSettings>>)
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }
        }

        [Fact]
        public void Apply_should_allow_default_framework_generic_types_constructed_over_allowed_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var allowedTypes = new[]
            {
                typeof(List<int>),
                typeof(List<TestClass>),
                typeof(List<GenericWrapper<TestClass>>),
                typeof(List<TestClass>[]),
                typeof(List<List<TestClass>>),
                typeof(Dictionary<string, TestClass>),
                typeof(GenericWrapper<List<int>>),
                typeof(GenericWrapper<List<TestClass>>),
                typeof(GenericWrapper<Dictionary<string, List<TestClass>>>)
            };

            foreach (var type in allowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeTrue(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeTrue(type.ToString());
            }
        }

        [Fact]
        public void Apply_should_not_allow_default_framework_generic_types_constructed_over_disallowed_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var disallowedTypes = new[]
            {
                typeof(List<JsonWriterSettings>),
                typeof(List<JsonWriterSettings>[]),
                typeof(List<GenericWrapper<JsonWriterSettings>>),
                typeof(List<List<JsonWriterSettings>>),
                typeof(Dictionary<string, JsonWriterSettings>),
                typeof(Dictionary<JsonWriterSettings, TestClass>),
                typeof(GenericWrapper<List<JsonWriterSettings>>),
                typeof(GenericWrapper<Dictionary<string, List<JsonWriterSettings>>>)
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }
        }

        [Fact]
        public void Apply_should_not_allow_framework_generic_types_that_are_not_default_framework_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var disallowedTypes = new[]
            {
                typeof(Func<TestClass>),
                typeof(Lazy<TestClass>),
                typeof(GenericWrapper<Func<TestClass>>),
                typeof(List<Lazy<TestClass>>)
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }
        }

        [Fact]
        public void Apply_should_not_allow_default_framework_generic_types_when_default_framework_types_are_disabled()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly()) { AllowDefaultFrameworkTypes = false };

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var disallowedTypes = new[]
            {
                typeof(List<TestClass>),
                typeof(List<TestClass>[]),
                typeof(Dictionary<string, TestClass>),
                typeof(GenericWrapper<List<TestClass>>)
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }

            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();
        }

        [Fact]
        public void Apply_should_validate_generic_arguments_against_assemblies_only_when_default_framework_types_are_disabled()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly()) { AllowDefaultFrameworkTypes = false };

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // int is not in an allowed assembly and default framework types are disabled
            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<int>)).Should().BeFalse();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<int>)).Should().BeFalse();

            serializer.AllowedDeserializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();
            serializer.AllowedSerializationTypes(typeof(GenericWrapper<TestClass>)).Should().BeTrue();
        }

        [Fact]
        public void Constructor_with_allowed_assemblies_should_throw_when_assemblies_is_null()
        {
            var exception = Record.Exception(() => new ObjectSerializerAllowedTypesConvention((Assembly[])null));

            exception.Should().BeOfType<ArgumentNullException>().Which.ParamName.Should().Be("allowedAssemblies");
        }

        [Fact]
        public void Apply_should_not_allow_generic_type_from_disallowed_assembly_with_allowed_generic_arguments()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // SerializerBase<T> is in MongoDB.Bson, which is not an allowed assembly, so its arguments being allowed is not enough
            var disallowedTypes = new[]
            {
                typeof(SerializerBase<int>),
                typeof(SerializerBase<TestClass>),
                typeof(SerializerBase<List<TestClass>>),
                typeof(List<SerializerBase<TestClass>>),
                typeof(GenericWrapper<SerializerBase<TestClass>>)
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }
        }

        [Fact]
        public void Constructor_with_allowed_assemblies_should_allow_only_default_framework_types_when_assemblies_is_empty()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Array.Empty<Assembly>());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var allowedTypes = new[]
            {
                typeof(int),
                typeof(List<int>),
                typeof(Dictionary<string, List<int>>)
            };

            foreach (var type in allowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeTrue(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeTrue(type.ToString());
            }

            var disallowedTypes = new[]
            {
                typeof(TestClass),
                typeof(GenericWrapper<int>),
                typeof(List<TestClass>),
                typeof(TestClass[])
            };

            foreach (var type in disallowedTypes)
            {
                serializer.AllowedDeserializationTypes(type).Should().BeFalse(type.ToString());
                serializer.AllowedSerializationTypes(type).Should().BeFalse(type.ToString());
            }
        }

        [Fact]
        public void Deserialize_should_throw_when_generic_argument_is_from_disallowed_assembly()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var document = new BsonDocument
            {
                { "_t", TypeNameDiscriminator.GetDiscriminator(typeof(GenericWrapper<JsonWriterSettings>)) },
                { "Value", new BsonDocument("IndentChars", "attacker controlled") }
            };

            var exception = Record.Exception(() => Deserialize(serializer, document));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("is not configured as a type that is allowed to be deserialized");
        }

        [Fact]
        public void Serialize_should_throw_when_generic_argument_is_from_disallowed_assembly()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var exception = Record.Exception(() => Serialize(serializer, new GenericWrapper<JsonWriterSettings>()));

            exception.Should().BeOfType<BsonSerializationException>();
            exception.Message.Should().Contain("is not configured as a type that is allowed to be serialized");
        }

        [Fact]
        public void Apply_should_validate_the_generic_arguments_of_anonymous_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            var allowedType = new { A = 1, B = new TestClass() }.GetType();
            var disallowedType = new { A = 1, B = new JsonWriterSettings() }.GetType();

            serializer.AllowedDeserializationTypes(allowedType).Should().BeTrue();
            serializer.AllowedSerializationTypes(allowedType).Should().BeTrue();
            serializer.AllowedDeserializationTypes(disallowedType).Should().BeFalse();
            serializer.AllowedSerializationTypes(disallowedType).Should().BeFalse();
        }

        // private methods
        private static BsonMemberMap CreateMemberMap<TMember>(Expression<Func<TestClass, TMember>> member)
        {
            var classMap = new BsonClassMap<TestClass>();
            return classMap.MapMember(member);
        }

        private static object Deserialize(IBsonSerializer serializer, BsonDocument document)
        {
            using var reader = new BsonDocumentReader(document);
            var context = BsonDeserializationContext.CreateRoot(reader);
            return serializer.Deserialize(context, new BsonDeserializationArgs { NominalType = typeof(object) });
        }

        private static void Serialize(IBsonSerializer serializer, object value)
        {
            using var writer = new BsonDocumentWriter(new BsonDocument());
            var context = BsonSerializationContext.CreateRoot(writer);
            serializer.Serialize(context, new BsonSerializationArgs { NominalType = typeof(object) }, value);
        }
    }
}
