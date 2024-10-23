/* Copyright 2021-present MongoDB Inc.
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

using System.Net;
using System;
using System.Runtime.Serialization;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB connection pool paused exception.
    /// </summary>
    public class MongoConnectionPoolPausedException : MongoClientException
    {
        #region static
        // static methods
        internal static MongoConnectionPoolPausedException ForConnectionPool(string poolIdentifier)
        {
            var message = $"The connection pool is in paused state for server {poolIdentifier}.";
            return new MongoConnectionPoolPausedException(message);
        }

        internal static MongoConnectionPoolPausedException ForConnectionPool(EndPoint endPoint) =>
            ForConnectionPool(EndPointHelper.ToString(endPoint));

        #endregion

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionPoolPausedException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public MongoConnectionPoolPausedException(string message)
            : base(message, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoConnectionPoolPausedException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        protected MongoConnectionPoolPausedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
