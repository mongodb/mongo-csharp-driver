/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver
{
    [Serializable]
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
                connectionId.ServerValue);
        }
        #endregion

        // fields
        private readonly long _cursorId;

        // constructors
        public MongoCursorNotFoundException(ConnectionId connectionId, long cursorId, BsonDocument query)
            : base(connectionId, FormatMessage(connectionId, cursorId), query, null, null)
        {
            _cursorId = cursorId;
        }

        protected MongoCursorNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _cursorId = info.GetInt64("_cursorId");
        }

        // properties
        public long CursorId
        {
            get { return _cursorId; }
        }

        // methods
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_cursorId", _cursorId);
        }
    }
}
