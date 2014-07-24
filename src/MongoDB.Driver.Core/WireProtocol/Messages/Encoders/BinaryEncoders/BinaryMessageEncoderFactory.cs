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

using System.IO;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.WireProtocol.Messages.Encoders.BinaryEncoders
{
    public class BinaryMessageEncoderFactory : IMessageEncoderFactory
    {
        // fields
        private readonly BsonBinaryReader _binaryReader;
        private readonly BsonBinaryWriter _binaryWriter;

        // constructors
        public BinaryMessageEncoderFactory(BsonBinaryReader binaryReader)
            : this(Ensure.IsNotNull(binaryReader, "binaryReader"), null)
        {
        }

        public BinaryMessageEncoderFactory(BsonBinaryWriter binaryWriter)
            : this(null, Ensure.IsNotNull(binaryWriter, "binaryWriter"))
        {
        }

        public BinaryMessageEncoderFactory(BsonBinaryReader binaryReader, BsonBinaryWriter binaryWriter)
        {
            Ensure.That(binaryReader != null || binaryWriter != null, "bsonReader and bsonWriter cannot both be null.");
            _binaryReader = binaryReader;
            _binaryWriter = binaryWriter;
        }

        // methods
        public IMessageEncoder<DeleteMessage> GetDeleteMessageEncoder()
        {
            return new DeleteMessageBinaryEncoder(_binaryReader, _binaryWriter);
        }

        public IMessageEncoder<GetMoreMessage> GetGetMoreMessageEncoder()
        {
            return new GetMoreMessageBinaryEncoder(_binaryReader, _binaryWriter);
        }

        public IMessageEncoder<InsertMessage<TDocument>> GetInsertMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new InsertMessageBinaryEncoder<TDocument>(_binaryReader, _binaryWriter, serializer);
        }

        public IMessageEncoder<KillCursorsMessage> GetKillCursorsMessageEncoder()
        {
            return new KillCursorsMessageBinaryEncoder(_binaryReader, _binaryWriter);
        }

        public IMessageEncoder<QueryMessage> GetQueryMessageEncoder()
        {
            return new QueryMessageBinaryEncoder(_binaryReader, _binaryWriter);
        }

        public IMessageEncoder<ReplyMessage<TDocument>> GetReplyMessageEncoder<TDocument>(IBsonSerializer<TDocument> serializer)
        {
            return new ReplyMessageBinaryEncoder<TDocument>(_binaryReader, _binaryWriter, serializer);
        }

        public IMessageEncoder<UpdateMessage> GetUpdateMessageEncoder()
        {
            return new UpdateMessageBinaryEncoder(_binaryReader, _binaryWriter);
        }
    }
}
