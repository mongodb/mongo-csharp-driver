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
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class GetMoreMessageJsonEncoder : MessageJsonEncoderBase, IMessageEncoder<GetMoreMessage>
    {
        // constructors
        public GetMoreMessageJsonEncoder(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
            : base(textReader, textWriter, encoderSettings)
        {
        }

        // methods
        public GetMoreMessage ReadMessage()
        {
            var jsonReader = CreateJsonReader();
            var messageContext = BsonDeserializationContext.CreateRoot<BsonDocument>(jsonReader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(messageContext);

            var opcode = messageDocument["opcode"].AsString;
            if (opcode != "getMore")
            {
                throw new FormatException("Opcode is not getMore.");
            }

            var requestId = messageDocument["requestId"].ToInt32();
            var databaseName = messageDocument["database"].AsString;
            var collectionName = messageDocument["collection"].AsString;
            var cursorId = messageDocument["cursorId"].ToInt32();
            var batchSize = messageDocument["batchSize"].ToInt32();

            return new GetMoreMessage(
                requestId,
                databaseName,
                collectionName,
                cursorId,
                batchSize);
        }

        public void WriteMessage(GetMoreMessage message)
        {
            Ensure.IsNotNull(message, "message");

            var messageDocument = new BsonDocument
            {
                { "opcode", "getMore" },
                { "requestId", message.RequestId },
                { "database", message.DatabaseName },
                { "collection", message.CollectionName },
                { "cursorId", message.CursorId },
                { "batchSize", message.BatchSize }
            };

            var jsonWriter = CreateJsonWriter();
            var messageContext = BsonSerializationContext.CreateRoot<BsonDocument>(jsonWriter);
            BsonDocumentSerializer.Instance.Serialize(messageContext, messageDocument);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((GetMoreMessage)message);
        }
    }
}
