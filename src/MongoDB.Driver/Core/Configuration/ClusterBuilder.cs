/* Copyright 2010-present MongoDB Inc.
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
using System.Threading;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Represents a cluster builder.
    /// </summary>
    public class ClusterBuilder
    {
        // fields
        private EventAggregator _eventAggregator;
        private ClusterSettings _clusterSettings;
        private ConnectionPoolSettings _connectionPoolSettings;
        private ConnectionSettings _connectionSettings;
        private LoggingSettings _loggingSettings;
        private TracingOptions _tracingOptions;
        private ServerSettings _serverSettings;
        private SslStreamSettings _sslStreamSettings;
        private Func<IStreamFactory, IStreamFactory> _streamFactoryWrapper;
        private TcpStreamSettings _tcpStreamSettings;
        private Socks5ProxyStreamSettings _socks5ProxyStreamSettings;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterBuilder"/> class.
        /// </summary>
        public ClusterBuilder()
        {
            _clusterSettings = new ClusterSettings();
            _serverSettings = new ServerSettings();
            _connectionPoolSettings = new ConnectionPoolSettings();
            _connectionSettings = new ConnectionSettings();
            _tcpStreamSettings = new TcpStreamSettings();
            _streamFactoryWrapper = inner => inner;
            _eventAggregator = new EventAggregator();
        }

        // public methods
        /// <summary>
        /// Builds the cluster.
        /// </summary>
        /// <returns>A cluster.</returns>
        public ICluster BuildCluster() => BuildClusterInternal();

        internal IClusterInternal BuildClusterInternal()
        {
            var clusterFactory = CreateClusterFactory();
            return clusterFactory.CreateCluster();
        }

        /// <summary>
        /// Configures the cluster settings.
        /// </summary>
        /// <param name="configurator">The cluster settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureCluster(Func<ClusterSettings, ClusterSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _clusterSettings = configurator(_clusterSettings);
            return this;
        }

        /// <summary>
        /// Configures the connection settings.
        /// </summary>
        /// <param name="configurator">The connection settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureConnection(Func<ConnectionSettings, ConnectionSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _connectionSettings = configurator(_connectionSettings);
            return this;
        }

        /// <summary>
        /// Configures the connection pool settings.
        /// </summary>
        /// <param name="configurator">The connection pool settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureConnectionPool(Func<ConnectionPoolSettings, ConnectionPoolSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _connectionPoolSettings = configurator(_connectionPoolSettings);
            return this;
        }

        /// <summary>
        /// Configures the logging settings.
        /// </summary>
        /// <param name="configurator">The logging settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        [CLSCompliant(false)]
        public ClusterBuilder ConfigureLoggingSettings(Func<LoggingSettings, LoggingSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _loggingSettings = configurator(_loggingSettings);
            return this;
        }

        /// <summary>
        /// Configures the tracing options.
        /// </summary>
        /// <param name="configurator">The tracing options configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureTracingOptions(Func<TracingOptions, TracingOptions> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _tracingOptions = configurator(_tracingOptions);
            return this;
        }

        /// <summary>
        /// Configures the server settings.
        /// </summary>
        /// <param name="configurator">The server settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureServer(Func<ServerSettings, ServerSettings> configurator)
        {
            _serverSettings = configurator(_serverSettings);
            return this;
        }

        /// <summary>
        /// Configures the SSL stream settings.
        /// </summary>
        /// <param name="configurator">The SSL stream settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureSsl(Func<SslStreamSettings, SslStreamSettings> configurator)
        {
            _sslStreamSettings = configurator(_sslStreamSettings ?? new SslStreamSettings());
            return this;
        }

        /// <summary>
        /// Configures the TCP stream settings.
        /// </summary>
        /// <param name="configurator">The TCP stream settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureTcp(Func<TcpStreamSettings, TcpStreamSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _tcpStreamSettings = configurator(_tcpStreamSettings);
            return this;
        }

        /// <summary>
        /// Configures the SOCKS5 proxy settings for the cluster.
        /// </summary>
        /// <param name="configurator"> The SOCKS5 proxy settings configurator delegate.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder ConfigureSocks5Proxy(Func<Socks5ProxyStreamSettings, Socks5ProxyStreamSettings> configurator)
        {
            Ensure.IsNotNull(configurator, nameof(configurator));

            _socks5ProxyStreamSettings = configurator(_socks5ProxyStreamSettings ?? new Socks5ProxyStreamSettings());
            return this;
        }

        internal ClusterBuilder RegisterStreamFactory(Func<IStreamFactory, IStreamFactory> wrapper)
        {
            Ensure.IsNotNull(wrapper, nameof(wrapper));

            var previous = _streamFactoryWrapper; // use a local variable to ensure the previous value is captured properly by the lambda
            _streamFactoryWrapper = inner => wrapper(previous(inner));
            return this;
        }

        /// <summary>
        /// Subscribes to events of type <typeparamref name="TEvent"/>.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="handler">The handler.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder Subscribe<TEvent>(Action<TEvent> handler)
        {
            Ensure.IsNotNull(handler, nameof(handler));
            _eventAggregator.Subscribe(handler);
            return this;
        }

        /// <summary>
        /// Subscribes the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public ClusterBuilder Subscribe(IEventSubscriber subscriber)
        {
            Ensure.IsNotNull(subscriber, nameof(subscriber));

            _eventAggregator.Subscribe(subscriber);
            return this;
        }

        // private methods
        private IClusterFactory CreateClusterFactory()
        {
            var serverFactory = CreateServerFactory();

            return new ClusterFactory(
                _clusterSettings,
                serverFactory,
                _eventAggregator,
                _loggingSettings?.ToInternalLoggerFactory());
        }

        private IConnectionPoolFactory CreateConnectionPoolFactory()
        {
            var streamFactory = CreateTcpStreamFactory(_tcpStreamSettings);

            var connectionFactory = new BinaryConnectionFactory(
                _connectionSettings,
                streamFactory,
                _eventAggregator,
                _clusterSettings.ServerApi,
                _loggingSettings.ToInternalLoggerFactory(),
                _tracingOptions,
                _tcpStreamSettings.ReadTimeout,
                _tcpStreamSettings.WriteTimeout);

            var connectionPoolSettings = _connectionPoolSettings.WithInternal(isPausable: !_connectionSettings.LoadBalanced);

            return new ExclusiveConnectionPoolFactory(
                connectionPoolSettings,
                connectionFactory,
                _eventAggregator,
                _loggingSettings.ToInternalLoggerFactory());
        }

        private ServerFactory CreateServerFactory()
        {
            var connectionPoolFactory = CreateConnectionPoolFactory();
            var serverMonitorFactory = CreateServerMonitorFactory();

            return new ServerFactory(
                _clusterSettings.DirectConnection,
                _serverSettings,
                connectionPoolFactory,
                serverMonitorFactory,
                _eventAggregator,
                _clusterSettings.ServerApi,
                _loggingSettings.ToInternalLoggerFactory());
        }

        private IServerMonitorFactory CreateServerMonitorFactory()
        {
            var serverMonitorConnectionSettings = _connectionSettings
                .WithInternal(authenticatorFactory: null);

            var heartbeatConnectTimeout = _tcpStreamSettings.ConnectTimeout;
            if (heartbeatConnectTimeout == TimeSpan.Zero || heartbeatConnectTimeout == Timeout.InfiniteTimeSpan)
            {
                heartbeatConnectTimeout = TimeSpan.FromSeconds(30);
            }
            var heartbeatTimeout = _serverSettings.HeartbeatTimeout;
            if (heartbeatTimeout == TimeSpan.Zero || heartbeatTimeout == Timeout.InfiniteTimeSpan)
            {
                heartbeatTimeout = heartbeatConnectTimeout;
            }
            var serverMonitorTcpStreamSettings = new TcpStreamSettings(_tcpStreamSettings)
                .With(
                    connectTimeout: heartbeatConnectTimeout,
                    readTimeout: null,
                    writeTimeout: null
                );

            var serverMonitorStreamFactory = CreateTcpStreamFactory(serverMonitorTcpStreamSettings);
            var serverMonitorSettings = new ServerMonitorSettings(
                ConnectTimeout: heartbeatConnectTimeout,
                HeartbeatInterval: _serverSettings.HeartbeatInterval,
                HeartbeatTimeout: heartbeatTimeout,
                _serverSettings.ServerMonitoringMode);

            var serverMonitorConnectionFactory = new BinaryConnectionFactory(
                serverMonitorConnectionSettings,
                serverMonitorStreamFactory,
                new EventAggregator(),
                _clusterSettings.ServerApi,
                loggerFactory: null,
                tracingOptions: new TracingOptions { Disabled = true },
                _tcpStreamSettings.ReadTimeout,
                _tcpStreamSettings.WriteTimeout);

            return new ServerMonitorFactory(
                serverMonitorSettings,
                serverMonitorConnectionFactory,
                _eventAggregator,
                _clusterSettings.ServerApi,
                _loggingSettings.ToInternalLoggerFactory());
        }

        private IStreamFactory CreateTcpStreamFactory(TcpStreamSettings tcpStreamSettings)
        {
            var streamFactory = (IStreamFactory)new TcpStreamFactory(tcpStreamSettings);

            if (_socks5ProxyStreamSettings != null)
            {
                streamFactory = new Socks5ProxyStreamFactory(_socks5ProxyStreamSettings, streamFactory);
            }

            if (_sslStreamSettings != null)
            {
                streamFactory = new SslStreamFactory(_sslStreamSettings, streamFactory);
            }

            return _streamFactoryWrapper(streamFactory);
        }
    }
}
