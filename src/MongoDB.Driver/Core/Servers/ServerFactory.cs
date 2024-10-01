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

using System.Net;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    internal sealed class ServerFactory : IClusterableServerFactory
    {
        // fields
        private readonly IConnectionPoolFactory _connectionPoolFactory;
        private readonly bool _directConnection;
        private readonly IServerMonitorFactory _serverMonitorFactory;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ServerApi _serverApi;
        private readonly ServerSettings _settings;
        private readonly ILoggerFactory _loggerFactory;

        // constructors
        public ServerFactory(
            bool directConnection,
            ServerSettings settings,
            IConnectionPoolFactory connectionPoolFactory,
            IServerMonitorFactory serverMonitoryFactory,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi,
            ILoggerFactory loggerFactory)
        {
            _directConnection = directConnection;
            _settings = Ensure.IsNotNull(settings, nameof(settings));
            _connectionPoolFactory = Ensure.IsNotNull(connectionPoolFactory, nameof(connectionPoolFactory));
            _serverMonitorFactory = Ensure.IsNotNull(serverMonitoryFactory, nameof(serverMonitoryFactory));
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _serverApi = serverApi; // can be null
            _loggerFactory = loggerFactory;
        }

        // methods
        /// <inheritdoc/>
        public IClusterableServer CreateServer(ClusterType clusterType, ClusterId clusterId, IClusterClock clusterClock, EndPoint endPoint) =>
            clusterType switch
            {
                ClusterType.LoadBalanced =>
                    new LoadBalancedServer(
                        clusterId,
                        clusterClock,
                        _settings,
                        endPoint,
                        _connectionPoolFactory,
                        _serverApi,
                        _loggerFactory.CreateEventLogger<LogCategories.SDAM>(_eventSubscriber)),

                _ =>
                    new DefaultServer(
                        clusterId,
                        clusterClock,
                        _directConnection,
                        _settings,
                        endPoint,
                        _connectionPoolFactory,
                        _serverMonitorFactory,
                        _serverApi,
                        _loggerFactory.CreateEventLogger<LogCategories.SDAM>(_eventSubscriber))
            };
    }
}
