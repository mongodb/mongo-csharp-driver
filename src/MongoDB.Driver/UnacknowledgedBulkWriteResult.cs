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
    /// Represents the result of an unacknowledged BulkWrite operation.
    /// </summary>
    internal class UnacknowledgedBulkWriteResult : BulkWriteResult
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AcknowledgedBulkWriteResult" /> class.
        /// </summary>
        /// <param name="requestCount">The request count.</param>
        /// <param name="processedRequests">The processed requests.</param>
        public UnacknowledgedBulkWriteResult(
            int requestCount,
            IEnumerable<WriteRequest> processedRequests)
            : base(requestCount, processedRequests)
        {
        }

        // public properties
        /// <summary>
        /// Gets the number of documents that were deleted.
        /// </summary>
        /// <value>
        /// The number of document that were deleted.
        /// </value>
        /// <exception cref="System.NotSupportedException">Only acknowledged writes support the DeletedCount property.</exception>
        public override long DeletedCount
        {
            get { throw new NotSupportedException("Only acknowledged writes support the DeletedCount property."); }
        }

        /// <summary>
        /// Gets the number of documents that were inserted.
        /// </summary>
        /// <value>
        /// The number of document that were inserted.
        /// </value>
        /// <exception cref="System.NotSupportedException">Only acknowledged writes support the InsertedCount property.</exception>
        public override long InsertedCount
        {
            get { throw new NotSupportedException("Only acknowledged writes support the InsertedCount property."); }
        }

        /// <summary>
        /// Gets a value indicating whether the modified count is available.
        /// </summary>
        /// <remarks>
        /// The modified count is only available when all servers have been upgraded to 2.6 or above.
        /// </remarks>
        /// <value>
        /// <c>true</c> if the modified count is available; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsModifiedCountAvailable
        {
            get { throw new NotSupportedException("Only acknowledged writes support the IsModifiedCountAvailable property."); }
        }

        /// <summary>
        /// Gets the number of documents that were matched.
        /// </summary>
        /// <value>
        /// The number of document that were matched
        /// .
        /// </value>
        /// <exception cref="System.NotSupportedException">Only acknowledged writes support the MatchedCount property.</exception>
        public override long MatchedCount
        {
            get { throw new NotSupportedException("Only acknowledged writes support the MatchedCount property."); }
        }

        /// <summary>
        /// Gets the number of documents that were actually modified during an update.
        /// </summary>
        /// <value>
        /// The number of document that were actually modified during an update.
        /// </value>
        /// <exception cref="System.NotSupportedException">Only acknowledged writes support the ModifiedCount property.</exception>
        public override long ModifiedCount
        {
            get { throw new NotSupportedException("Only acknowledged writes support the ModifiedCount property."); }
        }

        /// <summary>
        /// Gets a value indicating whether the bulk write operation was acknowledged.
        /// </summary>
        /// <value>
        /// <c>true</c> if the bulk write operation was acknowledged; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAcknowledged
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a list with information about each request that resulted in an upsert.
        /// </summary>
        /// <value>
        /// The list with information about each request that resulted in an upsert.
        /// </value>
        public override ReadOnlyCollection<BulkWriteUpsert> Upserts
        {
            get { throw new NotSupportedException("Only acknowledged writes support the Upserts property."); }
        }
    }
}
