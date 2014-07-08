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
    public class DeleteMessageBinaryEncoder : IMessageEncoder<DeleteMessage>
    {
        // fields
        private readonly BsonBinaryReader _binaryReader;
        private readonly BsonBinaryWriter _binaryWriter;

        // constructors
        public DeleteMessageBinaryEncoder(BsonBinaryReader binaryReader, BsonBinaryWriter binaryWriter)
        {
            _binaryReader = binaryReader;
            _binaryWriter = binaryWriter;
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
            var streamReader = _binaryReader.StreamReader;

            var messageSize = streamReader.ReadInt32();
            var requestId = streamReader.ReadInt32();
            var responseTo = streamReader.ReadInt32();
            var opcode = (Opcode)streamReader.ReadInt32();
            var reserved = streamReader.ReadInt32();
            var fullCollectionName = streamReader.ReadCString();
            var flags = (DeleteFlags)streamReader.ReadInt32();
            var context = BsonDeserializationContext.CreateRoot<BsonDocument>(_binaryReader);
            var query = BsonDocumentSerializer.Instance.Deserialize(context);

            var firstDot = fullCollectionName.IndexOf('.');
            var databaseName = fullCollectionName.Substring(0, firstDot);
            var collectionName = fullCollectionName.Substring(firstDot + 1);
            var isMulti = !flags.HasFlag(DeleteFlags.Single);

            return new DeleteMessage(
                requestId,
                databaseName,
                collectionName,
                query,
                isMulti);
        }

        public void WriteMessage(DeleteMessage message)
        {
            var streamWriter = _binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Delete);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(message.DatabaseName + "." + message.CollectionName);
            streamWriter.WriteInt32((int)BuildDeleteFlags(message));
            var context = BsonSerializationContext.CreateRoot<BsonDocument>(_binaryWriter);
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
