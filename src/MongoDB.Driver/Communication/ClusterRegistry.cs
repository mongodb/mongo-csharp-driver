/* Copyright 2010-2014 MongoDB Inc.
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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Communication
{
    /// <summary>
    /// Represents a registry of already created clusters.
    /// </summary>
    internal class ClusterRegistry
    {
        #region static
        // static fields
        private static readonly ClusterRegistry __instance = new ClusterRegistry();

        // static properties
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static ClusterRegistry Instance
        {
            get { return __instance; }
        }
        #endregion

        // fields
        private readonly object _lock = new object();
        private readonly Dictionary<ClusterKey, ICluster> _registry = new Dictionary<ClusterKey, ICluster>();

        // methods
        private ICluster CreateCluster(ClusterKey clusterKey)
        {
            var clusterSettings = CreateClusterSettings(clusterKey);
            var serverSettings = CreateServerSettings(clusterKey);
            var connectionSettings = CreateConnectionSettings(clusterKey);
            var connectionPoolSettings = CreateConnectionPoolSettings(clusterKey);

            var listener = EmptyListener.Instance;
            var streamFactory = CreateStreamFactory(clusterKey);
            var connectionFactory = new BinaryConnectionFactory(connectionSettings, streamFactory, listener);
            var connectionPoolFactory = new ExclusiveConnectionPoolFactory(connectionPoolSettings, connectionFactory, listener);
            var serverFactory = new ServerFactory(serverSettings, connectionPoolFactory, connectionFactory, listener);
            var clusterFactory = new ClusterFactory(clusterSettings, serverFactory, listener);

            var cluster = clusterFactory.CreateCluster();
            cluster.Initialize();

            return cluster;
        }

        private ClusterSettings CreateClusterSettings(ClusterKey clusterKey)
        {
            var endPoints = clusterKey.Servers.Select(s => new DnsEndPoint(s.Host, s.Port));
            return new ClusterSettings()
                .WithConnectionMode(clusterKey.ConnectionMode.ToCore())
                .WithEndPoints(endPoints)
                .WithReplicaSetName(clusterKey.ReplicaSetName);
        }

        private ConnectionPoolSettings CreateConnectionPoolSettings(ClusterKey clusterKey)
        {
            return new ConnectionPoolSettings()
                // .WithMaintenanceInterval(_connectionPoolMaintenanceInterval) // TODO: is this configurable?
                .WithMaxConnections(clusterKey.MaxConnectionPoolSize)
                .WithMinConnections(clusterKey.MinConnectionPoolSize)
                .WithWaitQueueSize(clusterKey.WaitQueueSize)
                .WithWaitQueueTimeout(clusterKey.WaitQueueTimeout);
        }

        private ConnectionSettings CreateConnectionSettings(ClusterKey clusterKey)
        {
            return new ConnectionSettings()
                .WithAuthenticators(clusterKey.Credentials.Select(c => c.ToAuthenticator()))
                .WithMaxIdleTime(clusterKey.MaxConnectionIdleTime)
                .WithMaxLifeTime(clusterKey.MaxConnectionLifeTime);
        }

        private ServerSettings CreateServerSettings(ClusterKey clusterKey)
        {
            return new ServerSettings()
                .WithHeartbeatInterval(clusterKey.HeartbeatInterval)
                .WithHeartbeatTimeout(clusterKey.HeartbeatTimeout);
        }

        private IStreamFactory CreateStreamFactory(ClusterKey clusterKey)
        {
            // TODO: handle SSL
            var tcpStreamSettings = CreateTcpStreamSettings(clusterKey);
            return new TcpStreamFactory(tcpStreamSettings);
        }

        private TcpStreamSettings CreateTcpStreamSettings(ClusterKey clusterKey)
        {
            return new TcpStreamSettings()
                .WithReadTimeout(clusterKey.SocketTimeout)
                .WithReceiveBufferSize(clusterKey.ReceiveBufferSize)
                .WithSendBufferSize(clusterKey.SendBufferSize)
                .WithWriteTimeout(clusterKey.SocketTimeout);
        }

        private ICluster GetOrCreateCluster(ClusterKey clusterKey)
        {
            lock (_lock)
            {
                ICluster cluster;
                if (!_registry.TryGetValue(clusterKey, out cluster))
                {
                    cluster = CreateCluster(clusterKey);
                    _registry.Add(clusterKey, cluster);
                }
                return cluster;
            }
        }

        /// <summary>
        /// Gets an existing cluster or creates a new one.
        /// </summary>
        /// <param name="clientSettings">The client settings.</param>
        /// <returns></returns>
        public ICluster GetOrCreateCluster(MongoClientSettings clientSettings)
        {
            var clusterKey = new ClusterKey(clientSettings);
            return GetOrCreateCluster(clusterKey);
        }

        /// <summary>
        /// Gets an existing cluster or creates a new one.
        /// </summary>
        /// <param name="serverSettings">The server settings.</param>
        /// <returns></returns>
        public ICluster GetOrCreateCluster(MongoServerSettings serverSettings)
        {
            var clusterKey = new ClusterKey(serverSettings);
            return GetOrCreateCluster(clusterKey);
        }
    }
}
