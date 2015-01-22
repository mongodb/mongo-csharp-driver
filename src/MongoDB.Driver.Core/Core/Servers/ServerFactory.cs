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

using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents the default server factory.
    /// </summary>
    public class ServerFactory : IClusterableServerFactory
    {
        // fields
        private readonly ClusterConnectionMode _clusterConnectionMode;
        private readonly IConnectionPoolFactory _connectionPoolFactory;
        private readonly IConnectionFactory _heartbeatConnectionFactory;
        private readonly IServerListener _listener;
        private readonly ServerSettings _settings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerFactory"/> class.
        /// </summary>
        /// <param name="clusterConnectionMode">The cluster connection mode.</param>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionPoolFactory">The connection pool factory.</param>
        /// <param name="heartbeatConnectionFactory">The heartbeat connection factory.</param>
        /// <param name="listener">The listener.</param>
        public ServerFactory(ClusterConnectionMode clusterConnectionMode, ServerSettings settings, IConnectionPoolFactory connectionPoolFactory, IConnectionFactory heartbeatConnectionFactory, IServerListener listener)
        {
            _clusterConnectionMode = clusterConnectionMode;
            _settings = Ensure.IsNotNull(settings, "settings");
            _connectionPoolFactory = Ensure.IsNotNull(connectionPoolFactory, "connectionPoolFactory");
            _heartbeatConnectionFactory = Ensure.IsNotNull(heartbeatConnectionFactory, "heartbeatConnectionFactory");
            _listener = listener;
        }

        // methods
        /// <inheritdoc/>
        public IClusterableServer CreateServer(ClusterId clusterId, EndPoint endPoint)
        {
            return new ClusterableServer(clusterId, _clusterConnectionMode, _settings, endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _listener);
        }
    }
}
