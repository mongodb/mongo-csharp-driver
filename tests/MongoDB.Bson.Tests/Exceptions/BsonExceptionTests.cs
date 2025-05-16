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
using System.Runtime.Serialization;
using FluentAssertions;
using Xunit;

namespace MongoDB.Bson.Tests.Exceptions
{
    public class BsonExceptionTests
    {
        [Fact]
        public void constructor_with_format_and_args_should_format_message_correctly()
        {
            // Act
            var exception = new BsonException("Error code: {0}, message: {1}", 123, "Test error");

            // Assert
            exception.Message.Should().Be("Error code: 123, message: Test error");
        }

        [Fact]
        public void constructor_with_format_and_args_should_handle_empty_args()
        {
            // Act
            var exception = new BsonException("Simple message");

            // Assert
            exception.Message.Should().Be("Simple message");
        }

        [Fact]
        public void constructor_with_format_and_args_should_handle_null_args()
        {
            // Act
            var exception = new BsonException("Message with {0}", (object)null);

            // Assert
            exception.Message.Should().Be("Message with ");
        }

        [Fact]
        public void constructor_with_serialization_info_should_deserialize_correctly()
        {
            // Arrange
            var expectedMessage = "Test serialized exception";
            var info = new SerializationInfo(typeof(BsonException), new FormatterConverter());
            info.AddValue("Message", expectedMessage);
            info.AddValue("ClassName", typeof(BsonException).FullName);
            info.AddValue("Data", null);
            info.AddValue("InnerException", null);
            info.AddValue("HelpURL", null);
            info.AddValue("StackTraceString", null);
            info.AddValue("RemoteStackTraceString", null);
            info.AddValue("RemoteStackIndex", 0);
            info.AddValue("ExceptionMethod", null);
            info.AddValue("HResult", -2146233088);
            info.AddValue("Source", null);

            // Act
            var exception = new BsonException(info, new StreamingContext());

            // Assert
            exception.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void constructor_with_serialization_info_should_preserve_inner_exception()
        {
            // Arrange
            var innerExceptionMessage = "Inner exception message";
            var innerException = new InvalidOperationException(innerExceptionMessage);
            var originalException = new BsonException("Outer message", innerException);

            var info = new SerializationInfo(typeof(BsonException), new FormatterConverter());
            originalException.GetObjectData(info, new StreamingContext());

            // Act
            var deserializedException = new BsonException(info, new StreamingContext());

            // Assert
            deserializedException.InnerException.Should().NotBeNull();
            deserializedException.InnerException.Message.Should().Be(innerExceptionMessage);
        }
    }
}
