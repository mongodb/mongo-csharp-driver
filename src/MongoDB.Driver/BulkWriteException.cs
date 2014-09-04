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
using MongoDB.Driver.Core.Operations;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a bulk write exception.
    /// </summary>
    [Serializable]
    public class BulkWriteException : MongoException
    {
        // private fields
        private WriteConcernError _writeConcernError;
        private IReadOnlyList<BulkWriteError> _writeErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteException"/> class.
        /// </summary>
        /// <param name="writeErrors">The write errors.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        public BulkWriteException(
            IEnumerable<BulkWriteError> writeErrors,
            WriteConcernError writeConcernError)
            : base("A bulk write operation resulted in one or more errors.")
        {
            _writeErrors = writeErrors.ToList();
            _writeConcernError = writeConcernError;
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public BulkWriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // properties
        /// <summary>
        /// Gets the write concern error.
        /// </summary>
        /// <value>
        /// The write concern error.
        /// </value>
        public WriteConcernError WriteConcernError
        {
            get { return _writeConcernError; }
        }

        /// <summary>
        /// Gets the write errors.
        /// </summary>
        /// <value>
        /// The write errors.
        /// </value>
        public IReadOnlyList<BulkWriteError> WriteErrors
        {
            get { return _writeErrors; }
        }
    }

    /// <summary>
    /// Represents a bulk write exception.
    /// </summary>
    [Serializable]
    public class BulkWriteException<T> : BulkWriteException
    {
        // private fields
        private BulkWriteResult<T> _result;
        private IReadOnlyList<WriteModel<T>> _unprocessedRequests;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteException" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="writeErrors">The write errors.</param>
        /// <param name="unprocessedRequests">The unprocessed requests.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        public BulkWriteException(
            BulkWriteResult<T> result, 
            IEnumerable<BulkWriteError> writeErrors,
            WriteConcernError writeConcernError,
            IEnumerable<WriteModel<T>> unprocessedRequests)
            : base(writeErrors, writeConcernError)
        {
            _result = result;

            _unprocessedRequests = unprocessedRequests.ToList();
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public BulkWriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // public properties
        /// <summary>
        /// Gets the result of the bulk write operation.
        /// </summary>
        public BulkWriteResult<T> Result
        {
            get { return _result; }
        }

        /// <summary>
        /// Gets the unprocessed requests.
        /// </summary>
        /// <value>
        /// The unprocessed requests.
        /// </value>
        /// <exception cref="System.NotImplementedException"></exception>
        public IReadOnlyList<WriteModel<T>> UnprocessedRequests
        {
            get { return _unprocessedRequests; }
        }

        // internal static methods
        internal static BulkWriteException<T> FromCore(BulkWriteOperationException ex)
        {
            return new BulkWriteException<T>(
                BulkWriteResult<T>.FromCore(ex.Result),
                ex.WriteErrors.Select(e => BulkWriteError.FromCore(e)),
                WriteConcernError.FromCore(ex.WriteConcernError),
                ex.UnprocessedRequests.Select(r => WriteModel<T>.FromCore(r)));
        }

        internal static BulkWriteException<T> FromCore(BulkWriteOperationException ex, IReadOnlyList<WriteModel<T>> requests)
        {
            var processedRequests = ex.Result.ProcessedRequests
                .Select(r => new { CorrelationId = r.CorrelationId.Value, Request = requests[r.CorrelationId.Value] })
                .OrderBy(x => x.CorrelationId)
                .Select(x => x.Request);

            var unprocessedRequests = ex.UnprocessedRequests
                .Select(r => new { CorrelationId = r.CorrelationId.Value, Request = requests[r.CorrelationId.Value] })
                .OrderBy(x => x.CorrelationId)
                .Select(x => x.Request);

            return new BulkWriteException<T>(
                BulkWriteResult<T>.FromCore(ex.Result, processedRequests),
                ex.WriteErrors.Select(e => BulkWriteError.FromCore(e)),
                WriteConcernError.FromCore(ex.WriteConcernError),
                unprocessedRequests);
        }
    }
}
