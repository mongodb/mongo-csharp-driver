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
* See the License for the specific language governing per
* ssions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Misc;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class CommandMessageJsonEncoderTests
    {
        [Fact]
        public void constructor_should_initialize_instance()
        {
            var textReader = new StringReader("");
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();

            var result = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);

            result._encoderSettings().Should().BeSameAs(encoderSettings);
            result._textReader().Should().BeSameAs(textReader);
            result._textWriter().Should().BeSameAs(textWriter);
        }

        [Fact]
        public void ReadMessage_should_throw_when_opcode_is_invalid()
        {
            var message = CreateMessage();
            var messageDocument = CreateMessageDocument(message);
            messageDocument["opcode"] = "xyz";
            var subject = CreateSubject(messageDocument);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message invalid opcode: \"xyz\"");
        }

        [Fact]
        public void ReadMessage_should_throw_when_opcode_is_missing()
        {
            var message = CreateMessage();
            var messageDocument = CreateMessageDocument(message);
            messageDocument.Remove("opcode");
            var subject = CreateSubject(messageDocument);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<KeyNotFoundException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_requestId(
            [Values(1, 2)] int requestId)
        {
            var message = CreateMessage(requestId: requestId);
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.RequestId.Should().Be(requestId);
        }

        [Fact]
        public void ReadMessage_should_throw_when_requestId_is_missing()
        {
            var message = CreateMessage();
            var messageDocument = CreateMessageDocument(message);
            messageDocument.Remove("requestId");
            var subject = CreateSubject(messageDocument);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<KeyNotFoundException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_responseTo(
            [Values(1, 2)] int responseTo)
        {
            var message = CreateMessage(responseTo: responseTo);
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.ResponseTo.Should().Be(responseTo);
        }

        [Fact]
        public void ReadMessage_should_throw_when_responseTo_is_missing()
        {
            var message = CreateMessage();
            var messageDocument = CreateMessageDocument(message);
            messageDocument.Remove("responseTo");
            var subject = CreateSubject(messageDocument);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<KeyNotFoundException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void ReadMessage_should_read_flags(
            [Values(false, true)] bool exhaustAllowed,
            [Values(false, true)] bool moreToCome)
        {
            var message = CreateMessage(moreToCome: moreToCome, exhaustAllowed: exhaustAllowed);
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.ExhaustAllowed.Should().Be(exhaustAllowed);
            result.MoreToCome.Should().Be(moreToCome);
        }

        [Fact]
        public void ReadMessage_should_read_type_0_section()
        {
            var type0Section = CreateType0Section();
            var message = CreateMessage(sections: new[] { type0Section });
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.Sections.Should().HaveCount(1);
            var resultType0Section = result.Sections[0].Should().BeOfType<Type0CommandMessageSection<BsonDocument>>().Subject;
            resultType0Section.Document.Should().Be(type0Section.Document);
        }

        [Fact]
        public void ReadMessage_should_read_type_1_section()
        {
            var type0Section = CreateType0Section();
            var type1Section = CreateType1Section();
            var message = CreateMessage(sections: new CommandMessageSection[] { type0Section, type1Section });
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.Sections.Should().HaveCount(2);
            var resultType1Section = result.Sections[1].Should().BeOfType<Type1CommandMessageSection<BsonDocument>>().Subject;
            resultType1Section.Identifier.Should().Be(type1Section.Identifier);
            resultType1Section.Documents.GetBatchItems().Should().Equal(type1Section.Documents.GetBatchItems());
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        public void ReadMessage_should_read_sections(int[] sectionTypes)
        {
            var sections = CreateSections(sectionTypes);
            var message = CreateMessage(sections: sections);
            var subject = CreateSubject(message);

            var result = subject.ReadMessage();

            result.Sections.Should().Equal(sections, CommandMessageSectionEqualityComparer.Instance.Equals);
        }

        [Fact]
        public void ReadMessage_should_throw_when_payloadType_is_invalid()
        {
            var message = CreateMessage();
            var messageDocument = CreateMessageDocument(message);
            messageDocument["sections"][0]["payloadType"] = 0xff;
            var subject = CreateSubject(messageDocument);

            var exception = Record.Exception(() => subject.ReadMessage());

            exception.Should().BeOfType<FormatException>();
            exception.Message.Should().Contain("Command message invalid payload type: 255");
        }

        [Fact]
        public void WriteMessage_should_write_opcode()
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var message = CreateMessage();

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            resultDocument["opcode"].AsString.Should().Be("opmsg");
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_requestId(
            [Values(1, 2)] int requestId)
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var message = CreateMessage(requestId: requestId);

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            resultDocument["requestId"].AsInt32.Should().Be(requestId);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_responseTo(
            [Values(1, 2)] int responseTo)
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var message = CreateMessage(responseTo: responseTo);

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            resultDocument["responseTo"].AsInt32.Should().Be(responseTo);
        }

        [Theory]
        [ParameterAttributeData]
        public void WriteMessage_should_write_flags(
            [Values(false, true)] bool exhaustAllowed,
            [Values(false, true)] bool moreToCome)
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var message = CreateMessage(moreToCome: moreToCome, exhaustAllowed: exhaustAllowed);

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            AssertField("moreToCome", moreToCome);
            AssertField("exhaustAllowed", exhaustAllowed);

            void AssertField(string key, bool expectedValue)
            {
                if (expectedValue)
                {
                    resultDocument[key].AsBoolean.Should().BeTrue();
                }
                else
                {
                    resultDocument.Contains(key).Should().BeFalse();
                }
            }
        }

        [Fact]
        public void WriteMessage_should_write_type_0_section()
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var type0Section = CreateType0Section();
            var message = CreateMessage(sections: new[] { type0Section });

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            var resultSections = resultDocument["sections"].AsBsonArray;
            resultSections.Count.Should().Be(1);
            var resultType0Section = resultSections[0].AsBsonDocument;
            resultType0Section["payloadType"].AsInt32.Should().Be(0);
            resultType0Section["document"].Should().Be(type0Section.Document);
        }

        [Fact]
        public void WriteMessage_should_write_type_1_section()
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var type0Section = CreateType0Section();
            var type1Section = CreateType1Section();
            var message = CreateMessage(sections: new CommandMessageSection[] { type0Section, type1Section });

            subject.WriteMessage(message);
            var result = writer.ToString();

            var resultDocument = BsonDocument.Parse(result);
            var resultSections = resultDocument["sections"].AsBsonArray;
            resultSections.Count.Should().Be(2);
            var resultType1Section = resultSections[1].AsBsonDocument;
            resultType1Section["payloadType"].AsInt32.Should().Be(1);
            resultType1Section["identifier"].AsString.Should().Be(type1Section.Identifier);
            resultType1Section["documents"].AsBsonArray.Cast<BsonDocument>().Should().Equal(type1Section.Documents.GetBatchItems());
        }

        [Theory]
        [InlineData(new[] { 0 })]
        [InlineData(new[] { 0, 1 })]
        [InlineData(new[] { 1, 0 })]
        public void WriteMessage_should_write_sections(int[] sectionTypes)
        {
            var writer = new StringWriter();
            var subject = CreateSubject(textWriter: writer);
            var sections = CreateSections(sectionTypes);
            var message = CreateMessage(sections: sections);

            subject.WriteMessage(message);
            var result = writer.ToString();

            var rehydrated = ReadMessage(result);
            rehydrated.Sections.Should().HaveCount(sectionTypes.Length);
            rehydrated.Sections.Should().Equal(sections, CommandMessageSectionEqualityComparer.Instance.Equals);
        }

        // private methods
        private CommandMessage CreateMessage(
            int requestId = 1,
            int responseTo = 2,
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

        private BsonDocument CreateMessageDocument(CommandMessage message)
        {
            var messageDocument = new BsonDocument
            {
                { "opcode", "opmsg" },
                { "requestId", message.RequestId },
                { "responseTo", message.ResponseTo },
                { "exhaustAllowed", true, message.ExhaustAllowed },
                { "moreToCome", true, message.MoreToCome },
                { "sections", new BsonArray(message.Sections.Select(s => CreateSectionDocument(s))) }
            };

            return messageDocument;
        }

        private BsonDocument CreateSectionDocument(CommandMessageSection section)
        {
            switch (section.PayloadType)
            {
                case PayloadType.Type0:
                    return CreateType0SectionDocument((Type0CommandMessageSection<BsonDocument>)section);

                case PayloadType.Type1:
                    return CreateType1SectionDocument((Type1CommandMessageSection<BsonDocument>)section);

                default:
                    throw new ArgumentException($"Invalid payload type: {section.PayloadType}.", nameof(section));
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

        private CommandMessageJsonEncoder CreateSubject(BsonDocument document)
        {
            return CreateSubject(document.ToJson());
        }

        private CommandMessageJsonEncoder CreateSubject(CommandMessage message)
        {
            var messageDocument = CreateMessageDocument(message);
            return CreateSubject(messageDocument);
        }

        private CommandMessageJsonEncoder CreateSubject(string json)
        {
            var textReader = new StringReader(json);
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();
            return new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
        }

        private CommandMessageJsonEncoder CreateSubject(
            TextReader textReader = null,
            TextWriter textWriter = null,
            MessageEncoderSettings encoderSettings = null)
        {
            textReader = textReader ?? new StringReader("");
            textWriter = textWriter ?? new StringWriter();
            encoderSettings = encoderSettings ?? new MessageEncoderSettings();
            return new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
        }

        private Type0CommandMessageSection<BsonDocument> CreateType0Section(
            BsonDocument document = null)
        {
            document = document ?? new BsonDocument("x", 1);
            return new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance);
        }

        private BsonDocument CreateType0SectionDocument(Type0CommandMessageSection<BsonDocument> section)
        {
            return new BsonDocument
            {
                { "payloadType", 0 },
                { "document", section.Document }
            };
        }

        private Type1CommandMessageSection<BsonDocument> CreateType1Section(
            string identifier = null,
            BsonDocument[] documents = null)
        {
            identifier = identifier ?? "id";
            documents = documents ?? new BsonDocument[0];
            var batch = new BatchableSource<BsonDocument>(documents, canBeSplit: false);
            return new Type1CommandMessageSection<BsonDocument>(identifier, batch, BsonDocumentSerializer.Instance, NoOpElementNameValidator.Instance, null, null);
        }

        private BsonDocument CreateType1SectionDocument(Type1CommandMessageSection<BsonDocument> section)
        {
            return new BsonDocument
            {
                { "payloadType", 1 },
                { "identifier", section.Identifier },
                { "documents", new BsonArray(section.Documents.GetBatchItems()) }
            };
        }

        private CommandMessage ReadMessage(string json)
        {
            var textReader = new StringReader(json);
            var textWriter = new StringWriter();
            var encoderSettings = new MessageEncoderSettings();
            var encoder = new CommandMessageJsonEncoder(textReader, textWriter, encoderSettings);
            return (CommandMessage)encoder.ReadMessage();
        }
    }
}
