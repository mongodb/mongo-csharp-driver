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

using System;
using System.Collections.Generic;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class QueryOperation<TDocument> : ReadOperationBase
    {
        private readonly int _batchSize;
        private readonly IMongoFields _fields;
        private readonly QueryFlags _flags;
        private readonly int _limit;
        private readonly BsonDocument _options;
        private readonly IMongoQuery _query;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly int _skip;

        public QueryOperation(
            string databaseName,
            string collectionName,
            BsonBinaryReaderSettings readerSettings,
            BsonBinaryWriterSettings writerSettings,
            int batchSize,
            IMongoFields fields,
            QueryFlags flags,
            int limit,
            BsonDocument options,
            IMongoQuery query,
            ReadPreference readPreference,
            IBsonSerializer<TDocument> serializer,
            int skip)
            : base(databaseName, collectionName, readerSettings, writerSettings)
        {
            _batchSize = batchSize;
            _fields = fields;
            _flags = flags;
            _limit = limit;
            _options = options;
            _query = query;
            _readPreference = readPreference;
            _serializer = serializer;
            _skip = skip;

            // since we're going to block anyway when a tailable cursor is temporarily out of data
            // we might as well do it as efficiently as possible
            if ((_flags & QueryFlags.TailableCursor) != 0)
            {
                _flags |= QueryFlags.AwaitData;
            }
        }

        public IEnumerator<TDocument> Execute(IConnectionProvider connectionProvider)
        {
            var reply = GetFirstBatch(connectionProvider);
            return new CursorEnumerator<TDocument>(
                connectionProvider,
                CollectionFullName,
                reply.Documents, // firstBatch
                reply.CursorId,
                _batchSize,
                _limit,
                ReaderSettings,
                _serializer);
        }

        private MongoReplyMessage<TDocument> GetFirstBatch(IConnectionProvider connectionProvider)
        {
            // some of these weird conditions are necessary to get commands to run correctly
            // specifically numberToReturn has to be 1 or -1 for commands
            int numberToReturn;
            if (_limit < 0)
            {
                numberToReturn = _limit;
            }
            else if (_limit == 0)
            {
                numberToReturn = _batchSize;
            }
            else if (_batchSize == 0)
            {
                numberToReturn = _limit;
            }
            else
            {
                numberToReturn = Math.Min(_batchSize, _limit);
            }

            var connection = connectionProvider.AcquireConnection();
            try
            {
                var maxDocumentSize = connection.ServerInstance.MaxDocumentSize;
                var forShardRouter = connection.ServerInstance.InstanceType == MongoServerInstanceType.ShardRouter;
                var wrappedQuery = WrapQuery(_query, _options, _readPreference, forShardRouter);
                var queryMessage = new MongoQueryMessage(WriterSettings, CollectionFullName, _flags, maxDocumentSize, _skip, numberToReturn, wrappedQuery, _fields);
                connection.SendMessage(queryMessage);
                return connection.ReceiveMessage<TDocument>(ReaderSettings, _serializer);
            }
            finally
            {
                connectionProvider.ReleaseConnection(connection);
            }
        }
    }
}
