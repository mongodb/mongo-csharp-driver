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
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class DeleteMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder<DeleteMessage>
    {
        // constructors
        public DeleteMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
        }

        // methods
        private DeleteFlags BuildDeleteFlags(DeleteMessage message)
        {
            var flags = DeleteFlags.None;
            if (!message.IsMulti)
            {
                flags |= DeleteFlags.Single;
            }
            return flags;
        }

        public DeleteMessage ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;

            streamReader.ReadInt32(); // messageSize
            var requestId = streamReader.ReadInt32();
            streamReader.ReadInt32(); // responseTo
            streamReader.ReadInt32(); // opcode
            streamReader.ReadInt32(); // reserved
            var fullCollectionName = streamReader.ReadCString();
            var flags = (DeleteFlags)streamReader.ReadInt32();
            var context = BsonDeserializationContext.CreateRoot<BsonDocument>(binaryReader);
            var query = BsonDocumentSerializer.Instance.Deserialize(context);

            var isMulti = !flags.HasFlag(DeleteFlags.Single);

            return new DeleteMessage(
                requestId,
                CollectionNamespace.FromFullName(fullCollectionName),
                query,
                isMulti);
        }

        public void WriteMessage(DeleteMessage message)
        {
            Ensure.IsNotNull(message, "message");

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Delete);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(message.CollectionNamespace.FullName);
            streamWriter.WriteInt32((int)BuildDeleteFlags(message));
            var context = BsonSerializationContext.CreateRoot<BsonDocument>(binaryWriter);
            BsonDocumentSerializer.Instance.Serialize(context, message.Query ?? new BsonDocument());
            streamWriter.BackpatchSize(startPosition);
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

        // nested types
        [Flags]
        private enum DeleteFlags
        {
            None = 0,
            Single = 1
        }
    }
}
