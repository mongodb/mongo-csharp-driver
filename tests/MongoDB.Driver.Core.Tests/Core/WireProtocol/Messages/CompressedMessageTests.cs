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