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
    public class DowncastingSerializerTests
    {
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
    }
}
