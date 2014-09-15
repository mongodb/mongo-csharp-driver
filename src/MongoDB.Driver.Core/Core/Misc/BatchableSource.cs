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

namespace MongoDB.Driver.Core.Misc
{
    public sealed class BatchableSource<T>
    {
        #region static
        // static fields
        private static readonly T[] __emptyBatch = new T[0];
        #endregion

        // fields
        private IReadOnlyList<T> _batch;
        private IEnumerator<T> _enumerator;
        private bool _hasMore;
        private Overflow _overflow;

        // constructors
        public BatchableSource(IEnumerable<T> batch)
        {
            _batch = Ensure.IsNotNull(batch, "batch").ToList();
        }

        public BatchableSource(IEnumerator<T> enumerator)
        {
            _enumerator = Ensure.IsNotNull(enumerator, "enumerator");
            _hasMore = true;
        }

        // properties
        public IReadOnlyList<T> Batch
        {
            get { return _batch; }
        }

        public T Current
        {
            get
            {
                ThrowIfNotBatchable();
                ThrowIfHasBatch();
                return _enumerator.Current;
            }
        }

        public bool HasMore
        {
            get
            {
                return _hasMore;
            }
        }

        // methods
        public void ClearBatch()
        {
            ThrowIfNotBatchable();
            _batch = null;
        }

        public void EndBatch(IReadOnlyList<T> batch)
        {
            ThrowIfNotBatchable();
            ThrowIfHasBatch();
            _batch = batch;
            _hasMore = false;
        }

        public IEnumerable<T> GetRemainingItems()
        {
            if (_overflow != null)
            {
                yield return _overflow.Item;
                _overflow = null;
            }

            if (_enumerator != null)
            {
                while (_enumerator.MoveNext())
                {
                    yield return _enumerator.Current;
                }
            }
            else
            {
                foreach (var item in _batch)
                {
                    yield return item;
                }
                _batch = __emptyBatch;
            }

            _hasMore = false;
        }

        public void EndBatch(IReadOnlyList<T> batch, Overflow overflow)
        {
            ThrowIfNotBatchable();
            ThrowIfHasBatch();
            _batch = batch;
            _overflow = overflow;
            _hasMore = true;
        }

        public bool MoveNext()
        {
            ThrowIfNotBatchable();
            ThrowIfHasBatch();
            return _enumerator.MoveNext();
        }

        public Overflow StartBatch()
        {
            ThrowIfNotBatchable();
            ThrowIfHasBatch();
            var overflow = _overflow;
            _overflow = null;
            return overflow;
        }

        private void ThrowIfHasBatch()
        {
            if (_batch != null)
            {
                throw new InvalidOperationException("This method can only be called when there is no current batch.");
            }
        }

        private void ThrowIfNotBatchable()
        {
            if (_enumerator == null)
            {
                throw new InvalidOperationException("This method can only be called when an enumerator was provided.");
            }
        }

        // nested types
        public class Overflow
        {
            public T Item;
            public object State;
        }
    }
}
