/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB connection failed exception.
    /// </summary>
    [Serializable]
    public class MongoConnectionClosedException : MongoConnectionException
    {
        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionClosedException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        public MongoConnectionClosedException(ConnectionId connectionId)
            : base(connectionId, "The connection was closed while we were waiting our turn to use it.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionClosedException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        protected MongoConnectionClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}