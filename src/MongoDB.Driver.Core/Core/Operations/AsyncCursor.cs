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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;

namespace MongoDB.Driver.Core.Operations
{
    public class AsyncCursor<TDocument> : IAsyncCursor<TDocument>
    {
        // fields
        private readonly int _batchSize;
        private readonly CancellationToken _cancellationToken;
        private readonly CollectionNamespace _collectionNamespace;
        private readonly IConnectionSource _connectionSource;
        private int _count;
        private IReadOnlyList<TDocument> _currentBatch;
        private long _cursorId;
        private bool _disposed;
        private IReadOnlyList<TDocument> _firstBatch;
        private readonly int _limit;
        private readonly MessageEncoderSettings _messageEncoderSettings;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly TimeSpan _timeout;

        // constructors
        public AsyncCursor(
            IConnectionSource connectionSource,
            CollectionNamespace collectionNamespace,
            BsonDocument query,
            IReadOnlyList<TDocument> firstBatch,
            long cursorId,
            int batchSize,
            int limit,
            IBsonSerializer<TDocument> serializer,
            MessageEncoderSettings messageEncoderSettings,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            _connectionSource = connectionSource;
            _collectionNamespace = Ensure.IsNotNull(collectionNamespace, "collectionNamespace");
            _query = Ensure.IsNotNull(query, "query");
            _firstBatch = Ensure.IsNotNull(firstBatch, "firstBatch");
            _cursorId = cursorId;
            _batchSize = Ensure.IsGreaterThanOrEqualToZero(batchSize, "batchSize");
            _limit = Ensure.IsGreaterThanOrEqualToZero(limit, "limit");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _messageEncoderSettings = messageEncoderSettings;
            _timeout = timeout;
            _cancellationToken = cancellationToken;

            if (_limit == 0)
            {
                _limit = int.MaxValue;
            }
            if (_firstBatch.Count > _limit)
            {
                _firstBatch = _firstBatch.Take(_limit).ToList();
            }
            _count = _firstBatch.Count;

            // if we aren't going to need the connection source we can go ahead and Dispose it now
            if (_cursorId == 0 && _connectionSource != null)
            {
                _connectionSource.Dispose();
                _connectionSource = null;
            }
        }

        // properties
        public IEnumerable<TDocument> Current
        {
            get
            {
                ThrowIfDisposed();
                return _currentBatch;
            }
        }

        // methods
        private GetMoreWireProtocol<TDocument> CreateGetMoreProtocol()
        {
            return new GetMoreWireProtocol<TDocument>(
                _collectionNamespace,
                _query,
                _cursorId,
                _batchSize,
                _serializer,
                _messageEncoderSettings);
        }

        private KillCursorsWireProtocol CreateKillCursorsProtocol()
        {
            return new KillCursorsWireProtocol(new[] { _cursorId }, _messageEncoderSettings);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    try
                    {
                        if (_cursorId != 0)
                        {
                            KillCursorAsync(_cursorId).GetAwaiter().GetResult();
                        }
                    }
                    catch
                    {
                        // ignore exceptions
                    }
                    if (_connectionSource != null)
                    {
                        _connectionSource.Dispose();
                    }
                }
            }
            _disposed = true;
        }

        private async Task<CursorBatch<TDocument>> GetNextBatchAsync()
        {
            using (var connection = await _connectionSource.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).ConfigureAwait(false))
            {
                var protocol = CreateGetMoreProtocol();
                return await protocol.ExecuteAsync(connection, _cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task KillCursorAsync(long cursorId)
        {
            try
            {
                var slidingTimeout = new SlidingTimeout(TimeSpan.FromSeconds(10));
                using (var connection = await _connectionSource.GetConnectionAsync(slidingTimeout, default(CancellationToken)).ConfigureAwait(false))
                {
                    var protocol = CreateKillCursorsProtocol();
                    await protocol.ExecuteAsync(connection, slidingTimeout, default(CancellationToken)).ConfigureAwait(false);
                }
            }
            catch
            {
                // ignore exceptions
            }
        }

        public async Task<bool> MoveNextAsync()
        {
            ThrowIfDisposed();

            if (_firstBatch != null)
            {
                _currentBatch = _firstBatch;
                _firstBatch = null;
                return true;
            }

            if (_currentBatch == null)
            {
                return false;
            }

            if (_cursorId == 0 || _count == _limit)
            {
                _currentBatch = null;
                return false;
            }

            var batch = await GetNextBatchAsync().ConfigureAwait(false);
            var cursorId = batch.CursorId;
            var documents = batch.Documents;

            _count += documents.Count;
            if (_count > _limit)
            {
                var remove = _count - _limit;
                var take = documents.Count - remove;
                documents = documents.Take(take).ToList();
                _count = _limit;
            }

            _currentBatch = documents;
            _cursorId = cursorId;
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
