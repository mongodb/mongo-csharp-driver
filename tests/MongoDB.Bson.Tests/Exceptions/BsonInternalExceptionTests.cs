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
    public class BsonInternalExceptionTests
    {
        [Fact]
        public void constructor_should_initialize_empty_instance()
        {
            // Act
            var exception = new BsonInternalException();

            // Assert
            exception.Message.Should().Be("Exception of type 'MongoDB.Bson.BsonInternalException' was thrown.");
            exception.InnerException.Should().BeNull();
        }

        [Fact]
        public void constructor_should_initialize_instance_with_message()
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
        public void constructor_should_initialize_instance_with_message_and_inner_exception()
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
        public void constructor_should_initialize_instance_with_serialization_info()
        {
            // Arrange
            var message = "Test serialized internal exception";
            var info = new SerializationInfo(typeof(BsonInternalException), new FormatterConverter());
            info.AddValue("Message", message);
            info.AddValue("ClassName", typeof(BsonInternalException).FullName);
            info.AddValue("Data", null);
            info.AddValue("InnerException", null);
            info.AddValue("HelpURL", null);
            info.AddValue("StackTraceString", null);
            info.AddValue("RemoteStackTraceString", null);
            info.AddValue("RemoteStackIndex", 0);
            info.AddValue("ExceptionMethod", null);
            info.AddValue("HResult", -2146233088);
            info.AddValue("Source", null);
            var context = new StreamingContext();

            // Act
            var exception = new BsonInternalException(info, context);

            // Assert
            exception.Message.Should().Be(message);
        }

        [Fact]
        public void constructor_with_serialization_info_should_preserve_inner_exception()
        {
            // Arrange
            var message = "Test serialized internal exception";
            var innerExceptionMessage = "Inner exception message";
            var innerException = new Exception(innerExceptionMessage);

            // Create an exception with an inner exception
            var originalException = new BsonInternalException(message, innerException);

            // Serialize it
            var info = new SerializationInfo(typeof(BsonInternalException), new FormatterConverter());
            originalException.GetObjectData(info, new StreamingContext());

            // Act
            var deserializedException = new BsonInternalException(info, new StreamingContext());

            // Assert
            deserializedException.Message.Should().Be(message);
            deserializedException.InnerException.Should().NotBeNull();
            deserializedException.InnerException.Message.Should().Be(innerExceptionMessage);
        }

        [Fact]
        public void should_inherit_from_bson_exception()
        {
            // Act
            var exception = new BsonInternalException();

            // Assert
            exception.Should().BeAssignableTo<BsonException>();
        }
    }
}
