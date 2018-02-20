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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class CommandResponseMessageJsonEncoderTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var textReader = new StringReader("");
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();
            var wrappedEncoder = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);

            var result = new CommandResponseMessageJsonEncoder(wrappedEncoder);

            result._wrappedEncoder().Should().BeSameAs(wrappedEncoder);
        }

        [Fact]
        public void ReadMessage_should_delegate_to_wrapped_encoder()
        {
            var document = new BsonDocument("x", 1);
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance) };
            var message = new CommandMessage(1, 2, sections, false);
            var json = CreateMessageJson(message);
            var subject = CreateSubject(json);

            var result = subject.ReadMessage();

            var resultMessage = result.Should().BeOfType<CommandResponseMessage>().Subject;
            var wrappedMessage = resultMessage.WrappedMessage.Should().BeOfType<CommandMessage>().Subject;
            wrappedMessage.RequestId.Should().Be(message.RequestId);
            wrappedMessage.ResponseTo.Should().Be(message.ResponseTo);
            wrappedMessage.MoreToCome.Should().Be(message.MoreToCome);
            wrappedMessage.Sections.Should().HaveCount(1);
            var wrappedMessageType0Section = wrappedMessage.Sections[0].Should().BeOfType<Type0CommandMessageSection<BsonDocument>>().Subject;
            wrappedMessageType0Section.Document.Should().Be(document);
        }

        [Fact]
        public void WriteMessage_should_delegate_to_wrapped_encoder()
        {
            var textWriter = new StringWriter();
            var subject = CreateSubject(textWriter: textWriter);
            var document = new BsonDocument("x", 1);
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance) };
            var wrappedMessage = new CommandMessage(1, 2, sections, false);
            var message = new CommandResponseMessage(wrappedMessage);
            var expectedJson = CreateMessageJson(wrappedMessage);

            subject.WriteMessage(message);
            var result = textWriter.ToString();

            result.Should().Be(expectedJson);
        }

        // private methods
        private string CreateMessageJson(CommandMessage message)
        {
            var textReader = new StringReader("");
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();
            var encoder = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
            encoder.WriteMessage(message);
            return textWriter.ToString();
        }

        private CommandResponseMessageJsonEncoder CreateSubject(string json)
        {
            var textReader = new StringReader(json);
            return CreateSubject(textReader: textReader);
        }

        private CommandResponseMessageJsonEncoder CreateSubject(
            TextReader textReader = null,
            TextWriter textWriter = null,
            MessageEncoderSettings encoderSettings = null)
        {
            textReader = textReader ?? new StringReader("");
            textWriter = textWriter ?? new StringWriter();
            encoderSettings = encoderSettings ?? new MessageEncoderSettings();
            var wrappedEncoder = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
            return new CommandResponseMessageJsonEncoder(wrappedEncoder);
        }
    }

    public static class CommandResponseMessageJsonEncoderReflector
    {
        public static CommandMessageJsonEncoder _wrappedEncoder(this CommandResponseMessageJsonEncoder obj)
        {
            var fieldInfo = typeof(CommandResponseMessageJsonEncoder).GetField("_wrappedEncoder", BindingFlags.NonPublic | BindingFlags.Instance);
            return (CommandMessageJsonEncoder)fieldInfo.GetValue(obj);
        }
    }
}
