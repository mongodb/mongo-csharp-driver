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
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Misc
{
    public abstract class Batch<TItem>
    {
        // fields
        private readonly IEnumerator<TItem> _enumerator;
        private BatchResult<TItem> _result;

        // constructors
        protected Batch(IEnumerator<TItem> enumerator)
        {
            _enumerator = enumerator;
        }

        // properties
        public abstract bool CanBeSplit { get; }

        internal IEnumerator<TItem> Enumerator
        {
            get { return _enumerator; }
        }

        public virtual IEnumerable<TItem> RemainingItems
        {
            get
            {
                while (_enumerator.MoveNext())
                {
                    yield return _enumerator.Current;
                }
            }
        }

        public BatchResult<TItem> Result
        {
            get { return _result; }
        }

        // methods
        internal void SetResult(BatchResult<TItem> result)
        {
            _result = result;
        }
    }

    public class ContinuationBatch<TItem, TState> : Batch<TItem>
    {
        // fields
        private TItem _pendingItem;
        private TState _pendingState;

        // constructors
        public ContinuationBatch(IEnumerator<TItem> enumerator, TItem pendingItem, TState pendingState)
            : base(enumerator)
        {
            _pendingItem = pendingItem;
            _pendingState = pendingState;
        }

        // properties
        public override bool CanBeSplit
        {
            get { return true; }
        }

        public TItem PendingItem
        {
            get { return _pendingItem; }
        }

        public TState PendingState
        {
            get { return _pendingState; }
        }

        public override IEnumerable<TItem> RemainingItems
        {
            get
            {
                if (_pendingItem != null)
                {
                    yield return _pendingItem;
                }
                foreach (var item in base.RemainingItems)
                {
                    yield return item;
                }
            }
        }

        // methods
        public void ClearPending()
        {
            _pendingItem = default(TItem);
            _pendingState = default(TState);
        }
    }

    public class FirstBatch<TItem> : Batch<TItem>
    {
        // fields
        private readonly bool _canBeSplit;

        // constructors
        public FirstBatch(IEnumerator<TItem> enumerator, bool canBeSplit = true)
            : base(enumerator)
        {
            _canBeSplit = canBeSplit;
        }

        // properties
        public override bool CanBeSplit
        {
            get { return _canBeSplit; }
        }
    }
}
