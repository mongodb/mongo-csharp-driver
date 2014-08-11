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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class InsertMessageJsonEncoder<TDocument> : IMessageEncoder<InsertMessage<TDocument>>
    {
        // fields
        private readonly JsonReader _jsonReader;
        private readonly JsonWriter _jsonWriter;
        private readonly IBsonSerializer<TDocument> _serializer;

        // constructors
        public InsertMessageJsonEncoder(JsonReader jsonReader, JsonWriter jsonWriter, IBsonSerializer<TDocument> serializer)
        {
            Ensure.That(jsonReader != null || jsonWriter != null, "jsonReader and jsonWriter cannot both be null.");
            _jsonReader = jsonReader;
            _jsonWriter = jsonWriter;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        // methods
        public InsertMessage<TDocument> ReadMessage()
        {
            if (_jsonReader == null)
            {
                throw new InvalidOperationException("No jsonReader was provided.");
            }

            var messageContext = BsonDeserializationContext.CreateRoot<BsonDocument>(_jsonReader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(messageContext);

            var opcode = messageDocument["opcode"].AsString;
            if (opcode != "insert")
            {
                throw new FormatException("Opcode is not insert.");
            }

            var requestId = messageDocument["requestId"].ToInt32();
            var databaseName = messageDocument["database"].AsString;
            var collectionName = messageDocument["collection"].AsString;
            var maxBatchCount = messageDocument["maxBatchCount"].ToInt32();
            var maxMessageSize = messageDocument["maxMessageSize"].ToInt32();
            var continueOnError = messageDocument["continueOnError"].ToBoolean();
            var documents = messageDocument["documents"];

            if (documents.IsBsonNull)
            {
                throw new FormatException("InsertMessageJsonEncoder requires documents to not be null.");
            }

            var batch = new List<TDocument>();
            foreach (BsonDocument serializedDocument in documents.AsBsonArray)
            {
                using (var documentReader = new BsonDocumentReader(serializedDocument))
                {
                    var documentContext = BsonDeserializationContext.CreateRoot<TDocument>(documentReader);
                    var document = _serializer.Deserialize(documentContext);
                    batch.Add(document);
                }
            }
            var documentSource = new BatchableSource<TDocument>(batch);

            return new InsertMessage<TDocument>(
                requestId,
                databaseName,
                collectionName,
                _serializer,
                documentSource,
                maxBatchCount,
                maxMessageSize,
                continueOnError);
        }

        public void WriteMessage(InsertMessage<TDocument> message)
        {
            Ensure.IsNotNull(message, "message");
            if (_jsonWriter == null)
            {
                throw new InvalidOperationException("No jsonWriter was provided.");
            }

            BsonValue documents;
            if (message.DocumentSource.Batch == null)
            {
                documents = BsonNull.Value;
            }
            else
            {
                var array = new BsonArray();
                foreach (var document in message.DocumentSource.Batch)
                {
                    var wrappedDocument = new BsonDocumentWrapper(document, _serializer);
                    array.Add(wrappedDocument);
                }
                documents = array;
            }

            var messageDocument = new BsonDocument
            {
                { "opcode", "insert" },
                { "requestId", message.RequestId },
                { "database", message.DatabaseName },
                { "collection", message.CollectionName },
                { "maxBatchCount", message.MaxBatchCount },
                { "maxMessageSize", message.MaxMessageSize },
                { "continueOnError", message.ContinueOnError },
                { "documents", documents }
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
            WriteMessage((InsertMessage<TDocument>)message);
        }
    }
}
