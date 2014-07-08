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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.JsonEncoders
{
    public class UpdateMessageJsonEncoder : IMessageEncoder<UpdateMessage>
    {
        // fields
        private readonly JsonReader _jsonReader;
        private readonly JsonWriter _jsonWriter;

        // constructors
        public UpdateMessageJsonEncoder(JsonReader jsonReader, JsonWriter jsonWriter)
        {
            _jsonReader = jsonReader;
            _jsonWriter = jsonWriter;
        }

        // methods
        public UpdateMessage ReadMessage()
        {
            throw new NotImplementedException();
        }

        public void WriteMessage(UpdateMessage message)
        {
            var document = new BsonDocument
            {
                { "opcode", "update" },
                { "requestId", message.RequestId },
                { "database", message.DatabaseName },
                { "collection", message.CollectionName },
                { "isMulti", true, message.IsMulti },
                { "isUpsert", true, message.IsUpsert },
                { "query", (BsonValue)message.Query ?? BsonNull.Value },
                { "update", (BsonValue)message.Update ?? BsonNull.Value }
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
            WriteMessage((UpdateMessage)message);
        }
    }
}
