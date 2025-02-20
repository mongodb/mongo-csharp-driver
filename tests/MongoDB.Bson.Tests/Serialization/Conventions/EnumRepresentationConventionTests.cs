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
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class EnumRepresentationConventionTests
    {
        public enum E { A, B };

        public class C
        {
            public E E { get; set; }
            public E? NE { get; set; }
            public E[] ArrayEnum { get; set; }
            public E[][] ArrayOfArrayEnum { get; set; }
            public Dictionary<string, E> DictionaryEnum { get; set; }
            public Dictionary<string, E[]> NestedDictionaryEnum { get; set; }
            public Dictionary<E, string> DictionaryKeyEnum { get; set; }
            public int I { get; set; }
            public int NI { get; set; }
            public int[] ArrayInt { get; set; }
            public C RecursiveProp { get; set; }
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        public void Apply_should_configure_serializer_when_member_is_an_enum(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation);
            var memberMap = CreateMemberMap(c => c.E);

            subject.Apply(memberMap);

            var serializer = (EnumSerializer<E>)memberMap.GetSerializer();
            serializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_a_nullable_enum(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation);
            var memberMap = CreateMemberMap(c => c.NE);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_an_enum_collection(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, false);
            var memberMap = CreateMemberMap(c => c.ArrayEnum);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_a_nested_enum_collection(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, false);
            var memberMap = CreateMemberMap(c => c.ArrayOfArrayEnum);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)((IChildSerializerConfigurable)serializer.ChildSerializer).ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_an_enum_dictionary(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, false);
            var memberMap = CreateMemberMap(c => c.DictionaryEnum);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_an_enum_dictionary_key(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, false);
            var memberMap = CreateMemberMap(c => c.DictionaryKeyEnum);

            subject.Apply(memberMap);

            var serializer = (IMultipleChildSerializersConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializers[0];
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_configure_serializer_when_member_is_an_enum_dictionary_value(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, false);
            var memberMap = CreateMemberMap(c => c.NestedDictionaryEnum);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)((IChildSerializerConfigurable)serializer.ChildSerializer).ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
        }

        [Theory]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void Apply_should_do_nothing_when_member_is_an_enum_collection_and_top_level_only_is_true(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation, true);
            var memberMap = CreateMemberMap(c => c.ArrayEnum);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializer;
            childSerializer.Representation.Should().Be(BsonType.Int32);
        }

        [Fact]
        public void Apply_should_do_nothing_when_member_is_not_an_enum()
        {
            var subject = new EnumRepresentationConvention(BsonType.String);
            var memberMap = CreateMemberMap(c => c.I);
            var serializer = memberMap.GetSerializer();

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(serializer);
        }

        [Fact]
        public void Apply_should_do_nothing_when_member_is_not_an_enum_and_nullable()
        {
            var subject = new EnumRepresentationConvention(BsonType.String);
            var memberMap = CreateMemberMap(c => c.NI);
            var serializer = memberMap.GetSerializer();

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(serializer);
        }

        [Fact]
        public void Apply_should_do_nothing_when_member_is_not_an_enum_collection()
        {
            var subject = new EnumRepresentationConvention(BsonType.String);
            var memberMap = CreateMemberMap(c => c.ArrayInt);
            var serializer = memberMap.GetSerializer();

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(serializer);
        }

        [Fact]
        public void Convention_should_work_with_recursive_type()
        {
            var pack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };
            ConventionRegistry.Register("enumRecursive", pack, t => t == typeof(C));

            _ = new BsonClassMap<C>(cm => cm.AutoMap()).Freeze();

            ConventionRegistry.Remove("enumRecursive");
        }

        [Theory]
        [InlineData((BsonType)0)]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void constructor_with_representation_should_return_expected_result(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation);

            subject.Representation.Should().Be(representation);
        }

        [Theory]
        [ParameterAttributeData]
        public void constructor_with_representation_and_should_apply_to_collection_should_return_expected_result(
            [Values((BsonType)0, BsonType.Int32, BsonType.Int64, BsonType.String)] BsonType representation,
            [Values(true, false)] bool topLevelOnly)
        {
            var subject = new EnumRepresentationConvention(representation, topLevelOnly);

            subject.Representation.Should().Be(representation);
            subject.TopLevelOnly.Should().Be(topLevelOnly);
        }

        [Theory]
        [InlineData(BsonType.Decimal128)]
        [InlineData(BsonType.Double)]
        public void constructor_should_throw_when_representation_is_not_valid(BsonType representation)
        {
            var exception = Record.Exception(() => new EnumRepresentationConvention(representation));

            var argumentException = exception.Should().BeOfType<ArgumentException>().Subject;
            argumentException.ParamName.Should().Be("representation");
        }

        // private methods
        private BsonMemberMap CreateMemberMap<TMember>(Expression<Func<C, TMember>> member)
        {
            var classMap = new BsonClassMap<C>();
            return classMap.MapMember(member);
        }
    }
}
