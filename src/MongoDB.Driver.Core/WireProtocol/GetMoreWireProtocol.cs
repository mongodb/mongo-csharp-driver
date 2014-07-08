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
using MongoDB.Driver.Core.Exceptions;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol.Messages;

namespace MongoDB.Driver.Core.WireProtocol
{
    public class GetMoreWireProtocol<TDocument> : IWireProtocol<CursorBatch<TDocument>>
    {
        // fields
        private readonly int _batchSize;
        private readonly string _collectionName;
        private readonly long _cursorId;
        private readonly string _databaseName;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;

        // constructors
        public GetMoreWireProtocol(
            string databaseName,
            string collectionName,
            BsonDocument query,
            long cursorId,
            int batchSize,
            IBsonSerializer<TDocument> serializer)
        {
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _query = Ensure.IsNotNull(query, "query");
            _cursorId = cursorId;
            _batchSize = Ensure.IsGreaterThanOrEqualToZero(batchSize, "batchSize");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
        }

        // methods
        private GetMoreMessage CreateMessage()
        {
            return new GetMoreMessage(
                RequestMessage.GetNextRequestId(),
                _databaseName,
                _collectionName,
                _cursorId,
                _batchSize);
        }

        public async Task<CursorBatch<TDocument>> ExecuteAsync(IConnection connection, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var slidingTimeout = new SlidingTimeout(timeout);
            var message = CreateMessage();
            await connection.SendMessageAsync(message, slidingTimeout, cancellationToken);
            var reply = await connection.ReceiveMessageAsync<TDocument>(message.RequestId, _serializer, slidingTimeout, cancellationToken);
            if (reply.QueryFailure)
            {
                var errorMessage = string.Format("GetMore QueryFailure: {0}.", reply.QueryFailureDocument);
                throw new QueryException(errorMessage, _query, reply.QueryFailureDocument);
            }
            return new CursorBatch<TDocument>(reply.CursorId, reply.Documents);
        }
    }
}
