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
using MongoDB.Shared;
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonJavaScriptTests
    {
        [Fact]
        public void BsonType_should_return_JavaScript()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);

            // Act
            var result = subject.BsonType;

            // Assert
            result.Should().Be(BsonType.JavaScript);
        }

        [Fact]
        public void Code_should_return_code_provided_in_constructor()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);

            // Act
            var result = subject.Code;

            // Assert
            result.Should().Be(code);
        }

        [Fact]
        public void CompareTo_BsonJavaScript_should_return_0_when_codes_are_equal()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);
            var other = new BsonJavaScript(code);

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CompareTo_BsonJavaScript_should_return_1_when_other_is_null()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");

            // Act
            var result = subject.CompareTo((BsonJavaScript)null);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonJavaScript_should_return_negative_when_this_code_is_less_than_other_code()
        {
            // Arrange
            var subject = new BsonJavaScript("a");
            var other = new BsonJavaScript("b");

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().BeLessThan(0);
        }

        [Fact]
        public void CompareTo_BsonJavaScript_should_return_positive_when_this_code_is_greater_than_other_code()
        {
            // Arrange
            var subject = new BsonJavaScript("b");
            var other = new BsonJavaScript("a");

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().BeGreaterThan(0);
        }
        [Fact]
        public void CompareTo_BsonValue_should_compare_code_when_other_is_BsonJavaScript()
        {
            // Arrange
            var subject = new BsonJavaScript("a");
            BsonValue other = new BsonJavaScript("b");

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().BeLessThan(0);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_1_when_other_is_null()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");

            // Act
            var result = subject.CompareTo((BsonValue)null);

            // Assert
            result.Should().Be(1);
        }
        [Fact]
        public void CompareTo_BsonValue_should_use_CompareTypeTo_when_other_is_not_BsonJavaScript()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");
            var other = new BsonInt32(1);

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().NotBe(0);
            // The actual comparison result depends on BsonType enum values
        }

        [Fact]
        public void Constructor_should_initialize_Code_property()
        {
            // Arrange
            var code = "function() { return 1; }";

            // Act
            var subject = new BsonJavaScript(code);

            // Assert
            subject.Code.Should().Be(code);
        }

        [Fact]
        public void Constructor_should_throw_when_code_is_null()
        {
            // Arrange & Act
            Action act = () => new BsonJavaScript(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("code");
        }

        [Fact]
        public void Create_should_map_value_to_BsonJavaScript()
        {
            // Arrange
            var code = "function() { return 1; }";

            // Act
            var result = BsonJavaScript.Create(code);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be(code);
        }

        [Fact]
        public void Create_should_throw_when_value_is_null()
        {
            // Act
            Action act = () => BsonJavaScript.Create(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Equality_operator_should_return_false_when_codes_are_not_equal()
        {
            // Arrange
            var lhs = new BsonJavaScript("function() { return 1; }");
            var rhs = new BsonJavaScript("function() { return 2; }");

            // Act
            var result = lhs == rhs;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equality_operator_should_return_true_when_codes_are_equal()
        {
            // Arrange
            var code = "function() { return 1; }";
            var lhs = new BsonJavaScript(code);
            var rhs = new BsonJavaScript(code);

            // Act
            var result = lhs == rhs;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_BsonJavaScript_should_return_false_when_codes_are_not_equal()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");
            var other = new BsonJavaScript("function() { return 2; }");

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonJavaScript_should_return_false_when_other_is_different_type()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");
            var other = new BsonJavaScriptWithScope("function() { return 1; }", new BsonDocument());

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonJavaScript_should_return_false_when_other_is_null()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");

            // Act
            var result = subject.Equals((BsonJavaScript)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonJavaScript_should_return_true_when_codes_are_equal()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);
            var other = new BsonJavaScript(code);

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_object_should_return_false_when_other_is_not_BsonJavaScript()
        {
            // Arrange
            var subject = new BsonJavaScript("function() { return 1; }");
            object other = "function() { return 1; }";

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_true_when_other_is_BsonJavaScript_with_equal_code()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);
            object other = new BsonJavaScript(code);

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void GetHashCode_should_return_combination_of_BsonType_and_code_hash_codes()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);

            // Calculate expected hash code using same algorithm as in the class
            int expectedHash = 17;
            expectedHash = 37 * expectedHash + Hasher.GetHashCode(BsonType.JavaScript);
            expectedHash = 37 * expectedHash + code.GetHashCode();

            // Act
            var result = subject.GetHashCode();

            // Assert
            result.Should().Be(expectedHash);
        }

        [Fact]
        public void Implicit_conversion_from_string_should_create_BsonJavaScript_instance()
        {
            // Arrange
            var code = "function() { return 1; }";

            // Act
            BsonJavaScript result = code;

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be(code);
        }

        [Fact]
        public void Inequality_operator_should_return_false_when_codes_are_equal()
        {
            // Arrange
            var code = "function() { return 1; }";
            var lhs = new BsonJavaScript(code);
            var rhs = new BsonJavaScript(code);

            // Act
            var result = lhs != rhs;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Inequality_operator_should_return_true_when_codes_are_not_equal()
        {
            // Arrange
            var lhs = new BsonJavaScript("function() { return 1; }");
            var rhs = new BsonJavaScript("function() { return 2; }");

            // Act
            var result = lhs != rhs;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ToString_should_return_formatted_string_representation()
        {
            // Arrange
            var code = "function() { return 1; }";
            var subject = new BsonJavaScript(code);
            var expected = $"new BsonJavaScript(\"{code}\")";

            // Act
            var result = subject.ToString();

            // Assert
            result.Should().Be(expected);
        }
    }
}
