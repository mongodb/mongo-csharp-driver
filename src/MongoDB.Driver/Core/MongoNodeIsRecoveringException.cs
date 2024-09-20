/* Copyright 2013-present MongoDB Inc.
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
    /// Represents a MongoDB node is recovering exception.
    /// </summary>
    public class MongoNodeIsRecoveringException : MongoCommandException
    {
        #region static
        // private static methods
        private static string CreateMessage(BsonDocument result)
        {
            var code = result.GetValue("code", -1).ToInt32();
            var codeName = result.GetValue("codeName", null)?.AsString;

            if (codeName == null)
            {
                return $"Server returned node is recovering error (code = {code}).";
            }
            else
            {
                return $"Server returned node is recovering error (code = {code}, codeName = \"{codeName}\").";
            }
        }
        #endregion

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoNodeIsRecoveringException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="command">The command.</param>
        /// <param name="result">The result.</param>
        public MongoNodeIsRecoveringException(ConnectionId connectionId, BsonDocument command, BsonDocument result)
            : base(connectionId, CreateMessage(result), command, result)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoNodeIsRecoveringException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo.</param>
        /// <param name="context">The StreamingContext.</param>
        protected MongoNodeIsRecoveringException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets whether it caused by shutdown or no.
        /// </summary>
        public bool IsShutdownError
        {
            get
            {
                var errorCode = (ServerErrorCode)Code;
                switch (errorCode)
                {
                    case ServerErrorCode.InterruptedAtShutdown: // 1160
                    case ServerErrorCode.ShutdownInProgress: // 91
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
