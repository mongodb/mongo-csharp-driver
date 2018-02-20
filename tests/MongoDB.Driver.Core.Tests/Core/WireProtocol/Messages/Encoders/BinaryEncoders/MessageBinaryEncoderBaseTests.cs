/* Copyright 2018-present MongoDB Inc.
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

using System.IO;
using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class MessageBinaryEncoderBaseTests
    {
        [Theory]
        [ParameterAttributeData]
        public void MaxMessageSize_should_return_expected_result(
            [Values(null, 1, 2)] int? maxMessageSize)
        {
            var encoderSettings = new MessageEncoderSettings();
            encoderSettings.Add(MessageEncoderSettingsName.MaxMessageSize, maxMessageSize);
            var subject = CreateSubject(encoderSettings: encoderSettings);

            var result = subject.MaxMessageSize();

            result.Should().Be(maxMessageSize);
        }

        // private methods
        private MessageBinaryEncoderBase CreateSubject(
            Stream stream = null,
            MessageEncoderSettings encoderSettings = null)
        {
            stream = stream ?? new MemoryStream();
            encoderSettings = encoderSettings ?? new MessageEncoderSettings();
            return new FakeMessageBinaryEncoder(stream, encoderSettings);
        }

        // nested types
        private class FakeMessageBinaryEncoder : MessageBinaryEncoderBase
        {
            public FakeMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
                : base(stream, encoderSettings)
            {
            }
        }
    }

    public static class MessageBinaryEncoderBaseReflector
    {
        public static MessageEncoderSettings _encoderSettings(this MessageBinaryEncoderBase obj)
        {
            var fieldInfo = typeof(MessageBinaryEncoderBase).GetField("_encoderSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            return (MessageEncoderSettings)fieldInfo.GetValue(obj);
        }

        public static Stream _stream(this MessageBinaryEncoderBase obj)
        {
            var fieldInfo = typeof(MessageBinaryEncoderBase).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance);
            return (Stream)fieldInfo.GetValue(obj);
        }

        public static int? MaxMessageSize(this MessageBinaryEncoderBase obj)
        {
            var propertyInfo = typeof(MessageBinaryEncoderBase).GetProperty("MaxMessageSize", BindingFlags.NonPublic | BindingFlags.Instance);
            return (int?)propertyInfo.GetValue(obj);
        }
    }
}
