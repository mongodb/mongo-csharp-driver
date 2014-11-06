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
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    public sealed class DeferredAsyncCursor<TDocument> : IAsyncCursor<TDocument>
    {
        // fields
        private readonly Func<Task<IAsyncCursor<TDocument>>> _executeAsync;
        private IAsyncCursor<TDocument> _cursor;
        private bool _disposed;

        // constructors
        public DeferredAsyncCursor(Func<Task<IAsyncCursor<TDocument>>> executeAsync)
        {
            _executeAsync = Ensure.IsNotNull(executeAsync, "executeAsync");
        }

        // properties
        public IEnumerable<TDocument> Current
        {
            get
            {
                ThrowIfDisposed();
                if (_cursor == null)
                {
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNextAsync.");
                }

                return _cursor.Current;
            }
        }

        // methods
        public async Task<bool> MoveNextAsync()
        {
            ThrowIfDisposed();

            if (_cursor == null)
            {
                _cursor = await _executeAsync().ConfigureAwait(false);
            }

            return await _cursor.MoveNextAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_cursor != null)
            {
                _cursor.Dispose();
                _cursor = null;
                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if(_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
