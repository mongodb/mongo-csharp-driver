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
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB execution timeout exception.
    /// </summary>
    [Serializable]
    public class MongoExecutionTimeoutException : MongoServerException
    {
        private readonly BsonDocument _result;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoExecutionTimeoutException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        public MongoExecutionTimeoutException(ConnectionId connectionId, string message)
            : base(connectionId, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoExecutionTimeoutException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoExecutionTimeoutException(ConnectionId connectionId, string message, Exception innerException)
            : base(connectionId, message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoExecutionTimeoutException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="result">The command result.</param>
        public MongoExecutionTimeoutException(ConnectionId connectionId, string message, BsonDocument result)
            : this(connectionId, message, null, result)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoExecutionTimeoutException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="result">The command result.</param>
        public MongoExecutionTimeoutException(ConnectionId connectionId, string message, Exception innerException, BsonDocument result)
            : base(connectionId, message, innerException)
        {
            _result = result;
            AddErrorLabelsFromCommandResult(this, result);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoExecutionTimeoutException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoExecutionTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _result = (BsonDocument)info.GetValue("_result", typeof(BsonDocument));
        }

        // properties
        /// <summary>
        /// Gets the error code.
        /// </summary>
        /// <value>
        /// The error code.
        /// </value>
        public int Code =>
            _result != null && _result.TryGetValue("code", out var code)
                ? code.ToInt32()
                : -1;

        /// <summary>
        /// Gets the name of the error code.
        /// </summary>
        /// <value>
        /// The name of the error code.
        /// </value>
        public string CodeName => _result?.GetValue("codeName", null)?.AsString;

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_result", _result);
        }
    }
}
