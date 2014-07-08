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
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.WireProtocol;

namespace MongoDB.Driver.Core.Operations
{
    public class Cursor<TDocument> : IAsyncEnumerator<IReadOnlyList<TDocument>>
    {
        // fields
        private readonly int _batchSize;
        private readonly CancellationToken _cancellationToken;
        private readonly string _collectionName;
        private readonly IConnectionSource _connectionSource;
        private IReadOnlyList<TDocument> _currentBatch;
        private long _cursorId;
        private readonly string _databaseName;
        private bool _disposed;
        private IReadOnlyList<TDocument> _firstBatch;
        private readonly BsonDocument _query;
        private readonly IBsonSerializer<TDocument> _serializer;
        private readonly TimeSpan _timeout;

        // constructors
        public Cursor(
            IConnectionSource connectionSource,
            string databaseName,
            string collectionName,
            BsonDocument query,
            IReadOnlyList<TDocument> firstBatch,
            long cursorId,
            int batchSize,
            IBsonSerializer<TDocument> serializer,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            _connectionSource = cursorId == 0 ? connectionSource : Ensure.IsNotNull(connectionSource, "connectionSource");
            _databaseName = Ensure.IsNotNullOrEmpty(databaseName, "databaseName");
            _collectionName = Ensure.IsNotNullOrEmpty(collectionName, "collectionName");
            _query = Ensure.IsNotNull(query, "query");
            _firstBatch = Ensure.IsNotNull(firstBatch, "firstBatch");
            _cursorId = cursorId;
            _batchSize = Ensure.IsGreaterThanOrEqualToZero(batchSize, "batchSize");
            _serializer = Ensure.IsNotNull(serializer, "serializer");
            _timeout = timeout;
            _cancellationToken = cancellationToken;
        }

        // properties
        public IReadOnlyList<TDocument> Current
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
                _databaseName,
                _collectionName,
                _query,
                _cursorId,
                _batchSize,
                _serializer);
        }

        private KillCursorsWireProtocol CreateKillCursorsProtocol()
        {
            return new KillCursorsWireProtocol(new[] { _cursorId });
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
            using (var connection = await _connectionSource.GetConnectionAsync())
            {
                var protocol = CreateGetMoreProtocol();
                return await protocol.ExecuteAsync(connection, _timeout, _cancellationToken);
            }
        }

        private async Task KillCursorAsync(long cursorId)
        {
            try
            {
                var slidingTimeout = new SlidingTimeout(TimeSpan.FromSeconds(10));
                using (var connection = await _connectionSource.GetConnectionAsync(slidingTimeout, default(CancellationToken)))
                {
                    var protocol = CreateKillCursorsProtocol();
                    await protocol.ExecuteAsync(connection, slidingTimeout, default(CancellationToken));
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

            if (_cursorId == 0)
            {
                _currentBatch = null;
                return false;
            }

            var batch = await GetNextBatchAsync();
            _currentBatch = batch.Documents;
            _cursorId = batch.CursorId;
            return true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public DocumentCursor<TDocument> ToDocumentCursor()
        {
            ThrowIfDisposed();
            return new DocumentCursor<TDocument>(this);
        }
    }
}
