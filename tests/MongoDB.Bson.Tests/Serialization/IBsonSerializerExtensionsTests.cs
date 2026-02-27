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

using FluentAssertions;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization
{
    public class IBsonSerializerExtensionsTests
    {
        [Fact]
        public void ToBsonValue_generic_should_convert_value_to_BsonValue()
        {
            // Arrange
            var serializer = new Int32Serializer();
            var value = 42;

            // Act
            var result = serializer.ToBsonValue(value);

            // Assert
            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(value);
        }

        [Fact]
        public void ToBsonValue_generic_should_handle_complex_objects()
        {
            // Arrange
            var serializer = new BsonDocumentSerializer();
            var document = new BsonDocument
            {
                { "field1", 123 },
                { "field2", "value" }
            };

            // Act
            var result = serializer.ToBsonValue(document);

            // Assert
            result.Should().BeOfType<BsonDocument>();
            result.AsBsonDocument.Should().Be(document);
        }

        [Fact]
        public void ToBsonValue_generic_should_handle_null_value()
        {
            // Arrange
            var serializer = new StringSerializer();
            string value = null;

            // Act
            var result = serializer.ToBsonValue(value);

            // Assert
            result.Should().BeOfType<BsonNull>();
        }

        [Fact]
        public void ToBsonValue_generic_should_use_correct_serialization_context()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<int>>();
            mockSerializer.Setup(s => s.ValueType).Returns(typeof(int));
            mockSerializer
                .Setup(s => s.Serialize(
                    It.IsAny<BsonSerializationContext>(),
                    It.IsAny<BsonSerializationArgs>(),
                    It.IsAny<int>()))
                .Callback<BsonSerializationContext, BsonSerializationArgs, int>((context, args, value) =>
                {
                    // Write a known value to verify it was called correctly
                    context.Writer.WriteInt32(42);
                });

            // Act
            var result = mockSerializer.Object.ToBsonValue(123);

            // Assert
            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(42);
        }

        [Fact]
        public void ToBsonValue_generic_should_wrap_value_in_document()
        {
            // Arrange
            var serializer = new StringSerializer();
            var value = "test";

            // Act
            var result = serializer.ToBsonValue(value);

            // Assert
            result.Should().BeOfType<BsonString>();
            result.AsString.Should().Be(value);
        }

        [Fact]
        public void ToBsonValue_non_generic_should_convert_value_to_BsonValue()
        {
            // Arrange
            var serializer = new Int32Serializer();
            var value = 42;

            // Act
            var result = serializer.ToBsonValue(value);

            // Assert
            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(value);
        }

        [Fact]
        public void ToBsonValue_non_generic_should_handle_complex_objects()
        {
            // Arrange
            var documentSerializer = new BsonDocumentSerializer();
            var document = new BsonDocument
            {
                { "field1", 123 },
                { "field2", "value" }
            };

            // Act
            var result = documentSerializer.ToBsonValue(document);

            // Assert
            result.Should().BeOfType<BsonDocument>();
            result.AsBsonDocument.Should().Be(document);
        }

        [Fact]
        public void ToBsonValue_non_generic_should_handle_null_value()
        {
            // Arrange
            var serializer = new StringSerializer();
            string value = null;

            // Act
            var result = serializer.ToBsonValue((object)value);

            // Assert
            result.Should().BeOfType<BsonNull>();
        }

        [Fact]
        public void ToBsonValue_non_generic_should_use_correct_serialization_context()
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer>();
            mockSerializer.Setup(s => s.ValueType).Returns(typeof(int));
            mockSerializer
                .Setup(s => s.Serialize(
                    It.IsAny<BsonSerializationContext>(),
                    It.IsAny<BsonSerializationArgs>(),
                    It.IsAny<object>()))
                .Callback<BsonSerializationContext, BsonSerializationArgs, object>((context, args, value) =>
                {
                    // Write a known value to verify it was called correctly
                    context.Writer.WriteInt32(42);
                });

            // Act
            var result = mockSerializer.Object.ToBsonValue(123);

            // Assert
            result.Should().BeOfType<BsonInt32>();
            result.AsInt32.Should().Be(42);
        }

        [Fact]
        public void ToBsonValue_non_generic_should_wrap_value_in_document()
        {
            // Arrange
            var serializer = new StringSerializer();
            var value = "test";

            // Act
            var result = serializer.ToBsonValue(value);

            // Assert
            result.Should().BeOfType<BsonString>();
            result.AsString.Should().Be(value);
        }
    }
}
