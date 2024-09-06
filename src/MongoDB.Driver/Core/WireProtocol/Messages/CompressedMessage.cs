/* Copyright 2010-present MongoDB Inc.
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
    internal sealed class CompressedMessage : MongoDBMessage
    {
        private readonly CompressorType _compressorType;
        private readonly MongoDBMessage _originalMessage;
        private readonly BsonStream _originalMessageStream;  // not owned by this class

        public CompressedMessage(
            MongoDBMessage originalMessage,
            BsonStream originalMessageStream,
            CompressorType compressorType)
        {
            _originalMessage = Ensure.IsNotNull(originalMessage, nameof(originalMessage));
            _originalMessageStream = originalMessageStream;
            _compressorType = compressorType;
        }

        public CompressorType CompressorType => _compressorType;
        public override MongoDBMessageType MessageType => MongoDBMessageType.Compressed;
        public MongoDBMessage OriginalMessage => _originalMessage;
        public BsonStream OriginalMessageStream => _originalMessageStream;

        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetCompressedMessageEncoder(null);
        }
    }
}
