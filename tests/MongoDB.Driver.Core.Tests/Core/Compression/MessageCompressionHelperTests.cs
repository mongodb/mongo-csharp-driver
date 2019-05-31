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
using System.Text;
using FluentAssertions;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.Compression
{
	public class MessageCompressionHelperTests
	{
		private MessageCompressionHelper _subject;
		private MessageEncoderSettings _messageEncoderSettings;
		private ZlibCompressor _compressor;

		public MessageCompressionHelperTests()
		{
			_compressor = new ZlibCompressor(-1);
			var compressorDictionary = new Dictionary<CompressorId, ICompressor>
			{
				{CompressorId.zlib, _compressor}
			};

			_messageEncoderSettings = new MessageEncoderSettings();

			_subject = new MessageCompressionHelper(compressorDictionary, _messageEncoderSettings);
		}

		[Fact]
		public void IsCompressedMessage_does_not_advance_stream()
		{
			var stream = SetupStreamHeaderWith(Opcode.Compressed);

			stream.Position = stream.Length;
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
			{
				writer.Write(1);
				writer.Write(1);
				writer.Write(1);
			}

			stream.Position = stream.Length - 16;

			_subject.IsCompressedMessage(stream);

			stream.Position.Should().Be(stream.Length - 16);
		}

		[Fact]
		public void IsCompressedMessage_checks_for_compressed_opcode()
		{
			var streamWithCompressed = SetupStreamHeaderWith(Opcode.Compressed);
			var streamWithoutCompressed = SetupStreamHeaderWith(Opcode.OpMsg);

			var compressed = _subject.IsCompressedMessage(streamWithCompressed);
			var notCompressed = _subject.IsCompressedMessage(streamWithoutCompressed);

			compressed.Should().BeTrue();
			notCompressed.Should().BeFalse();
		}

		[Fact]
		public void UncompressMessage_throws_if_unsupported_compressor()
		{
			byte compressorId = 3;
			var commandMessageBytes = CommandMessageHelper.CreateMessageBytes();
			var compressedMessageBytes = CompressedMessageHelper.CreateCompressedMessageBytes(
				commandMessageBytes,
				_compressor,
				compressorId);

			using (var memoryStream = new MemoryStream(compressedMessageBytes))
			{
				Action action = () => _subject.UncompressMessage(memoryStream);

				action.ShouldThrow<MongoClientException>().WithMessage($"Unsupported*{compressorId}");
			}
		}
		
		[Fact]
		public void UncompressMessage_returns_uncompressed_message_stream()
		{
			var commandMessageBytes = CommandMessageHelper.CreateMessageBytes();
			var compressedMessageBytes =
				CompressedMessageHelper.CreateCompressedMessageBytes(commandMessageBytes, _compressor);

			Stream uncompressedStream;
			using (var memoryStream = new MemoryStream(compressedMessageBytes))
				uncompressedStream = _subject.UncompressMessage(memoryStream);

			using (var reader = new BinaryReader(uncompressedStream))
			{
				var messageSize = reader.ReadInt32();
				var requestId = reader.ReadInt32();
				var respondTo = reader.ReadInt32();
				var opcode = reader.ReadInt32();

				messageSize.Should().Be(commandMessageBytes.Length);
				requestId.Should().Be(0);
				respondTo.Should().Be(0);
				opcode.Should().Be((int) Opcode.OpMsg);
			}
		}

		[Fact]
		public void ReadCompressedResponseMessage_returns_uncompressed_message()
		{
			var commandMessageBytes = CommandMessageHelper.CreateMessageBytes();
			var compressedMessageBytes =
				CompressedMessageHelper.CreateCompressedMessageBytes(commandMessageBytes, _compressor);

			var encoderSelector = new CommandResponseMessageEncoderSelector();
			
			CommandResponseMessage response;
			using (var memoryStream = new MemoryStream(compressedMessageBytes))
				response = (CommandResponseMessage)_subject.ReadCompressedResponseMessage(memoryStream, encoderSelector);

			response.RequestId.Should().Be(0);
			response.ResponseTo.Should().Be(0);
			response.MessageType.Should().Be(MongoDBMessageType.Command);
		}
		
		[Fact]
		public void ReadCompressedResponseMessage_throws_if_unsupported_compressor()
		{
			byte compressorId = 3;
			var commandMessageBytes = CommandMessageHelper.CreateMessageBytes();
			var compressedMessageBytes = CompressedMessageHelper.CreateCompressedMessageBytes(
				commandMessageBytes,
				_compressor,
				compressorId);

			var encoderSelector = new CommandResponseMessageEncoderSelector();
			
			using (var memoryStream = new MemoryStream(compressedMessageBytes))
			{
				Action action = () => _subject.ReadCompressedResponseMessage(memoryStream, encoderSelector);
				
				action.ShouldThrow<MongoClientException>().WithMessage($"Unsupported*{compressorId}");
			}
		}
		
		private Stream SetupStreamHeaderWith(Opcode opcode)
		{
			var stream = new MemoryStream();

			using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
			{
				writer.Write(1);
				writer.Write(1);
				writer.Write(1);
				writer.Write((int)opcode);
			}

			stream.Position = 0;
			
			return stream;
		}
	}
}