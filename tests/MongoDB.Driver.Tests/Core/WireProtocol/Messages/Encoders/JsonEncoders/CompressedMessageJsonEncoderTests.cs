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
using System.IO;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class CompressedMessageJsonEncoderTests
    {
        #region static
        // static fields
        private static readonly CompressorType __compressorType = CompressorType.Zlib;
        private static readonly string __jsonMessage;
        private static readonly MessageEncoderSettings __messageEncoderSettings = new MessageEncoderSettings();
        private static readonly IMessageEncoderSelector __originalEncoderSelector;
        private static readonly CommandMessage __originalMessage;
        private static readonly int __requestId = 1;
        private static readonly CompressedMessage __testMessage;

        // static constructor
        static CompressedMessageJsonEncoderTests()
        {
            var document = new BsonDocument("x", 1);
            var sections = new[] { new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance) };
            __originalMessage = new RequestCommandMessage(__requestId, sections, false);
            __jsonMessage = CreateCompressedMessageJson(__originalMessage, __compressorType);
            __testMessage = new CompressedMessage(__originalMessage, null, __compressorType);

            __originalEncoderSelector = new CommandMessageEncoderSelector();
        }
        #endregion

        [Fact]
        public void Constructor_should_not_throw_if_only_textReader_is_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var encoder = new CompressedMessageJsonEncoder(textReader, null, __originalEncoderSelector, __messageEncoderSettings);
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_only_textWriter_is_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var encoder = new CompressedMessageJsonEncoder(null, textWriter, __originalEncoderSelector, __messageEncoderSettings);
            }
        }

        [Fact]
        public void Constructor_should_not_throw_if_textReader_and_textWriter_are_both_provided()
        {
            using (var textReader = new StringReader(""))
            using (var textWriter = new StringWriter())
            {
                var encoder = new CompressedMessageJsonEncoder(textReader, textWriter, __originalEncoderSelector, __messageEncoderSettings);
            }
        }

        [Fact]
        public void Constructor_should_throw_if_textReader_and_textWriter_are_both_null()
        {
            var exception = Record.Exception(() => { new CompressedMessageJsonEncoder(null, null, __originalEncoderSelector, __messageEncoderSettings); });

            exception.Should().BeOfType<ArgumentException>();
        }

        [Fact]
        public void ReadMessage_should_read_a_message()
        {
            using (var textReader = new StringReader(__jsonMessage))
            {
                var subject = new CompressedMessageJsonEncoder(textReader, null, __originalEncoderSelector, __messageEncoderSettings);
                var message = subject.ReadMessage();

                message.CompressorType.Should().Be(__compressorType);

                var originalMessage = (CommandMessage)message.OriginalMessage;
                originalMessage.ShouldBeEquivalentTo(__originalMessage);
            }
        }

        [Fact]
        public void ReadMessage_should_throw_if_textReader_was_not_provided()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new CompressedMessageJsonEncoder(null, textWriter, __originalEncoderSelector, __messageEncoderSettings);

                var exception = Record.Exception(() => subject.ReadMessage());

                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_message_is_null()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new CompressedMessageJsonEncoder(null, textWriter, __originalEncoderSelector, __messageEncoderSettings);

                var exception = Record.Exception(() => subject.WriteMessage(null));

                exception.Should().BeOfType<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_should_throw_if_textWriter_was_not_provided()
        {
            using (var textReader = new StringReader(""))
            {
                var subject = new CompressedMessageJsonEncoder(textReader, null, __originalEncoderSelector, __messageEncoderSettings);

                var exception = Record.Exception(() => subject.WriteMessage(__testMessage));

                exception.Should().BeOfType<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_should_write_a_message()
        {
            using (var textWriter = new StringWriter())
            {
                var subject = new CompressedMessageJsonEncoder(null, textWriter, __originalEncoderSelector, __messageEncoderSettings);
                subject.WriteMessage(__testMessage);

                var json = textWriter.ToString();
                json.Should().Be(__jsonMessage);
            }
        }

        private static string CreateCompressedMessageJson(BsonDocument originalMessage, CompressorType compressorType)
        {
            var compressedMessage = new BsonDocument
            {
                { "opcode", (int)Opcode.Compressed },
                { "compressorId", (int)compressorType },
                { "compressedMessage", originalMessage.ToJson() }
            };

            return compressedMessage.ToJson();
        }

        private static string CreateCompressedMessageJson(CommandMessage message, CompressorType compressorType)
        {
            var textReader = new StringReader("");
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();
            var commandMessageEncoder = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
            commandMessageEncoder.WriteMessage(message);
            var originalMessage = BsonDocument.Parse(textWriter.ToString());

            return CreateCompressedMessageJson(originalMessage, compressorType);
        }
    }
}
