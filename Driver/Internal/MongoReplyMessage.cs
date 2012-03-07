/* Copyright 2010-2012 10gen Inc.
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

using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;

namespace MongoDB.Driver.Internal
{
    internal class MongoReplyMessage<TDocument> : MongoMessage
    {
        // private fields
        private BsonBinaryReaderSettings _readerSettings;
        private ResponseFlags _responseFlags;
        private long _cursorId;
        private int _startingFrom;
        private int _numberReturned;
        private List<TDocument> _documents;

        // constructors
        internal MongoReplyMessage(BsonBinaryReaderSettings readerSettings)
            : base(MessageOpcode.Reply)
        {
            _readerSettings = readerSettings;
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
        internal void ReadFrom(BsonBuffer buffer, IBsonSerializationOptions serializationOptions)
        {
            if (serializationOptions == null && typeof(TDocument) == typeof(BsonDocument))
            {
                serializationOptions = DocumentSerializationOptions.AllowDuplicateNamesInstance;
            }

            var messageStartPosition = buffer.Position;

            ReadMessageHeaderFrom(buffer);
            _responseFlags = (ResponseFlags)buffer.ReadInt32();
            _cursorId = buffer.ReadInt64();
            _startingFrom = buffer.ReadInt32();
            _numberReturned = buffer.ReadInt32();

            using (BsonReader bsonReader = BsonReader.Create(buffer, _readerSettings))
            {
                if ((_responseFlags & ResponseFlags.CursorNotFound) != 0)
                {
                    throw new MongoQueryException("Cursor not found.");
                }
                if ((_responseFlags & ResponseFlags.QueryFailure) != 0)
                {
                    var document = BsonDocument.ReadFrom(bsonReader);
                    var err = document["$err", null].AsString ?? "Unknown error.";
                    var message = string.Format("QueryFailure flag was {0} (response was {1}).", err, document.ToJson());
                    throw new MongoQueryException(message, document);
                }

                _documents = new List<TDocument>(_numberReturned);
                while (buffer.Position - messageStartPosition < MessageLength)
                {
                    var document = (TDocument)BsonSerializer.Deserialize(bsonReader, typeof(TDocument), serializationOptions);
                    _documents.Add(document);
                }
            }
        }
    }
}
