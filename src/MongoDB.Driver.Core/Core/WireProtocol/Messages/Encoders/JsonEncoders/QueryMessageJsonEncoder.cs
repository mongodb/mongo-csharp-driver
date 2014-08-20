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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class QueryMessageJsonEncoder : IMessageEncoder<QueryMessage>
    {
        // fields
        private readonly JsonReader _jsonReader;
        private readonly JsonWriter _jsonWriter;

        // constructors
        public QueryMessageJsonEncoder(JsonReader jsonReader, JsonWriter jsonWriter)
        {
            Ensure.That(jsonReader != null || jsonWriter != null, "jsonReader and jsonWriter cannot both be null.");
            _jsonReader = jsonReader;
            _jsonWriter = jsonWriter;
        }

        // methods
        public QueryMessage ReadMessage()
        {
            if (_jsonReader == null)
            {
                throw new InvalidOperationException("No jsonReader was provided.");
            }

            var messageContext = BsonDeserializationContext.CreateRoot<BsonDocument>(_jsonReader);
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
            var slaveOk = messageDocument.GetValue("slaveOk", false).ToBoolean();
            var partialOk = messageDocument.GetValue("partialOk", false).ToBoolean();
            var noCursorTimeout = messageDocument.GetValue("noCursorTimeout", false).ToBoolean();
            var tailableCursor = messageDocument.GetValue("tailableCursor", false).ToBoolean();
            var awaitData = messageDocument.GetValue("awaitData", false).ToBoolean();

            return new QueryMessage(
                requestId,
                databaseName,
                collectionName,
                query,
                fields,
                skip,
                batchSize,
                slaveOk,
                partialOk,
                noCursorTimeout,
                tailableCursor,
                awaitData);
        }

        public void WriteMessage(QueryMessage message)
        {
            Ensure.IsNotNull(message, "message");
            if (_jsonWriter == null)
            {
                throw new InvalidOperationException("No jsonWriter was provided.");
            }

            var messageDocument = new BsonDocument
            {
                { "opcode", "query" },
                { "requestId", message.RequestId },
                { "database", message.DatabaseName },
                { "collection", message.CollectionName },
                { "fields", message.Fields, message.Fields != null },
                { "skip", message.Skip, message.Skip != 0 },
                { "batchSize", message.BatchSize, message.BatchSize != 0 },
                { "slaveOk", true, message.SlaveOk },
                { "partialOk", true, message.PartialOk },
                { "noCursorTimeout", true, message.NoCursorTimeout },
                { "tailableCursor", true, message.TailableCursor },
                { "awaitData", true, message.AwaitData },
                { "query", message.Query }
            };

            var messageContext = BsonSerializationContext.CreateRoot<BsonDocument>(_jsonWriter);
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
