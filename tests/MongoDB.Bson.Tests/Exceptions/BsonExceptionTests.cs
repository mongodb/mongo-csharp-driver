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
using Xunit;

namespace MongoDB.Bson.Tests.Exceptions
{
    public class BsonExceptionTests
    {
        [Fact]
        public void Constructor_with_format_and_args_should_format_message_correctly()
        {
            // Act
            var exception = new BsonException("Error code: {0}, message: {1}", 123, "Test error");

            // Assert
            exception.Message.Should().Be("Error code: 123, message: Test error");
        }

        [Fact]
        public void Constructor_with_format_and_args_should_handle_empty_args()
        {
            // Act
            var exception = new BsonException("Simple message");

            // Assert
            exception.Message.Should().Be("Simple message");
        }

        [Fact]
        public void Constructor_with_format_and_args_should_handle_null_args()
        {
            // Act
            var exception = new BsonException("Message with {0}", (object)null);

            // Assert
            exception.Message.Should().Be("Message with ");
        }
    }
}
