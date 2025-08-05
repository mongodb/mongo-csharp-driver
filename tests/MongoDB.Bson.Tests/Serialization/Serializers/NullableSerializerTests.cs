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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Moq;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.Serializers
{
    public class NullableSerializerTests
    {
        [Theory]
        [InlineData(42)]
        [InlineData(null)]
        public void Constructor_with_serializer_should_create_instance_with_assigned_value_serializer(int? testValue)
        {
            // Arrange
            var mockSerializer = new Mock<IBsonSerializer<int>>();
            mockSerializer.SetupGet(s => s.ValueType).Returns(typeof(int));

            // Act
            var subject = new NullableSerializer<int>(mockSerializer.Object);

            // Assert
            subject.Should().NotBeNull();
            subject.ValueSerializer.Should().BeSameAs(mockSerializer.Object);
        }

        [Fact]
        public void Create_should_create_nullable_serializer_with_correct_value_type()
        {
            // Arrange
            var valueSerializer = new Int32Serializer();

            // Act
            var result = NullableSerializer.Create(valueSerializer);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType(typeof(NullableSerializer<int>));
            result.ValueType.Should().Be(typeof(int?));

            var nullableSerializer = (INullableSerializer)result;
            nullableSerializer.ValueSerializer.Should().BeSameAs(valueSerializer);
        }

        [Fact]
        public void Create_should_throw_when_valueSerializer_is_null()
        {
            // Arrange
            IBsonSerializer valueSerializer = null;

            // Act
            Action act = () => NullableSerializer.Create(valueSerializer);
            var exception = Record.Exception(act);

            // Assert
            exception.Should().BeOfType<ArgumentNullException>();
        }
        [Fact]
        public void Equals_null_should_return_false()
        {
            var x = new NullableSerializer<int>();

            var result = x.Equals(null);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_object_should_return_false()
        {
            var x = new NullableSerializer<int>();
            var y = new object();

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void Equals_self_should_return_true()
        {
            var x = new NullableSerializer<int>();

            var result = x.Equals(x);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_equal_fields_should_return_true()
        {
            var x = new NullableSerializer<int>();
            var y = new NullableSerializer<int>();

            var result = x.Equals(y);

            result.Should().Be(true);
        }

        [Fact]
        public void Equals_with_not_equal_field_should_return_false()
        {
            var serializer1 = new Int32Serializer(BsonType.Int32);
            var serializer2 = new Int32Serializer(BsonType.String);
            var x = new NullableSerializer<int>(serializer1);
            var y = new NullableSerializer<int>(serializer2);

            var result = x.Equals(y);

            result.Should().Be(false);
        }

        [Fact]
        public void GetHashCode_should_return_zero()
        {
            var x = new NullableSerializer<int>();

            var result = x.GetHashCode();

            result.Should().Be(0);
        }
    }
}
