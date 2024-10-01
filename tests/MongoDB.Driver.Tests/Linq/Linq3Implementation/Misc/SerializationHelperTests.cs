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

using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Linq.Linq3Implementation.Misc;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Linq.Linq3Implementation.Jira
{
#pragma warning disable CA1040 // Avoid empty interfaces
    public class SerializationHelperTests
    {
        [Theory]
        [InlineData(BsonType.Array, false)]
        [InlineData(BsonType.String, true)]
        public void EnsureRepresentationIsArray_should_work(BsonType type, bool shouldThrow)
        {
            var expression = Expression.Constant(1);
            var serializer = new FakeSerializerWithRepresentation<C>(type);

            var exception = Record.Exception(() => SerializationHelper.EnsureRepresentationIsArray(expression, serializer));

            if (shouldThrow)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("the expression is not represented as an array in the database");
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(BsonType.Decimal128, false)]
        [InlineData(BsonType.Double, false)]
        [InlineData(BsonType.Int32, false)]
        [InlineData(BsonType.Int64, false)]
        [InlineData(BsonType.String, true)]
        public void EnsureRepresentationIsNumeric_should_work(BsonType type, bool shouldThrow)
        {
            var expression = Expression.Constant(1);
            var subExpression = Expression.Constant(2);
            var serializer = new FakeSerializerWithRepresentation<C>(type);

            var exception = Record.Exception(() => SerializationHelper.EnsureRepresentationIsNumeric(expression, subExpression, serializer));

            if (shouldThrow)
            {
                exception.Should().BeOfType<ExpressionNotSupportedException>();
                exception.Message.Should().Contain("uses a non-numeric representation");
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        public void GetRepresentation_with_DiscriminatedInterfaceSerializer_should_work()
        {
            var discriminatorConvention = new ScalarDiscriminatorConvention("_t");
            var interfaceSerializer = new FakeSerializerWithRepresentation<I>(BsonType.String);
            var serializer = new DiscriminatedInterfaceSerializer<I>(discriminatorConvention, interfaceSerializer);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.String);
        }

        [Fact]
        public void GetRepresentation_with_DowncastingSerializer_should_work()
        {
            var derivedSerializer = new FakeSerializerWithRepresentation<D>(BsonType.String);
            var serializer = new DowncastingSerializer<C, D>(derivedSerializer);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.String);
        }

        [Fact]
        public void GetRepresentation_with_ImpliedImplementationInterfaceSerializer_should_work()
        {
            var implementationSerializer = new FakeSerializerWithRepresentation<C>(BsonType.String);
            var serializer = new ImpliedImplementationInterfaceSerializer<I, C>(implementationSerializer);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.String);
        }

        [Fact]
        public void GetRepresentation_with_serializer_implementing_IHasRepresentationSerializer_should_work()
        {
            var serializer = new FakeSerializerWithRepresentation<C>(BsonType.String);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.String);
        }

        [Theory]
        [InlineData(DictionaryRepresentation.Document, BsonType.Document)]
        [InlineData(DictionaryRepresentation.ArrayOfArrays, BsonType.Array)]
        [InlineData(DictionaryRepresentation.ArrayOfDocuments, BsonType.Array)]
        public void GetRepresentation_with_serializer_implementing_IBsonDictionarySerializer_should_work(DictionaryRepresentation dictionaryRepresentation, BsonType expectedResult)
        {
            var serializer = new DictionaryInterfaceImplementerSerializer<Dictionary<string, int>, string, int>(dictionaryRepresentation);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(BsonType.Array)]
        [InlineData(BsonType.Document)]
        public void GetRepresentation_with_serializer_implementing_IKeyValuePairSerializer_should_work(BsonType representation)
        {
            var serializer = new KeyValuePairSerializer<string, int>(representation);

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(representation);
        }

        [Fact]
        public void GetRepresentation_with_serializer_implementing_IBsonDocumentSerializer_should_work()
        {
            var serializer = new FakeDocumentSerializer();

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.Document);
        }

        [Fact]
        public void GetRepresentation_with_serializer_implementing_IBsonArraySerializer_should_work()
        {
            var serializer = new FakeArraySerializer();

            var result = SerializationHelper.GetRepresentation(serializer);

            result.Should().Be(BsonType.Array);
        }

        [Theory]
        [InlineData(BsonType.Document, true)]
        [InlineData(BsonType.String, false)]
        public void IsRepresentedAsDocument_should_work(BsonType representation, bool expectedResult)
        {
            var serializer = new FakeSerializerWithRepresentation<C>(representation);

            var result = SerializationHelper.IsRepresentedAsDocument(serializer);

            result.Should().Be(expectedResult);
        }

        public class C : I { }

        public class D : C { }

        public interface I { }

        public class FakeSerializerWithRepresentation<TValue> : ClassSerializerBase<TValue>, IHasRepresentationSerializer
            where TValue : class
        {
            private readonly BsonType _representation;

            public FakeSerializerWithRepresentation(BsonType representation)
            {
                _representation = representation;
            }

            public BsonType Representation => _representation;
        }

        public class FakeDocumentSerializer : ClassSerializerBase<object>, IBsonDocumentSerializer
        {
            public bool TryGetMemberSerializationInfo(string memberName, out BsonSerializationInfo serializationInfo) => throw new System.NotImplementedException();
        }

        public class FakeArraySerializer : ClassSerializerBase<object>, IBsonArraySerializer
        {
            public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo) => throw new System.NotImplementedException();
        }
    }
#pragma warning restore CA1040 // Avoid empty interfaces
}
