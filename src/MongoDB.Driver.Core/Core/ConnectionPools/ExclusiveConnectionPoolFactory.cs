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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.ConnectionPools
{
    internal sealed class ExclusiveConnectionPoolFactory : IConnectionPoolFactory
    {
        // fields
        private readonly IConnectionFactory _connectionFactory;
        private readonly IConnectionPoolListener _listener;
        private readonly ConnectionPoolSettings _settings;

        public ExclusiveConnectionPoolFactory()
            : this(new ConnectionPoolSettings(), new BinaryConnectionFactory(), null)
        {
        }

        public ExclusiveConnectionPoolFactory(ConnectionPoolSettings settings, IConnectionFactory connectionFactory, IConnectionPoolListener listener)
        {
            _settings = Ensure.IsNotNull(settings, "settings");
            _connectionFactory = Ensure.IsNotNull(connectionFactory, "connectionFactory");
            _listener = listener;
        }

        public IConnectionPool CreateConnectionPool(ServerId serverId, EndPoint endPoint)
        {
            Ensure.IsNotNull(serverId, "serverId");
            Ensure.IsNotNull(endPoint, "endPoint");

            return new ExclusiveConnectionPool(serverId, endPoint, _settings, _connectionFactory, _listener);
        }
    }
}
