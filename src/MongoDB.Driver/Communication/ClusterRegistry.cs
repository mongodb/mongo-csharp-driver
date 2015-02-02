﻿/* Copyright 2010-2014 MongoDB Inc.
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Misc;
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
            var builder = new ClusterBuilder()
                .ConfigureCluster(settings => ConfigureCluster(settings, clusterKey))
                .ConfigureServer(settings => ConfigureServer(settings, clusterKey))
                .ConfigureConnectionPool(settings => ConfigureConnectionPool(settings, clusterKey))
                .ConfigureConnection(settings => ConfigureConnection(settings, clusterKey))
                .ConfigureTcp(settings => ConfigureTcp(settings, clusterKey));

            if (clusterKey.UseSsl)
            {
                builder.ConfigureSsl(settings => ConfigureSsl(settings, clusterKey));
            }

            if (clusterKey.ClusterConfigurator != null)
            {
                clusterKey.ClusterConfigurator(builder);
            }

            var cluster = builder.BuildCluster();
            cluster.Initialize();

            return cluster;
        }

        private ClusterSettings ConfigureCluster(ClusterSettings settings, ClusterKey clusterKey)
        {
            var endPoints = clusterKey.Servers.Select(s => (EndPoint)new DnsEndPoint(s.Host, s.Port));
            return settings.With(
                connectionMode: clusterKey.ConnectionMode.ToCore(),
                endPoints: Optional.Create(endPoints),
                replicaSetName: clusterKey.ReplicaSetName,
                postServerSelector: new LatencyLimitingServerSelector(clusterKey.LocalThreshold));
        }

        private ConnectionPoolSettings ConfigureConnectionPool(ConnectionPoolSettings settings, ClusterKey clusterKey)
        {
            return settings.With(
                // maintenanceInterval: TODO: should this be configurable?
                maxConnections: clusterKey.MaxConnectionPoolSize,
                minConnections: clusterKey.MinConnectionPoolSize,
                waitQueueSize: clusterKey.WaitQueueSize,
                waitQueueTimeout: clusterKey.WaitQueueTimeout);
        }

        private ConnectionSettings ConfigureConnection(ConnectionSettings settings, ClusterKey clusterKey)
        {
            var authenticators = clusterKey.Credentials.Select(c => c.ToAuthenticator());
            return settings.With(
                authenticators: Optional.Create(authenticators),
                maxIdleTime: clusterKey.MaxConnectionIdleTime,
                maxLifeTime: clusterKey.MaxConnectionLifeTime);
        }

        private ServerSettings ConfigureServer(ServerSettings settings, ClusterKey clusterKey)
        {
            return settings.With(
                heartbeatInterval: clusterKey.HeartbeatInterval,
                heartbeatTimeout: clusterKey.HeartbeatTimeout);
        }

        private SslStreamSettings ConfigureSsl(SslStreamSettings settings, ClusterKey clusterKey)
        {
            if (clusterKey.SslSettings != null)
            {
                var validationCallback = clusterKey.SslSettings.ServerCertificateValidationCallback;
                if (validationCallback == null && !clusterKey.VerifySslCertificate)
                {
                    validationCallback = AcceptAnySslCertificate;
                }

                return settings.With(
                    clientCertificates: Optional.Create(clusterKey.SslSettings.ClientCertificates ?? Enumerable.Empty<X509Certificate>()),
                    checkCertificateRevocation: clusterKey.SslSettings.CheckCertificateRevocation,
                    clientCertificateSelectionCallback: clusterKey.SslSettings.ClientCertificateSelectionCallback,
                    enabledProtocols: clusterKey.SslSettings.EnabledSslProtocols,
                    serverCertificateValidationCallback: validationCallback);
            }

            return settings;
        }

        private TcpStreamSettings ConfigureTcp(TcpStreamSettings settings, ClusterKey clusterKey)
        {
            return settings.With(
                connectTimeout: clusterKey.ConnectTimeout,
                readTimeout: clusterKey.SocketTimeout,
                receiveBufferSize: clusterKey.ReceiveBufferSize,
                sendBufferSize: clusterKey.SendBufferSize,
                writeTimeout: clusterKey.SocketTimeout);
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

        public ICluster GetOrCreateCluster(MongoClientSettings clientSettings)
        {
            var clusterKey = new ClusterKey(clientSettings);
            return GetOrCreateCluster(clusterKey);
        }

        public ICluster GetOrCreateCluster(MongoServerSettings serverSettings)
        {
            var clusterKey = new ClusterKey(serverSettings);
            return GetOrCreateCluster(clusterKey);
        }

        private static bool AcceptAnySslCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            return true;
        }
    }
}
