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

using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs when a heartbeat succeeded.
    /// </summary>
    public struct ServerHeartbeatStartedEvent
    {
        private readonly bool _awaited;
        private readonly ConnectionId _connectionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerHeartbeatStartedEvent"/> struct.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="awaited">The awaited flag.</param>
        public ServerHeartbeatStartedEvent(ConnectionId connectionId, bool awaited)
        {
            _awaited = awaited;
            _connectionId = connectionId;
        }

        /// <summary>
        /// Determines if this heartbeat event is for an awaitable isMaster.
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
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _connectionId.ServerId; }
        }
    }
}
