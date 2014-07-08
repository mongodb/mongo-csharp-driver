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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Clusters.Events;
using MongoDB.Driver.Core.ConnectionPools;

namespace MongoDB.Driver.Core.Servers
{
    /// <summary>
    /// Represents settings for a server.
    /// </summary>
    public class ServerSettings
    {
        // fields
        private readonly IClusterListener _clusterListener;
        private readonly IConnectionPoolFactory _connectionPoolFactory;
        private readonly TimeSpan _pingInterval;
        private readonly TimeSpan _pingTimeout;

        // constructors
        public ServerSettings()
        {
            _connectionPoolFactory = new ConnectionPoolFactory();
            _pingInterval = TimeSpan.FromSeconds(10);
            _pingTimeout = TimeSpan.FromSeconds(10);
        }

        internal ServerSettings(
            IClusterListener clusterListener,
            IConnectionPoolFactory connectionPoolFactory,
            TimeSpan pingInterval,
            TimeSpan pingTimeout)
        {
            _clusterListener = clusterListener;
            _connectionPoolFactory = connectionPoolFactory;
            _pingInterval = pingInterval;
            _pingTimeout = pingTimeout;
        }

        // properties
        public IClusterListener ClusterListener
        {
            get { return _clusterListener; }
        }

        public IConnectionPoolFactory ConnectionPoolFactory
        {
            get { return _connectionPoolFactory; }
        }

        public TimeSpan PingInterval
        {
            get { return _pingInterval; }
        }

        public TimeSpan PingTimeout
        {
            get { return _pingTimeout; }
        }

        // methods
        public ServerSettings WithClusterListener(IClusterListener value)
        {
            return object.ReferenceEquals(_clusterListener, value) ? this : new Builder(this) { _clusterListener = value }.Build();
        }

        public ServerSettings WithConnectionPoolFactory(IConnectionPoolFactory value)
        {
            return object.ReferenceEquals(_connectionPoolFactory, value) ? this : new Builder(this) { _connectionPoolFactory = value }.Build();
        }

        public ServerSettings WithPingInterval(TimeSpan value)
        {
            return (_pingInterval == value) ? this : new Builder(this) { _pingInterval = value }.Build();
        }

        public ServerSettings WithPingTimeout(TimeSpan value)
        {
            return (_pingTimeout == value) ? this : new Builder(this) { _pingTimeout = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public IClusterListener _clusterListener;
            public IConnectionPoolFactory _connectionPoolFactory;
            public TimeSpan _pingInterval;
            public TimeSpan _pingTimeout;

            // constructors
            public Builder(ServerSettings other)
            {
                _clusterListener = other._clusterListener;
                _connectionPoolFactory = other._connectionPoolFactory;
                _pingInterval = other._pingInterval;
                _pingTimeout = other._pingTimeout;
            }

            // methods
            public ServerSettings Build()
            {
                return new ServerSettings(
                    _clusterListener,
                    _connectionPoolFactory,
                    _pingInterval,
                    _pingTimeout);
            }
        }
    }
}
