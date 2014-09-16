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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    internal sealed class AsyncCursorAsyncEnumerable<TDocument> : IAsyncEnumerable<TDocument>
    {
        // fields
        private readonly Func<Task<IAsyncCursor<TDocument>>> _executorAsync;
        private readonly long? _limit;

        // constructors
        public AsyncCursorAsyncEnumerable(Func<Task<IAsyncCursor<TDocument>>> executorAsync, long? limit)
        {
            _executorAsync = Ensure.IsNotNull(executorAsync, "executor");
            _limit = limit;
        }

        // methods
        public IAsyncEnumerator<TDocument> GetAsyncEnumerator()
        {
            return new Enumerator(_executorAsync, _limit);
        }

        // nested classes
        private class Enumerator : IAsyncEnumerator<TDocument>
        {
            private bool _disposed;
            private IAsyncEnumerator<TDocument> _enumerator;
            private readonly Func<Task<IAsyncCursor<TDocument>>> _executorAsync;
            private readonly long? _limit;

            public Enumerator(Func<Task<IAsyncCursor<TDocument>>> executor, long? limit)
            {
                _executorAsync = executor;
                _limit = limit;
            }

            public TDocument Current
            {
                get
                {
                    ThrowIfDisposed();
                    if (_enumerator == null)
                    {
                        throw new InvalidOperationException("Enumeration has not started. Call MoveNextAsync.");
                    }

                    return _enumerator.Current;
                }
            }

            public async Task<bool> MoveNextAsync(TimeSpan? timeout, CancellationToken cancellationToken)
            {
                ThrowIfDisposed();
                if (_enumerator == null)
                {
                    var cursor = await _executorAsync();
                    _enumerator = new AsyncCursorEnumerator<TDocument>(cursor, _limit);
                }

                return await _enumerator.MoveNextAsync();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _enumerator.Dispose();
                    _disposed = true;
                }
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(GetType().FullName);
                }
            }
        }

    }
}
