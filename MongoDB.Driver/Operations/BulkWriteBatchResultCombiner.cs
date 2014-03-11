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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Driver.Operations
{
    internal class BulkWriteBatchResultCombiner
    {
        // private fields
        private readonly IList<BulkWriteBatchResult> _batchResults;
        private readonly bool _isAcknowledged;

        // constructors
        public BulkWriteBatchResultCombiner(IList<BulkWriteBatchResult> batchResults, bool isAcknowledged)
        {
            _batchResults = batchResults;
            _isAcknowledged = isAcknowledged;
        }

        // public methods
        public BulkWriteResult CreateResultOrThrowIfHasErrors(IEnumerable<WriteRequest> remainingRequests)
        {
            if (_batchResults.Any(r => r.HasWriteErrors || r.HasWriteConcernError))
            {
                throw CreateBulkWriteException(remainingRequests);
            }

            return CreateBulkWriteResult(0);
        }

        // private methods
        private int CombineBatchCount()
        {
            return _batchResults.Sum(r => r.BatchCount);
        }

        private long CombineDeletedCount()
        {
            return _batchResults.Sum(r => r.DeletedCount);
        }

        private long CombineInsertedCount()
        {
            return _batchResults.Sum(r => r.InsertedCount);
        }

        private long CombineMatchedCount()
        {
            return _batchResults.Sum(r => r.MatchedCount);
        }

        private long? CombineModifiedCount()
        {
            if (_batchResults.All(r => r.ModifiedCount.HasValue))
            {
                return _batchResults.Sum(r => r.ModifiedCount.Value);
            }
            else
            {
                return null;
            }
        }

        private IEnumerable<WriteRequest> CombineProcessedRequests()
        {
            return _batchResults.SelectMany(r => r.ProcessedRequests);
        }

        private IEnumerable<WriteRequest> CombineUnprocessedRequests()
        {
            return _batchResults.SelectMany(r => r.UnprocessedRequests);
        }

        private IEnumerable<BulkWriteUpsert> CombineUpserts()
        {
            return _batchResults.SelectMany(r => r.Upserts.Select(u => u.WithMappedIndex(r.IndexMap))).OrderBy(u => u.Index);
        }

        private WriteConcernError CombineWriteConcernErrors()
        {
            return _batchResults.Select(r => r.WriteConcernError).LastOrDefault(e => e != null);
        }

        private IEnumerable<BulkWriteError> CombineWriteErrors()
        {
            return _batchResults.SelectMany(r => r.WriteErrors.Select(e => e.WithMappedIndex(r.IndexMap))).OrderBy(e => e.Index);
        }

        private BulkWriteException CreateBulkWriteException(IEnumerable<WriteRequest> remainingRequests)
        {
            var remainingRequestsList = remainingRequests.ToList();
            var result = CreateBulkWriteResult(remainingRequestsList.Count);
            var writeErrors = CombineWriteErrors();
            var writeConcernError = CombineWriteConcernErrors();
            var unprocessedRequests = CombineUnprocessedRequests().Concat(remainingRequestsList);

            return new BulkWriteException(result, writeErrors, writeConcernError, unprocessedRequests);
        }

        private BulkWriteResult CreateBulkWriteResult(int remainingRequestsCount)
        {
            var requestCount = CombineBatchCount() + remainingRequestsCount;
            var processedRequests = CombineProcessedRequests();

            if (!_isAcknowledged)
            {
                return new UnacknowledgedBulkWriteResult(
                    requestCount,
                    processedRequests);
            }

            var matchedCount = CombineMatchedCount();
            var deletedCount = CombineDeletedCount();
            var insertedCount = CombineInsertedCount();
            var modifiedCount = CombineModifiedCount();
            var upserts = CombineUpserts();

            return new AcknowledgedBulkWriteResult(
                requestCount,
                matchedCount,
                deletedCount,
                insertedCount,
                modifiedCount,
                processedRequests,
                upserts);
        }
    }
}
