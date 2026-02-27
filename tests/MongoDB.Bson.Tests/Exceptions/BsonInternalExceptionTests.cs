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

namespace MongoDB.Bson.Tests.Exceptions
{
    public class BsonInternalExceptionTests
    {
        [Fact]
        public void Constructor_should_initialize_empty_instance()
        {
            // Act
            var exception = new BsonInternalException();

            // Assert
            exception.Message.Should().Be("Exception of type 'MongoDB.Bson.BsonInternalException' was thrown.");
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_should_initialize_instance_with_message()
        {
            // Arrange
            var message = "Test internal exception message";

            // Act
            var exception = new BsonInternalException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void Constructor_should_initialize_instance_with_message_and_inner_exception()
        {
            // Arrange
            var message = "Test internal exception message";
            var innerException = new Exception("Inner exception message");

            // Act
            var exception = new BsonInternalException(message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeSameAs(innerException);
        }

        [Fact]
        public void Should_inherit_from_bson_exception()
        {
            // Act
            var exception = new BsonInternalException();

            // Assert
            exception.Should().BeAssignableTo<BsonException>();
        }
    }
}
