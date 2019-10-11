/* Copyright 2019-present MongoDB Inc.
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

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Represents the reason a connection was closed.
    /// </summary>
    public enum ConnectionClosedReason
    {
        /// <summary>
        /// The pool was cleared, making the connection no longer valid.
        /// </summary>
        Stale,

        /// <summary>
        /// The connection became stale by being available for too long.
        /// </summary>
        Idle,

        /// <summary>
        /// The connection experienced an error, making it no longer valid.
        /// </summary>
        Error,

        /// <summary>
        /// The pool was closed, making the connection no longer valid.
        /// </summary>
        PoolClosed,

        /// <summary>
        /// The reason the connection was closed is unknown.
        /// </summary>
        Unknown
    }
}
