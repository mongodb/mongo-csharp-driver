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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Operations
{
#pragma warning disable CA1001
    internal class AsyncCursorSourceEnumerator<TDocument> : IAsyncEnumerator<TDocument>
#pragma warning restore CA1001
    {
        private readonly CancellationToken _cancellationToken;
        private AsyncCursorEnumerator<TDocument> _cursorEnumerator;
        private readonly IAsyncCursorSource<TDocument> _cursorSource;
        private bool _disposed;

        public AsyncCursorSourceEnumerator(IAsyncCursorSource<TDocument> cursorSource, CancellationToken cancellationToken)
        {
            _cursorSource = Ensure.IsNotNull(cursorSource, nameof(cursorSource));
            _cancellationToken = cancellationToken;
        }

        public TDocument Current
        {
            get
            {
                if (_cursorEnumerator == null)
                {
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNextAsync.");
                }
                return _cursorEnumerator.Current;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (_cursorEnumerator != null)
                {
                    await _cursorEnumerator.DisposeAsync().ConfigureAwait(false);
                }

                GC.SuppressFinalize(this);
            }
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            ThrowIfDisposed();

            if (_cursorEnumerator == null)
            {
                var cursor = await _cursorSource.ToCursorAsync(_cancellationToken).ConfigureAwait(false);
                _cursorEnumerator = new AsyncCursorEnumerator<TDocument>(cursor, _cancellationToken);
            }

            return await _cursorEnumerator.MoveNextAsync().ConfigureAwait(false);
        }

        public void Reset()
        {
            ThrowIfDisposed();
            throw new NotSupportedException();
        }

        // private methods
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}