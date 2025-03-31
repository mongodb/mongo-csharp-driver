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
using System.Linq.Expressions;
using System.Reflection;
using Shouldly;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class ObjectSerializerAllowedTypesConventionTests
    {
        private class TestClass
        {
            public object ObjectProp { get; set; }
            public object[] ArrayOfObjectProp { get; set; }
            public object[][] ArrayOfArrayOfObjectProp { get; set; }
            public TestClass RecursiveProp { get; set; }
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_single_delegate()
        {
            var allowedDelegate = (Type t) => t.Name.Contains("t");
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDelegate);
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Allowed type
            allowedDelegate(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();

            // Not allowed type
            allowedDelegate(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_double_delegate()
        {
            var allowedDeserializationDelegate = (Type t) => t.Name.Contains("t");
            var allowedSerializationDelegate = (Type t) => t.Name.Contains("n");
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDeserializationDelegate, allowedSerializationDelegate);
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Deserialization
            allowedDeserializationDelegate(typeof(TestClass)).ShouldBeTrue();
            allowedDeserializationDelegate(typeof(EnumSerializer)).ShouldBeFalse();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();

            // Serialization
            allowedSerializationDelegate(typeof(TestClass)).ShouldBeFalse();
            allowedSerializationDelegate(typeof(EnumSerializer)).ShouldBeTrue();

            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_single_enumerable()
        {
            var allowedTypes = new[] { typeof(TestClass) };
            var subject = new ObjectSerializerAllowedTypesConvention(allowedTypes);
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Allowed type
            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();

            // Not allowed type
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_double_enumerable()
        {
            var allowedDeserializableTypes = new[] { typeof(TestClass) };
            var allowedSerializableTypes = new[] { typeof(EnumSerializer) };
            var subject = new ObjectSerializerAllowedTypesConvention(allowedDeserializableTypes, allowedSerializableTypes);
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Deserialization
            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();

            // Serialization
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_no_arguments()
        {
            var subject = new ObjectSerializerAllowedTypesConvention();
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Type in default framework types
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeTrue();

            // Type not in default framework types
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_constructor_assemblies()
        {
            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetAssembly(typeof(TestClass)),
                Assembly.GetAssembly(typeof(System.Linq.Enumerable)));
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Types in input assemblies
            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(System.Linq.Enumerable)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(System.Linq.Enumerable)).ShouldBeTrue();

            // Type not in input assemblies
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_without_default_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention { AllowDefaultFrameworkTypes = false };
            subject.AllowDefaultFrameworkTypes.ShouldBeFalse();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Default framework type
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_building_with_default_types()
        {
            var subject = new ObjectSerializerAllowedTypesConvention { AllowDefaultFrameworkTypes = true };
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            // Default framework type
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeTrue();
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
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
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
            childSerializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            childSerializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();

            // Type not in assembly
            childSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            childSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowAllTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowAllTypes;
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeTrue();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowNoTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowNoTypes;
            subject.AllowDefaultFrameworkTypes.ShouldBeFalse();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeFalse();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowOnlyDefaultFrameworkTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.AllowOnlyDefaultFrameworkTypes;
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeFalse();
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Apply_should_configure_serializer_when_using_static_AllowAllCallingAssemblyAndDefaultFrameworkTypes()
        {
            var subject = ObjectSerializerAllowedTypesConvention.GetAllowAllCallingAssemblyAndDefaultFrameworkTypesConvention();
            subject.AllowDefaultFrameworkTypes.ShouldBeTrue();

            var memberMap = CreateMemberMap(c => c.ObjectProp);
            subject.Apply(memberMap);

            var serializer = (ObjectSerializer)memberMap.GetSerializer();

            serializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedSerializationTypes(typeof(long)).ShouldBeTrue();
            serializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            serializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
        }

        [Fact]
        public void Convention_should_be_applied_during_automapping()
        {
            var conventionName = Guid.NewGuid().ToString();

            var subject = new ObjectSerializerAllowedTypesConvention(Assembly.GetExecutingAssembly());
            ConventionRegistry.Register(conventionName, new ConventionPack {subject}, t => t == typeof(TestClass));

            var classMap = new BsonClassMap<TestClass>(cm => cm.AutoMap());
            var memberMap = classMap.GetMemberMap("ObjectProp");

            var serializer = memberMap.GetSerializer();
            serializer.ShouldBeOfType<ObjectSerializer>();
            var typedSerializer = (ObjectSerializer)serializer;

            // Type in assembly
            typedSerializer.AllowedDeserializationTypes(typeof(TestClass)).ShouldBeTrue();
            typedSerializer.AllowedSerializationTypes(typeof(TestClass)).ShouldBeTrue();

            // Type not in assembly
            typedSerializer.AllowedDeserializationTypes(typeof(EnumSerializer)).ShouldBeFalse();
            typedSerializer.AllowedSerializationTypes(typeof(EnumSerializer)).ShouldBeFalse();

            ConventionRegistry.Remove(conventionName);
        }

        [Fact]
        public void Convention_should_work_with_recursive_type()
        {
            var pack = new ConventionPack { new ObjectSerializerAllowedTypesConvention() };
            ConventionRegistry.Register("objectRecursive", pack, t => t == typeof(TestClass));

            _ = new BsonClassMap<TestClass>(cm => cm.AutoMap()).Freeze();

            ConventionRegistry.Remove("enumRecursive");
        }

        // private methods
        private static BsonMemberMap CreateMemberMap<TMember>(Expression<Func<TestClass, TMember>> member)
        {
            var classMap = new BsonClassMap<TestClass>();
            return classMap.MapMember(member);
        }
    }
}
