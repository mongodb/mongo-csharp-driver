﻿/* Copyright 2013-2014 MongoDB Inc.
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
    public class JsonMessageEncoderFactory : IMessageEncoderFactory
    {
        // fields
        private readonly MessageEncoderSettings _encoderSettings;
        private readonly TextReader _textReader;
        private readonly TextWriter _textWriter;

        // constructors
        public JsonMessageEncoderFactory(TextReader textReader, MessageEncoderSettings encoderSettings)
            : this(Ensure.IsNotNull(textReader, "textReader"), null, encoderSettings)
        {
        }

        public JsonMessageEncoderFactory(TextWriter textWriter, MessageEncoderSettings encoderSettings)
            : this(null, Ensure.IsNotNull(textWriter, "textWriter"), encoderSettings)
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
        public IMessageEncoder<DeleteMessage> GetDeleteMessageEncoder()
        {
            return new DeleteMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder<GetMoreMessage> GetGetMoreMessageEncoder()
        {
            return new GetMoreMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder<InsertMessage<TDocument>> GetInsertMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new InsertMessageJsonEncoder<TDocument>(_textReader, _textWriter, _encoderSettings, serializer);
        }

        public IMessageEncoder<KillCursorsMessage> GetKillCursorsMessageEncoder()
        {
            return new KillCursorsMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder<QueryMessage> GetQueryMessageEncoder()
        {
            return new QueryMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }

        public IMessageEncoder<ReplyMessage<TDocument>> GetReplyMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new ReplyMessageJsonEncoder<TDocument>(_textReader, _textWriter, _encoderSettings, serializer);
        }

        public IMessageEncoder<UpdateMessage> GetUpdateMessageEncoder()
        {
            return new UpdateMessageJsonEncoder(_textReader, _textWriter, _encoderSettings);
        }
    }
}
