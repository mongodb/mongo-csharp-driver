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
    public class BsonSymbolTests
    {
        [Fact]
        public void BsonType_should_return_Symbol()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");

            // Act
            var bsonType = symbol.BsonType;

            // Assert
            bsonType.Should().Be(BsonType.Symbol);
        }

        [Fact]
        public void CompareTo_BsonSymbol_should_compare_names()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("abc");
            var symbol2 = BsonSymbolTable.Lookup("def");
            var symbol3 = BsonSymbolTable.Lookup("abc");

            // Act
            var result1 = symbol1.CompareTo(symbol2);
            var result2 = symbol2.CompareTo(symbol1);
            var result3 = symbol1.CompareTo(symbol3);

            // Assert
            result1.Should().BeLessThan(0);
            result2.Should().BeGreaterThan(0);
            result3.Should().Be(0);
        }

        [Fact]
        public void CompareTo_BsonSymbol_should_return_1_when_other_is_null()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol.CompareTo((BsonSymbol)null);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_compare_names_when_other_is_BsonString()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("abc");
            var bsonString = new BsonString("def");

            // Act
            var result = symbol.CompareTo(bsonString);

            // Assert
            result.Should().BeLessThan(0);
        }

        [Fact]
        public void CompareTo_BsonValue_should_compare_names_when_other_is_BsonSymbol()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("abc");
            var symbol2 = BsonSymbolTable.Lookup("def");
            BsonValue value2 = symbol2;

            // Act
            var result = symbol1.CompareTo(value2);

            // Assert
            result.Should().BeLessThan(0);
        }
        [Fact]
        public void CompareTo_BsonValue_should_return_1_when_other_is_null()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol.CompareTo((BsonValue)null);

            // Assert
            result.Should().Be(1);
        }

        [Fact]
        public void CompareTo_BsonValue_should_use_CompareTypeTo_when_other_is_different_BsonType()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("abc");
            var int32 = new BsonInt32(123);

            // Act
            var result = symbol.CompareTo(int32);

            // Assert
            // Symbol's enum value is greater than Int32's enum value
            result.Should().BeGreaterThan(0);
        }
        [Fact]
        public void Create_should_map_value_to_BsonSymbol()
        {
            // Arrange
            var value = "test";

            // Act
            var symbol = BsonSymbol.Create(value);

            // Assert
            symbol.Should().NotBeNull();
            symbol.Name.Should().Be(value);
        }

        [Fact]
        public void Create_should_throw_when_value_is_null()
        {
            // Act
            Action act = () => BsonSymbol.Create(null);

            // Assert
            act.ShouldThrow<ArgumentNullException>()
                .And.ParamName.Should().Be("value");
        }

        [Fact]
        public void Equality_operator_should_return_true_when_symbols_are_same_instance()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("test");
            var symbol2 = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol1 == symbol2;

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_BsonSymbol_should_return_false_when_other_is_null()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol.Equals((BsonSymbol)null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_BsonSymbol_should_return_true_when_symbols_are_same_instance()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("test");
            var symbol2 = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol1.Equals(symbol2);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void Equals_object_should_return_false_when_other_is_different_type()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");
            object other = "test";

            // Act
            var result = symbol.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_false_when_other_is_null()
        {
            // Arrange
            var symbol = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_object_should_return_true_when_other_is_same_symbol()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("test");
            object symbol2 = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol1.Equals(symbol2);

            // Assert
            result.Should().BeTrue();
        }
        [Fact]
        public void GetHashCode_should_return_same_value_for_same_symbol()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("test");
            var symbol2 = BsonSymbolTable.Lookup("test");

            // Act
            var hashCode1 = symbol1.GetHashCode();
            var hashCode2 = symbol2.GetHashCode();

            // Assert
            hashCode1.Should().Be(hashCode2);
        }

        [Fact]
        public void Implicit_operator_string_to_BsonSymbol_should_lookup_symbol()
        {
            // Arrange
            string symbolName = "test";

            // Act
            BsonSymbol symbol = symbolName;

            // Assert
            symbol.Should().NotBeNull();
            symbol.Name.Should().Be(symbolName);
        }

        [Fact]
        public void Inequality_operator_should_return_false_when_symbols_are_same_instance()
        {
            // Arrange
            var symbol1 = BsonSymbolTable.Lookup("test");
            var symbol2 = BsonSymbolTable.Lookup("test");

            // Act
            var result = symbol1 != symbol2;

            // Assert
            result.Should().BeFalse();
        }
        [Fact]
        public void ToString_should_return_name()
        {
            // Arrange
            var symbolName = "test";
            var symbol = BsonSymbolTable.Lookup(symbolName);

            // Act
            var result = symbol.ToString();

            // Assert
            result.Should().Be(symbolName);
        }
    }
}
