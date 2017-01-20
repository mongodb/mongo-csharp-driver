/* Copyright 2010-2016 MongoDB Inc.
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
using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
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
            public int I { get; set; }
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
        public void Apply_should_configure_serializer_when_member_is_a_nullable_enum(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation);
            var memberMap = CreateMemberMap(c => c.NE);

            subject.Apply(memberMap);

            var serializer = (IChildSerializerConfigurable)memberMap.GetSerializer();
            var childSerializer = (EnumSerializer<E>)serializer.ChildSerializer;
            childSerializer.Representation.Should().Be(representation);
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

        [Theory]
        [InlineData(0)]
        [InlineData(BsonType.Int32)]
        [InlineData(BsonType.Int64)]
        [InlineData(BsonType.String)]
        public void constructor_should_initialize_instance_when_representation_is_valid(BsonType representation)
        {
            var subject = new EnumRepresentationConvention(representation);

            subject.Representation.Should().Be(representation);
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
