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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class CommandResponseMessageBinaryEncoderTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();
            var wrappedEncoder = new CommandMessageBinaryEncoder(stream, encoderSettings);

            var result = new CommandResponseMessageBinaryEncoder(wrappedEncoder);

            result._wrappedEncoder().Should().BeSameAs(wrappedEncoder);
        }

        [Fact]
        public void ReadMessage_should_delegate_to_wrapped_encoder()
        {
            var document = new BsonDocument("x", 1);
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance) };
            var message = new CommandMessage(1, 2, sections, false);
            var bytes = CreateMessageBytes(message);
            var stream = new MemoryStream(bytes);
            var subject = CreateSubject(stream);

            var result = subject.ReadMessage();

            var resultMessage = result.Should().BeOfType<CommandResponseMessage>().Subject;
            var wrappedMessage = resultMessage.WrappedMessage.Should().BeOfType<CommandMessage>().Subject;
            wrappedMessage.RequestId.Should().Be(message.RequestId);
            wrappedMessage.ResponseTo.Should().Be(message.ResponseTo);
            wrappedMessage.MoreToCome.Should().Be(message.MoreToCome);
            wrappedMessage.Sections.Should().HaveCount(1);
            var wrappedMessageType0Section = wrappedMessage.Sections[0].Should().BeOfType<Type0CommandMessageSection<RawBsonDocument>>().Subject;
            wrappedMessageType0Section.Document.Should().Be(document);
        }

        [Fact]
        public void WriteMessage_should_delegate_to_wrapped_encoder()
        {
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var document = new BsonDocument("x", 1);
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance) };
            var wrappedMessage = new CommandMessage(1, 2, sections, false);
            var message = new CommandResponseMessage(wrappedMessage);
            var expectedBytes = CreateMessageBytes(wrappedMessage);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            result.Should().Equal(expectedBytes);
        }

        // private methods
        private byte[] CreateMessageBytes(CommandMessage message)
        {
            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();
            var encoder = new CommandMessageBinaryEncoder(stream, encoderSettings);
            encoder.WriteMessage(message);
            return stream.ToArray();
        }

        private CommandResponseMessageBinaryEncoder CreateSubject(Stream stream)
        {
            var encoderSettings = new MessageEncoderSettings();
            var wrappedEncoder = new CommandMessageBinaryEncoder(stream, encoderSettings);
            return new CommandResponseMessageBinaryEncoder(wrappedEncoder);
        }
    }

    public static class CommandResponseMessageBinaryEncoderReflector
    {
        public static CommandMessageBinaryEncoder _wrappedEncoder(this CommandResponseMessageBinaryEncoder obj)
        {
            var fieldInfo = typeof(CommandResponseMessageBinaryEncoder).GetField("_wrappedEncoder", BindingFlags.NonPublic | BindingFlags.Instance);
            return (CommandMessageBinaryEncoder)fieldInfo.GetValue(obj);
        }
    }
}
