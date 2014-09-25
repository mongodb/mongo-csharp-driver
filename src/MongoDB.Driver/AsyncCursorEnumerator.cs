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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
    internal class AsyncCursorEnumerator<TDocument> : IAsyncEnumerator<TDocument>
    {
        // fields
        private readonly IAsyncCursor<TDocument> _cursor;
        private long _count;
        private IEnumerator<TDocument> _currentBatch;
        private bool _disposed;
        private bool _done;
        private readonly long? _limit;

        // constructors
        public AsyncCursorEnumerator(IAsyncCursor<TDocument> cursor)
            : this(cursor, null)
        {
        }

        public AsyncCursorEnumerator(IAsyncCursor<TDocument> cursor, long? limit)
        {
            _cursor = Ensure.IsNotNull(cursor, "cursor");
            _limit = Ensure.IsNullOrGreaterThanOrEqualToZero(limit, "limit");
        }

        // properties
        public TDocument Current
        {
            get
            {
                ThrowIfDisposed();
                if (_currentBatch == null)
                {
                    if (_done)
                    {
                        throw new InvalidOperationException("Enumeration already finished.");
                    }
                    else
                    {
                        throw new InvalidOperationException("Enumeration has not started. Call MoveNextAsync.");
                    }
                }
                return _currentBatch.Current;
            }
        }

        // methods
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_currentBatch != null)
                {
                    _currentBatch.Dispose();
                }
                _cursor.Dispose();
            }
            _disposed = true;
        }

        public async Task<bool> MoveNextAsync(TimeSpan? timeout, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_done)
            {
                return false;
            }

            if (_limit.HasValue && _count == _limit.Value)
            {
                _done = true;
                return false;
            }

            while (true)
            {
                if (_currentBatch == null)
                {
                    if (await _cursor.MoveNextAsync().ConfigureAwait(false))
                    {
                        _currentBatch = _cursor.Current.GetEnumerator();
                    }
                    else
                    {
                        _done = true;
                        return false;
                    }
                }

                if (_currentBatch.MoveNext())
                {
                    _count++;
                    return true;
                }
                else
                {
                    _currentBatch = null;
                }
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
