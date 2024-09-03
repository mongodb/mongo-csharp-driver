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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol.Messages
{
    internal sealed class QueryMessage : RequestMessage
    {
        // fields
        private readonly bool _awaitData;
        private readonly int _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly BsonDocument _fields;
        private readonly bool _noCursorTimeout;
        private readonly bool _oplogReplay;
        private readonly bool _partialOk;
        private Action<IMessageEncoderPostProcessor> _postWriteAction;
        private readonly BsonDocument _query;
        private readonly IElementNameValidator _queryValidator;
        private CommandResponseHandling _responseHandling = CommandResponseHandling.Return;
        private readonly int _skip;
        private readonly bool _secondaryOk;
        private readonly bool _tailableCursor;

        // constructors
        public QueryMessage(
            int requestId,
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool tailableCursor,
            bool awaitData,
            Func<bool> shouldBeSent = null)
#pragma warning disable 618
            : this(
                  requestId,
                  collectionNamespace,
                  query,
                  fields,
                  queryValidator,
                  skip,
                  batchSize,
                  secondaryOk,
                  partialOk,
                  noCursorTimeout,
                  oplogReplay: false,
                  tailableCursor,
                  awaitData,
                  shouldBeSent)
#pragma warning restore 618
        {
        }

        [Obsolete("Use a constructor that does not have an oplogReplay parameter instead.")]
        public QueryMessage(
            int requestId,
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            IElementNameValidator queryValidator,
            int skip,
            int batchSize,
            bool secondaryOk,
            bool partialOk,
            bool noCursorTimeout,
            bool oplogReplay, // obsolete: OplogReplay is ignored by server versions 4.4.0 and newer
            bool tailableCursor,
            bool awaitData,
            Func<bool> shouldBeSent = null)
            : base(requestId, shouldBeSent)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
            _query = Ensure.IsNotNull(query, nameof(query));
            _fields = fields; // can be null
            _queryValidator = Ensure.IsNotNull(queryValidator, nameof(queryValidator));
            _skip = Ensure.IsGreaterThanOrEqualToZero(skip, nameof(skip));
            _batchSize = batchSize; // can be negative
            _secondaryOk = secondaryOk;
            _partialOk = partialOk;
            _noCursorTimeout = noCursorTimeout;
            _oplogReplay = oplogReplay;
            _tailableCursor = tailableCursor;
            _awaitData = awaitData;
        }

        // properties
        public bool AwaitData
        {
            get { return _awaitData; }
        }

        public int BatchSize
        {
            get { return _batchSize; }
        }

        public CollectionNamespace CollectionNamespace
        {
            get { return _collectionNamespace; }
        }

        public BsonDocument Fields
        {
            get { return _fields; }
        }

        public override MongoDBMessageType MessageType
        {
            get { return MongoDBMessageType.Query; }
        }

        public bool NoCursorTimeout
        {
            get { return _noCursorTimeout; }
        }

        [Obsolete("OplogReplay is ignored by server versions 4.4.0 and newer.")]
        public bool OplogReplay
        {
            get { return _oplogReplay; }
        }

        public bool PartialOk
        {
            get { return _partialOk; }
        }

        public Action<IMessageEncoderPostProcessor> PostWriteAction
        {
            get { return _postWriteAction; }
            set { _postWriteAction = value; }
        }

        public BsonDocument Query
        {
            get { return _query; }
        }

        public IElementNameValidator QueryValidator
        {
            get { return _queryValidator; }
        }

        public CommandResponseHandling ResponseHandling
        {
            get { return _responseHandling; }
            set
            {
                if (value != CommandResponseHandling.Return && value != CommandResponseHandling.Ignore)
                {
                    throw new ArgumentException("CommandResponseHandling must be Return or Ignore.", nameof(value));
                }
                _responseHandling = value;
            }
        }

        public bool SecondaryOk
        {
            get { return _secondaryOk; }
        }

        public int Skip
        {
            get { return _skip; }
        }

        public bool TailableCursor
        {
            get { return _tailableCursor; }
        }

        // methods
        public override IMessageEncoder GetEncoder(IMessageEncoderFactory encoderFactory)
        {
            return encoderFactory.GetQueryMessageEncoder();
        }
    }
}
