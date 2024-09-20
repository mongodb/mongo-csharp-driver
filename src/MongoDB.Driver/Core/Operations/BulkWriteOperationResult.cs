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

namespace MongoDB.Driver.Core.Operations
{
    internal abstract class BulkWriteOperationResult
    {
        private readonly IReadOnlyList<WriteRequest> _processedRequests;
        private readonly int _requestCount;

        protected BulkWriteOperationResult(
            int requestCount,
            IReadOnlyList<WriteRequest> processedRequests)
        {
            _requestCount = requestCount;
            _processedRequests = processedRequests;
        }

        public abstract long DeletedCount { get; }
        public abstract long InsertedCount { get; }
        public abstract bool IsAcknowledged { get; }
        public abstract bool IsModifiedCountAvailable { get; }
        public abstract long MatchedCount { get; }
        public abstract long ModifiedCount { get; }

        public IReadOnlyList<WriteRequest> ProcessedRequests
        {
            get { return _processedRequests; }
        }

        public int RequestCount
        {
            get { return _requestCount; }
        }

        public abstract IReadOnlyList<BulkWriteOperationUpsert> Upserts { get; }

        [Serializable]
        public class Acknowledged : BulkWriteOperationResult
        {
            private readonly long _deletedCount;
            private readonly long _insertedCount;
            private readonly long _matchedCount;
            private readonly long? _modifiedCount;
            private readonly IReadOnlyList<BulkWriteOperationUpsert> _upserts;

            public Acknowledged(
                int requestCount,
                long matchedCount,
                long deletedCount,
                long insertedCount,
                long? modifiedCount,
                IReadOnlyList<WriteRequest> processedRequests,
                IReadOnlyList<BulkWriteOperationUpsert> upserts)
                : base(requestCount, processedRequests)
            {
                _matchedCount = matchedCount;
                _deletedCount = deletedCount;
                _insertedCount = insertedCount;
                _modifiedCount = modifiedCount;
                _upserts = upserts;
            }

            public override long DeletedCount
            {
                get { return _deletedCount; }
            }

            public override long InsertedCount
            {
                get { return _insertedCount; }
            }

            public override bool IsModifiedCountAvailable
            {
                get { return _modifiedCount.HasValue; }
            }

            public override long MatchedCount
            {
                get { return _matchedCount; }
            }

            public override long ModifiedCount
            {
                get
                {
                    if (!_modifiedCount.HasValue)
                    {
                        throw new NotSupportedException("ModifiedCount is not available.");
                    }
                    return _modifiedCount.Value;
                }
            }

            public override bool IsAcknowledged
            {
                get { return true; }
            }

            public override IReadOnlyList<BulkWriteOperationUpsert> Upserts
            {
                get { return _upserts; }
            }
        }

        [Serializable]
        public class Unacknowledged : BulkWriteOperationResult
        {
            public Unacknowledged(
                int requestCount,
                IReadOnlyList<WriteRequest> processedRequests)
                : base(requestCount, processedRequests)
            {
            }

            public override long DeletedCount
            {
                get { throw new NotSupportedException("Only acknowledged writes support the DeletedCount property."); }
            }

            public override long InsertedCount
            {
                get { throw new NotSupportedException("Only acknowledged writes support the InsertedCount property."); }
            }

            public override bool IsModifiedCountAvailable
            {
                get { throw new NotSupportedException("Only acknowledged writes support the IsModifiedCountAvailable property."); }
            }

            public override long MatchedCount
            {
                get { throw new NotSupportedException("Only acknowledged writes support the MatchedCount property."); }
            }

            public override long ModifiedCount
            {
                get { throw new NotSupportedException("Only acknowledged writes support the ModifiedCount property."); }
            }

            public override bool IsAcknowledged
            {
                get { return false; }
            }

            public override IReadOnlyList<BulkWriteOperationUpsert> Upserts
            {
                get { throw new NotSupportedException("Only acknowledged writes support the Upserts property."); }
            }
        }
    }
}
