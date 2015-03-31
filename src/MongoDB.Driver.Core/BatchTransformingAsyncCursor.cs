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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    internal sealed class BatchTransformingAsyncCursor<TFromDocument, TToDocument> : IAsyncCursor<TToDocument>
    {
        private bool _disposed;
        private readonly Func<IEnumerable<TFromDocument>, IEnumerable<TToDocument>> _transformer;
        private readonly IAsyncCursor<TFromDocument> _wrapped;
        private List<TToDocument> _current;

        public BatchTransformingAsyncCursor(IAsyncCursor<TFromDocument> wrapped, Func<IEnumerable<TFromDocument>, IEnumerable<TToDocument>> transformer)
        {
            _wrapped = Ensure.IsNotNull(wrapped, "wrapped");
            _transformer = Ensure.IsNotNull(transformer, "transformer");
        }

        public IEnumerable<TToDocument> Current
        {
            get
            {
                ThrowIfDisposed();
                if (_current == null)
                {
                    throw new InvalidOperationException("Must call MoveNextAsync first.");
                }

                return _current;
            }
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            while (await _wrapped.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            {
                _current = _transformer(_wrapped.Current).ToList();
                if (_current.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            _disposed = true;
            _current = null;
            _wrapped.Dispose();
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
