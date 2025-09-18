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

using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    internal sealed class QueryMessageJsonEncoder : MessageJsonEncoderBase, IMessageEncoder
    {
        // constructors
        public QueryMessageJsonEncoder(TextReader textReader, TextWriter textWriter, MessageEncoderSettings encoderSettings)
            : base(textReader, textWriter, encoderSettings)
        {
        }

        // methods
        public QueryMessage ReadMessage()
        {
            var jsonReader = CreateJsonReader();
            var messageContext = BsonDeserializationContext.CreateRoot(jsonReader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(messageContext);

            var opcode = messageDocument["opcode"].AsString;
            if (opcode != "query")
            {
                throw new FormatException("Opcode is not query.");
            }

            var requestId = messageDocument["requestId"].ToInt32();
            var databaseName = messageDocument["database"].AsString;
            var collectionName = messageDocument["collection"].AsString;
            var query = messageDocument["query"].AsBsonDocument;
            var fields = (BsonDocument)messageDocument.GetValue("fields", null);
            var skip = messageDocument.GetValue("skip", 0).ToInt32();
            var batchSize = messageDocument.GetValue("batchSize", 0).ToInt32();
            var secondaryOk = messageDocument.GetValue("secondaryOk", false).ToBoolean();
            var partialOk = messageDocument.GetValue("partialOk", false).ToBoolean();
            var noCursorTimeout = messageDocument.GetValue("noCursorTimeout", false).ToBoolean();
            var oplogReplay = messageDocument.GetValue("oplogReplay", false).ToBoolean();
            var tailableCursor = messageDocument.GetValue("tailableCursor", false).ToBoolean();
            var awaitData = messageDocument.GetValue("awaitData", false).ToBoolean();

#pragma warning disable 618
            return new QueryMessage(
                requestId,
                new CollectionNamespace(databaseName, collectionName),
                query,
                fields,
                NoOpElementNameValidator.Instance,
                skip,
                batchSize,
                secondaryOk,
                partialOk,
                noCursorTimeout,
                oplogReplay,
                tailableCursor,
                awaitData);
#pragma warning restore 618
        }

        public void WriteMessage(QueryMessage message)
        {
            Ensure.IsNotNull(message, nameof(message));

            var messageDocument = new BsonDocument
            {
                { "opcode", "query" },
                { "requestId", message.RequestId },
                { "database", message.CollectionNamespace.DatabaseNamespace.DatabaseName },
                { "collection", message.CollectionNamespace.CollectionName },
                { "fields", message.Fields, message.Fields != null },
                { "skip", message.Skip, message.Skip != 0 },
                { "batchSize", message.BatchSize, message.BatchSize != 0 },
                { "secondaryOk", true, message.SecondaryOk },
                { "partialOk", true, message.PartialOk },
                { "noCursorTimeout", true, message.NoCursorTimeout },
#pragma warning disable 618
                { "oplogReplay", true, message.OplogReplay },
#pragma warning restore 618
                { "tailableCursor", true, message.TailableCursor },
                { "awaitData", true, message.AwaitData },
                { "query", message.Query }
            };

            var jsonWriter = CreateJsonWriter();
            var messageContext = BsonSerializationContext.CreateRoot(jsonWriter);
            BsonDocumentSerializer.Instance.Serialize(messageContext, messageDocument);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((QueryMessage)message);
        }
    }
}
