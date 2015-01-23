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

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    /// <summary>
    /// Represents a binary encoder for an Update message.
    /// </summary>
    public class UpdateMessageBinaryEncoder : MessageBinaryEncoderBase, IMessageEncoder
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateMessageBinaryEncoder"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="encoderSettings">The encoder settings.</param>
        public UpdateMessageBinaryEncoder(Stream stream, MessageEncoderSettings encoderSettings)
            : base(stream, encoderSettings)
        {
        }

        // methods
        private UpdateFlags BuildUpdateFlags(UpdateMessage message)
        {
            var flags = UpdateFlags.None;
            if (message.IsMulti)
            {
                flags |= UpdateFlags.Multi;
            }
            if (message.IsUpsert)
            {
                flags |= UpdateFlags.Upsert;
            }
            return flags;
        }

        /// <inheritdoc/>
        public UpdateMessage ReadMessage()
        {
            var binaryReader = CreateBinaryReader();
            var streamReader = binaryReader.StreamReader;

            streamReader.ReadInt32(); // messageSize
            var requestId = streamReader.ReadInt32();
            streamReader.ReadInt32(); // responseTo
            streamReader.ReadInt32(); // opcode
            streamReader.ReadInt32(); // reserved
            var fullCollectionName = streamReader.ReadCString();
            var flags = (UpdateFlags)streamReader.ReadInt32();
            var context = BsonDeserializationContext.CreateRoot(binaryReader);
            var query = BsonDocumentSerializer.Instance.Deserialize(context);
            var update = BsonDocumentSerializer.Instance.Deserialize(context);

            var isMulti = (flags & UpdateFlags.Multi) == UpdateFlags.Multi;
            var isUpsert = (flags & UpdateFlags.Upsert) == UpdateFlags.Upsert;

            return new UpdateMessage(
                requestId,
                CollectionNamespace.FromFullName(fullCollectionName),
                query,
                update,
                NoOpElementNameValidator.Instance,
                isMulti,
                isUpsert);
        }

        /// <inheritdoc/>
        public void WriteMessage(UpdateMessage message)
        {
            Ensure.IsNotNull(message, "message");

            var binaryWriter = CreateBinaryWriter();
            var streamWriter = binaryWriter.StreamWriter;
            var startPosition = streamWriter.Position;

            streamWriter.WriteInt32(0); // messageSize
            streamWriter.WriteInt32(message.RequestId);
            streamWriter.WriteInt32(0); // responseTo
            streamWriter.WriteInt32((int)Opcode.Update);
            streamWriter.WriteInt32(0); // reserved
            streamWriter.WriteCString(message.CollectionNamespace.FullName);
            streamWriter.WriteInt32((int)BuildUpdateFlags(message));
            WriteQuery(binaryWriter, message.Query);
            WriteUpdate(binaryWriter, message.Update, message.UpdateValidator);
            streamWriter.BackpatchSize(startPosition);
        }

        private void WriteQuery(BsonBinaryWriter binaryWriter, BsonDocument query)
        {
            var context = BsonSerializationContext.CreateRoot(binaryWriter);
            BsonDocumentSerializer.Instance.Serialize(context, query);
        }

        private void WriteUpdate(BsonBinaryWriter binaryWriter, BsonDocument update, IElementNameValidator updateValidator)
        {
            binaryWriter.PushElementNameValidator(updateValidator);
            try
            {
                var context = BsonSerializationContext.CreateRoot(binaryWriter);
                BsonDocumentSerializer.Instance.Serialize(context, update);
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
            WriteMessage((UpdateMessage)message);
        }

        // nested types
        [Flags]
        private enum UpdateFlags
        {
            None = 0,
            Upsert = 1,
            Multi = 2
        }
    }
}
