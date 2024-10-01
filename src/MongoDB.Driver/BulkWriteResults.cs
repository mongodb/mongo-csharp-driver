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

using System.Collections.Generic;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents BulkWrite operation results.
    /// </summary>
    public interface IBulkWriteResults
    {
        /// <summary>
        /// The total number of documents inserted across all insert operations.
        /// </summary>
        long InsertedCount { get; }

        /// <summary>
        /// The total number of documents upserted across all update operations.
        /// </summary>
        long UpsertedCount { get; }

        /// <summary>
        /// The total number of documents matched across all update operations.
        /// </summary>
        long MatchedCount { get; }

        /// <summary>
        /// The total number of documents modified across all update operations.
        /// </summary>
        long ModifiedCount { get; }

        /// <summary>
        /// The total number of documents deleted across all delete operations.
        /// </summary>
        long DeletedCount { get; }

        /// <summary>
        /// The results of each individual insert operation that was successfully performed.
        /// </summary>
        IReadOnlyDictionary<long, BulkWriteInsertOneResult> InsertResults { get; }

        /// <summary>
        /// The results of each individual update operation that was successfully performed.
        /// </summary>
        IReadOnlyDictionary<long, BulkWriteUpdateResult> UpdateResults { get; }

        /// <summary>
        /// The results of each individual delete operation that was successfully performed.
        /// </summary>
        IReadOnlyDictionary<long, BulkWriteDeleteResult> DeleteResults { get; }
    }

    /// <summary>
    /// Represents BulkWrite operation acknowledged results.
    /// </summary>
    public sealed class AcknowledgedBulkWriteResults : IBulkWriteResults
    {
        /// <inheritdoc/>
        public long InsertedCount { get; set; }

        /// <inheritdoc/>
        public long UpsertedCount { get; set; }

        /// <inheritdoc/>
        public long MatchedCount { get; set; }

        /// <inheritdoc/>
        public long ModifiedCount { get; set; }

        /// <inheritdoc/>
        public long DeletedCount { get; set; }

        /// <inheritdoc/>
        IReadOnlyDictionary<long, BulkWriteInsertOneResult> IBulkWriteResults.InsertResults
            => InsertResults;

        /// <inheritdoc/>
        IReadOnlyDictionary<long, BulkWriteUpdateResult> IBulkWriteResults.UpdateResults
            => UpdateResults;

        /// <inheritdoc/>
        IReadOnlyDictionary<long, BulkWriteDeleteResult> IBulkWriteResults.DeleteResults
            => DeleteResults;

        internal Dictionary<long, BulkWriteInsertOneResult> InsertResults { get; set; }

        internal Dictionary<long, BulkWriteUpdateResult> UpdateResults { get; set; }

        internal Dictionary<long, BulkWriteDeleteResult> DeleteResults { get; set; }
    }

    //TODO: add UnAcknowledged
}
