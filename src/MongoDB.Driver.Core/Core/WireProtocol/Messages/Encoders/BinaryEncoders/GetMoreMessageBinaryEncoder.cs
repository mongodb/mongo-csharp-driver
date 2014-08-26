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
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class GetMoreMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder<GetMoreMessage>
    {
        // constructors
        public GetMoreMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
        }

        // methods
        public GetMoreMessage ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;

            var messageSize = streamReader.ReadInt32();
            var requestId = streamReader.ReadInt32();
            var responseTo = streamReader.ReadInt32();
            var opcode = (Opcode)streamReader.ReadInt32();
            var reserved = streamReader.ReadInt32();
            var fullCollectionName = streamReader.ReadCString();
            var batchSize = streamReader.ReadInt32();
            var cursorId = streamReader.ReadInt64();

            var firstDot = fullCollectionName.IndexOf('.');
            var databaseName = fullCollectionName.Substring(0, firstDot);
            var collectionName = fullCollectionName.Substring(firstDot + 1);

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

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.GetMore);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(message.DatabaseName + "." + message.CollectionName);
            streamWriter.WriteInt32(message.BatchSize);
            streamWriter.WriteInt64(message.CursorId);
            streamWriter.BackpatchSize(startPosition);
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
