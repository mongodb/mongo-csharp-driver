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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
#pragma warning disable CA1040 // Avoid empty interfaces
    public class DocumentSerializerHelperTests
    {
        [Fact]
        public void AreMembersRepresentedAsFields_with_DiscriminatedInterfaceSerializer_should_work()
        {
            var discriminatorConvention = new ScalarDiscriminatorConvention("_t");
            var implementationSerializer = BsonSerializer.LookupSerializer<C>();
            var interfaceSerializer = new ImpliedImplementationInterfaceSerializer<I, C>(implementationSerializer);
            var serializer = new DiscriminatedInterfaceSerializer<I>(discriminatorConvention, interfaceSerializer);

            var result = DocumentSerializerHelper.AreMembersRepresentedAsFields(serializer, out var documentSerializer);

            result.Should().BeTrue();
            documentSerializer.Should().BeSameAs(implementationSerializer);
        }

        [Fact]
        public void AreMembersRepresentedAsFields_with_DowncastingSerializer_should_work()
        {
            var derivedSerializer = BsonSerializer.LookupSerializer<D>();
            var serializer = new DowncastingSerializer<C, D>(derivedSerializer);

            var result = DocumentSerializerHelper.AreMembersRepresentedAsFields(serializer, out var documentSerializer);

            result.Should().BeTrue();
            documentSerializer.Should().BeSameAs(derivedSerializer);
        }

        [Fact]
        public void AreMembersRepresentedAsFields_with_ImpliedImplementationInterfaceSerializer_should_work()
        {
            var implementationSerializer = BsonSerializer.LookupSerializer<C>();
            var serializer = new ImpliedImplementationInterfaceSerializer<I, C>(implementationSerializer);

            var result = DocumentSerializerHelper.AreMembersRepresentedAsFields(serializer, out var documentSerializer);

            result.Should().BeTrue();
            documentSerializer.Should().BeSameAs(implementationSerializer);
        }

        [Theory]
        [InlineData(BsonType.Array, false)]
        [InlineData(BsonType.Document, true)]
        public void AreMembersRepresentedAsFields_with_KeyValuePairSerializer_should_work(BsonType representation, bool expectedResult)
        {
            var serializer = new KeyValuePairSerializer<string, int>(representation);

            var result = DocumentSerializerHelper.AreMembersRepresentedAsFields(serializer, out var documentSerializer);

            result.Should().Be(expectedResult);
            documentSerializer.Should().BeSameAs(expectedResult ? serializer : null);
        }

        [Fact]
        public void GetMemberSerializationInfo_should_throw_when_members_are_not_represented_as_fields()
        {
            var serializer = new DictionaryInterfaceImplementerSerializer<Dictionary<string, int>>(DictionaryRepresentation.Document);

            var exception = Record.Exception(() => DocumentSerializerHelper.GetMemberSerializationInfo(serializer, "abc"));

            exception.Should().BeOfType<NotSupportedException>();
            exception.Message.Should().Contain("does not represent members as fields");
        }

        public class C : I { }

        public class D : C { }

        public interface I { }
    }
#pragma warning restore CA1040 // Avoid empty interfaces
}
