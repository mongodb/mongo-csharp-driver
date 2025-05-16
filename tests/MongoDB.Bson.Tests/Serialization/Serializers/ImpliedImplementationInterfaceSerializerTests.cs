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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class ImpliedImplementationInterfaceSerializerTests
    {
        // Test interfaces and implementations
        public interface ITestInterface { }

        public class TestImplementation : ITestInterface { }

        public class DerivedImplementation : TestImplementation { }

        [Fact]
        public void Constructor_should_throw_when_implementation_serializer_is_null()
        {
            // Arrange
            IBsonSerializer<TestImplementation> implementationSerializer = null;

            // Act
            Action act = () => new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(implementationSerializer);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("implementationSerializer");
        }

        [Fact]
        public void Constructor_should_throw_when_TInterface_is_not_interface()
        {
            // Act
            Action act = () => new ImpliedImplementationInterfaceSerializer<TestImplementation, TestImplementation>();

            // Assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage($"{typeof(TestImplementation).FullName} is not an interface.*")
                .And.ParamName.Should().Be("<TInterface>");
        }

        [Fact]
        public void Constructor_with_implementation_serializer_should_initialize_instance()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;

            // Act
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(implementationSerializer);

            // Assert
            subject.Should().NotBeNull();
            subject.ImplementationSerializer.Should().BeSameAs(implementationSerializer);
        }
        [Fact]
        public void Constructor_with_serializer_registry_should_initialize_instance()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var mockRegistry = new Mock<IBsonSerializerRegistry>();
            mockRegistry.Setup(r => r.GetSerializer<TestImplementation>()).Returns(implementationSerializer);

            // Act
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(mockRegistry.Object);
            var actualSerializer = subject.ImplementationSerializer;

            // Assert
            subject.Should().NotBeNull();
            actualSerializer.Should().Be(implementationSerializer);

            mockRegistry.Verify(r => r.GetSerializer<TestImplementation>(), Times.Once);
        }

        [Fact]
        public void Constructor_with_serializer_registry_should_throw_when_registry_is_null()
        {
            // Arrange
            IBsonSerializerRegistry serializerRegistry = null;

            // Act
            Action act = () => new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(serializerRegistry);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("serializerRegistry");
        }
        [Fact]
        public void Deserialize_should_delegate_to_implementation_serializer()
        {
            // Arrange
            var expected = new TestImplementation();
            var mockImplementationSerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockImplementationSerializer.Setup(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>()))
                .Returns(expected);

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockImplementationSerializer.Object);

            var json = "{ }";
            var bsonDocument = BsonDocument.Parse("{ value: " + json + " }");
            using var reader = new BsonDocumentReader(bsonDocument);
            reader.ReadStartDocument();
            reader.ReadName("value");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            // Act
            var result = subject.Deserialize(context, args);

            // Assert
            result.Should().BeSameAs(expected);
            mockImplementationSerializer.Verify(s => s.Deserialize(context, args), Times.Once);
        }

        [Fact]
        public void Deserialize_should_deserialize_null_as_default_value()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(implementationSerializer);

            var json = "null";
            var bsonDocument = BsonDocument.Parse("{ value: " + json + " }");
            using var reader = new BsonDocumentReader(bsonDocument);
            reader.ReadStartDocument();
            reader.ReadName("value");
            var context = BsonDeserializationContext.CreateRoot(reader);
            var args = new BsonDeserializationArgs();

            // Act
            var result = subject.Deserialize(context, args);

            // Assert
            result.Should().BeNull();
            reader.ReadEndDocument();
        }

        [Fact]
        public void DictionaryRepresentation_should_return_value_from_implementation_serializer()
        {
            // Arrange
            var expected = DictionaryRepresentation.ArrayOfDocuments;
            var mockDictionarySerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockDictionarySerializer.As<IBsonDictionarySerializer>()
                .Setup(s => s.DictionaryRepresentation)
                .Returns(expected);

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockDictionarySerializer.Object);

            // Act
            var result = subject.DictionaryRepresentation;

            // Assert
            result.Should().Be(expected);
            mockDictionarySerializer.As<IBsonDictionarySerializer>().Verify(s => s.DictionaryRepresentation, Times.Once);
        }

        [Fact]
        public void DictionaryRepresentation_should_throw_when_implementation_serializer_is_not_IDictionarySerializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            Action act = () => { var _ = subject.DictionaryRepresentation; };

            // Assert
            act.ShouldThrow<NotSupportedException>()
                .WithMessage($"{BsonUtils.GetFriendlyTypeName(implementationSerializer.GetType())} does not have a DictionaryRepresentation.*");
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_should_return_false_when_implementation_serializers_are_not_equal()
        {
            // Arrange
            var implementationSerializer1 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var implementationSerializer2 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject1 = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer1);
            var subject2 = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer2);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_false_when_other_is_null()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_should_return_true_when_implementation_serializers_are_equal()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject1 = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);
            var subject2 = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_should_return_true_when_same_instance()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject.Equals(subject);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();
            var y = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var int32Serializer1 = new Int32Serializer(BsonType.Int32);
            var int32Serializer2 = new Int32Serializer(BsonType.String);
            var implementationSerializer1 = new EnumerableInterfaceImplementerSerializer<List<int>, int>(int32Serializer1);
            var implementationSerializer2 = new EnumerableInterfaceImplementerSerializer<List<int>, int>(int32Serializer2);
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>(implementationSerializer1);
            var y = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>(implementationSerializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new ImpliedImplementationInterfaceSerializer<IEnumerable<int>, List<int>>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void IChildSerializerConfigurable_ChildSerializer_should_return_implementation_serializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);
            var configurable = (IChildSerializerConfigurable)subject;

            // Act
            var result = configurable.ChildSerializer;

            // Assert
            result.Should().BeSameAs(implementationSerializer);
        }

        [Fact]
        public void IChildSerializerConfigurable_WithChildSerializer_should_return_new_instance_with_updated_serializer()
        {
            // Arrange
            var implementationSerializer1 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var implementationSerializer2 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer1);
            var configurable = (IChildSerializerConfigurable)subject;

            // Act
            var result = configurable.WithChildSerializer(implementationSerializer2);

            // Assert
            result.Should().NotBeSameAs(subject);
            result.Should().BeOfType<ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>>();
            var typedResult = (ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>)result;
            typedResult.ImplementationSerializer.Should().BeSameAs(implementationSerializer2);
        }

        [Fact]
        public void IImpliedImplementationInterfaceSerializer_ImplementationSerializer_should_return_implementation_serializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);
            var interfaceSerializer = (IImpliedImplementationInterfaceSerializer)subject;

            // Act
            var result = interfaceSerializer.ImplementationSerializer;

            // Assert
            result.Should().BeSameAs(implementationSerializer);
        }

        [Fact]
        public void KeySerializer_should_return_value_from_implementation_serializer()
        {
            // Arrange
            var expected = new StringSerializer();
            var mockDictionarySerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockDictionarySerializer.As<IBsonDictionarySerializer>()
                .Setup(s => s.KeySerializer)
                .Returns(expected);

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockDictionarySerializer.Object);

            // Act
            var result = subject.KeySerializer;

            // Assert
            result.Should().BeSameAs(expected);
            mockDictionarySerializer.As<IBsonDictionarySerializer>().Verify(s => s.KeySerializer, Times.Once);
        }

        [Fact]
        public void KeySerializer_should_throw_when_implementation_serializer_is_not_IDictionarySerializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            Action act = () => { var _ = subject.KeySerializer; };

            // Assert
            act.ShouldThrow<NotSupportedException>()
                .WithMessage($"{BsonUtils.GetFriendlyTypeName(implementationSerializer.GetType())} does not have a KeySerializer.*");
        }

        [Fact]
        public void Serialize_should_delegate_to_implementation_serializer_when_value_type_matches_TImplementation()
        {
            // Arrange
            var mockImplementationSerializer = new Mock<IBsonSerializer<TestImplementation>>();
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockImplementationSerializer.Object);

            ITestInterface value = new TestImplementation();
            var bsonDocument = new BsonDocument();
            using var writer = new BsonDocumentWriter(bsonDocument);
            writer.WriteStartDocument();
            writer.WriteName("value");
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();

            // Act
            subject.Serialize(context, args, value);
            //writer.WriteEndDocument();

            // Assert
            mockImplementationSerializer.Verify(s => s.Serialize(context, args, (TestImplementation)value), Times.Once);
        }

        [Fact]
        public void Serialize_should_use_appropriate_serializer_when_value_type_is_not_TImplementation()
        {
            // Arrange
            // We'll need to set up BsonSerializer.LookupSerializer, which is a static method
            // This test demonstrates the behavior by checking the document structure

            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            ITestInterface value = new DerivedImplementation();
            var bsonDocument = new BsonDocument();
            using var writer = new BsonDocumentWriter(bsonDocument);
            writer.WriteStartDocument();
            writer.WriteName("value");
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();

            // The test will pass if no exception is thrown and the document is not null

            // Act
            subject.Serialize(context, args, value);
            writer.WriteEndDocument();

            // Assert
            bsonDocument["value"].Should().NotBeNull();
        }

        [Fact]
        public void Serialize_should_write_null_when_value_is_null()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            ITestInterface value = null;
            var bsonDocument = new BsonDocument();
            using var writer = new BsonDocumentWriter(bsonDocument);
            writer.WriteStartDocument();
            writer.WriteName("value");
            var context = BsonSerializationContext.CreateRoot(writer);
            var args = new BsonSerializationArgs();

            // Act
            subject.Serialize(context, args, value);
            writer.WriteEndDocument();

            // Assert
            bsonDocument["value"].Should().Be(BsonNull.Value);
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_delegate_to_implementation_serializer()
        {
            // Arrange
            var expectedInfo = new BsonSerializationInfo("item", new Int32Serializer(), typeof(int));
            var mockArraySerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockArraySerializer.As<IBsonArraySerializer>()
                .Setup(s => s.TryGetItemSerializationInfo(out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns((out BsonSerializationInfo info) =>
                {
                    info = expectedInfo;
                    return true;
                });

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockArraySerializer.Object);

            // Act
            var result = subject.TryGetItemSerializationInfo(out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().BeSameAs(expectedInfo);
            mockArraySerializer.As<IBsonArraySerializer>().Verify(
                s => s.TryGetItemSerializationInfo(out It.Ref<BsonSerializationInfo>.IsAny),
                Times.Once);
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_return_false_when_implementation_serializer_is_not_IArraySerializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject.TryGetItemSerializationInfo(out var serializationInfo);

            // Assert
            result.Should().BeFalse();
            serializationInfo.Should().BeNull();
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_delegate_to_implementation_serializer()
        {
            // Arrange
            var memberName = "memberName";
            var expectedInfo = new BsonSerializationInfo(memberName, new Int32Serializer(), typeof(int));
            var mockDocumentSerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockDocumentSerializer.As<IBsonDocumentSerializer>()
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns((string name, out BsonSerializationInfo info) =>
                {
                    info = expectedInfo;
                    return true;
                });

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockDocumentSerializer.Object);

            // Act
            var result = subject.TryGetMemberSerializationInfo(memberName, out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().BeSameAs(expectedInfo);
            mockDocumentSerializer.As<IBsonDocumentSerializer>().Verify(
                s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny),
                Times.Once);
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_return_false_when_implementation_serializer_is_not_IDocumentSerializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject.TryGetMemberSerializationInfo("memberName", out var serializationInfo);

            // Assert
            result.Should().BeFalse();
            serializationInfo.Should().BeNull();
        }

        [Fact]
        public void ValueSerializer_should_return_value_from_implementation_serializer()
        {
            // Arrange
            var expected = new Int32Serializer();
            var mockDictionarySerializer = new Mock<IBsonSerializer<TestImplementation>>();
            mockDictionarySerializer.As<IBsonDictionarySerializer>()
                .Setup(s => s.ValueSerializer)
                .Returns(expected);

            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                mockDictionarySerializer.Object);

            // Act
            var result = subject.ValueSerializer;

            // Assert
            result.Should().BeSameAs(expected);
            mockDictionarySerializer.As<IBsonDictionarySerializer>().Verify(s => s.ValueSerializer, Times.Once);
        }

        [Fact]
        public void ValueSerializer_should_throw_when_implementation_serializer_is_not_IDictionarySerializer()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            Action act = () => { var _ = subject.ValueSerializer; };

            // Assert
            act.ShouldThrow<NotSupportedException>()
                .WithMessage($"{BsonUtils.GetFriendlyTypeName(implementationSerializer.GetType())} does not have a ValueSerializer.*");
        }
        [Fact]
        public void WithImplementationSerializer_should_return_new_instance_when_serializer_is_different()
        {
            // Arrange
            var implementationSerializer1 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var implementationSerializer2 = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer1);

            // Act
            var result = subject.WithImplementationSerializer(implementationSerializer2);

            // Assert
            result.Should().NotBeSameAs(subject);
            result.ImplementationSerializer.Should().BeSameAs(implementationSerializer2);
        }

        [Fact]
        public void WithImplementationSerializer_should_return_same_instance_when_serializer_is_same()
        {
            // Arrange
            var implementationSerializer = new Mock<IBsonSerializer<TestImplementation>>().Object;
            var subject = new ImpliedImplementationInterfaceSerializer<ITestInterface, TestImplementation>(
                implementationSerializer);

            // Act
            var result = subject.WithImplementationSerializer(implementationSerializer);

            // Assert
            result.Should().BeSameAs(subject);
        }
    }
}
