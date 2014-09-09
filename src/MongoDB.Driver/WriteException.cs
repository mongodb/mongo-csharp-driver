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
    /// Represents a write exception.
    /// </summary>
    [Serializable]
    public class WriteException : MongoException
    {
        // static
        internal static WriteException FromBulkWriteException(BulkWriteException bulkException)
        {
            var writeConcernError = bulkException.WriteConcernError;
            var writeError = bulkException.WriteErrors.Count > 0
                ? bulkException.WriteErrors[0]
                : null;

            return new WriteException(writeError, writeConcernError, bulkException);
        }

        // private fields
        private WriteConcernError _writeConcernError;
        private WriteError _writeError;

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteException" /> class.
        /// </summary>
        /// <param name="writeError">The write error.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        public WriteException(
            WriteError writeError,
            WriteConcernError writeConcernError)
            : base("A write operation resulted in an error.")
        {
            _writeError = writeError;
            _writeConcernError = writeConcernError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteException" /> class.
        /// </summary>
        /// <param name="writeError">The write error.</param>
        /// <param name="writeConcernError">The write concern error.</param>
        /// <param name="innerException">The inner exception.</param>
        public WriteException(
            WriteError writeError,
            WriteConcernError writeConcernError,
            Exception innerException)
            : base("A write operation resulted in an error.", innerException)
        {
            _writeError = writeError;
            _writeConcernError = writeConcernError;
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public WriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // properties
        /// <summary>
        /// Gets the write concern error.
        /// </summary>
        public WriteConcernError WriteConcernError
        {
            get { return _writeConcernError; }
        }

        /// <summary>
        /// Gets the write error.
        /// </summary>
        public WriteError WriteError
        {
            get { return _writeError; }
        }
    }
}
