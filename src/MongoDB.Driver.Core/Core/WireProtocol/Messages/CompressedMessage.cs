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

using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
	/// <summary>
	/// Represents a compressed message.
	/// </summary>
	/// <seealso cref="MongoDB.Driver.Core.WireProtocol.Messages.MongoDBMessage" />
	public class CompressedMessage : MongoDBMessage
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