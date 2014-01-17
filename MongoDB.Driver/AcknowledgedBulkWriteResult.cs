/* Copyright 2010-2013 10gen Inc.
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
    /// Represents the result of an acknowledged bulk write operation.
    /// </summary>
    internal class AcknowledgedBulkWriteResult : BulkWriteResult
    {
        // private fields
        private readonly long _deletedCount;
        private readonly long _insertedCount;
        private readonly long _modifiedCount;
        private readonly long _updatedCount;
        private readonly ReadOnlyCollection<BulkWriteUpsert> _upserts;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="AcknowledgedBulkWriteResult" /> class.
        /// </summary>
        /// <param name="requestCount">The request count.</param>
        /// <param name="deletedCount">The deleted count.</param>
        /// <param name="insertedCount">The inserted count.</param>
        /// <param name="modifiedCount">The modified count.</param>
        /// <param name="updatedCount">The updated count.</param>
        /// <param name="processedRequests">The processed requests.</param>
        /// <param name="upserts">The upserts.</param>
        public AcknowledgedBulkWriteResult(
            int requestCount,
            long deletedCount,
            long insertedCount,
            long modifiedCount,
            long updatedCount,
            IEnumerable<WriteRequest> processedRequests,
            IEnumerable<BulkWriteUpsert> upserts)
            : base(requestCount, processedRequests)
        {
            _deletedCount = deletedCount;
            _insertedCount = insertedCount;
            _modifiedCount = modifiedCount;
            _updatedCount = updatedCount;
            _upserts = new ReadOnlyCollection<BulkWriteUpsert>(upserts.ToList());
        }

        // public properties
        /// <summary>
        /// Gets the number of documents that were deleted.
        /// </summary>
        /// <value>
        /// The number of document that were deleted.
        /// </value>
        public override long DeletedCount
        {
            get { return _deletedCount; }
        }

        /// <summary>
        /// Gets the number of documents that were inserted.
        /// </summary>
        /// <value>
        /// The number of document that were inserted.
        /// </value>
        public override long InsertedCount
        {
            get { return _insertedCount; }
        }

        /// <summary>
        /// Gets the number of documents that were actually modified during an update.
        /// When connected to server versions before 2.6 ModifiedCount will equal UpdatedCount.
        /// </summary>
        /// <value>
        /// The number of document that were actually modified during an update.
        /// </value>
        public override long ModifiedCount
        {
            get { return _modifiedCount; }
        }

        /// <summary>
        /// Gets a value indicating whether the bulk write operation was acknowledged.
        /// </summary>
        /// <value>
        /// <c>true</c> if the bulk write operation was acknowledged; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAcknowledged
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the number of documents that were updated.
        /// </summary>
        /// <value>
        /// The number of document that were updated.
        /// </value>
        public override long UpdatedCount
        {
            get { return _updatedCount; }
        }

        /// <summary>
        /// Gets a list with information about each request that resulted in an upsert.
        /// </summary>
        /// <value>
        /// The list with information about each request that resulted in an upsert.
        /// </value>
        public override ReadOnlyCollection<BulkWriteUpsert> Upserts
        {
            get { return _upserts; }
        }
    }
}
