/* Copyright 2010-present MongoDB Inc.
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
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal sealed class ReplyMessage<TDocument> : ResponseMessage
    {
        // fields
        private readonly bool _awaitCapable;
        private readonly long _cursorId;
        private readonly bool _cursorNotFound;
        private readonly List<TDocument> _documents;
        private readonly int _numberReturned;
        private readonly bool _queryFailure;
        private readonly BsonDocument _queryFailureDocument;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly int _startingFrom;

        // constructors
        public ReplyMessage(
            bool awaitCapable,
            long cursorId,
            bool cursorNotFound,
            List<TDocument> documents,
            int numberReturned,
            bool queryFailure,
            BsonDocument queryFailureDocument,
            int requestId,
            int responseTo,
            IBsonSerializer<TDocument> serializer,
            int startingFrom)
            : base(requestId, responseTo)
        {
            if (documents == null && queryFailureDocument == null && !cursorNotFound)
            {
                throw new ArgumentException("Either documents or queryFailureDocument must be provided.");
            }
            if (documents != null && queryFailureDocument != null)
            {
                throw new ArgumentException("Documents and queryFailureDocument cannot both be provided.");
            }
            _awaitCapable = awaitCapable;
            _cursorId = cursorId;
            _cursorNotFound = cursorNotFound;
            _documents = documents; // can be null
            _numberReturned = numberReturned;
            _queryFailure = queryFailure;
            _queryFailureDocument = queryFailureDocument; // can be null
            _serializer = Ensure.IsNotNull(serializer, nameof(serializer));
            _startingFrom = startingFrom;
        }

        // properties
        public bool AwaitCapable
        {
            get { return _awaitCapable; }
        }

        public long CursorId
        {
            get { return _cursorId; }
        }

        public bool CursorNotFound
        {
            get { return _cursorNotFound; }
        }

        public List<TDocument> Documents
        {
            get { return _documents; }
        }

        public override MongoDBMessageType MessageType
        {
            get { return MongoDBMessageType.Reply; }
        }

        public int NumberReturned
        {
            get { return _numberReturned; }
        }

        public bool QueryFailure
        {
            get { return _queryFailure; }
        }

        public BsonDocument QueryFailureDocument
        {
            get { return _queryFailureDocument; }
        }

        public IBsonSerializer<TDocument> Serializer
        {
            get { return _serializer; }
        }

        public int StartingFrom
        {
            get { return _startingFrom; }
        }

        // methods
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetReplyMessageEncoder<TDocument>(_serializer);
        }
    }
}
