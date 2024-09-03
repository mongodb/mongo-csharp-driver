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

using System.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    internal class BinaryMessageEncoderFactory : IMessageEncoderFactory
    {
        // fields
        private readonly ICompressorSource _compressorSource;
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly Stream _stream;

        // constructors
        public BinaryMessageEncoderFactory(
            Stream stream,
            MessageEncoderSettings encoderSettings,
            ICompressorSource compressorSource = null)
        {
            _compressorSource = compressorSource;
            _stream = Ensure.IsNotNull(stream, nameof(stream));
            _encoderSettings = encoderSettings; // can be null
        }

        // methods
        public IMessageEncoder GetCommandMessageEncoder()
        {
            return new CommandMessageBinaryEncoder(_stream, _encoderSettings);
        }

        public IMessageEncoder GetCommandRequestMessageEncoder()
        {
            var wrappedEncoder = (CommandMessageBinaryEncoder)GetCommandMessageEncoder();
            return new CommandRequestMessageBinaryEncoder(wrappedEncoder);
        }

        public IMessageEncoder GetCommandResponseMessageEncoder()
        {
            var wrappedEncoder = (CommandMessageBinaryEncoder)GetCommandMessageEncoder();
            return new CommandResponseMessageBinaryEncoder(wrappedEncoder);
        }

        public IMessageEncoder GetCompressedMessageEncoder(IMessageEncoderSelector originalEncoderSelector)
        {
            return new CompressedMessageBinaryEncoder(_stream, originalEncoderSelector, _compressorSource, _encoderSettings);
        }

        public IMessageEncoder GetQueryMessageEncoder()
        {
            return new QueryMessageBinaryEncoder(_stream, _encoderSettings);
        }

        public IMessageEncoder GetReplyMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new ReplyMessageBinaryEncoder<TDocument>(_stream, _encoderSettings, serializer);
        }
    }
}
