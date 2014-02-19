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

using System.Collections.Generic;

namespace MongoDB.Driver.Support
{
    internal abstract class Batch<TItem>
    {
        // private fields
        private readonly IEnumerator<TItem> _enumerator;

        // constructors
        public Batch(IEnumerator<TItem> enumerator)
        {
            _enumerator = enumerator;
        }

        // public properties
        public IEnumerator<TItem> Enumerator
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
    }

    internal class FirstBatch<TItem> : Batch<TItem>
    {
        // constructors
        public FirstBatch(IEnumerator<TItem> enumerator)
            : base(enumerator)
        {
        }
    }

    internal class ContinuationBatch<TItem, TState> : Batch<TItem>
    {
        // private fields
        private TItem _pendingItem;
        private TState _pendingState;

        // constructors
        public ContinuationBatch(IEnumerator<TItem> enumerator, TItem pendingItem, TState pendingState)
            : base(enumerator)
        {
            _pendingItem = pendingItem;
            _pendingState = pendingState;
        }

        // public properties
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

        // public methods
        public void ClearPending()
        {
            _pendingItem = default(TItem);
            _pendingState = default(TState);
        }
    }
}
