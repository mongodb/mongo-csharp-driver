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

namespace MongoDB.Driver
{
    /// <summary>
    /// Flags used with queries (see the SetQueryFlags method of MongoCursor).
    /// </summary>
    [Flags]
    public enum QueryFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,
        /// <summary>
        /// This cursor should be tailable.
        /// </summary>
        TailableCursor = 2,
        /// <summary>
        /// It's OK for the query to be handled by a secondary server.
        /// </summary>
        SlaveOk = 4,
        /// <summary>
        /// Tell the server not to let the cursor timeout.
        /// </summary>
        NoCursorTimeout = 16,
        /// <summary>
        /// Tell the server to wait for data to become available before returning (only used with TailableCursor).
        /// </summary>
        AwaitData = 32,
        /// <summary>
        /// Tell the server to send all the data at once (in multiple messages if necessary) without waiting for GetMore messages.
        /// </summary>
        Exhaust = 64,
        /// <summary>
        /// Allow partial results in a sharded system if some of the shards are down.
        /// </summary>
        Partial = 128
    }
}
