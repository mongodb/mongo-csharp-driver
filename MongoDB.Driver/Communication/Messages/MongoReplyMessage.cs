/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDB.Driver.Internal
{
    internal class MongoReplyMessage<TDocument> : MongoMessage
    {
        // private fields
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly IBsonSerializer _serializer;
        private readonly IBsonSerializationOptions _serializationOptions;
        private ResponseFlags _responseFlags;
        private long _cursorId;
        private int _startingFrom;
        private int _numberReturned;
        private List<TDocument> _documents;

        // constructors
        internal MongoReplyMessage(BsonBinaryReaderSettings readerSettings, IBsonSerializer serializer, IBsonSerializationOptions serializationOptions)
            : base(MessageOpcode.Reply)
        {
            _readerSettings = readerSettings;
            _serializer = serializer;
            _serializationOptions = serializationOptions;
        }

        // internal properties
        internal ResponseFlags ResponseFlags
        {
            get { return _responseFlags; }
        }

        internal long CursorId
        {
            get { return _cursorId; }
        }

        internal int StartingFrom
        {
            get { return _startingFrom; }
        }

        internal int NumberReturned
        {
            get { return _numberReturned; }
        }

        internal List<TDocument> Documents
        {
            get { return _documents; }
        }

        // internal methods
        internal void ReadBodyFrom(BsonBuffer buffer)
        {
            var serializationOptions = _serializationOptions;
            if (serializationOptions == null && typeof(TDocument) == typeof(BsonDocument))
            {
                serializationOptions = DocumentSerializationOptions.AllowDuplicateNamesInstance;
            }

            _documents = new List<TDocument>(_numberReturned);
            for (int i = 0; i < _numberReturned; i++)
            {
                BsonBuffer sliceBuffer;
                if (buffer.ByteBuffer is MultiChunkBuffer)
                {
                    // we can use slightly faster SingleChunkBuffers for all documents that don't span chunk boundaries
                    var position = buffer.Position;
                    var length = buffer.ReadInt32();
                    var slice = buffer.ByteBuffer.GetSlice(position, length);
                    buffer.Position = position + length;
                    sliceBuffer = new BsonBuffer(slice, true);
                }
                else
                {
                    sliceBuffer = new BsonBuffer(buffer.ByteBuffer, false);
                }

                using (var bsonReader = new BsonBinaryReader(sliceBuffer, true, _readerSettings))
                {
                    var document = (TDocument)_serializer.Deserialize(bsonReader, typeof(TDocument), serializationOptions);
                    _documents.Add(document);
                }
            }
        }

        internal void ReadFrom(BsonBuffer buffer)
        {
            ReadHeaderFrom(buffer);
            ReadBodyFrom(buffer);
        }

        internal override void ReadHeaderFrom(BsonBuffer buffer)
        {
            base.ReadHeaderFrom(buffer);
            _responseFlags = (ResponseFlags)buffer.ReadInt32();
            _cursorId = buffer.ReadInt64();
            _startingFrom = buffer.ReadInt32();
            _numberReturned = buffer.ReadInt32();

            if ((_responseFlags & ResponseFlags.CursorNotFound) != 0)
            {
                throw new MongoQueryException("Cursor not found.");
            }
            if ((_responseFlags & ResponseFlags.QueryFailure) != 0)
            {
                BsonDocument document;
                using (BsonReader bsonReader = new BsonBinaryReader(buffer, false, _readerSettings))
                {
                    document = (BsonDocument)BsonDocumentSerializer.Instance.Deserialize(bsonReader, typeof(BsonDocument), null);
                }

                var mappedException = ExceptionMapper.Map(document);
                if (mappedException != null)
                {
                    throw mappedException;
                }

                var err = document.GetValue("$err", "Unknown error.");
                var message = string.Format("QueryFailure flag was {0} (response was {1}).", err, document.ToJson());
                throw new MongoQueryException(message, document);
            }
        }
    }
}