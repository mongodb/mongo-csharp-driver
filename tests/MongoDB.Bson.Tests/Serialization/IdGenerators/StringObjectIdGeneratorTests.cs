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
    public class StringObjectIdGeneratorTests
    {
        [Fact]
        public void GenerateId_should_create_unique_ids_on_multiple_calls()
        {
            // Arrange
            var generator = new StringObjectIdGenerator();

            // Act
            var result1 = (string)generator.GenerateId(null, null);
            var result2 = (string)generator.GenerateId(null, null);

            // Assert
            result1.Should().NotBe(result2);
            ObjectId.Parse(result1).Should().NotBe(ObjectId.Parse(result2));
        }

        [Fact]
        public void GenerateId_should_return_string_representation_of_new_ObjectId()
        {
            // Arrange
            var generator = new StringObjectIdGenerator();

            // Act
            var result = generator.GenerateId(null, null);

            // Assert
            result.Should().BeOfType<string>();
            var resultString = (string)result;
            resultString.Should().NotBeNullOrEmpty();
            resultString.Length.Should().Be(24); // ObjectId string representation length
            ObjectId.TryParse(resultString, out var objectId).Should().BeTrue();
            objectId.Should().NotBe(ObjectId.Empty);
        }

        [Fact]
        public void Instance_should_return_singleton_instance()
        {
            // Act
            var instance1 = StringObjectIdGenerator.Instance;
            var instance2 = StringObjectIdGenerator.Instance;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }

        [Fact]
        public void IsEmpty_should_return_false_when_id_is_not_empty()
        {
            // Arrange
            var generator = new StringObjectIdGenerator();
            var id = ObjectId.GenerateNewId().ToString();

            // Act
            var result = generator.IsEmpty(id);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void IsEmpty_should_return_true_when_id_is_null_or_empty(string id)
        {
            // Arrange
            var generator = new StringObjectIdGenerator();

            // Act
            var result = generator.IsEmpty(id);

            // Assert
            result.Should().BeTrue();
        }
    }
}
