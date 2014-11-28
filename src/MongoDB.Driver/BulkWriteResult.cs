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
using System.Runtime.Serialization;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents the result of a bulk write operation.
    /// </summary>
    [Serializable]
    public abstract class BulkWriteResult
    {
        // fields
        private readonly int _requestCount;

        //constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteResult"/> class.
        /// </summary>
        /// <param name="requestCount">The request count.</param>
        protected BulkWriteResult(int requestCount)
        {
            _requestCount = requestCount;
        }

        // properties
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
        public abstract bool IsModifiedCountAvailable { get; }

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
        /// 
        public abstract long ModifiedCount { get; }

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
        public abstract IReadOnlyList<BulkWriteUpsert> Upserts { get; }
    }

    /// <summary>
    /// Represents the result of a bulk write operation.
    /// </summary>
    [Serializable]
    public abstract class BulkWriteResult<T> : BulkWriteResult
    {
        // private fields
        private readonly IReadOnlyList<WriteModel<T>> _processedRequests;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteResult" /> class.
        /// </summary>
        /// <param name="requestCount">The request count.</param>
        /// <param name="processedRequests">The processed requests.</param>
        protected BulkWriteResult(
            int requestCount,
            IEnumerable<WriteModel<T>> processedRequests)
            : base(requestCount)
        {
            _processedRequests = processedRequests.ToList();
        }

        // public properties
        /// <summary>
        /// Gets the processed requests.
        /// </summary>
        /// <value>
        /// The processed requests.
        /// </value>
        public IReadOnlyList<WriteModel<T>> ProcessedRequests
        {
            get { return _processedRequests; }
        }

        // internal static methods
        internal static BulkWriteResult<T> FromCore(Core.Operations.BulkWriteOperationResult result)
        {
            if (result.IsAcknowledged)
            {
                return new Acknowledged(
                    result.RequestCount,
                    result.MatchedCount,
                    result.DeletedCount,
                    result.InsertedCount,
                    result.IsModifiedCountAvailable ? (long?)result.ModifiedCount : null,
                    result.ProcessedRequests.Select(r => WriteModel<T>.FromCore(r)),
                    result.Upserts.Select(u => BulkWriteUpsert.FromCore(u)));
            }

            return new Unacknowledged(
                result.RequestCount,
                result.ProcessedRequests.Select(r => WriteModel<T>.FromCore(r)));
        }

        internal static BulkWriteResult<T> FromCore(Core.Operations.BulkWriteOperationResult result, IEnumerable<WriteModel<T>> requests)
        {
            if (result.IsAcknowledged)
            {
                return new Acknowledged(
                    result.RequestCount,
                    result.MatchedCount,
                    result.DeletedCount,
                    result.InsertedCount,
                    result.IsModifiedCountAvailable ? (long?)result.ModifiedCount : null,
                    requests,
                    result.Upserts.Select(u => BulkWriteUpsert.FromCore(u)));
            }

            return new Unacknowledged(
                result.RequestCount,
                requests);
        }

        // nested classes
        /// <summary>
        /// Result from an acknowledged write concern.
        /// </summary>
        [Serializable]
        public class Acknowledged : BulkWriteResult<T>
        {
            // private fields
            private readonly long _deletedCount;
            private readonly long _insertedCount;
            private readonly long _matchedCount;
            private readonly long? _modifiedCount;
            private readonly IReadOnlyList<BulkWriteUpsert> _upserts;

            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Acknowledged" /> class.
            /// </summary>
            /// <param name="requestCount">The request count.</param>
            /// <param name="matchedCount">The matched count.</param>
            /// <param name="deletedCount">The deleted count.</param>
            /// <param name="insertedCount">The inserted count.</param>
            /// <param name="modifiedCount">The modified count.</param>
            /// <param name="processedRequests">The processed requests.</param>
            /// <param name="upserts">The upserts.</param>
            public Acknowledged(
                int requestCount,
                long matchedCount,
                long deletedCount,
                long insertedCount,
                long? modifiedCount,
                IEnumerable<WriteModel<T>> processedRequests,
                IEnumerable<BulkWriteUpsert> upserts)
                : base(requestCount, processedRequests)
            {
                _matchedCount = matchedCount;
                _deletedCount = deletedCount;
                _insertedCount = insertedCount;
                _modifiedCount = modifiedCount;
                _upserts = upserts.ToList();
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
            /// Gets a value indicating whether the modified count is available.
            /// </summary>
            /// <value>
            /// <c>true</c> if the modified count is available; otherwise, <c>false</c>.
            /// </value>
            /// <remarks>
            /// The modified count is only available when all servers have been upgraded to 2.6 or above.
            /// </remarks>
            public override bool IsModifiedCountAvailable
            {
                get { return _modifiedCount.HasValue; }
            }

            /// <summary>
            /// Gets the number of documents that were matched.
            /// </summary>
            /// <value>
            /// The number of document that were matched.
            /// </value>
            public override long MatchedCount
            {
                get { return _matchedCount; }
            }

            /// <summary>
            /// Gets the number of documents that were actually modified during an update.
            /// </summary>
            /// <value>
            /// The number of document that were actually modified during an update.
            /// </value>
            /// <exception cref="System.NotSupportedException">ModifiedCount is not available.</exception>
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

            /// <summary>
            /// Gets a list with information about each request that resulted in an upsert.
            /// </summary>
            /// <value>
            /// The list with information about each request that resulted in an upsert.
            /// </value>
            public override IReadOnlyList<BulkWriteUpsert> Upserts
            {
                get { return _upserts; }
            }
        }

        /// <summary>
        /// Result from an unacknowledged write concern.
        /// </summary>
        [Serializable]
        public class Unacknowledged : BulkWriteResult<T>
        {
            // constructors
            /// <summary>
            /// Initializes a new instance of the <see cref="Unacknowledged"/> class.
            /// </summary>
            /// <param name="requestCount">The request count.</param>
            /// <param name="processedRequests">The processed requests.</param>
            public Unacknowledged(
                int requestCount,
                IEnumerable<WriteModel<T>> processedRequests)
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
            /// Gets a value indicating whether the modified count is available.
            /// </summary>
            /// <value>
            /// <c>true</c> if the modified count is available; otherwise, <c>false</c>.
            /// </value>
            /// <exception cref="System.NotSupportedException">Only acknowledged writes support the IsModifiedCountAvailable property.</exception>
            /// <remarks>
            /// The modified count is only available when all servers have been upgraded to 2.6 or above.
            /// </remarks>
            public override bool IsModifiedCountAvailable
            {
                get { throw new NotSupportedException("Only acknowledged writes support the IsModifiedCountAvailable property."); }
            }

            /// <summary>
            /// Gets the matched count.
            /// </summary>
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
            /// Gets a list with information about each request that resulted in an upsert.
            /// </summary>
            /// <value>
            /// The list with information about each request that resulted in an upsert.
            /// </value>
            /// <exception cref="System.NotSupportedException">Only acknowledged writes support the Upserts property.</exception>
            public override IReadOnlyList<BulkWriteUpsert> Upserts
            {
                get { throw new NotSupportedException("Only acknowledged writes support the Upserts property."); }
            }
        }
    }
}
