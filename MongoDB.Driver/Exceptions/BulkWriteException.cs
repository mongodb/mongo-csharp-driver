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
    [Obsolete("Use MongoBulkWriteException instead.")]
    public abstract class BulkWriteException : MongoException
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriteException" /> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BulkWriteException(string message)
            : base(message)
        {
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
        public abstract BulkWriteResult Result { get; }

        /// <summary>
        /// Gets the unprocessed requests.
        /// </summary>
        /// <value>
        /// The unprocessed requests.
        /// </value>
        /// <exception cref="System.NotImplementedException"></exception>
        public abstract ReadOnlyCollection<WriteRequest> UnprocessedRequests { get; }

        /// <summary>
        /// Gets the write concern error.
        /// </summary>
        /// <value>
        /// The write concern error.
        /// </value>
        public abstract WriteConcernError WriteConcernError { get; }

        /// <summary>
        /// Gets the write errors.
        /// </summary>
        /// <value>
        /// The write errors.
        /// </value>
        public abstract ReadOnlyCollection<BulkWriteError> WriteErrors { get; }
    }
}
