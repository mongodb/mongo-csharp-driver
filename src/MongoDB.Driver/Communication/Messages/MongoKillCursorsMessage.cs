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

using MongoDB.Bson.IO;

namespace MongoDB.Driver.Internal
{
    internal class MongoKillCursorsMessage : MongoRequestMessage
    {
        // private fields
        private long[] _cursorIds;

        // constructors
        internal MongoKillCursorsMessage(params long[] cursorIds)
            : base(MessageOpcode.KillCursors, null)
        {
            _cursorIds = cursorIds;
        }

        // internal methods
        internal override void WriteBodyTo(BsonStreamWriter streamWriter)
        {
            streamWriter.WriteInt32(_cursorIds.Length);
            foreach (long cursorId in _cursorIds)
            {
                streamWriter.WriteInt64(cursorId);
            }
        }

        internal override void WriteHeaderTo(BsonStreamWriter streamWriter)
        {
            base.WriteHeaderTo(streamWriter);
            streamWriter.WriteInt32(0); // reserved
        }
    }
}
