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
    public class KillCursorsMessageJsonEncoder : IMessageEncoder<KillCursorsMessage>
    {
        // fields
        private readonly JsonReader _jsonReader;
        private readonly JsonWriter _jsonWriter;

        // constructors
        public KillCursorsMessageJsonEncoder(JsonReader jsonReader, JsonWriter jsonWriter)
        {
            Ensure.That(jsonReader != null || jsonWriter != null, "jsonReader and jsonWriter cannot both be null.");
            _jsonReader = jsonReader;
            _jsonWriter = jsonWriter;
        }

        // methods
        public KillCursorsMessage ReadMessage()
        {
            if (_jsonReader == null)
            {
                throw new InvalidOperationException("No jsonReader was provided.");
            }

            var messageContext = BsonDeserializationContext.CreateRoot<BsonDocument>(_jsonReader);
            var messageDocument = BsonDocumentSerializer.Instance.Deserialize(messageContext);

            var opcode = messageDocument["opcode"].AsString;
            if (opcode != "killCursors")
            {
                throw new FormatException("Opcode is not killCursors.");
            }

            var requestId = messageDocument["requestId"].ToInt32();
            var cursorIds = messageDocument["cursorIds"].AsBsonArray.Select(v => v.ToInt64());

            return new KillCursorsMessage(
                requestId,
                cursorIds);
        }

        public void WriteMessage(KillCursorsMessage message)
        {
            Ensure.IsNotNull(message, "message");
            if (_jsonWriter == null)
            {
                throw new InvalidOperationException("No jsonWriter was provided.");
            }

            var messageDocument = new BsonDocument
            {
                { "opcode", "killCursors" },
                { "requestId", message.RequestId },
                { "cursorIds", new BsonArray(message.CursorIds.Select(id => new BsonInt64(id))) }
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
            WriteMessage((KillCursorsMessage)message);
        }
    }
}
