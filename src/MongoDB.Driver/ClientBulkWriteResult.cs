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
using System.Runtime.CompilerServices;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents BulkWrite operation results.
    /// </summary>
    public sealed class ClientBulkWriteResult
    {
        private readonly bool _acknowledged;
        private readonly long _insertedCount;
        private readonly long _upsertedCount;
        private readonly long _matchedCount;
        private readonly long _modifiedCount;
        private readonly long _deletedCount;
        private readonly IReadOnlyDictionary<int, BulkWriteInsertOneResult> _insertResults;
        private readonly IReadOnlyDictionary<int, BulkWriteUpdateResult> _updateResults;
        private readonly IReadOnlyDictionary<int, BulkWriteDeleteResult> _deleteResults;

        /// <summary>
        /// Indicates whether this bulk write result was acknowledged.
        /// </summary>
        public bool Acknowledged
        {
            get => _acknowledged;
            init => _acknowledged = value;
        }

        /// <summary>
        /// The total number of documents inserted across all insert operations.
        /// </summary>
        public long InsertedCount
        {
            get => ThrowIfNotAcknowledged(_insertedCount);
            init => _insertedCount = value;
        }

        /// <summary>
        /// The total number of documents upserted across all update operations.
        /// </summary>
        public long UpsertedCount
        {
            get => ThrowIfNotAcknowledged(_upsertedCount);
            init => _upsertedCount = value;
        }

        /// <summary>
        /// The total number of documents matched across all update operations.
        /// </summary>
        public long MatchedCount
        {
            get => ThrowIfNotAcknowledged(_matchedCount);
            init => _matchedCount = value;
        }

        /// <summary>
        /// The total number of documents modified across all update operations.
        /// </summary>
        public long ModifiedCount
        {
            get => ThrowIfNotAcknowledged(_modifiedCount);
            init => _modifiedCount = value;
        }

        /// <summary>
        /// The total number of documents deleted across all delete operations.
        /// </summary>
        public long DeletedCount
        {
            get => ThrowIfNotAcknowledged(_deletedCount);
            init => _deletedCount = value;
        }

        /// <summary>
        /// The results of each individual insert operation that was successfully performed.
        /// </summary>
        public IReadOnlyDictionary<int, BulkWriteInsertOneResult> InsertResults
        {
            get => ThrowIfNotAcknowledged(_insertResults);
            init => _insertResults = value;
        }

        /// <summary>
        /// The results of each individual update operation that was successfully performed.
        /// </summary>
        public IReadOnlyDictionary<int, BulkWriteUpdateResult> UpdateResults
        {
            get => ThrowIfNotAcknowledged(_updateResults);
            init => _updateResults = value;
        }

        /// <summary>
        /// The results of each individual delete operation that was successfully performed.
        /// </summary>
        public IReadOnlyDictionary<int, BulkWriteDeleteResult> DeleteResults
        {
            get => ThrowIfNotAcknowledged(_deleteResults);
            init => _deleteResults = value;
        }

        private TValue ThrowIfNotAcknowledged<TValue>(TValue value, [CallerMemberName] string callerMethod = null)
            => _acknowledged ? value : throw new NotSupportedException($"Only acknowledged writes support the {callerMethod} property.");
    }
}
