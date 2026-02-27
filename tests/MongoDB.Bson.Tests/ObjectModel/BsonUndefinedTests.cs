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
using MongoDB.Shared;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonUndefinedTests
    {
        [Fact]
        public void BsonType_should_return_Undefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.BsonType;

            // Assert
            result.Should().Be(BsonType.Undefined);
        }

        [Fact]
        public void CompareTo_BsonUndefined_should_return_0_when_comparing_to_another_BsonUndefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            var other = BsonUndefined.Value;

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CompareTo_BsonUndefined_should_return_1_when_other_is_null()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.CompareTo((BsonUndefined)null);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_0_when_other_is_BsonUndefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            BsonValue other = BsonUndefined.Value;

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_1_when_other_is_BsonMinKey()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            BsonValue other = BsonMinKey.Value;

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_1_when_other_is_null()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.CompareTo((BsonValue)null);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_negative_1_when_other_is_neither_BsonUndefined_nor_BsonMinKey()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            BsonValue other = new BsonInt32(1);

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(-1);
        }

        [Fact]
        public void Equality_operator_should_return_false_when_one_operand_is_null()
        {
            // Arrange
            var lhs = BsonUndefined.Value;
            BsonUndefined rhs = null;

            // Act
            var result = lhs == rhs;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equality_operator_should_return_true_when_both_operands_are_BsonUndefined()
        {
            // Arrange
            var lhs = BsonUndefined.Value;
            var rhs = BsonUndefined.Value;

            // Act
            var result = lhs == rhs;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_BsonUndefined_should_return_false_when_other_is_different_type()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            var other = new object();

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonUndefined_should_return_false_when_other_is_null()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.Equals((BsonUndefined)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonUndefined_should_return_true_when_other_is_BsonUndefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            var other = BsonUndefined.Value;

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_object_should_return_false_when_other_is_different_type()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            object other = new BsonInt32(1);

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_false_when_other_is_null()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.Equals((object)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_true_when_other_is_BsonUndefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;
            object other = BsonUndefined.Value;

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_should_return_hash_code_of_BsonType()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.GetHashCode();

            // Assert
            result.Should().Be(Hasher.GetHashCode(BsonType.Undefined));
        }

        [Fact]
        public void Inequality_operator_should_return_false_when_both_operands_are_BsonUndefined()
        {
            // Arrange
            var lhs = BsonUndefined.Value;
            var rhs = BsonUndefined.Value;

            // Act
            var result = lhs != rhs;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Inequality_operator_should_return_true_when_one_operand_is_null()
        {
            // Arrange
            var lhs = BsonUndefined.Value;
            BsonUndefined rhs = null;

            // Act
            var result = lhs != rhs;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ToBoolean_should_return_false()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.ToBoolean();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ToString_should_return_BsonUndefined()
        {
            // Arrange
            var subject = BsonUndefined.Value;

            // Act
            var result = subject.ToString();

            // Assert
            result.Should().Be("BsonUndefined");
        }

        [Fact]
        public void Value_should_return_singleton_instance()
        {
            // Act
            var instance1 = BsonUndefined.Value;
            var instance2 = BsonUndefined.Value;

            // Assert
            instance1.Should().NotBeNull();
            instance1.Should().BeSameAs(instance2);
        }
    }
}
