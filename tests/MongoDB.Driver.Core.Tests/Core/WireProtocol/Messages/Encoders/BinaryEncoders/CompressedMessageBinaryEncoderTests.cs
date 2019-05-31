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
using System.IO;
using System.Linq;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Helpers;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
	public class CompressedMessageBinaryEncoderTests
	{
		private const int MessageHeaderLength = 16;
		private ZlibCompressor _compressor;

		public CompressedMessageBinaryEncoderTests()
		{
			_compressor = new ZlibCompressor(-1);
		}

		[Fact]
		public void Constructor_should_initialize_instance()
		{
			var stream = new MemoryStream();
			var encoderSettings = new MessageEncoderSettings();

			var result = new CompressedMessageBinaryEncoder(stream, encoderSettings);

			result._encoderSettings().Should().BeSameAs(encoderSettings);
			result._stream().Should().BeSameAs(stream);		
		}

		[Fact]
		public void ReadMessage_should_throw_not_supported_exception()
		{
			var stream = new MemoryStream();
			var encoderSettings = new MessageEncoderSettings();

			var subject = new CompressedMessageBinaryEncoder(stream, encoderSettings);

			Action action = () => subject.ReadMessage();

			action.ShouldThrow<NotSupportedException>();
		}

		[Fact]
		public void WriteMessage_should_write_messageLength()
		{
			var commandMessage = CommandMessageHelper.CreateMessage();
			var uncompressedCommandMessageBytes = CommandMessageHelper.CreateMessageBytes(commandMessage);
			var compressedMessageBytes = CompressedMessageHelper.CreateCompressedMessageBytes(uncompressedCommandMessageBytes, _compressor);
			var compressedMessage = CreateCompressedMessage(commandMessage);
			var stream = new MemoryStream();
			var expectedMessageLength = compressedMessageBytes.Length;
			
			var subject = CreateSubject(stream);
			subject.WriteMessage(compressedMessage);

			var result = stream.ToArray();

			result.Should().HaveCount(expectedMessageLength);
			var messageLength = BitConverter.ToInt32(result, 0);
			messageLength.Should().Be(expectedMessageLength);
		}

		[Theory]
		[ParameterAttributeData]
		public void WriteMessage_should_write_requestId(
			[Values(1, 2)] int requestId)
		{
			var message = CommandMessageHelper.CreateMessage(requestId: requestId);
			var compressedMessage = CreateCompressedMessage(message);
			
			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
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
			var compressedMessage = CreateCompressedMessage(message);
			
			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray();

			var resultResponseTo = BitConverter.ToInt32(result, 8);
			resultResponseTo.Should().Be(responseTo);
		}
		
		[Fact]
		public void WriteMessage_should_write_expected_opcode()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressedMessage = CreateCompressedMessage(message);

			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray();

			var opcode = BitConverter.ToInt32(result, 12);
			opcode.Should().Be((int)Opcode.Compressed);
		}		
		
		[Fact]
		public void WriteMessage_should_write_expected_original_opcode()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressedMessage = CreateCompressedMessage(message);

			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray();

			var originalOpcode = BitConverter.ToInt32(result, 16);
			originalOpcode.Should().Be((int)Opcode.OpMsg);
		}	
		
		[Fact]
		public void WriteMessage_should_write_expected_uncompressed_size()
		{
			var message = CommandMessageHelper.CreateMessage();
			var uncompressedCommandMessageBytes = CommandMessageHelper.CreateMessageBytes(message);
			var compressedMessage = CreateCompressedMessage(message);

			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray();

			var uncompressedSize = BitConverter.ToInt32(result, 20);
			uncompressedSize.Should().Be(uncompressedCommandMessageBytes.Length - MessageHeaderLength);
		}		
		
		[Fact]
		public void WriteMessage_should_write_expected_compressorId()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressedMessage = CreateCompressedMessage(message);

			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray();

			var compressorId = result[24];
			compressorId.Should().Be((byte)_compressor.Id);
		}	
		
		[Fact]
		public void WriteMessage_should_write_expected_compressedMessageBytes()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressedMessage = CreateCompressedMessage(message);

			var stream = new MemoryStream();
			var subject = CreateSubject(stream);

			subject.WriteMessage(compressedMessage);
			var result = stream.ToArray().ToList();

			var uncompressedMessageBytes = CommandMessageHelper.CreateMessageBytes(message);
			var expectedCompressedBytes = _compressor.Compress(uncompressedMessageBytes, MessageHeaderLength);
			
			var compressedBytes = result.GetRange(25, result.Count - 25);
			compressedBytes.Should().HaveSameCount(expectedCompressedBytes);
			compressedBytes.ShouldBeEquivalentTo(expectedCompressedBytes, o => o.WithStrictOrdering());
		}
		
		private CompressedMessage CreateCompressedMessage(CommandMessage commandMessage)
		{
			return new CompressedMessage(commandMessage, _compressor);
		}

		private static CompressedMessageBinaryEncoder CreateSubject(
			Stream stream = null,
			MessageEncoderSettings encoderSettings = null)
		{
			stream = stream ?? new MemoryStream();
			encoderSettings = encoderSettings ?? new MessageEncoderSettings();
			return new CompressedMessageBinaryEncoder(stream, encoderSettings);
		}	
	}
}