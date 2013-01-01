/* Copyright 2010-2013 10gen Inc.
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
using System.Runtime.Serialization;
using MongoDB.Bson;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB query exception.
    /// </summary>
    [Serializable]
    public class MongoQueryException : MongoException
    {
        // private fields
        private BsonDocument _queryResult;

        // constructors
        /// <summary>
        /// Initializes a new instance of the MongoQueryException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MongoQueryException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoQueryException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="queryResult">The error document returned by the server.</param>
        public MongoQueryException(string message, BsonDocument queryResult)
            : base(message)
        {
            _queryResult = queryResult;
        }

        /// <summary>
        /// Initializes a new instance of the MongoQueryException class (this overload supports deserialization).
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoQueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        // public properties
        /// <summary>
        /// Gets the error document returned by the server.
        /// </summary>
        public BsonDocument QueryResult
        {
            get { return _queryResult; }
        }
    }
}
