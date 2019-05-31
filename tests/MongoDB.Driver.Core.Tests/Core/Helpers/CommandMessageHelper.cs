/* Copyright 2013-present MongoDB Inc.
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
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Helpers
{
	internal static class CommandMessageHelper
	{
		public static CommandMessage CreateMessage(
			int requestId = 0,
			int responseTo = 0,
			IEnumerable<CommandMessageSection> sections = null,
			bool moreToCome = false)
		{
			sections = sections ?? new[] {CreateType0Section()};
			return new CommandMessage(requestId, responseTo, sections, moreToCome);
		}

		public static Type0CommandMessageSection<BsonDocument> CreateType0Section(BsonDocument document = null)
		{
			document = document ?? new BsonDocument("t", 0);
			return new Type0CommandMessageSection<BsonDocument>(document, BsonDocumentSerializer.Instance);
		}

		private static byte[] CreateType0SectionBytes(Type0CommandMessageSection<BsonDocument> section)
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

		public static Type1CommandMessageSection<BsonDocument> CreateType1Section(
			string identifier = null,
			BsonDocument[] documents = null,
			bool canBeSplit = false)
		{
			identifier = identifier ?? "id";
			documents = documents ?? new BsonDocument[0];
			var batch = new BatchableSource<BsonDocument>(documents, canBeSplit: canBeSplit);
			return new Type1CommandMessageSection<BsonDocument>(
				identifier,
				batch,
				BsonDocumentSerializer.Instance,
				NoOpElementNameValidator.Instance,
				null,
				null);
		}

		private static byte[] CreateType1SectionBytes(Type1CommandMessageSection<BsonDocument> section)
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

		public static byte[] CreateSectionBytes(CommandMessageSection section)
		{
			switch (section.PayloadType)
			{
				case PayloadType.Type0:
					return CreateType0SectionBytes((Type0CommandMessageSection<BsonDocument>) section);

				case PayloadType.Type1:
					return CreateType1SectionBytes((Type1CommandMessageSection<BsonDocument>) section);

				default:
					throw new ArgumentException($"Invalid payload type: {section.PayloadType}.");
			}
		}

		public static List<CommandMessageSection> CreateSections(params int[] sectionTypes)
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

		private static int CreateFlags(CommandMessage message)
		{
			return message.MoreToCome
				? (int) OpMsgFlags.MoreToCome
				: 0;
		}

		public static byte[] CreateHeaderBytes(int messageLength, int requestId, int responseTo, int flags)
		{
			using (var memoryStream = new MemoryStream())
			using (var stream = new BsonStreamAdapter(memoryStream))
			{
				stream.WriteInt32(messageLength);
				stream.WriteInt32(requestId);
				stream.WriteInt32(responseTo);
				stream.WriteInt32((int) Opcode.OpMsg);
				stream.WriteInt32(flags);
				return memoryStream.ToArray();
			}
		}

		public static byte[] CreateMessageBytes(byte[] header, byte[][] sections)
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

		public static byte[] CreateMessageBytes(CommandMessage message)
		{
			var sections = message.Sections.Select(s => CreateSectionBytes(s)).ToArray();
			var messageLength = 20 + sections.Select(s => s.Length).Sum();
			var header = CreateHeaderBytes(messageLength, message.RequestId, message.ResponseTo, CreateFlags(message));
			return CreateMessageBytes(header, sections);
		}

		public static byte[] CreateMessageBytes(
			int requestId = 0,
			int responseTo = 0,
			IEnumerable<CommandMessageSection> sections = null,
			bool moreToCome = false)
		{
			var message = CreateMessage(requestId, responseTo, sections, moreToCome);
			return CreateMessageBytes(message);
		}
	}
}