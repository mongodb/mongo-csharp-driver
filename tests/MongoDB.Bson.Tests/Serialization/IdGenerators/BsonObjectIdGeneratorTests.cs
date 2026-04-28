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
using MongoDB.Bson.Serialization.IdGenerators;
using Xunit;

namespace MongoDB.Bson.Tests.Serialization.IdGenerators
{
    public class BsonObjectIdGeneratorTests
    {
        [Fact]
        public void GenerateId_should_create_new_BsonObjectId_with_unique_ObjectId()
        {
            // Arrange
            var generator = new BsonObjectIdGenerator();

            // Act
            var result1 = generator.GenerateId(null, null);
            var result2 = generator.GenerateId(null, null);

            // Assert
            result1.Should().BeOfType<BsonObjectId>();
            result2.Should().BeOfType<BsonObjectId>();
            result1.Should().NotBeSameAs(result2);
            ((BsonObjectId)result1).Value.Should().NotBe(ObjectId.Empty);
            ((BsonObjectId)result2).Value.Should().NotBe(ObjectId.Empty);
            ((BsonObjectId)result1).Value.Should().NotBe(((BsonObjectId)result2).Value);
        }

        [Fact]
        public void Instance_should_return_singleton_instance()
        {
            // Act
            var instance1 = BsonObjectIdGenerator.Instance;
            var instance2 = BsonObjectIdGenerator.Instance;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void IsEmpty_should_return_false_when_id_contains_non_empty_ObjectId()
        {
            // Arrange
            var generator = new BsonObjectIdGenerator();
            var id = new BsonObjectId(ObjectId.GenerateNewId());

            // Act
            var result = generator.IsEmpty(id);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void IsEmpty_should_return_true_when_id_contains_ObjectId_Empty()
        {
            // Arrange
            var generator = new BsonObjectIdGenerator();
            var id = new BsonObjectId(ObjectId.Empty);

            // Act
            var result = generator.IsEmpty(id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_should_return_true_when_id_is_BsonNull()
        {
            // Arrange
            var generator = new BsonObjectIdGenerator();
            var id = BsonNull.Value;

            // Act
            var result = generator.IsEmpty(id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsEmpty_should_return_true_when_id_is_null()
        {
            // Arrange
            var generator = new BsonObjectIdGenerator();

            // Act
            var result = generator.IsEmpty(null);

            // Assert
            result.Should().BeTrue();
        }
    }
}
