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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class QueryWireProtocol<TDocument> : IWireProtocol<CursorBatch<TDocument>>
    {
        // fields
        private readonly bool _awaitData;
        private readonly int _batchSize;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly BsonDocument _fields;
        private readonly bool _noCursorTimeout;
        private readonly bool _partialOk;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly int _skip;
        private readonly bool _slaveOk;
        private readonly bool _tailableCursor;

        // constructors
        public QueryWireProtocol(
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            BsonDocument fields,
            int skip,
            int batchSize,
            bool slaveOk,
            bool partialOk,
            bool noCursorTimeout,
            bool tailableCursor,
            bool awaitData,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings)
        {
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _query = Ensure.IsNotNull(query, "query");
            _fields = fields; // can be null
            _skip = Ensure.IsGreaterThanOrEqualToZero(skip, "skip");
            _batchSize = batchSize; // can be negative
            _slaveOk = slaveOk;
            _partialOk = partialOk;
            _noCursorTimeout = noCursorTimeout;
            _tailableCursor = tailableCursor;
            _awaitData = awaitData;
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = messageEncoderSettings;
        }

        // methods
        private QueryMessage CreateMessage()
        {
            return new QueryMessage(
                RequestMessage.GetNextRequestId(),
                _collectionNamespace,
                _query,
                _fields,
                _skip,
                _batchSize,
                _slaveOk,
                _partialOk,
                _noCursorTimeout,
                _tailableCursor,
                _awaitData);
        }

        public async Task<CursorBatch<TDocument>> ExecuteAsync(IConnection connection, TimeSpan timeout = default(TimeSpan), CancellationToken cancellationToken = default(CancellationToken))
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var message = CreateMessage();
            await connection.SendMessageAsync(message, _messageEncoderSettings, slidingTimeout, cancellationToken);
            var reply = await connection.ReceiveMessageAsync<TDocument>(message.RequestId, _serializer, _messageEncoderSettings, slidingTimeout, cancellationToken);
            return ProcessReply(reply);
        }

        private CursorBatch<TDocument> ProcessReply(ReplyMessage<TDocument> reply)
        {
            if (reply.QueryFailure)
            {
                var document = reply.QueryFailureDocument;

                var mappedException = ExceptionMapper.Map(document);
                if (mappedException != null)
                {
                    throw mappedException;
                }

                var err = document.GetValue("$err", "Unknown error.");
                var message = string.Format("QueryFailure flag was {0} (response was {1}).", err, document.ToJson());
                throw new MongoQueryException(message, _query, document);
            }

            return new CursorBatch<TDocument>(reply.CursorId, reply.Documents);
        }
    }
}
