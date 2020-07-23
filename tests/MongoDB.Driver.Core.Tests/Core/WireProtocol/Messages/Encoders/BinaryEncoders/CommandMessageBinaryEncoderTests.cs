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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
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
            var bytes = CreateMessageBytes();
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
            var bytes = CreateMessageBytes();
            bytes[0]--;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message did not end at the expected end position");
        }

        [Fact]
        public void ReadMessage_should_throw_when_end_of_file_is_reached()
        {
            var bytes = CreateMessageBytes();
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
            var bytes = CreateMessageBytes(requestId: requestId);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.RequestId.Should().Be(requestId);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_responseTo(
            [Values(1, 2)] int responseTo)
        {
            var bytes = CreateMessageBytes(responseTo: responseTo);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.ResponseTo.Should().Be(responseTo);
        }

        [Fact]
        public void ReadMessage_should_throw_when_opcode_is_invalid()
        {
            var bytes = CreateMessageBytes();
            bytes[12]++;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message opcode is not OP_MSG");
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_flags(
            [Values(false, true)] bool exhaustAllowed,
            [Values(false, true)] bool moreToCome)
        {
            var bytes = CreateMessageBytes(moreToCome: moreToCome, exhaustAllowed: exhaustAllowed);
            var subject = CreateSubject(bytes);

            var result = subject.ReadMessage();

            result.ExhaustAllowed.Should().Be(exhaustAllowed);
            result.MoreToCome.Should().Be(moreToCome);
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_throw_when_flags_is_invalid(
           [Values(-1, 1, 4)] int flags)
        {
            var bytes = CreateMessageBytes();
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
            var sections = CreateSections(sectionTypes);
            var bytes = CreateMessageBytes(sections: sections);
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
            var sections = CreateSections(sectionTypes);
            var sectionBytes = sections.Select(s => CreateSectionBytes(s)).ToArray();
            var messageLength = 20 + sectionBytes.Select(s => s.Length).Sum();
            var header = CreateHeaderBytes(messageLength, 0, 0, 0);
            var bytes = CreateMessageBytes(header, sectionBytes);
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
            var sections = CreateSections(sectionTypes);
            var sectionBytes = sections.Select(s => CreateSectionBytes(s)).ToArray();
            var messageLength = 20 + sectionBytes.Select(s => s.Length).Sum();
            var header = CreateHeaderBytes(messageLength, 0, 0, 0);
            var bytes = CreateMessageBytes(header, sectionBytes);
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message has more than one type 0 section");
        }

        [Fact]
        public void ReadMessage_should_throw_when_payload_type_is_invalid()
        {
            var message = CreateMessage();
            var bytes = CreateMessageBytes(message);
            bytes[20] = 0xff;
            var subject = CreateSubject(bytes);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message invalid payload type: 255");
        }

        [Fact]
        public void ReadMessage_should_throw_when_type_1_payload_size_is_negative()
        {
            var sections = CreateSections(sectionTypes: new[] { 1, 0 });
            var message = CreateMessage(sections: sections);
            var bytes = CreateMessageBytes(message);
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
            var sections = CreateSections(sectionTypes: new[] { 1, 0 });
            var message = CreateMessage(sections: sections);
            var bytes = CreateMessageBytes(message);
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
            var sections = CreateSections(sectionTypes);
            var message = CreateMessage(sections: sections);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var bytes = CreateMessageBytes(message);
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
            var message = CreateMessage(requestId: requestId);
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
            var message = CreateMessage(responseTo: responseTo);
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
            var message = CreateMessage();
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
            [Values(false, true)] bool moreToCome,
            [Values(false, true)] bool exhaustAllowed)
        {
            var message = CreateMessage(moreToCome: moreToCome, exhaustAllowed: exhaustAllowed);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);

            subject.WriteMessage(message);

            var result = stream.ToArray();
            var flags = (OpMsgFlags)BitConverter.ToInt32(result, 16);
            flags.HasFlag(OpMsgFlags.MoreToCome).Should().Be(moreToCome);
            flags.HasFlag(OpMsgFlags.ExhaustAllowed).Should().Be(exhaustAllowed);
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        public void WriteMessage_should_write_sections(int[] sectionTypes)
        {
            var sections = CreateSections(sectionTypes);
            var message = CreateMessage(sections: sections);
            var stream = new MemoryStream();
            var subject = CreateSubject(stream);
            var expectedMessageBytes = CreateMessageBytes(message);

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
            var message = CreateMessage(sections: sections, moreToCome: true);
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
            var type0Section = CreateType0Section();
            var type1Section = CreateType1Section("id", documents, canBeSplit: true);
            var message = CreateMessage(sections: new CommandMessageSection[] { type0Section, type1Section });
            var messageBytes = CreateMessageBytes(message);
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
        private int CreateFlags(CommandMessage message)
        {
            var flags = (OpMsgFlags)0;
            if (message.MoreToCome)
            {
                flags |= OpMsgFlags.MoreToCome;
            }
            if (message.ExhaustAllowed)
            {
                flags |= OpMsgFlags.ExhaustAllowed;
            }
            return (int)flags;
        }

        private byte[] CreateHeaderBytes(int messageLength, int requestId, int responseTo, int flags)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            {
                stream.WriteInt32(messageLength);
                stream.WriteInt32(requestId);
                stream.WriteInt32(responseTo);
                stream.WriteInt32((int)Opcode.OpMsg);
                stream.WriteInt32(flags);
                return memoryStream.ToArray();
            }
        }

        private CommandMessage CreateMessage(
            int requestId = 0,
            int responseTo = 0,
            IEnumerable<CommandMessageSection> sections = null,
            bool moreToCome = false,
            bool exhaustAllowed = false)
        {
            sections = sections ?? new[] { CreateType0Section() };
            return new CommandMessage(requestId, responseTo, sections, moreToCome)
            {
                ExhaustAllowed = exhaustAllowed
            };
        }

        private byte[] CreateMessageBytes(byte[] header, byte[][] sections)
        {
            var messageLength = header.Length + sections.Select(s => s.Length).Sum();
            var message = new byte[messageLength];
            header.CopyTo(message, 0);
            var offset = header.Length;
            foreach (var section in sections)
            {
                section.CopyTo(message, offset);
                offset += section.Length;
            }
            return message;
        }

        private byte[] CreateMessageBytes(CommandMessage message)
        {
            var sections = message.Sections.Select(s => CreateSectionBytes(s)).ToArray();
            var messageLength = 20 + sections.Select(s => s.Length).Sum();
            var header = CreateHeaderBytes(messageLength, message.RequestId, message.ResponseTo, CreateFlags(message));
            return CreateMessageBytes(header, sections);
        }

        private byte[] CreateMessageBytes(
            int requestId = 0,
            int responseTo = 0,
            IEnumerable<CommandMessageSection> sections = null,
            bool moreToCome = false,
            bool exhaustAllowed = false)
        {
            var message = CreateMessage(requestId, responseTo, sections, moreToCome, exhaustAllowed);
            return CreateMessageBytes(message);
        }

        private byte[] CreateSectionBytes(CommandMessageSection section)
        {
            switch (section.PayloadType)
            {
                case PayloadType.Type0:
                    return CreateType0SectionBytes((Type0CommandMessageSection<BsonDocument>)section);

                case PayloadType.Type1:
                    return CreateType1SectionBytes((Type1CommandMessageSection<BsonDocument>)section);

                default:
                    throw new ArgumentException($"Invalid payload type: {section.PayloadType}.");
            }
        }

        private List<CommandMessageSection> CreateSections(params int[] sectionTypes)
        {
            var sections = new List<CommandMessageSection>();
            for (var i = 0; i < sectionTypes.Length; i++)
            {
                var sectionType = sectionTypes[i];

                CommandMessageSection section;
                switch (sectionType)
                {
                    case 0:
                        section = CreateType0Section();
                        break;

                    case 1:
                        var identifier = $"id{i}";
                        var documents = Enumerable.Range(0, i + 1).Select(n => new BsonDocument("n", n)).ToArray();
                        section = CreateType1Section(identifier, documents);
                        break;

                    default:
                        throw new ArgumentException($"Invalid payload type: {sectionType}.", nameof(sectionTypes));
                }
                sections.Add(section);
            }
            return sections;
        }

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

        private Type0CommandMessageSection<BsonDocument> CreateType0Section(BsonDocument document = null)
        {
            document = document ?? new BsonDocument("t", 0);
            return new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance);
        }

        private byte[] CreateType0SectionBytes(Type0CommandMessageSection<BsonDocument> section)
        {
            using (var memoryStream = new MemoryStream())
            using (var writer = new BsonBinaryWriter(memoryStream))
            {
                memoryStream.WriteByte(0);
                var context = BsonSerializationContext.CreateRoot(writer);
                BsonDocumentSerializer.Instance.Serialize(context, section.Document);
                return memoryStream.ToArray();
            }
        }

        private Type1CommandMessageSection<BsonDocument> CreateType1Section(
            string identifier = null,
            BsonDocument[] documents = null,
            bool canBeSplit = false)
        {
            identifier = identifier ?? "id";
            documents = documents ?? new BsonDocument[0];
            var batch = new BatchableSource<BsonDocument>(documents, canBeSplit: canBeSplit);
            return new Type1CommandMessageSection<BsonDocument>(identifier, batch, BsonDocumentSerializer.Instance, NoOpElementNameValidator.Instance, null, null);
        }

        private byte[] CreateType1SectionBytes(Type1CommandMessageSection<BsonDocument> section)
        {
            using (var memoryStream = new MemoryStream())
            using (var stream = new BsonStreamAdapter(memoryStream))
            using (var writer = new BsonBinaryWriter(stream))
            {
                stream.WriteByte(1);
                var payloadStartPosition = stream.Position;
                stream.WriteInt32(0); // size
                stream.WriteCString(section.Identifier);
                var context = BsonSerializationContext.CreateRoot(writer);
                var batch = section.Documents;
                for (var i = 0; i < batch.Count; i++)
                {
                    var document = batch.Items[batch.Offset + i];
                    BsonDocumentSerializer.Instance.Serialize(context, document);
                }
                stream.BackpatchSize(payloadStartPosition);
                return memoryStream.ToArray();
            }
        }
    }
}
