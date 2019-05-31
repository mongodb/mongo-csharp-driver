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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
	/// <summary>
	/// Represents a binary encoder for a compressed message.
	/// </summary>
	public sealed class CompressedMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder
	{
		private readonly MessageEncoderSettings _encoderSettings;
		private const int MessageHeaderLength = 16;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressedMessageBinaryEncoder" /> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="encoderSettings">The encoder settings.</param>
		public CompressedMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
			: base(stream, encoderSettings)
		{
			_encoderSettings = encoderSettings;
		}

		/// <summary>
		/// Reads the message.
		/// </summary>
		/// <returns>A message.</returns>
		public CompressedMessage ReadMessage()
		{
			throw new NotSupportedException();
		}
		
		/// <summary>
		/// Writes the message.
		/// </summary>
		/// <param name="message">The message.</param>
		public void WriteMessage(CompressedMessage message)
		{
			Ensure.IsNotNull(message, nameof(message));

			var writer = CreateBinaryWriter();
			var stream = writer.BsonStream;
			var messageStartPosition = stream.Position;

			var originalMessageBytes = EncodeOriginalMessage(message.MessageToBeCompressed);

			WriteHeader(stream, originalMessageBytes, message.Compressor.Id);
			var compressedBytes = message.Compressor.Compress(originalMessageBytes, MessageHeaderLength);

			stream.Write(compressedBytes, 0, compressedBytes.Length);
			
			stream.BackpatchSize(messageStartPosition);
		}

		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		private static void WriteHeader(BsonStream bsonStream, byte[] originalMessageBytes, CompressorId compressorId)
		{
			using (var stream = new MemoryStream(originalMessageBytes))
			using (var reader = new BinaryReader(stream))
			{
				var messageSize = reader.ReadInt32();
				var messageRequestId = reader.ReadInt32();
				var responseTo = reader.ReadInt32();
				var originalOpCode = reader.ReadInt32();
				
				bsonStream.WriteInt32(0); // Compressed message size
				bsonStream.WriteInt32(messageRequestId);
				bsonStream.WriteInt32(responseTo);
				bsonStream.WriteInt32((int)Opcode.Compressed);
				bsonStream.WriteInt32(originalOpCode);
				bsonStream.WriteInt32(messageSize - MessageHeaderLength);
				bsonStream.WriteByte((byte)compressorId);
			}
		}

		private byte[] EncodeOriginalMessage(MongoDBMessage originalMessage)
		{
			using (var stream = new MemoryStream())
			{
				var encoderFactory = new BinaryMessageEncoderFactory(stream, _encoderSettings);

				var encoder = originalMessage.GetEncoder(encoderFactory);
				encoder.WriteMessage(originalMessage);
				
				return stream.ToArray();
			}
		}
		
		MongoDBMessage IMessageEncoder.ReadMessage()
		{
			return ReadMessage();
		}
		
		void IMessageEncoder.WriteMessage(MongoDBMessage message)
		{
			WriteMessage((CompressedMessage)message);
		}
	}
}