/* Copyright 2010-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Sync
{
    internal sealed class AsyncCursorEnumeratorAdapter<TDocument> : IDisposable
    {
        // fields
        private bool _disposed;
        private readonly CancellationToken _cancellationToken;
        private readonly IAsyncCursor<TDocument> _cursor;

        // constructor
        public AsyncCursorEnumeratorAdapter(IAsyncCursor<TDocument> cursor, CancellationToken cancellationToken)
        {
            _cursor = Ensure.IsNotNull(cursor, nameof(cursor));
            _cancellationToken = cancellationToken;
        }

        // methods
        public IEnumerator<TDocument> GetEnumerator()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            while (_cursor.MoveNext(_cancellationToken))
            {
                var batch = _cursor.Current;
                foreach (var document in batch)
                {
                    yield return document;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cursor.Dispose();
                _disposed = true;
            }
        }
    }
}
