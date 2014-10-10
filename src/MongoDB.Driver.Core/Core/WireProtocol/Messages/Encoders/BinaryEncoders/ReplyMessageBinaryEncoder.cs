/* Copyright 2013-2014 MongoDB Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class ReplyMessageBinaryEncoder<TDocument> : MessageBinaryEncoderBase, IMessageEncoder<ReplyMessage<TDocument>>
    {
        // fields
        private readonly IBsonSerializer<TDocument> _serializer;

        // constructors
        public ReplyMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings, IBsonSerializer<TDocument> serializer)
            : base(stream, encoderSettings)
        {
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        // methods
        public ReplyMessage<TDocument> ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;

            streamReader.ReadInt32(); // messageSize
            var requestId = streamReader.ReadInt32();
            var responseTo = streamReader.ReadInt32();
            streamReader.ReadInt32(); // opcode
            var flags = (ResponseFlags)streamReader.ReadInt32();
            var cursorId = streamReader.ReadInt64();
            var startingFrom = streamReader.ReadInt32();
            var numberReturned = streamReader.ReadInt32();
            List<TDocument> documents = null;
            BsonDocument queryFailureDocument = null;

            var awaitCapable = flags.HasFlag(ResponseFlags.AwaitCapable);
            var cursorNotFound = flags.HasFlag(ResponseFlags.CursorNotFound);
            var queryFailure = flags.HasFlag(ResponseFlags.QueryFailure);

            if (queryFailure)
            {
                var context = BsonDeserializationContext.CreateRoot<BsonDocument>(binaryReader);
                queryFailureDocument = BsonDocumentSerializer.Instance.Deserialize(context);
            }
            else
            {
                documents = new List<TDocument>();
                for (var i = 0; i < numberReturned; i++)
                {
                    var context = BsonDeserializationContext.CreateRoot<TDocument>(binaryReader);
                    documents.Add(_serializer.Deserialize(context));
                }
            }

            return new ReplyMessage<TDocument>(
                awaitCapable,
                cursorId,
                cursorNotFound,
                documents,
                numberReturned,
                queryFailure,
                queryFailureDocument,
                requestId,
                responseTo,
                _serializer,
                startingFrom);
        }

        public void WriteMessage(ReplyMessage<TDocument> message)
        {
            Ensure.IsNotNull(message, "message");

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(message.ResponseTo);
            streamWriter.WriteInt32((int)Opcode.Reply);

            var flags = ResponseFlags.None;
            if (message.AwaitCapable)
            {
                flags |= ResponseFlags.AwaitCapable;
            }
            if (message.QueryFailure)
            {
                flags |= ResponseFlags.QueryFailure;
            }
            if (message.CursorNotFound)
            {
                flags |= ResponseFlags.CursorNotFound;
            }
            streamWriter.WriteInt32((int)flags);

            streamWriter.WriteInt64(message.CursorId);
            streamWriter.WriteInt32(message.StartingFrom);
            streamWriter.WriteInt32(message.NumberReturned);
            if (message.QueryFailure)
            {
                var context = BsonSerializationContext.CreateRoot<TDocument>(binaryWriter);
                _serializer.Serialize(context, message.QueryFailureDocument);
            }
            else
            {
                foreach (var doc in message.Documents)
                {
                    var context = BsonSerializationContext.CreateRoot<TDocument>(binaryWriter);
                    _serializer.Serialize(context, doc);
                }
            }
            streamWriter.BackpatchSize(startPosition);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((ReplyMessage<TDocument>)message);
        }

        // nested types
        [Flags]
        internal enum ResponseFlags
        {
            None = 0,
            CursorNotFound = 1,
            QueryFailure = 2,
            AwaitCapable = 8
        }
    }
}
