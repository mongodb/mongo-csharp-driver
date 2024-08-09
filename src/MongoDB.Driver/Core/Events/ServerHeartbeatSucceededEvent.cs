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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a heartbeat succeeded.
    /// </summary>
    public struct ServerHeartbeatSucceededEvent : IEvent
    {
        private readonly bool _awaited;
        private readonly ConnectionId _connectionId;
        private readonly TimeSpan _duration;
        private readonly BsonDocument _reply;
        private readonly DateTime _timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHeartbeatSucceededEvent"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="duration">The duration of time it took to complete the heartbeat.</param>
        /// <param name="awaited">The awaited flag.</param>
        public ServerHeartbeatSucceededEvent(ConnectionId connectionId, TimeSpan duration, bool awaited) :
            this(connectionId, duration, awaited, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHeartbeatSucceededEvent"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="duration">The duration of time it took to complete the heartbeat.</param>
        /// <param name="awaited">The awaited flag.</param>
        /// <param name="reply">The server response.</param>
        public ServerHeartbeatSucceededEvent(ConnectionId connectionId, TimeSpan duration, bool awaited, BsonDocument reply)
        {
            _awaited = awaited;
            _connectionId = connectionId;
            _duration = duration;
            _reply = reply;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Determines if this heartbeat event is for an awaitable hello.
        /// </summary>
        public bool Awaited => _awaited;

        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _connectionId.ServerId.ClusterId; }
        }

        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Gets the duration of time it took to complete the heartbeat.
        /// </summary>
        public TimeSpan Duration
        {
            get { return _duration; }
        }

        /// <summary>
        /// Gets the server response.
        /// </summary>
        public BsonDocument Reply
        {
            get { return _reply; }
        }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _connectionId.ServerId; }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        // explicit interface implementations
        EventType IEvent.Type => EventType.ServerHeartbeatSucceeded;
    }
}
