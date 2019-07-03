/* Copyright 2019-present MongoDB Inc.
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
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Conventions
{
    public class StringIdStoredAsObjectIdConventionTests
    {
        private readonly IBsonSerializer _defaultInt32Serializer = BsonSerializer.LookupSerializer(typeof(int));
        private readonly IBsonSerializer _defaultStringSerializer = BsonSerializer.LookupSerializer(typeof(string));

        [Fact]
        public void Apply_should_ignore_any_member_that_is_not_the_id()
        {
            var subject = CreateSubject();
            var memberMap = GetMemberMap<TestClassWithStringId>("X");

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(_defaultStringSerializer);
            memberMap.IdGenerator.Should().BeNull();
        }

        [Fact]
        public void Apply_should_ignore_any_id_that_is_not_of_type_string()
        {
            var subject = CreateSubject();
            var memberMap = GetIdMemberMap<TestClassWithIntId>();

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(_defaultInt32Serializer);
            memberMap.IdGenerator.Should().BeNull();
        }

        [Fact]
        public void Apply_should_ignore_any_id_that_already_has_a_serializer_configured()
        {
            var subject = CreateSubject();
            var memberMap = GetIdMemberMap<TestClassWithStringId>();
            var serializer = new StringSerializer();
            memberMap.SetSerializer(serializer);

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(serializer);
            memberMap.IdGenerator.Should().BeNull();
        }

        [Fact]
        public void Apply_should_ignore_any_id_that_already_has_an_idGenerator_configured()
        {
            var subject = CreateSubject();
            var memberMap = GetIdMemberMap<TestClassWithStringId>();
            var idGenerator = Mock.Of<IIdGenerator>();
            memberMap.SetIdGenerator(idGenerator);

            subject.Apply(memberMap);

            memberMap.GetSerializer().Should().BeSameAs(_defaultStringSerializer);
            memberMap.IdGenerator.Should().BeSameAs(idGenerator);
        }

        [Fact]
        public void Apply_should_configure_id_serializer_and_idGenerator()
        {
            var subject = CreateSubject();
            var memberMap = GetIdMemberMap<TestClassWithStringId>();

            subject.Apply(memberMap);

            var stringSerializer = memberMap.GetSerializer().Should().BeOfType<StringSerializer>().Subject;
            stringSerializer.Representation.Should().Be(BsonType.ObjectId);
            memberMap.IdGenerator.Should().BeOfType<StringObjectIdGenerator>();
        }

        // private methods
        private StringIdStoredAsObjectIdConvention CreateSubject()
            => new StringIdStoredAsObjectIdConvention();

        private BsonMemberMap GetIdMemberMap<T>()
            => GetMemberMap<T>("Id");

        private BsonMemberMap GetMemberMap<T>(string memberName)
            => new BsonClassMap<T>(cm => cm.AutoMap()).GetMemberMap(memberName);

        // nested types
        private class TestClassWithIntId
        {
            public int Id { get; set; }
        }

        private class TestClassWithStringId
        {
            public string Id { get; set; }
            public string X { get; set; }
        }
    }
}
