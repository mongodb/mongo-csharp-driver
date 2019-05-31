/* Copyright 2019-present MongoDB Inc.
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

using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    /// <summary>
    /// Represents a compressed message.
    /// </summary>
    public class CompressedMessage : MongoDBMessage
    {
        private readonly CompressorType _compressorType;
        private readonly MongoDBMessage _originalMessage;
        private readonly BsonStream _originalMessageStream;  // not owned by this class

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressedMessage"/> class.
        /// </summary>
        /// <param name="originalMessage">The original message.</param>
        /// <param name="originalMessageStream">The original message stream.</param>
        /// <param name="compressorType">The compressor type.</param>
        public CompressedMessage(
            MongoDBMessage originalMessage,
            BsonStream originalMessageStream,
            CompressorType compressorType)
        {
            _originalMessage = Ensure.IsNotNull(originalMessage, nameof(originalMessage));
            _originalMessageStream = originalMessageStream;
            _compressorType = compressorType;
        }

        /// <summary>
        /// The compressor type.
        /// </summary>
        public CompressorType CompressorType => _compressorType;

        /// <inheritdoc />
        public override MongoDBMessageType MessageType => MongoDBMessageType.Compressed;

        /// <summary>
        /// The original message.
        /// </summary>
        public MongoDBMessage OriginalMessage => _originalMessage;

        /// <summary>
        /// The uncompressed original message stream.
        /// </summary>
        public BsonStream OriginalMessageStream => _originalMessageStream;

        /// <inheritdoc />
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCompressedMessageEncoder(null);
        }
    }
}