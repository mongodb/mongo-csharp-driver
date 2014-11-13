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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    public abstract class ReplyMessage : MongoDBMessage
    {
    }

    public class ReplyMessage<TDocument> : ReplyMessage
    {
        // fields
        private readonly bool _awaitCapable;
        private readonly long _cursorId;
        private readonly bool _cursorNotFound;
        private readonly List<TDocument> _documents;
        private readonly int _numberReturned;
        private readonly bool _queryFailure;
        private readonly BsonDocument _queryFailureDocument;
        private readonly int _requestId;
        private readonly int _responseTo;
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
            _requestId = requestId;
            _responseTo = responseTo;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
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

        public int RequestId
        {
            get { return _requestId; }
        }

        public int ResponseTo
        {
            get { return _responseTo; }
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
        public new IMessageEncoder<ReplyMessage<TDocument>> GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetReplyMessageEncoder<TDocument>(_serializer);
        }

        protected override IMessageEncoder GetNonGenericEncoder(IMessageEncoderFactory encoderFactory)
        {
            return GetEncoder(encoderFactory);
        }
    }
}
