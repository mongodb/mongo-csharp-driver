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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    internal sealed class CommandMessageJsonEncoder : MessageJsonEncoderBase, IMessageEncoder
    {
        private static readonly ICommandMessageSectionFormatter<Type0CommandMessageSection> __type0SectionFormatter = new Type0SectionFormatter();
        private static readonly ICommandMessageSectionFormatter<Type1CommandMessageSection> __type1SectionFormatter = new Type1SectionFormatter();

        public CommandMessageJsonEncoder(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
            : base(textReader, textWriter, encoderSettings)
        {
        }

        // public methods
        public CommandMessage ReadMessage()
        {
            var reader = CreateJsonReader();
            var context = BsonDeserializationContext.CreateRoot(reader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(context);

            var opcode = messageDocument["opcode"].AsString;
            if (opcode != "opmsg")
            {
                throw new FormatException($"Command message invalid opcode: \"{opcode}\".");
            }
            var exhaustAllowed = messageDocument.GetValue("exhaustAllowed", false).ToBoolean();
            var requestId = messageDocument["requestId"].ToInt32();
            var responseTo = messageDocument["responseTo"].ToInt32();
            var moreToCome = messageDocument.GetValue("moreToCome", false).ToBoolean();
            var sections = ReadSections(messageDocument["sections"].AsBsonArray.Cast<BsonDocument>());

            return new CommandMessage(requestId, responseTo, sections, moreToCome)
            {
                ExhaustAllowed = exhaustAllowed
            };
        }

        public void WriteMessage(CommandMessage message)
        {
            Ensure.IsNotNull(message, nameof(message));

            var writer = CreateJsonWriter();

            writer.WriteStartDocument();
            writer.WriteString("opcode", "opmsg");
            writer.WriteInt32("requestId", message.RequestId);
            writer.WriteInt32("responseTo", message.ResponseTo);
            if (message.MoreToCome)
            {
                writer.WriteBoolean("moreToCome", true);
            }
            if (message.ExhaustAllowed)
            {
                writer.WriteBoolean("exhaustAllowed", true);
            }
            writer.WriteName("sections");
            WriteSections(writer, message.Sections);
            writer.WriteEndDocument();
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((CommandMessage)message);
        }

        // private methods
        private CommandMessageSection ReadSection(BsonDocument sectionDocument)
        {
            var payloadType = sectionDocument["payloadType"].ToInt32();
            switch (payloadType)
            {
                case 0:
                    return ReadType0Section(sectionDocument);

                case 1:
                    return ReadType1Section(sectionDocument);

                default:
                    throw new FormatException($"Command message invalid payload type: {payloadType}.");
            }
        }

        private IEnumerable<CommandMessageSection> ReadSections(IEnumerable<BsonDocument> sectionDocuments)
        {
            var sections = new List<CommandMessageSection>();
            foreach (var sectionDocument in sectionDocuments)
            {
                var section = ReadSection(sectionDocument);
                sections.Add(section);
            }
            return sections;
        }

        private CommandMessageSection ReadType0Section(BsonDocument sectionDocument)
        {
            var document = sectionDocument["document"].AsBsonDocument;
            return new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance);
        }

        private CommandMessageSection ReadType1Section(BsonDocument sectionDocument)
        {
            var identifier = sectionDocument["identifier"].AsString;
            var documents = sectionDocument["documents"].AsBsonArray.Cast<BsonDocument>().ToList();
            var batch = new BatchableSource<BsonDocument>(documents, canBeSplit: false);
            return new Type1CommandMessageSection<BsonDocument>(identifier, batch, BsonDocumentSerializer.Instance, NoOpElementNameValidator.Instance, null, null);
        }

        private void WriteSection(IBsonWriter writer, CommandMessageSection section)
        {
            writer.WriteStartDocument();
            writer.WriteInt32("payloadType", (int)section.PayloadType);

            switch (section)
            {
                case Type0CommandMessageSection type0Section:
                    __type0SectionFormatter.FormatSection(type0Section, writer);
                    break;

                case Type1CommandMessageSection type1Section:
                    __type1SectionFormatter.FormatSection(type1Section, writer);
                    break;

                default:
                    throw new NotSupportedException($"Cannot format command message section of type '{section.GetType().FullName}'.");
            }

            writer.WriteEndDocument();
        }

        private void WriteSections(IBsonWriter writer, IEnumerable<CommandMessageSection> sections)
        {
            writer.WriteStartArray();
            foreach (var section in sections)
            {
                WriteSection(writer, section);
            }
            writer.WriteEndArray();
        }
    }
}
