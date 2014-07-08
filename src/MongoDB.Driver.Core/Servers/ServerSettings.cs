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
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _heartbeatTimeout;

        // constructors
        public ServerSettings()
        {
            _connectionPoolFactory = new ConnectionPoolFactory();
            _heartbeatInterval = TimeSpan.FromSeconds(10);
            _heartbeatTimeout = TimeSpan.FromSeconds(10);
        }

        internal ServerSettings(
            IClusterListener clusterListener,
            IConnectionPoolFactory connectionPoolFactory,
            TimeSpan heartbeatInterval,
            TimeSpan heartbeatTimeout)
        {
            _clusterListener = clusterListener;
            _connectionPoolFactory = connectionPoolFactory;
            _heartbeatInterval = heartbeatInterval;
            _heartbeatTimeout = heartbeatTimeout;
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

        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
        }

        public TimeSpan HeartbeatTimeout
        {
            get { return _heartbeatTimeout; }
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

        public ServerSettings WithHeartbeatInterval(TimeSpan value)
        {
            return (_heartbeatInterval == value) ? this : new Builder(this) { _heartbeatInterval = value }.Build();
        }

        public ServerSettings WithHeartbeatTimeout(TimeSpan value)
        {
            return (_heartbeatTimeout == value) ? this : new Builder(this) { _heartbeatTimeout = value }.Build();
        }

        // nested types
        private struct Builder
        {
            // fields
            public IClusterListener _clusterListener;
            public IConnectionPoolFactory _connectionPoolFactory;
            public TimeSpan _heartbeatInterval;
            public TimeSpan _heartbeatTimeout;

            // constructors
            public Builder(ServerSettings other)
            {
                _clusterListener = other._clusterListener;
                _connectionPoolFactory = other._connectionPoolFactory;
                _heartbeatInterval = other._heartbeatInterval;
                _heartbeatTimeout = other._heartbeatTimeout;
            }

            // methods
            public ServerSettings Build()
            {
                return new ServerSettings(
                    _clusterListener,
                    _connectionPoolFactory,
                    _heartbeatInterval,
                    _heartbeatTimeout);
            }
        }
    }
}
