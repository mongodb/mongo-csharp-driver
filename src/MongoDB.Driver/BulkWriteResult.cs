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
using System.Collections.ObjectModel;
using System.Linq;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a bulk write operation.
    /// </summary>
    public abstract class BulkWriteResult
    {
        // private fields
        private readonly ReadOnlyCollection<WriteRequest> _processedRequests;
        private readonly int _requestCount;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteResult" /> class.
        /// </summary>
        /// <param name="requestCount">The request count.</param>
        /// <param name="processedRequests">The processed requests.</param>
        protected BulkWriteResult(
            int requestCount,
            IEnumerable<WriteRequest> processedRequests)
        {
            _requestCount = requestCount;
            _processedRequests = new ReadOnlyCollection<WriteRequest>(processedRequests.ToList());
        }

        // public properties
        /// <summary>
        /// Gets the number of documents that were deleted.
        /// </summary>
        /// <value>
        /// The number of document that were deleted.
        /// </value>
        public abstract long DeletedCount { get; }

        /// <summary>
        /// Gets the number of documents that were inserted.
        /// </summary>
        /// <value>
        /// The number of document that were inserted.
        /// </value>
        public abstract long InsertedCount { get; }

        /// <summary>
        /// Gets a value indicating whether the bulk write operation was acknowledged.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the bulk write operation was acknowledged; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsAcknowledged { get; }

        /// <summary>
        /// Gets a value indicating whether the modified count is available.
        /// </summary>
        /// <remarks>
        /// The modified count is only available when all servers have been upgraded to 2.6 or above.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the modified count is available; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsModifiedCountAvailable { get;  }

        /// <summary>
        /// Gets the number of documents that were matched.
        /// </summary>
        /// <value>
        /// The number of document that were matched.
        /// </value>
        public abstract long MatchedCount { get; }

        /// <summary>
        /// Gets the number of documents that were actually modified during an update.
        /// </summary>
        /// <value>
        /// The number of document that were actually modified during an update.
        /// </value>
        public abstract long ModifiedCount { get; }

        /// <summary>
        /// Gets the processed requests.
        /// </summary>
        /// <value>
        /// The processed requests.
        /// </value>
        public ReadOnlyCollection<WriteRequest> ProcessedRequests
        {
            get { return _processedRequests; }
        }

        /// <summary>
        /// Gets the request count.
        /// </summary>
        /// <value>
        /// The request count.
        /// </value>
        public int RequestCount
        {
            get { return _requestCount; }
        }

        /// <summary>
        /// Gets a list with information about each request that resulted in an upsert.
        /// </summary>
        /// <value>
        /// The list with information about each request that resulted in an upsert.
        /// </value>
        public abstract ReadOnlyCollection<BulkWriteUpsert> Upserts { get; }
    }
}
