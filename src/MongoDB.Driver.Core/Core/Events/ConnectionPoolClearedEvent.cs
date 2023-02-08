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

using System;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Events
{
    /// <summary>
    /// Occurs after the pool is cleared.
    /// </summary>
    public struct ConnectionPoolClearedEvent : IEvent
    {
        private readonly bool _closeInUseConnections;
        private readonly ConnectionPoolSettings _connectionPoolSettings;
        private readonly ServerId _serverId;
        private readonly ObjectId? _serviceId;
        private readonly DateTime _timestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolClearedEvent"/> struct.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="connectionPoolSettings">The connection pool settings.</param>
        public ConnectionPoolClearedEvent(ServerId serverId, ConnectionPoolSettings connectionPoolSettings)
            : this(serverId, connectionPoolSettings, closeInUseConnections: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolClearedEvent"/> struct.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="connectionPoolSettings">The connection pool settings.</param>
        /// <param name="closeInUseConnections">Whether in use connections should be closed.</param>
        public ConnectionPoolClearedEvent(ServerId serverId, ConnectionPoolSettings connectionPoolSettings, bool closeInUseConnections)
            : this(serverId, connectionPoolSettings, serviceId: null, closeInUseConnections)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolClearedEvent"/> struct.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="connectionPoolSettings">The connection pool settings.</param>
        /// <param name="serviceId">The service identifier.</param>
        public ConnectionPoolClearedEvent(ServerId serverId, ConnectionPoolSettings connectionPoolSettings, ObjectId? serviceId)
            : this(serverId, connectionPoolSettings, serviceId, closeInUseConnections: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionPoolClearedEvent"/> struct.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="connectionPoolSettings">The connection pool settings.</param>
        /// <param name="serviceId">The service identifier.</param>
        /// <param name="closeInUseConnections">Whether in use connections should be closed.</param>
        public ConnectionPoolClearedEvent(ServerId serverId, ConnectionPoolSettings connectionPoolSettings, ObjectId? serviceId, bool closeInUseConnections)
        {
            _serverId = Ensure.IsNotNull(serverId, nameof(serverId));
            _connectionPoolSettings = connectionPoolSettings;
            _serviceId = serviceId; // can be null
            _closeInUseConnections = closeInUseConnections;
            _timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether in use connections should be closed.
        /// </summary>
        public bool CloseInUseConnections => _closeInUseConnections;

        /// <summary>
        /// Gets the cluster identifier.
        /// </summary>
        public ClusterId ClusterId
        {
            get { return _serverId.ClusterId; }
        }

        /// <summary>
        /// Gets the connection pool settings.
        /// </summary>
        public ConnectionPoolSettings ConnectionPoolSettings
        {
            get { return _connectionPoolSettings; }
        }

        /// <summary>
        /// Gets the server identifier.
        /// </summary>
        public ServerId ServerId
        {
            get { return _serverId; }
        }

        /// <summary>
        /// Gets the service identifier.
        /// </summary>
        public ObjectId? ServiceId
        {
            get { return _serviceId; }
        }

        /// <summary>
        /// Gets the timestamp.
        /// </summary>
        public DateTime Timestamp
        {
            get { return _timestamp; }
        }

        // explicit interface implementations
        EventType IEvent.Type => EventType.ConnectionPoolCleared;
    }
}
