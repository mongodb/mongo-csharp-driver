﻿/* Copyright 2010-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Async;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver.Core.Sync
{
    public sealed class AsyncCursorEnumeratorAdapter<TDocument> : IDisposable
    {
        // fields
        private bool _disposed;
        private readonly IAsyncCursor<TDocument> _cursor;

        // constructor
        public AsyncCursorEnumeratorAdapter(IAsyncCursor<TDocument> cursor)
        {
            _cursor = Ensure.IsNotNull(cursor, "cursor");
        }

        // methods
        public IEnumerator<TDocument> GetEnumerator()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            while (_cursor.MoveNextAsync(CancellationToken.None).GetAwaiter().GetResult())
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
