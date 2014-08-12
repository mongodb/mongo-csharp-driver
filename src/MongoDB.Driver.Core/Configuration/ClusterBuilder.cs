/* Copyright 2010-2013 10gen Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Configuration
{
    public class ClusterBuilder
    {
        // fields
        private IClusterListener _clusterListener = null;
        private ClusterSettings _clusterSettings;
        private IConnectionListener _connectionListener;
        private IConnectionPoolListener _connectionPoolListener;
        private ConnectionPoolSettings _connectionPoolSettings;
        private ConnectionSettings _connectionSettings;
        private IServerListener _serverListener;
        private ServerSettings _serverSettings;
        private Func<IStreamFactory, IStreamFactory> _streamFactoryWrapper;
        private TcpStreamSettings _tcpStreamSettings;

        // constructors
        public ClusterBuilder()
        {
            _clusterSettings = new ClusterSettings();
            _serverSettings = new ServerSettings();
            _connectionPoolSettings = new ConnectionPoolSettings();
            _connectionSettings = new ConnectionSettings();
            _tcpStreamSettings = new TcpStreamSettings();
            _streamFactoryWrapper = inner => inner;
        }

        // methods
        public ICluster BuildCluster()
        {
            IStreamFactory streamFactory = new TcpStreamFactory(_tcpStreamSettings);
            // TODO: SSL gets handled here specifically...

            streamFactory = _streamFactoryWrapper(streamFactory);

            var connectionFactory = new BinaryConnectionFactory(
                _connectionSettings,
                streamFactory,
                _connectionListener);

            var connectionPoolFactory = new ExclusiveConnectionPoolFactory(
                _connectionPoolSettings,
                connectionFactory,
                _connectionPoolListener);

            var serverFactory = new ServerFactory(
                _serverSettings,
                connectionPoolFactory,
                connectionFactory,
                _serverListener);

            var clusterFactory = new ClusterFactory(
                _clusterSettings,
                serverFactory,
                _clusterListener);

            return clusterFactory.CreateCluster();
        }

        public ClusterBuilder ConfigureCluster(Func<ClusterSettings, ClusterSettings> configure)
        {
            Ensure.IsNotNull(configure, "configure");

            _clusterSettings = configure(_clusterSettings);
            return this;
        }

        public ClusterBuilder ConfigureConnection(Func<ConnectionSettings, ConnectionSettings> configure)
        {
            Ensure.IsNotNull(configure, "configure");

            _connectionSettings = configure(_connectionSettings);
            return this;
        }

        public ClusterBuilder ConfigureConnectionPool(Func<ConnectionPoolSettings, ConnectionPoolSettings> configure)
        {
            Ensure.IsNotNull(configure, "configure");

            _connectionPoolSettings = configure(_connectionPoolSettings);
            return this;
        }

        public ClusterBuilder ConfigureServer(Func<ServerSettings, ServerSettings> configure)
        {
            _serverSettings = configure(_serverSettings);
            return this;
        }

        public ClusterBuilder ConfigureTcp(Func<TcpStreamSettings, TcpStreamSettings> configure)
        {
            Ensure.IsNotNull(configure, "configure");

            _tcpStreamSettings = configure(_tcpStreamSettings);
            return this;
        }

        public ClusterBuilder RegisterStreamFactory(Func<IStreamFactory, IStreamFactory> wrapper)
        {
            Ensure.IsNotNull(wrapper, "wrapper");

            _streamFactoryWrapper = inner => wrapper(_streamFactoryWrapper(inner));
            return this;
        }

        public ClusterBuilder AddListener(IListener listener)
        {
            var clusterListener = listener as IClusterListener;
            if(clusterListener != null)
            {
                _clusterListener = ClusterListenerPair.Create(_clusterListener, clusterListener);
            }

            var serverListener = listener as IServerListener;
            if(serverListener != null)
            {
                _serverListener = ServerListenerPair.Create(_serverListener, serverListener);
            }

            var connectionPoolListener = listener as IConnectionPoolListener;
            if(connectionPoolListener != null)
            {
                _connectionPoolListener = ConnectionPoolListenerPair.Create(_connectionPoolListener, connectionPoolListener);
            }

            var connectionListener = listener as IConnectionListener;
            if(connectionListener != null)
            {
                _connectionListener = ConnectionListenerPair.Create(_connectionListener, connectionListener);
            }

            return this;
        }
    }
}