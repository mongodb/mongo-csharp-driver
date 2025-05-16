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
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class DowncastingSerializerTests
    {
        [Fact]
        public void BaseType_property_should_return_correct_type()
        {
            // Arrange
            var derivedSerializer = new StringSerializer();
            var subject = new DowncastingSerializer<object, string>(derivedSerializer);

            // Act
            var result = subject.BaseType;

            // Assert
            result.Should().Be(typeof(object));
        }

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Arrange
            var mockDerivedSerializer = new Mock<IBsonSerializer<string>>();

            // Act
            var subject = new MongoDB.Bson.Serialization.Serializers.DowncastingSerializer<object, string>(mockDerivedSerializer.Object);

            // Assert
            subject.DerivedSerializer.Should().BeSameAs(mockDerivedSerializer.Object);
            subject.BaseType.Should().Be(typeof(object));
            subject.DerivedType.Should().Be(typeof(string));
        }

        [Fact]
        public void Constructor_should_throw_when_derivedSerializer_is_null()
        {
            // Act
            Action act = () => new MongoDB.Bson.Serialization.Serializers.DowncastingSerializer<object, string>(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("derivedSerializer");
        }

        [Fact]
        public void Create_should_create_instance_with_correct_types()
        {
            // Arrange
            var baseType = typeof(object);
            var derivedType = typeof(string);
            var serializer = new StringSerializer();

            // Act
            var result = MongoDB.Bson.Serialization.Serializers.DowncastingSerializer.Create(
                baseType, derivedType, serializer);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType(typeof(MongoDB.Bson.Serialization.Serializers.DowncastingSerializer<object, string>));
            var downcastingSerializer = (MongoDB.Bson.Serialization.Serializers.IDowncastingSerializer)result;
            downcastingSerializer.BaseType.Should().Be(baseType);
            downcastingSerializer.DerivedType.Should().Be(derivedType);
            downcastingSerializer.DerivedSerializer.Should().BeSameAs(serializer);
        }

        [Fact]
        public void DerivedSerializer_property_should_return_provided_serializer()
        {
            // Arrange
            var derivedSerializer = new StringSerializer();
            var subject = new DowncastingSerializer<object, string>(derivedSerializer);

            // Act
            var result = subject.DerivedSerializer;

            // Assert
            result.Should().BeSameAs(derivedSerializer);
        }

        [Fact]
        public void DerivedType_property_should_return_correct_type()
        {
            // Arrange
            var derivedSerializer = new StringSerializer();
            var subject = new DowncastingSerializer<object, string>(derivedSerializer);

            // Act
            var result = subject.DerivedType;

            // Assert
            result.Should().Be(typeof(string));
        }

        [Fact]
        public void Deserialize_should_delegate_to_derived_serializer()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<string>>();
            var expectedResult = "test";
            mockSerializer
                .Setup(s => s.Deserialize(It.IsAny<BsonDeserializationContext>(), It.IsAny<BsonDeserializationArgs>()))
                .Returns(expectedResult);

            var subject = new DowncastingSerializer<object, string>(mockSerializer.Object);
            var context = BsonDeserializationContext.CreateRoot(new Mock<IBsonReader>().Object);
            var args = new BsonDeserializationArgs();

            // Act
            var result = subject.Deserialize(context, args);

            // Assert
            result.Should().Be(expectedResult);
            mockSerializer.Verify(s => s.Deserialize(context, args), Times.Once);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new DowncastingSerializer<object, int>(Int32Serializer.Instance);

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new DowncastingSerializer<object, int>(Int32Serializer.Instance);
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new DowncastingSerializer<object, int>(Int32Serializer.Instance);

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new DowncastingSerializer<object, int>(Int32Serializer.Instance);
            var y = new DowncastingSerializer<object, int>(Int32Serializer.Instance);

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new DowncastingSerializer<object, int>(new Int32Serializer(BsonType.Int32));
            var y = new DowncastingSerializer<object, int>(new Int32Serializer(BsonType.String));

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new DowncastingSerializer<object, int>(Int32Serializer.Instance);

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void IDowncastingSerializer_DerivedSerializer_should_return_same_serializer()
        {
            // Arrange
            var derivedSerializer = new StringSerializer();
            var subject = new DowncastingSerializer<object, string>(derivedSerializer);
            var downcastingSerializer = (IDowncastingSerializer)subject;

            // Act
            var result = downcastingSerializer.DerivedSerializer;

            // Assert
            result.Should().BeSameAs(derivedSerializer);
        }
        [Fact]
        public void Serialize_should_delegate_to_derived_serializer_with_downcast_value()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<string>>();
            var subject = new DowncastingSerializer<object, string>(mockSerializer.Object);
            var context = BsonSerializationContext.CreateRoot(new Mock<IBsonWriter>().Object);
            var args = new BsonSerializationArgs();
            var value = "test";

            // Act
            subject.Serialize(context, args, value);

            // Assert
            mockSerializer.Verify(s => s.Serialize(context, It.IsAny<BsonSerializationArgs>(), value), Times.Once);
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_delegate_to_derived_serializer_when_it_implements_IBsonArraySerializer()
        {
            // Arrange
            var mockArraySerializer = new Mock<IBsonSerializer<string>>();
            mockArraySerializer.As<IBsonArraySerializer>()
                .Setup(s => s.TryGetItemSerializationInfo(out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns((out BsonSerializationInfo info) =>
                {
                    info = new BsonSerializationInfo("item", new StringSerializer(), typeof(string));
                    return true;
                });

            var subject = new DowncastingSerializer<object, string>(mockArraySerializer.Object);

            // Act
            var result = subject.TryGetItemSerializationInfo(out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().NotBeNull();
            serializationInfo.ElementName.Should().Be("item");
            serializationInfo.NominalType.Should().Be(typeof(string));
            mockArraySerializer.As<IBsonArraySerializer>()
                .Verify(s => s.TryGetItemSerializationInfo(out It.Ref<BsonSerializationInfo>.IsAny), Times.Once);
        }

        [Fact]
        public void TryGetItemSerializationInfo_should_throw_when_derived_serializer_does_not_implement_IBsonArraySerializer()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<string>>();
            var subject = new DowncastingSerializer<object, string>(mockSerializer.Object);

            // Act
            Action act = () => subject.TryGetItemSerializationInfo(out _);

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage($"The class {mockSerializer.Object.GetType().FullName} does not implement IBsonArraySerializer.");
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_delegate_to_derived_serializer_when_it_implements_IBsonDocumentSerializer()
        {
            // Arrange
            var memberName = "testMember";
            var mockDocumentSerializer = new Mock<IBsonSerializer<string>>();
            mockDocumentSerializer.As<IBsonDocumentSerializer>()
                .Setup(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny))
                .Returns((string name, out BsonSerializationInfo info) =>
                {
                    info = new BsonSerializationInfo(name, new StringSerializer(), typeof(string));
                    return true;
                });

            var subject = new DowncastingSerializer<object, string>(mockDocumentSerializer.Object);

            // Act
            var result = subject.TryGetMemberSerializationInfo(memberName, out var serializationInfo);

            // Assert
            result.Should().BeTrue();
            serializationInfo.Should().NotBeNull();
            serializationInfo.ElementName.Should().Be(memberName);
            serializationInfo.NominalType.Should().Be(typeof(string));
            mockDocumentSerializer.As<IBsonDocumentSerializer>()
                .Verify(s => s.TryGetMemberSerializationInfo(memberName, out It.Ref<BsonSerializationInfo>.IsAny), Times.Once);
        }

        [Fact]
        public void TryGetMemberSerializationInfo_should_throw_when_derived_serializer_does_not_implement_IBsonDocumentSerializer()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<string>>();
            var subject = new DowncastingSerializer<object, string>(mockSerializer.Object);
            var memberName = "testMember";

            // Act
            Action act = () => subject.TryGetMemberSerializationInfo(memberName, out _);

            // Assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage($"The class {mockSerializer.Object.GetType().FullName} does not implement IBsonDocumentSerializer.");
        }
    }
}
