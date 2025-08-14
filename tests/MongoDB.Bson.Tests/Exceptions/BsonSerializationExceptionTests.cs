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
    public class BsonSerializationExceptionTests
    {
        [Fact]
        public void Constructor_with_message_and_inner_exception_should_initialize_properties()
        {
            // Arrange
            var message = "Test error message";
            var innerException = new ArgumentException("Inner exception message");

            // Act
            var exception = new BsonSerializationException(message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeSameAs(innerException);
        }
    }
}
