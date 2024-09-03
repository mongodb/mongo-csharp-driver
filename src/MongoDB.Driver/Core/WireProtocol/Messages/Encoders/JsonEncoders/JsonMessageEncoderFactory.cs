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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    internal sealed class JsonMessageEncoderFactory : IMessageEncoderFactory
    {
        // fields
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly TextReader _textReader;
        private readonly TextWriter _textWriter;

        // constructors
        public JsonMessageEncoderFactory(TextReader textReader, MessageEncoderSettings encoderSettings)
            : this(Ensure.IsNotNull(textReader, nameof(textReader)), null, encoderSettings)
        {
        }

        public JsonMessageEncoderFactory(TextWriter textWriter, MessageEncoderSettings encoderSettings)
            : this(null, Ensure.IsNotNull(textWriter, nameof(textWriter)), encoderSettings)
        {
        }

        public JsonMessageEncoderFactory(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
        {
            Ensure.That(textReader != null || textWriter != null, "textReader and textWriter cannot both be null.");
            _textReader = textReader;
            _textWriter = textWriter;
            _encoderSettings = encoderSettings;
        }

        // methods
        public IMessageEncoder GetCommandMessageEncoder()
        {
            return new CommandMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder GetCommandRequestMessageEncoder()
        {
            var wrappedEncoder = (CommandMessageJsonEncoder)GetCommandMessageEncoder();
            return new CommandRequestMessageJsonEncoder(wrappedEncoder);
        }

        public IMessageEncoder GetCommandResponseMessageEncoder()
        {
            var wrappedEncoder = (CommandMessageJsonEncoder)GetCommandMessageEncoder();
            return new CommandResponseMessageJsonEncoder(wrappedEncoder);
        }

        public IMessageEncoder GetCompressedMessageEncoder(IMessageEncoderSelector originalEncoderSelector)
        {
            return new CompressedMessageJsonEncoder(_textReader, _textWriter, originalEncoderSelector, _encoderSettings);
        }

        public IMessageEncoder GetQueryMessageEncoder()
        {
            return new QueryMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder GetReplyMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new ReplyMessageJsonEncoder<TDocument>(_textReader, _textWriter, _encoderSettings, serializer);
        }
    }
}
