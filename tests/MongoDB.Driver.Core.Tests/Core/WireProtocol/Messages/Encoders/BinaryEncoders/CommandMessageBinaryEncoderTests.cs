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

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Helpers;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class CommandMessageBinaryEncoderTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var stream = new MemoryStream();
            var encoderSettings = new MessageEncoderSettings();

            var result = new CommandMessageBinaryEncoder(stream, encoderSettings);

            result._encoderSettings().Should().BeSameAs(encoderSettings);
            result._stream().Should().BeSameAs(stream);
        }

        [Fact]
        public void ReadMessage_should_throw_when_message_length_is_negative()
        {
            var bytes = CommandMessageHelper.CreateMessageBytes();
            var messageLength = -1;
            BitConverter.GetBytes(messageLength).CopyTo(bytes, 0);
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message length is negative");
        }

        [Fact]
        public void ReadMessage_should_throw_when_message_did_not_end_at_end_position()
        {
            var bytes = CommandMessageHelper.CreateMessageBytes();
            bytes[0]--;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message did not end at the expected end position");
        }

        [Fact]
        public void ReadMessage_should_throw_when_end_of_file_is_reached()
        {
            var bytes = CommandMessageHelper.CreateMessageBytes();
            bytes[0]++;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<EndOfStreamException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_requestId(
            [Values(1, 2)] int requestId)
        {
            var bytes = CommandMessageHelper.CreateMessageBytes(requestId: requestId);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.RequestId.Should().Be(requestId);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_responseTo(
            [Values(1, 2)] int responseTo)
        {
            var bytes = CommandMessageHelper.CreateMessageBytes(responseTo: responseTo);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.ResponseTo.Should().Be(responseTo);
        }

        [Fact]
        public void ReadMessage_should_throw_when_opcode_is_invalid()
        {
            var bytes = CommandMessageHelper.CreateMessageBytes();
            bytes[12]++;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message opcode is not OP_MSG");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_moreToCome(
            [Values(false, true)] bool moreToCome)
        {
            var bytes = CommandMessageHelper.CreateMessageBytes(moreToCome: moreToCome);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.MoreToCome.Should().Be(moreToCome);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_throw_when_flags_is_invalid(
           [Values(-1, 1, 4)] int flags)
        {
            var bytes = CommandMessageHelper.CreateMessageBytes();
            BitConverter.GetBytes(flags).CopyTo(bytes, 16);
            var subject = CreateSubject(bytes);
            var expectedMessage = flags == 1 ? "Command message CheckSumPresent flag not supported." : "Command message has invalid flags";

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain(expectedMessage);
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        [InlineData(new[] { 0, 1, 1 })]
        [InlineData(new[] { 1, 0, 1 })]
        [InlineData(new[] { 1, 1, 0 })]
        public void ReadMessage_should_read_sections(int[] sectionTypes)
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes);
            var bytes = CommandMessageHelper.CreateMessageBytes(sections: sections);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.Sections.Should().Equal(sections, CommandMessageSectionEqualityComparer.Instance.Equals);
        }

        [Theory]
        [InlineData(new int[0])]
        [InlineData(new[] { 1 })]
        [InlineData(new[] { 1, 1 })]
        [InlineData(new[] { 1, 1, 1 })]
        public void ReadMessage_should_throw_when_no_type_0_section_is_present(int[] sectionTypes)
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes);
            var sectionBytes = sections.Select(s => CommandMessageHelper.CreateSectionBytes(s)).ToArray();
            var messageLength = 20 + sectionBytes.Select(s => s.Length).Sum();
            var header = CommandMessageHelper.CreateHeaderBytes(messageLength, 0, 0, 0);
            var bytes = CommandMessageHelper.CreateMessageBytes(header, sectionBytes);
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message has no type 0 section");
        }

        [Theory]
        [InlineData(new[] { 0, 0 })]
        [InlineData(new[] { 0, 0, 1 })]
        [InlineData(new[] { 0, 1, 0 })]
        [InlineData(new[] { 1, 0, 0 })]
        public void ReadMessage_should_throw_when_more_than_one_type_0_section_is_present(int[] sectionTypes)
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes);
            var sectionBytes = sections.Select(s => CommandMessageHelper.CreateSectionBytes(s)).ToArray();
            var messageLength = 20 + sectionBytes.Select(s => s.Length).Sum();
            var header = CommandMessageHelper.CreateHeaderBytes(messageLength, 0, 0, 0);
            var bytes = CommandMessageHelper.CreateMessageBytes(header, sectionBytes);
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message has more than one type 0 section");
        }

        [Fact]
        public void ReadMessage_should_throw_when_payload_type_is_invalid()
        {
            var message = CommandMessageHelper.CreateMessage();
            var bytes = CommandMessageHelper.CreateMessageBytes(message);
            bytes[20] = 0xff;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message invalid payload type: 255");
        }

        [Fact]
        public void ReadMessage_should_throw_when_type_1_payload_size_is_negative()
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes: new[] { 1, 0 });
            var message = CommandMessageHelper.CreateMessage(sections: sections);
            var bytes = CommandMessageHelper.CreateMessageBytes(message);
            var negativePayloadSize = -1;
            BitConverter.GetBytes(negativePayloadSize).CopyTo(bytes, 21);
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message type 1 payload length is negative");
        }

        [Fact]
        public void ReadMessage_should_throw_when_type_1_payload_does_not_end_at_expected_end_position()
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes: new[] { 1, 0 });
            var message = CommandMessageHelper.CreateMessage(sections: sections);
            var bytes = CommandMessageHelper.CreateMessageBytes(message);
            bytes[21]--;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message payload did not end at the expected end position");
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        public void WriteMessage_should_write_messageLength(int[] sectionTypes)
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes);
            var message = CommandMessageHelper.CreateMessage(sections: sections);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var bytes = CommandMessageHelper.CreateMessageBytes(message);
            var expectedMessageLength = bytes.Length;

            subject.WriteMessage(message);
            var result = stream.ToArray();

            result.Length.Should().Be(expectedMessageLength);
            var writtenMessageLength = BitConverter.ToInt32(result, 0);
            writtenMessageLength.Should().Be(expectedMessageLength);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_requestId(
            [Values(1, 2)] int requestId)
        {
            var message = CommandMessageHelper.CreateMessage(requestId: requestId);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            var resultRequestId = BitConverter.ToInt32(result, 4);
            resultRequestId.Should().Be(requestId);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_responseTo(
            [Values(1, 2)] int responseTo)
        {
            var message = CommandMessageHelper.CreateMessage(responseTo: responseTo);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            var resultResponseTo = BitConverter.ToInt32(result, 8);
            resultResponseTo.Should().Be(responseTo);
        }

        [Fact]
        public void WriteMessage_should_write_expected_opcode()
        {
            var message = CommandMessageHelper.CreateMessage();
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            var opcode = BitConverter.ToInt32(result, 12);
            opcode.Should().Be((int)Opcode.OpMsg);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_flags(
            [Values(false, true)] bool moreToCome)
        {
            var message = CommandMessageHelper.CreateMessage(moreToCome: moreToCome);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            var flags = BitConverter.ToInt32(result, 16);
            flags.Should().Be(moreToCome ? 2 : 0);
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        public void WriteMessage_should_write_sections(int[] sectionTypes)
        {
            var sections = CommandMessageHelper.CreateSections(sectionTypes);
            var message = CommandMessageHelper.CreateMessage(sections: sections);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var expectedMessageBytes = CommandMessageHelper.CreateMessageBytes(message);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            result.Should().Equal(expectedMessageBytes);
        }

        [Fact]
        public void WriteMessage_should_invoke_encoding_post_processor()
        {
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var command = BsonDocument.Parse("{ command : \"x\", writeConcern : { w : 0 } }");
            var section = new Type0CommandMessageSection<BsonDocument>(command, BsonDocumentSerializer.Instance);
            var sections = new CommandMessageSection[] { section };
            var message = CommandMessageHelper.CreateMessage(sections: sections, moreToCome: true);
            message.PostWriteAction = encoder =>
            {
                encoder.ChangeWriteConcernFromW0ToW1();
            };

            subject.WriteMessage(message);

            stream.Position = 0;
            var rehydratedMessage = subject.ReadMessage();
            rehydratedMessage.MoreToCome.Should().BeFalse();
            var rehydratedType0Section = rehydratedMessage.Sections.OfType<Type0CommandMessageSection<RawBsonDocument>>().Single();
            var rehyrdatedCommand = rehydratedType0Section.Document;
            rehyrdatedCommand["writeConcern"]["w"].Should().Be(1);

            // assert that the original message was altered only as expected
            message.MoreToCome.Should().BeFalse(); // was true before PostWriteAction
            var originalCommand = message.Sections.OfType<Type0CommandMessageSection<BsonDocument>>().Single().Document;
            originalCommand["writeConcern"]["w"].Should().Be(0); // unchanged
        }

        [Fact]
        public void WriteMessage_should_not_exceed_MaxMessageSize()
        {
            var documents = new BsonDocument[]
            {
                new BsonDocument("x", 1),
                new BsonDocument("x", 2)
            };
            var type0Section = CommandMessageHelper.CreateType0Section();
            var type1Section = CommandMessageHelper.CreateType1Section("id", documents, canBeSplit: true);
            var message = CommandMessageHelper.CreateMessage(sections: new CommandMessageSection[] { type0Section, type1Section });
            var messageBytes = CommandMessageHelper.CreateMessageBytes(message);
            var maxMessageSize = messageBytes.Length - 1;
            var encoderSettings = new MessageEncoderSettings();
            encoderSettings.Add(MessageEncoderSettingsName.MaxMessageSize, maxMessageSize);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream: stream, encoderSettings: encoderSettings);

            subject.WriteMessage(message);
            var result = stream.ToArray();

            result.Length.Should().BeLessOrEqualTo(maxMessageSize);
            var batch = type1Section.Documents;
            batch.AllItemsWereProcessed.Should().BeFalse();
            batch.Count.Should().Be(2); // no one has called AdvancePastProcessedItems yet
            batch.Offset.Should().Be(0);
            batch.ProcessedCount.Should().Be(1);
        }

        // private methods



        private CommandMessageBinaryEncoder CreateSubject(byte[] bytes)
        {
            var stream = new MemoryStream(bytes);
            return CreateSubject(stream);
        }

        private CommandMessageBinaryEncoder CreateSubject(
            Stream stream = null,
            MessageEncoderSettings encoderSettings = null)
        {
            stream = stream ?? new MemoryStream();
            encoderSettings = encoderSettings ?? new MessageEncoderSettings();
            return new CommandMessageBinaryEncoder(stream, encoderSettings);
        }

    }
}
