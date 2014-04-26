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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver.Operations
{
    internal class CursorEnumerator<TDocument> : IEnumerator<TDocument>
    {
        // private fields
        private readonly int _batchSize;
        private readonly string _collectionFullName;
        private readonly IConnectionProvider _connectionProvider;
        private readonly int _limit;
        private readonly BsonBinaryReaderSettings _readerSettings;
        private readonly IBsonSerializer<TDocument> _serializer;

        private long _count = 0;
        private List<TDocument> _currentBatch;
        private int _currentBatchIndex = -1;
        private long _cursorId;
        private bool _disposed;
        private bool _done;

        // constructors
        public CursorEnumerator(
            IConnectionProvider connectionProvider,
            string collectionFullName,
            IEnumerable<TDocument> firstBatch,
            long cursorId,
            int batchSize,
            int limit,
            BsonBinaryReaderSettings readerSettings,
            IBsonSerializer<TDocument> serializer)
        {
            _connectionProvider = connectionProvider;
            _collectionFullName = collectionFullName;
            _currentBatch = firstBatch.ToList();
            _cursorId = cursorId;
            _batchSize = batchSize;
            _limit = Math.Abs(limit);
            _readerSettings = readerSettings;
            _serializer = serializer;
        }

        // public properties
        public TDocument Current
        {
            get
            {
                ThrowIfDisposed();
                if (_currentBatchIndex == -1)
                {
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                }
                if (_done)
                {
                    throw new InvalidOperationException("Enumeration already finished.");
                }
                return _currentBatch[_currentBatchIndex];
            }
        }

        // public methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool MoveNext()
        {
            ThrowIfDisposed();
            if (_done)
            {
                return false;
            }

            if (_limit > 0 && _count >= _limit)
            {
                _done = true;
                return false;
            }

            _count++;
            _currentBatchIndex++;

            if (_currentBatchIndex < _currentBatch.Count)
            {
                return true;
            }

            while (_cursorId != 0)
            {
                var batch = GetNextBatch();
                _cursorId = batch.CursorId;
                _currentBatch = batch.Documents;
                _currentBatchIndex = 0;
                if (_currentBatch.Count > 0)
                {
                    return true;
                }
            }

            _done = true;
            return false;
        }

        // private methods
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_cursorId != 0)
                    {
                        try
                        {
                            KillCursor(_connectionProvider, _cursorId);
                        }
                        catch
                        {
                            // ignore exceptions in KillCursor
                        }
                    }
                }
                _disposed = true;
            }
        }

        private MongoReplyMessage<TDocument> GetNextBatch()
        {
            var connection = _connectionProvider.AcquireConnection();
            try
            {
                int numberToReturn;
                if (_limit == 0)
                {
                    numberToReturn = _batchSize;
                }
                else
                {
                    var numberToHitLimit = _limit - _count;
                    numberToReturn = (int)Math.Min(_batchSize, numberToHitLimit);
                }

                var getMoreMessage = new MongoGetMoreMessage(_collectionFullName, numberToReturn, _cursorId);
                connection.SendMessage(getMoreMessage);
                return connection.ReceiveMessage<TDocument>(_readerSettings, _serializer);
            }
            finally
            {
                _connectionProvider.ReleaseConnection(connection);
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("CursorEnumerator");
            }
        }

        // explicit interface implementations
        object IEnumerator.Current
        {
            get { return Current; }
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }
}
