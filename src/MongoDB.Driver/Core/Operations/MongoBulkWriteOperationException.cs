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
using System.Text;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Operations
{
    /// <summary>
    /// Represents a bulk write operation exception.
    /// </summary>
    internal sealed class MongoBulkWriteOperationException : MongoServerException
    {
        // fields
        private BulkWriteOperationResult _result;
        private IReadOnlyList<WriteRequest> _unprocessedRequests;
        private BulkWriteConcernError _writeConcernError;
        private IReadOnlyList<BulkWriteOperationError> _writeErrors;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoBulkWriteOperationException" /> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="result">The result.</param>
        /// <param name="writeErrors">The write errors.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        /// <param name="unprocessedRequests">The unprocessed requests.</param>
        public MongoBulkWriteOperationException(
            ConnectionId connectionId,
            BulkWriteOperationResult result,
            IReadOnlyList<BulkWriteOperationError> writeErrors,
            BulkWriteConcernError writeConcernError,
            IReadOnlyList<WriteRequest> unprocessedRequests)
            : base(connectionId, FormatMessage(writeErrors, writeConcernError))
        {
            _result = result;
            _writeErrors = writeErrors;
            _writeConcernError = writeConcernError;
            _unprocessedRequests = unprocessedRequests;
            if (_writeConcernError != null)
            {
                foreach (var errorLabel in _writeConcernError.ErrorLabels)
                {
                    AddErrorLabel(errorLabel);
                }
            }
        }

        // properties
        /// <summary>
        /// Gets the result of the bulk write operation.
        /// </summary>
        public BulkWriteOperationResult Result
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
        public IReadOnlyList<WriteRequest> UnprocessedRequests
        {
            get { return _unprocessedRequests; }
        }

        /// <summary>
        /// Gets the write concern error.
        /// </summary>
        /// <value>
        /// The write concern error.
        /// </value>
        public BulkWriteConcernError WriteConcernError
        {
            get { return _writeConcernError; }
        }

        /// <summary>
        /// Gets the write errors.
        /// </summary>
        /// <value>
        /// The write errors.
        /// </value>
        public IReadOnlyList<BulkWriteOperationError> WriteErrors
        {
            get { return _writeErrors; }
        }

        // methods
        private static string FormatMessage(IReadOnlyList<BulkWriteOperationError> writeErrors, BulkWriteConcernError writeConcernError)
        {
            var sb = new StringBuilder("A bulk write operation resulted in one or more errors.");
            if (writeErrors != null)
            {
                foreach (var writeError in writeErrors)
                {
                    sb.AppendLine().Append("  " + writeError.Message);
                }
            }
            if (writeConcernError != null)
            {
                sb.AppendLine().Append("  " + writeConcernError.Message);
            }

            return sb.ToString();
        }
    }
}
