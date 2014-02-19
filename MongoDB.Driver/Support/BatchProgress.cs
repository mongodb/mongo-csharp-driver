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
using System.Collections.ObjectModel;
using System.Linq;

namespace MongoDB.Driver.Support
{
    internal class BatchProgress<TItem>
    {
        // private fields
        private readonly int _batchCount;
        private readonly ReadOnlyCollection<TItem> _batchItems;
        private readonly int _batchLength;
        private readonly Batch<TItem> _nextBatch;

        // constructors
        public BatchProgress(int batchCount, int batchLength, IEnumerable<TItem> batchItems, Batch<TItem> nextBatch)
        {
            _batchCount = batchCount;
            _batchLength = batchLength;
            _batchItems = new ReadOnlyCollection<TItem>(batchItems.ToList());
            _nextBatch = nextBatch;
        }

        // public properties
        public int BatchCount
        {
            get { return _batchCount; }
        }

        public ReadOnlyCollection<TItem> BatchItems
        {
            get { return _batchItems; }
        }

        public int BatchLength
        {
            get { return _batchLength; }
        }

        public bool IsLast
        {
            get { return _nextBatch == null; }
        }

        public Batch<TItem> NextBatch
        {
            get { return _nextBatch; }
        }
    }
}
