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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;

namespace MongoDB.Driver.Core.Compression
{
	internal sealed class MessageCompressionHelper
	{
		private const int MessageHeaderLength = 16;
		private readonly IDictionary<CompressorId, ICompressor> _compressorDictionary;
		private readonly MessageEncoderSettings _messageEncoderSettings;

		public MessageCompressionHelper(IDictionary<CompressorId, ICompressor> compressorDictionary, MessageEncoderSettings messageEncoderSettings)
		{
			_compressorDictionary = compressorDictionary;
			_messageEncoderSettings = messageEncoderSettings;
		}

		public bool IsCompressedMessage(Stream stream)
		{
			var bsonStream = GetBsonStream(stream);
			
			var position = stream.Position;
			
			// Skip header to opcode
			bsonStream.ReadInt32();
			bsonStream.ReadInt32();
			bsonStream.ReadInt32();
			var isCompressed = bsonStream.ReadInt32() == (int)Opcode.Compressed;

			stream.Position = position;
               
			return isCompressed;
		}

		public Stream UncompressMessage(Stream stream)
		{
			var bsonStream = GetBsonStream(stream);
			return ReadCompressedStream(bsonStream);
		}

		public ResponseMessage ReadCompressedResponseMessage(Stream stream, IMessageEncoderSelector encoderSelector)
		{
			var bsonStream = GetBsonStream(stream);
			using (var uncompressedStream = ReadCompressedStream(bsonStream))
			{
				return (ResponseMessage) ReadMessage(uncompressedStream, encoderSelector);
			}
		}

		private static BsonStream GetBsonStream(Stream stream)
		{
			return stream as BsonStream ?? new BsonStreamAdapter(stream);
		}
		
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
		private ByteBufferStream ReadCompressedStream(BsonStream stream)
		{
			var compressedMessageLength = stream.ReadInt32();
			var requestId = stream.ReadInt32();
			var responseTo = stream.ReadInt32();
			stream.ReadInt32(); //OP_COMPRESSED

			var originalOpCode = (Opcode) stream.ReadInt32();
			var originalMessageSize = stream.ReadInt32();
			var compressorId = (CompressorId) stream.ReadByte();
			var compressor = GetCompressor(compressorId);

			var compressedBytes = stream.ReadBytes(compressedMessageLength - (int) stream.Position);

			var uncompressedBytes = compressor.Decompress(compressedBytes);

			using (var memStream = new MemoryStream())
			{
				using (var writer = new BinaryWriter(memStream, Encoding.UTF8, true))
				{
					writer.Write(originalMessageSize + MessageHeaderLength);
					writer.Write(requestId);
					writer.Write(responseTo);
					writer.Write((int)originalOpCode);
                        
					writer.Write(uncompressedBytes);
				}

				var buffer = new ByteArrayBuffer(memStream.ToArray());
				buffer.MakeReadOnly();
				return new ByteBufferStream(buffer);
			}
		}

		private ICompressor GetCompressor(CompressorId compressorId)
		{
			ICompressor compressor;

			if (_compressorDictionary.TryGetValue(compressorId, out compressor))
				return compressor;
                
			throw new MongoClientException($"Unsupported compressor with identifier {(int)compressorId}");
		}

		private MongoDBMessage ReadMessage(ByteBufferStream stream, IMessageEncoderSelector encoderSelector)
		{
			var encoderFactory = new BinaryMessageEncoderFactory(stream, _messageEncoderSettings);
			var encoder = encoderSelector.GetEncoder(encoderFactory);
			return encoder.ReadMessage();
		}
	}
}