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

using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Emitted when the connection being checked out is attempting to read and discard a pending server response.
    /// </summary>
    public struct ConnectionReadingPendingResponseEvent : IEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionReadingPendingResponseEvent" /> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="requestId">The driver-generated request ID associated with the network timeout.</param>
        public ConnectionReadingPendingResponseEvent(
            ConnectionId connectionId,
            long requestId)
        {
            ConnectionId = connectionId;
            RequestId = requestId;
        }

        /// <summary>
        /// The connection identifier.
        /// </summary>
        public ConnectionId ConnectionId { get; }

        /// <summary>
        /// The driver-generated request ID associated with the network timeout.
        /// </summary>
        public long RequestId { get; }

        EventType IEvent.Type => EventType.ConnectionReadingPendingResponse;
    }
}

