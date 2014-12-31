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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class QueryMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder<QueryMessage>
    {
        // constructors
        public QueryMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
        }

        // methods
        private QueryFlags BuildQueryFlags(QueryMessage message)
        {
            var flags = QueryFlags.None;
            if (message.NoCursorTimeout)
            {
                flags |= QueryFlags.NoCursorTimeout;
            }
            if (message.PartialOk)
            {
                flags |= QueryFlags.Partial;
            }
            if (message.SlaveOk)
            {
                flags |= QueryFlags.SlaveOk;
            }
            if (message.TailableCursor)
            {
                flags |= QueryFlags.TailableCursor;
            }
            if (message.AwaitData)
            {
                flags |= QueryFlags.AwaitData;
            }
            return flags;
        }

        public QueryMessage ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;
            var startPosition = streamReader.Position;

            var messageSize = streamReader.ReadInt32();
            var requestId = streamReader.ReadInt32();
            streamReader.ReadInt32(); // responseTo
            streamReader.ReadInt32(); // opcode
            var flags = (QueryFlags)streamReader.ReadInt32();
            var fullCollectionName = streamReader.ReadCString();
            var skip = streamReader.ReadInt32();
            var batchSize = streamReader.ReadInt32();
            var context = BsonDeserializationContext.CreateRoot(binaryReader);
            var query = BsonDocumentSerializer.Instance.Deserialize(context);
            BsonDocument fields = null;
            if (streamReader.Position < startPosition + messageSize)
            {
                fields = BsonDocumentSerializer.Instance.Deserialize(context);
            }

            var awaitData = (flags & QueryFlags.AwaitData) == QueryFlags.AwaitData;
            var slaveOk = (flags & QueryFlags.SlaveOk) == QueryFlags.SlaveOk;
            var partialOk = (flags & QueryFlags.Partial) == QueryFlags.Partial;
            var noCursorTimeout = (flags & QueryFlags.NoCursorTimeout) == QueryFlags.NoCursorTimeout;
            var tailableCursor = (flags & QueryFlags.TailableCursor) == QueryFlags.TailableCursor;

            return new QueryMessage(
                requestId,
                CollectionNamespace.FromFullName(fullCollectionName),
                query,
                fields,
                NoOpElementNameValidator.Instance,
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

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Query);
            streamWriter.WriteInt32((int)BuildQueryFlags(message));
            streamWriter.WriteCString(message.CollectionNamespace.FullName);
            streamWriter.WriteInt32(message.Skip);
            streamWriter.WriteInt32(message.BatchSize);
            WriteQuery(binaryWriter, message.Query, message.QueryValidator);
            WriteOptionalFields(binaryWriter, message.Fields);
            streamWriter.BackpatchSize(startPosition);
        }

        private void WriteOptionalFields(BsonBinaryWriter binaryWriter, BsonDocument fields)
        {
            if (fields != null)
            {
                var context = BsonSerializationContext.CreateRoot(binaryWriter);
                BsonDocumentSerializer.Instance.Serialize(context, fields);
            }
        }

        private void WriteQuery(BsonBinaryWriter binaryWriter, BsonDocument query, IElementNameValidator queryValidator)
        {
            binaryWriter.PushElementNameValidator(queryValidator);
            try
            {
                var context = BsonSerializationContext.CreateRoot(binaryWriter);
                BsonDocumentSerializer.Instance.Serialize(context, query ?? new BsonDocument());
            }
            finally
            {
                binaryWriter.PopElementNameValidator();
            }
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

        // nested types
        [Flags]
        private enum QueryFlags
        {
            None = 0,
            TailableCursor = 2,
            SlaveOk = 4,
            NoCursorTimeout = 16,
            AwaitData = 32,
            Exhaust = 64,
            Partial = 128
        }
    }
}
