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
using MongoDB.Bson;
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB server exception.
    /// </summary>
    [Serializable]
    public class MongoServerException : MongoException
    {
        #region static
        /// <summary>
        /// Adds error labels from a command result document into the top-level label list.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="result">The result document.</param>
        protected static void AddErrorLabelsFromCommandResult(MongoServerException exception, BsonDocument result)
        {
            // note: make a best effort to extract the error labels from the result, but never throw an exception
            if (result != null)
            {
                BsonValue errorLabels;
                if (result.TryGetValue("errorLabels", out errorLabels) && errorLabels.IsBsonArray)
                {
                    foreach (var errorLabel in errorLabels.AsBsonArray)
                    {
                        if (errorLabel.IsString)
                        {
                            exception.AddErrorLabel(errorLabel.AsString);
                        }
                    }
                }
            }
        }
        #endregion

        // fields
        private readonly ConnectionId _connectionId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        public MongoServerException(ConnectionId connectionId, string message)
            : this(connectionId, message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MongoServerException(ConnectionId connectionId, string message, Exception innerException)
            : base(message, innerException)
        {
            _connectionId = Ensure.IsNotNull(connectionId, nameof(connectionId));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoServerException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        public MongoServerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _connectionId = (ConnectionId)info.GetValue("_connectionId", typeof(ConnectionId));
        }

        // properties
        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        // methods
        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_connectionId", _connectionId);
        }
    }
}
