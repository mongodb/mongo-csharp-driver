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

using MongoDB.Bson;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents a MongoDB cursor not found exception.
    /// </summary>
    public class MongoCursorNotFoundException : MongoQueryException
    {
        #region static
        // static methods
        private static string FormatMessage(ConnectionId connectionId, long cursorId)
        {
            return string.Format(
                "Cursor {0} not found on server {1} using connection {2}.",
                cursorId,
                EndPointHelper.ToString(connectionId.ServerId.EndPoint),
                connectionId.LongServerValue);
        }
        #endregion

        // fields
        private readonly long _cursorId;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCursorNotFoundException"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="cursorId">The cursor identifier.</param>
        /// <param name="query">The query.</param>
        public MongoCursorNotFoundException(ConnectionId connectionId, long cursorId, BsonDocument query)
            : base(connectionId, FormatMessage(connectionId, cursorId), query, null)
        {
            _cursorId = cursorId;
        }

        // properties
        /// <summary>
        /// Gets the cursor identifier.
        /// </summary>
        /// <value>
        /// The cursor identifier.
        /// </value>
        public long CursorId
        {
            get { return _cursorId; }
        }
    }
}
