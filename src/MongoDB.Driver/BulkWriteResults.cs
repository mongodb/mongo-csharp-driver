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
    public abstract class BulkWriteResults
    {
        /// <summary>
        /// The total number of documents inserted across all insert operations.
        /// </summary>
        public virtual long InsertedCount { get; init; }

        /// <summary>
        /// The total number of documents upserted across all update operations.
        /// </summary>
        public virtual long UpsertedCount { get; init; }

        /// <summary>
        /// The total number of documents matched across all update operations.
        /// </summary>
        public virtual long MatchedCount { get; init; }

        /// <summary>
        /// The total number of documents modified across all update operations.
        /// </summary>
        public virtual long ModifiedCount { get; init; }

        /// <summary>
        /// The total number of documents deleted across all delete operations.
        /// </summary>
        public virtual long DeletedCount { get; init; }

        /// <summary>
        /// The results of each individual insert operation that was successfully performed.
        /// </summary>
        public virtual IReadOnlyDictionary<int, BulkWriteInsertOneResult> InsertResults { get; init; }

        /// <summary>
        /// The results of each individual update operation that was successfully performed.
        /// </summary>
        public virtual IReadOnlyDictionary<int, BulkWriteUpdateResult> UpdateResults { get; init; }

        /// <summary>
        /// The results of each individual delete operation that was successfully performed.
        /// </summary>
        public virtual IReadOnlyDictionary<int, BulkWriteDeleteResult> DeleteResults { get; init; }

        /// <summary>
        /// Represents BulkWrite operation acknowledged results.
        /// </summary>
        public sealed class Acknowledged : BulkWriteResults
        {}

        /// <summary>
        /// Represents BulkWrite operation unacknowledged results.
        /// </summary>
        public sealed class Unacknowledged : BulkWriteResults
        {
            /// <inheritdoc/>
            public override long InsertedCount
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override long UpsertedCount
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override long MatchedCount
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override long ModifiedCount
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override long DeletedCount
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override IReadOnlyDictionary<int, BulkWriteInsertOneResult> InsertResults
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override IReadOnlyDictionary<int, BulkWriteUpdateResult> UpdateResults
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            /// <inheritdoc/>
            public override IReadOnlyDictionary<int, BulkWriteDeleteResult> DeleteResults
            {
                get { throw CreateNotSupportedException(); }
                init { throw CreateNotSupportedException(); }
            }

            private NotSupportedException CreateNotSupportedException([CallerMemberName] string callerMethod = null)
                => throw new NotSupportedException($"Only acknowledged writes support the {callerMethod} property.");
        }
    }
}
