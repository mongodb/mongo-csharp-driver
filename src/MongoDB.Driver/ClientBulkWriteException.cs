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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a bulk write exception.
    /// </summary>
    public class ClientBulkWriteException : MongoServerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientBulkWriteException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="writeErrors">Errors that occurred during the execution of individual write operations.</param>
        /// <param name="partialResult">The results of any successful operations that were performed before the error was encountered.</param>
        /// <param name="writeConcernErrors">Write concern errors that occurred while executing the bulk write.</param>
        /// <param name="innerException">The inner exception.</param>
        public ClientBulkWriteException(
            ConnectionId connectionId,
            string message,
            IReadOnlyDictionary<int, WriteError> writeErrors,
            ClientBulkWriteResult partialResult,
            IReadOnlyList<MongoWriteConcernException> writeConcernErrors = null,
            Exception innerException = null)
            : base(connectionId, message, innerException)
        {
            WriteErrors = writeErrors;
            PartialResult = partialResult;
            WriteConcernErrors = writeConcernErrors;
        }

        /// <summary>
        /// The results of any successful operations that were performed before the error was encountered.
        /// </summary>
        public ClientBulkWriteResult PartialResult { get; init; }
        /// <summary>
        /// Write concern errors that occurred while executing the bulk write.
        /// </summary>
        public IReadOnlyList<MongoWriteConcernException> WriteConcernErrors { get; }

        /// <summary>
        /// Errors that occurred during the execution of individual write operations.
        /// </summary>
        public IReadOnlyDictionary<int, WriteError> WriteErrors { get; }
    }
}
