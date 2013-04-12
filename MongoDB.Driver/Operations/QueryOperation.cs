/* Copyright 2010-2013 10gen Inc.
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
    internal class QueryOperation<TDocument> : ReadOperation
    {
        private readonly int _batchSize;
        private readonly IMongoFields _fields;
        private readonly QueryFlags _flags;
        private readonly int _limit;
        private readonly BsonDocument _options;
        private readonly IMongoQuery _query;
        private readonly ReadPreference _readPreference;
        private readonly IBsonSerializationOptions _serializationOptions;
        private readonly IBsonSerializer _serializer;
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
            IBsonSerializationOptions serializationOptions,
            IBsonSerializer serializer,
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
            _serializationOptions = serializationOptions;
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
            MongoReplyMessage<TDocument> reply = null;
            try
            {
                var count = 0;
                var limit = (_limit >= 0) ? _limit : -_limit;

                reply = GetFirstBatch(connectionProvider);
                foreach (var document in reply.Documents)
                {
                    if (limit != 0 && count == limit)
                    {
                        yield break;
                    }
                    yield return document;
                    count++;
                }

                while (reply.CursorId != 0)
                {
                    reply = GetNextBatch(connectionProvider, reply.CursorId);
                    foreach (var document in reply.Documents)
                    {
                        if (limit != 0 && count == limit)
                        {
                            yield break;
                        }
                        yield return document;
                        count++;
                    }

                    if (reply.CursorId != 0 && (_flags & QueryFlags.TailableCursor) != 0)
                    {
                        if ((reply.ResponseFlags & ResponseFlags.AwaitCapable) == 0)
                        {
                            Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        }
                    }
                }
            }
            finally
            {
                if (reply != null && reply.CursorId != 0)
                {
                    KillCursor(connectionProvider, reply.CursorId);
                }
            }
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
            else if (_limit < _batchSize)
            {
                numberToReturn = _limit;
            }
            else
            {
                numberToReturn = _batchSize;
            }

            var connection = connectionProvider.AcquireConnection();
            try
            {
                var readerSettings = GetNodeAdjustedReaderSettings(connection.ServerInstance);
                var writerSettings = GetNodeAdjustedWriterSettings(connection.ServerInstance);
                var forShardRouter = connection.ServerInstance.InstanceType == MongoServerInstanceType.ShardRouter;
                var wrappedQuery = WrapQuery(_query, _options, _readPreference, forShardRouter);
                var queryMessage = new MongoQueryMessage(writerSettings, CollectionFullName, _flags, _skip, numberToReturn, wrappedQuery, _fields);
                connection.SendMessage(queryMessage);
                return connection.ReceiveMessage<TDocument>(readerSettings, _serializer, _serializationOptions);
            }
            finally
            {
                connectionProvider.ReleaseConnection(connection);
            }
        }

        private MongoReplyMessage<TDocument> GetNextBatch(IConnectionProvider connectionProvider, long cursorId)
        {
            var connection = connectionProvider.AcquireConnection();
            try
            {
                var readerSettings = GetNodeAdjustedReaderSettings(connection.ServerInstance);
                var getMoreMessage = new MongoGetMoreMessage(CollectionFullName, _batchSize, cursorId);
                connection.SendMessage(getMoreMessage);
                return connection.ReceiveMessage<TDocument>(readerSettings, _serializer, _serializationOptions);
            }
            finally
            {
                connectionProvider.ReleaseConnection(connection);
            }
        }

        private void KillCursor(IConnectionProvider connectionProvider, long cursorId)
        {
            var connection = connectionProvider.AcquireConnection();
            try
            {
                var killCursorsMessage = new MongoKillCursorsMessage(cursorId);
                connection.SendMessage(killCursorsMessage);
            }
            finally
            {
                connectionProvider.ReleaseConnection(connection);
            }
        }
    }
}
