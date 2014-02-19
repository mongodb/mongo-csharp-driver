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

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a bulk write exception.
    /// </summary>
    [Serializable]
    public class BulkWriteException : MongoException
    {
        // private fields
        private BulkWriteResult _result;
        private ReadOnlyCollection<WriteRequest> _unprocessedRequests;
        private WriteConcernError _writeConcernError;
        private ReadOnlyCollection<BulkWriteError> _writeErrors;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteException" /> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="writeErrors">The write errors.</param>
        /// <param name="unprocessedRequests">The unprocessed requests.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        public BulkWriteException(
            BulkWriteResult result, 
            IEnumerable<BulkWriteError> writeErrors,
            WriteConcernError writeConcernError,
            IEnumerable<WriteRequest> unprocessedRequests)
            : base("A bulk write operation resulted in one or more errors.")
        {
            _result = result;
            _writeErrors = new ReadOnlyCollection<BulkWriteError>(writeErrors.ToList());
            _writeConcernError = writeConcernError;
            _unprocessedRequests = new ReadOnlyCollection<WriteRequest>(unprocessedRequests.ToList());
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
        public BulkWriteResult Result
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
        public ReadOnlyCollection<WriteRequest> UnprocessedRequests
        {
            get { return _unprocessedRequests; }
        }

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
        public ReadOnlyCollection<BulkWriteError> WriteErrors
        {
            get { return _writeErrors; }
        }
    }
}
