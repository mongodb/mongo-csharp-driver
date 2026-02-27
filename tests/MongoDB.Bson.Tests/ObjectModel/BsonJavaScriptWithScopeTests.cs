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
using Xunit;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class BsonJavaScriptWithScopeTests
    {
        [Fact]
        public void BsonType_should_return_JavaScriptWithScope()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);

            // Act
            var result = subject.BsonType;

            // Assert
            result.Should().Be(BsonType.JavaScriptWithScope);
        }

        [Fact]
        public void Clone_should_create_shallow_copy()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);

            // Act
            var result = subject.Clone();

            // Assert
            result.Should().NotBeSameAs(subject);
            result.Should().BeOfType<BsonJavaScriptWithScope>();
            var clone = (BsonJavaScriptWithScope)result;
            clone.Code.Should().Be(subject.Code);
            clone.Scope.Should().NotBeSameAs(subject.Scope);
            clone.Scope["x"].Should().Be(1);
            clone.Scope["y"].Should().Be(2);
        }

        [Fact]
        public void CompareTo_BsonJavaScriptWithScope_should_compare_code_first_then_scope()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 2 }, { "y", 3 } };
            var subject1 = new BsonJavaScriptWithScope("a", scope1);
            var subject2 = new BsonJavaScriptWithScope("b", scope1);
            var subject3 = new BsonJavaScriptWithScope("a", scope2);

            // Act
            var result1 = subject1.CompareTo(subject2); // code comparison: a < b
            var result2 = subject2.CompareTo(subject1); // code comparison: b > a
            var result3 = subject1.CompareTo(subject3); // same code, different scope
            var result4 = subject1.CompareTo(subject1); // same instance

            // Assert
            result1.Should().BeLessThan(0);
            result2.Should().BeGreaterThan(0);
            result3.Should().BeLessThan(0); // scope1 < scope2
            result4.Should().Be(0);
        }

        [Fact]
        public void CompareTo_BsonJavaScriptWithScope_should_return_1_when_other_is_null()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);
            BsonJavaScriptWithScope other = null;

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_return_1_when_other_is_null()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);
            BsonValue other = null;

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_use_CompareTo_when_other_is_BsonJavaScriptWithScope()
        {
            // Arrange
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope("a", scope);
            BsonValue other = new BsonJavaScriptWithScope("b", scope);

            // Act
            var result = subject.CompareTo(other);

            // Assert
            result.Should().BeLessThan(0);
        }

        [Fact]
        public void CompareTo_BsonValue_should_use_CompareTypeTo_when_other_is_not_BsonJavaScriptWithScope()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);
            var other = new BsonInt32(1);

            // Act
            var result = subject.CompareTo(other);

            // Assert
            // BsonJavaScriptWithScope has a higher BsonType enum value than BsonInt32
            result.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Constructor_should_initialize_instance()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };

            // Act
            var result = new BsonJavaScriptWithScope(code, scope);

            // Assert
            result.Code.Should().Be(code);
            result.Scope.Should().BeSameAs(scope);
        }

        [Fact]
        public void Constructor_should_throw_when_scope_is_null()
        {
            // Arrange
            var code = "function() { return x + y; }";
            BsonDocument scope = null;

            // Act
            Action act = () => new BsonJavaScriptWithScope(code, scope);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("scope");
        }

        [Fact]
        public void DeepClone_should_create_deep_copy()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var nestedDoc = new BsonDocument { { "a", 3 } };
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 }, { "z", nestedDoc } };
            var subject = new BsonJavaScriptWithScope(code, scope);

            // Act
            var result = subject.DeepClone();

            // Assert
            result.Should().NotBeSameAs(subject);
            result.Should().BeOfType<BsonJavaScriptWithScope>();
            var clone = (BsonJavaScriptWithScope)result;
            clone.Code.Should().Be(subject.Code);
            clone.Scope.Should().NotBeSameAs(subject.Scope);
            clone.Scope["x"].Should().Be(1);
            clone.Scope["y"].Should().Be(2);
            clone.Scope["z"].Should().NotBeSameAs(nestedDoc);
            clone.Scope["z"].AsBsonDocument["a"].Should().Be(3);

            // Modify the nested document to ensure deep clone
            nestedDoc["a"] = 4;
            clone.Scope["z"].AsBsonDocument["a"].Should().Be(3); // Should still be 3
        }

        [Fact]
        public void Equality_operator_should_handle_null()
        {
            // Arrange
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope("code", scope);
            BsonJavaScriptWithScope nullValue = null;

            // Act
            var resultLeftNull = nullValue == subject;
            var resultRightNull = subject == nullValue;

            // Assert
            resultLeftNull.Should().BeFalse();
            resultRightNull.Should().BeFalse();
        }

        [Fact]
        public void Equality_operator_should_return_false_when_not_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 3 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);
            var subject3 = new BsonJavaScriptWithScope("different", scope1);

            // Act
            var resultDifferentScope = subject1 == subject2;
            var resultDifferentCode = subject1 == subject3;

            // Assert
            resultDifferentScope.Should().BeFalse();
            resultDifferentCode.Should().BeFalse();
        }

        [Fact]
        public void Equality_operator_should_return_true_when_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);

            // Act
            var result = subject1 == subject2;

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_BsonJavaScriptWithScope_should_return_false_when_not_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 3 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);
            var subject3 = new BsonJavaScriptWithScope("different", scope1);

            // Act
            var resultDifferentScope = subject1.Equals(subject2);
            var resultDifferentCode = subject1.Equals(subject3);

            // Assert
            resultDifferentScope.Should().BeFalse();
            resultDifferentCode.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonJavaScriptWithScope_should_return_false_when_other_is_null()
        {
            // Arrange
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope("code", scope);
            BsonJavaScriptWithScope other = null;

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonJavaScriptWithScope_should_return_true_when_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_object_should_return_false_when_other_is_different_type()
        {
            // Arrange
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope("code", scope);
            object other = "not a BsonJavaScriptWithScope";

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_false_when_other_is_null()
        {
            // Arrange
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope("code", scope);
            object other = null;

            // Act
            var result = subject.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_true_when_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            object subject2 = new BsonJavaScriptWithScope("code", scope2);

            // Act
            var result = subject1.Equals(subject2);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void GetHashCode_should_return_different_values_for_unequal_instances()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 3 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);
            var subject3 = new BsonJavaScriptWithScope("different", scope1);

            // Act
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();
            var hashCode3 = subject3.GetHashCode();

            // Assert
            hashCode1.Should().NotBe(hashCode2);
            hashCode1.Should().NotBe(hashCode3);
        }

        [Fact]
        public void GetHashCode_should_return_same_value_for_equal_instances()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);

            // Act
            var hashCode1 = subject1.GetHashCode();
            var hashCode2 = subject2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2);
        }
        [Fact]
        public void Inequality_operator_should_return_false_when_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);

            // Act
            var result = subject1 != subject2;

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Inequality_operator_should_return_true_when_not_equal()
        {
            // Arrange
            var scope1 = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var scope2 = new BsonDocument { { "x", 1 }, { "y", 3 } };
            var subject1 = new BsonJavaScriptWithScope("code", scope1);
            var subject2 = new BsonJavaScriptWithScope("code", scope2);
            var subject3 = new BsonJavaScriptWithScope("different", scope1);

            // Act
            var resultDifferentScope = subject1 != subject2;
            var resultDifferentCode = subject1 != subject3;

            // Assert
            resultDifferentScope.Should().BeTrue();
            resultDifferentCode.Should().BeTrue();
        }

        [Fact]
        public void Scope_property_should_return_the_scope()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);

            // Act
            var result = subject.Scope;

            // Assert
            result.Should().BeSameAs(scope);
        }

        [Fact]
        public void ToString_should_return_formatted_string()
        {
            // Arrange
            var code = "function() { return x + y; }";
            var scope = new BsonDocument { { "x", 1 }, { "y", 2 } };
            var subject = new BsonJavaScriptWithScope(code, scope);
            var expectedString = "new BsonJavaScript(\"function() { return x + y; }\", { \"x\" : 1, \"y\" : 2 })";

            // Act
            var result = subject.ToString();

            // Assert
            result.Should().Be(expectedString);
        }
    }
}
