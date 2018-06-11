using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
	/// <summary>
	/// Represents a compressed message.
	/// </summary>
	/// <seealso cref="MongoDB.Driver.Core.WireProtocol.Messages.MongoDBMessage" />
	public sealed class CompressedMessage : MongoDBMessage
	{
		private readonly MongoDBMessage _messageToBeCompressed;
		private readonly ICompressor _compressor;

		/// <summary>
		/// Initializes a new instance of the <see cref="CompressedMessage"/> class.
		/// </summary>
		/// <param name="messageToBeCompressed">The message to be compressed.</param>
		/// <param name="compressor">The compressor.</param>
		public CompressedMessage(MongoDBMessage messageToBeCompressed, ICompressor compressor)
		{
			_messageToBeCompressed = messageToBeCompressed;
			_compressor = compressor;
		}

		/// <summary>
		/// Gets the type of the message.
		/// </summary>
		public override MongoDBMessageType MessageType => MongoDBMessageType.Compressed;

		/// <summary>
		/// The message that should be compressed.
		/// </summary>
		public MongoDBMessage MessageToBeCompressed => _messageToBeCompressed;

		/// <summary>
		/// The compressor
		/// </summary>
		public ICompressor Compressor
		{
			get { return _compressor; }
		}

		/// <inheritdoc/>
		public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
		{
			return encoderFactory.GetCompressedMessageEncoder();
		}
	}
}