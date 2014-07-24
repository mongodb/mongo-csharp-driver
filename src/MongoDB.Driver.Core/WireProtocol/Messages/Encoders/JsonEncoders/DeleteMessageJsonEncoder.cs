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
    public class DeleteMessageJsonEncoder : IMessageEncoder<DeleteMessage>
    {
        // fields
        private readonly JsonReader _jsonReader;
        private readonly JsonWriter _jsonWriter;

        // constructors
        public DeleteMessageJsonEncoder(JsonReader jsonReader, JsonWriter jsonWriter)
        {
            Ensure.That(jsonReader != null || jsonWriter != null, "jsonReader and jsonWriter cannot both be null.");
            _jsonReader = jsonReader;
            _jsonWriter = jsonWriter;
        }

        // methods
        public DeleteMessage ReadMessage()
        {
            if (_jsonReader == null)
            {
                throw new InvalidOperationException("No jsonReader was provided.");
            }

            var context = BsonDeserializationContext.CreateRoot<BsonDocument>(_jsonReader);
            var document = BsonDocumentSerializer.Instance.Deserialize(context);

            var opcode = document["Opcode"].AsString;
            if (opcode != "Delete")
            {
                throw new FormatException("Opcode is not Delete.");
            }

            var requestId = document["RequestId"].ToInt32();
            var databaseName = document["DatabaseName"].AsString;
            var collectionName = document["CollectionName"].AsString;
            var query = document["Query"].AsBsonDocument;
            var isMulti = document["IsMulti"].ToBoolean();

            return new DeleteMessage(
                requestId,
                databaseName,
                collectionName,
                query,
                isMulti);
        }

        public void WriteMessage(DeleteMessage message)
        {
            Ensure.IsNotNull(message, "message");
            if (_jsonWriter == null)
            {
                throw new InvalidOperationException("No jsonWriter was provided.");
            }

            var document = new BsonDocument
            {
                { "Opcode", "Delete" },
                { "RequestId", message.RequestId },
                { "DatabaseName", message.DatabaseName },
                { "CollectionName", message.CollectionName },
                { "Query", message.Query ?? new BsonDocument() },
                { "IsMulti", message.IsMulti }
            };

            var context = BsonSerializationContext.CreateRoot<BsonDocument>(_jsonWriter);
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }

        // explicit interface implementations
        MongoDBMessage IMessageEncoder.ReadMessage()
        {
            return ReadMessage();
        }

        void IMessageEncoder.WriteMessage(MongoDBMessage message)
        {
            WriteMessage((DeleteMessage)message);
        }
    }
}
