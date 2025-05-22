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
using System.Text.RegularExpressions;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class RegexSerializerTests
    {
        [Theory]
        [InlineData(BsonType.Array)]
        [InlineData(BsonType.Boolean)]
        [InlineData(BsonType.DateTime)]
        [InlineData(BsonType.Document)]
        [InlineData(BsonType.Int32)]
        public void Constructor_with_invalid_representation_should_throw(BsonType representation)
        {
            // Act
            Action act = () => new RegexSerializer(representation);

            // Assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage($"{representation} is not a valid representation for an RegexSerializer.");
        }

        [Theory]
        [InlineData(BsonType.RegularExpression)]
        [InlineData(BsonType.String)]
        public void Constructor_with_valid_representation_should_initialize_instance(BsonType representation)
        {
            // Act
            var subject = new RegexSerializer(representation);

            // Assert
            subject.Representation.Should().Be(representation);
        }

        [Fact]
        public void Constructor_without_parameters_should_use_RegularExpression_representation()
        {
            // Act
            var subject = new RegexSerializer();

            // Assert
            subject.Representation.Should().Be(BsonType.RegularExpression);
        }

        [Fact]
        public void Deserialize_with_invalid_BsonType_should_throw()
        {
            // Arrange
            var subject = new RegexSerializer();
            var context = BsonDeserializationContext.CreateRoot(CreateBsonReaderWithValue(BsonType.Int32));

            // Act
            Action act = () => subject.Deserialize(context, new());

            // Assert
            act.ShouldThrow<FormatException>()
                .WithMessage("Cannot deserialize a 'Regex' from BsonType 'Int32'.");
        }

        [Fact]
        public void Deserialize_with_null_should_return_null()
        {
            // Arrange
            var subject = new RegexSerializer();
            var context = BsonDeserializationContext.CreateRoot(CreateBsonReaderWithValue(BsonType.Null));

            // Act
            var result = subject.Deserialize(context, new());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Deserialize_with_RegularExpression_should_return_regex()
        {
            // Arrange
            var subject = new RegexSerializer();
            var bsonRegex = new BsonRegularExpression("pattern", "i");
            var context = BsonDeserializationContext.CreateRoot(CreateBsonReaderWithValue(BsonType.RegularExpression, bsonRegex));

            // Act
            var result = subject.Deserialize(context, new());

            // Assert
            result.Should().NotBeNull();
            result.Options.Should().Be(RegexOptions.IgnoreCase);
            result.ToString().Should().Contain("pattern");
        }

        [Fact]
        public void Deserialize_with_String_should_return_regex()
        {
            // Arrange
            var subject = new RegexSerializer();
            var pattern = "pattern";
            var context = BsonDeserializationContext.CreateRoot(CreateBsonReaderWithValue(BsonType.String, pattern));

            // Act
            var result = subject.Deserialize(context, new());

            // Assert
            result.Should().NotBeNull();
            result.ToString().Should().Contain(pattern);
        }

        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new RegexSerializer();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new RegexSerializer();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new RegexSerializer();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_different_representation_should_return_false()
        {
            // Arrange
            var subject1 = new RegexSerializer(BsonType.RegularExpression);
            var subject2 = new RegexSerializer(BsonType.String);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_with_different_type_should_return_false()
        {
            // Arrange
            var subject = new RegexSerializer();
            var other = new object();

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new RegexSerializer();
            var y = new RegexSerializer();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var x = new RegexSerializer(BsonType.RegularExpression);
            var y = new RegexSerializer(BsonType.String);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_with_null_should_return_false()
        {
            // Arrange
            var subject = new RegexSerializer();

            // Act
            var result = subject.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_with_same_instance_should_return_true()
        {
            // Arrange
            var subject = new RegexSerializer();

            // Act
            var result = subject.Equals(subject);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_with_same_representation_should_return_true()
        {
            // Arrange
            var subject1 = new RegexSerializer(BsonType.RegularExpression);
            var subject2 = new RegexSerializer(BsonType.RegularExpression);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new RegexSerializer();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }

        [Fact]
        public void IRepresentationConfigurable_WithRepresentation_should_return_correct_instance()
        {
            // Arrange
            IRepresentationConfigurable subject = new RegexSerializer(BsonType.RegularExpression);

            // Act
            var result = subject.WithRepresentation(BsonType.String);

            // Assert
            result.Should().BeOfType<RegexSerializer>();
            ((RegexSerializer)result).Representation.Should().Be(BsonType.String);
        }

        [Fact]
        public void RegularExpressionInstance_should_return_cached_instance()
        {
            // Act
            var instance = RegexSerializer.RegularExpressionInstance;

            // Assert
            instance.Should().NotBeNull();
            instance.Representation.Should().Be(BsonType.RegularExpression);
        }

        [Fact]
        public void Serialize_with_null_value_should_write_null()
        {
            // Arrange
            var subject = new RegexSerializer();
            var writerMock = new Mock<IBsonWriter>();
            var context = BsonSerializationContext.CreateRoot(writerMock.Object);
            var args = new BsonSerializationArgs();

            // Act
            subject.Serialize(context, args, null);

            // Assert
            writerMock.Verify(x => x.WriteNull(), Times.Once);
        }

        [Fact]
        public void Serialize_with_RegularExpression_representation_should_write_regex()
        {
            // Arrange
            var subject = new RegexSerializer(BsonType.RegularExpression);
            var writerMock = new Mock<IBsonWriter>();
            var context = BsonSerializationContext.CreateRoot(writerMock.Object);
            var regex = new Regex("pattern");

            // Act
            subject.Serialize(context, new(), regex);

            // Assert
            writerMock.Verify(x => x.WriteRegularExpression(It.IsAny<BsonRegularExpression>()), Times.Once);
        }

        [Fact]
        public void Serialize_with_String_representation_should_write_string()
        {
            // Arrange
            var subject = new RegexSerializer(BsonType.String);
            var writerMock = new Mock<IBsonWriter>();
            var context = BsonSerializationContext.CreateRoot(writerMock.Object);
            var regex = new Regex("pattern");

            // Act
            subject.Serialize(context, new(), regex);

            // Assert
            writerMock.Verify(x => x.WriteString(regex.ToString()), Times.Once);
        }

        [Fact]
        public void WithRepresentation_with_different_representation_should_return_new_instance()
        {
            // Arrange
            var subject = new RegexSerializer(BsonType.RegularExpression);

            // Act
            var result = subject.WithRepresentation(BsonType.String);

            // Assert
            result.Should().NotBeSameAs(subject);
            result.Representation.Should().Be(BsonType.String);
        }

        [Fact]
        public void WithRepresentation_with_same_representation_should_return_same_instance()
        {
            // Arrange
            var subject = new RegexSerializer(BsonType.RegularExpression);

            // Act
            var result = subject.WithRepresentation(BsonType.RegularExpression);

            // Assert
            result.Should().BeSameAs(subject);
        }

        private IBsonReader CreateBsonReaderWithValue(BsonType bsonType, object value = null)
        {
            var readerMock = new Mock<IBsonReader>();
            readerMock.Setup(r => r.GetCurrentBsonType()).Returns(bsonType);

            if (bsonType == BsonType.Null)
            {
                readerMock.Setup(r => r.ReadNull());
            }
            else if (bsonType == BsonType.RegularExpression)
            {
                readerMock.Setup(r => r.ReadRegularExpression()).Returns((BsonRegularExpression)value);
            }
            else if (bsonType == BsonType.String)
            {
                readerMock.Setup(r => r.ReadString()).Returns((string)value);
            }

            return readerMock.Object;
        }
    }
}
