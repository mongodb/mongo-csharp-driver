/* Copyright 2016-present MongoDB Inc.
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
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Servers
{
    internal class ServerMonitorFactory : IServerMonitorFactory
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly IEventSubscriber _eventSubscriber;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ServerMonitorSettings _serverMonitorSettings;
        private readonly ServerApi _serverApi;

        public ServerMonitorFactory(
            ServerMonitorSettings serverMonitorSettings,
            IConnectionFactory connectionFactory,
            IEventSubscriber eventSubscriber,
            ServerApi serverApi,
            ILoggerFactory loggerFactory)
        {
            _serverMonitorSettings = Ensure.IsNotNull(serverMonitorSettings, nameof(serverMonitorSettings));
            _connectionFactory = Ensure.IsNotNull(connectionFactory, nameof(connectionFactory));
            _eventSubscriber = Ensure.IsNotNull(eventSubscriber, nameof(eventSubscriber));
            _serverApi = serverApi;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc/>
        public IServerMonitor Create(ServerId serverId, EndPoint endPoint)
        {
            return new ServerMonitor(serverId, endPoint, _connectionFactory, _serverMonitorSettings, _eventSubscriber, _serverApi, _loggerFactory);
        }
    }
}
