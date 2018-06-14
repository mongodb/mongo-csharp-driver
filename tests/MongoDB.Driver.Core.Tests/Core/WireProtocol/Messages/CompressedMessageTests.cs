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

using System.IO;
using FluentAssertions;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders;
using Xunit;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
	public class CompressedMessageTests
	{
		[Fact]
		public void Constructor_should_initialize_instance()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressor = new ZlibCompressor(-1);

			var result = new CompressedMessage(message, compressor);

			result.Compressor.Should().Be(compressor);
			result.MessageToBeCompressed.Should().Be(message);
		}

		[Fact]
		public void MessageType_should_return_expected_result()
		{
			var subject = CreateSubject();

			var result = subject.MessageType;

			result.Should().Be(MongoDBMessageType.Compressed);
		}

		[Fact]
		public void GetEncoder_should_return_a_CompressedMessageEncoder()
		{
			var subject = CreateSubject();

			var stream = new MemoryStream();
			var encoderSettings = new MessageEncoderSettings();
			var encoderFactory = new BinaryMessageEncoderFactory(stream, encoderSettings);

			var result = subject.GetEncoder(encoderFactory);

			result.Should().BeOfType<CompressedMessageBinaryEncoder>();
		}
		
		private CompressedMessage CreateSubject()
		{
			var message = CommandMessageHelper.CreateMessage();
			var compressor = new ZlibCompressor(-1);

			return new CompressedMessage(message, compressor);
		}
	}
}