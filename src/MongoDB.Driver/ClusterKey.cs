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
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    internal sealed class ClusterKey
    {
        // fields
        private readonly bool _allowInsecureTls;
        private readonly string _applicationName;
        private readonly Action<ClusterBuilder> _clusterConfigurator;
        private readonly IReadOnlyList<CompressorConfiguration> _compressors;
        private readonly TimeSpan _connectTimeout;
        private readonly MongoCredential _credential;
        private readonly CryptClientSettings _cryptClientSettings;
        private readonly bool _directConnection;
        private readonly int _hashCode;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _heartbeatTimeout;
        private readonly bool _ipv6;
        private readonly LibraryInfo _libraryInfo;
        private readonly bool _loadBalanced;
        private readonly TimeSpan _localThreshold;
        private readonly LoggingSettings _loggingSettings;
        private readonly int _maxConnecting;
        private readonly TimeSpan _maxConnectionIdleTime;
        private readonly TimeSpan _maxConnectionLifeTime;
        private readonly int _maxConnectionPoolSize;
        private readonly int _minConnectionPoolSize;
        private readonly int _receiveBufferSize;
        private readonly string _replicaSetName;
        private readonly ConnectionStringScheme _scheme;
        private readonly int _sendBufferSize;
        private readonly ServerApi _serverApi;
        private readonly IReadOnlyList<MongoServerAddress> _servers;
        private readonly ServerMonitoringMode _serverMonitoringMode;
        private readonly TimeSpan _serverSelectionTimeout;
        private readonly TimeSpan _socketTimeout;
        private readonly Socks5ProxySettings _socks5ProxySettings;
        private readonly int _srvMaxHosts;
        private readonly string _srvServiceName;
        private readonly SslSettings _sslSettings;
        private readonly TracingOptions _tracingOptions;
        private readonly bool _useTls;
        private readonly int _waitQueueSize;
        private readonly TimeSpan _waitQueueTimeout;

        // constructors
        public ClusterKey(
            bool allowInsecureTls,
            string applicationName,
            Action<ClusterBuilder> clusterConfigurator,
            IReadOnlyList<CompressorConfiguration> compressors,
            TimeSpan connectTimeout,
            MongoCredential credential,
            CryptClientSettings cryptClientSettings,
            bool directConnection,
            TimeSpan heartbeatInterval,
            TimeSpan heartbeatTimeout,
            bool ipv6,
            LibraryInfo libraryInfo,
            bool loadBalanced,
            TimeSpan localThreshold,
            LoggingSettings loggingSettings,
            int maxConnecting,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            int receiveBufferSize,
            string replicaSetName,
            ConnectionStringScheme scheme,
            int sendBufferSize,
            ServerApi serverApi,
            IReadOnlyList<MongoServerAddress> servers,
            ServerMonitoringMode serverMonitoringMode,
            TimeSpan serverSelectionTimeout,
            TimeSpan socketTimeout,
            Socks5ProxySettings socks5ProxySettings,
            int srvMaxHosts,
            string srvServiceName,
            SslSettings sslSettings,
            TracingOptions tracingOptions,
            bool useTls,
            int waitQueueSize,
            TimeSpan waitQueueTimeout)
        {
            _allowInsecureTls = allowInsecureTls;
            _applicationName = applicationName;
            _clusterConfigurator = clusterConfigurator;
            _compressors = compressors;
            _connectTimeout = connectTimeout;
            _credential = credential;
            _cryptClientSettings = cryptClientSettings;
            _directConnection = directConnection;
            _heartbeatInterval = heartbeatInterval;
            _heartbeatTimeout = heartbeatTimeout;
            _ipv6 = ipv6;
            _libraryInfo = libraryInfo;
            _loadBalanced = loadBalanced;
            _localThreshold = localThreshold;
            _loggingSettings = loggingSettings;
            _maxConnecting = maxConnecting;
            _maxConnectionIdleTime = maxConnectionIdleTime;
            _maxConnectionLifeTime = maxConnectionLifeTime;
            _maxConnectionPoolSize = maxConnectionPoolSize;
            _minConnectionPoolSize = minConnectionPoolSize;
            _receiveBufferSize = receiveBufferSize;
            _replicaSetName = replicaSetName;
            _scheme = scheme;
            _sendBufferSize = sendBufferSize;
            _serverApi = serverApi;
            _servers = servers;
            _serverMonitoringMode = serverMonitoringMode;
            _serverSelectionTimeout = serverSelectionTimeout;
            _socketTimeout = socketTimeout;
            _socks5ProxySettings = socks5ProxySettings;
            _srvMaxHosts = srvMaxHosts;
            _srvServiceName = srvServiceName;
            _sslSettings = sslSettings;
            _tracingOptions = tracingOptions;
            _useTls = useTls;
            _waitQueueSize = waitQueueSize;
            _waitQueueTimeout = waitQueueTimeout;

            _hashCode = CalculateHashCode();
        }

        // properties
        public bool AllowInsecureTls => _allowInsecureTls;
        public string ApplicationName { get { return _applicationName; } }
        public Action<ClusterBuilder> ClusterConfigurator { get { return _clusterConfigurator; } }
        public IReadOnlyList<CompressorConfiguration> Compressors { get { return _compressors; } }
        public TimeSpan ConnectTimeout { get { return _connectTimeout; } }
        public MongoCredential Credential { get { return _credential; } }
        public CryptClientSettings CryptClientSettings { get { return _cryptClientSettings; } }
        public bool DirectConnection { get { return _directConnection; } }
        public TimeSpan HeartbeatInterval { get { return _heartbeatInterval; } }
        public TimeSpan HeartbeatTimeout { get { return _heartbeatTimeout; } }
        public bool IPv6 { get { return _ipv6; } }
        public LibraryInfo LibraryInfo { get { return _libraryInfo; } }
        public bool LoadBalanced => _loadBalanced;
        public TimeSpan LocalThreshold { get { return _localThreshold; } }
        public LoggingSettings LoggingSettings { get { return _loggingSettings; } }
        public int MaxConnecting{ get { return _maxConnecting; } }
        public TimeSpan MaxConnectionIdleTime { get { return _maxConnectionIdleTime; } }
        public TimeSpan MaxConnectionLifeTime { get { return _maxConnectionLifeTime; } }
        public int MaxConnectionPoolSize { get { return _maxConnectionPoolSize; } }
        public int MinConnectionPoolSize { get { return _minConnectionPoolSize; } }
        public int ReceiveBufferSize { get { return _receiveBufferSize; } }
        public string ReplicaSetName { get { return _replicaSetName; } }
        public ConnectionStringScheme Scheme { get { return _scheme; } }
        public int SendBufferSize { get { return _sendBufferSize; } }
        public ServerApi ServerApi { get { return _serverApi; } }
        public IReadOnlyList<MongoServerAddress> Servers { get { return _servers; } }
        public ServerMonitoringMode ServerMonitoringMode { get { return _serverMonitoringMode; } }
        public TimeSpan ServerSelectionTimeout { get { return _serverSelectionTimeout; } }
        public TimeSpan SocketTimeout { get { return _socketTimeout; } }
        public Socks5ProxySettings Socks5ProxySettings { get { return _socks5ProxySettings; } }
        public int SrvMaxHosts { get { return _srvMaxHosts; } }
        public string SrvServiceName { get { return _srvServiceName; } }
        public SslSettings SslSettings { get { return _sslSettings; } }
        public TracingOptions TracingOptions { get { return _tracingOptions; } }
        public bool UseTls => _useTls;
        public int WaitQueueSize { get { return _waitQueueSize; } }
        public TimeSpan WaitQueueTimeout { get { return _waitQueueTimeout; } }

        // methods
        private int CalculateHashCode()
        {
            // keep calculation simple (leave out fields that are rarely used)
            return new Hasher()
                .Hash(_credential)
                .HashElements(_servers)
                .GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(ClusterKey))
            {
                return false;
            }
            var rhs = (ClusterKey)obj;
            return
                _hashCode == rhs._hashCode && // fail fast
                _allowInsecureTls == rhs._allowInsecureTls &&
                _applicationName == rhs._applicationName &&
                object.ReferenceEquals(_clusterConfigurator, rhs._clusterConfigurator) &&
                _compressors.SequenceEqual(rhs._compressors) &&
                _connectTimeout == rhs._connectTimeout &&
                _credential == rhs._credential &&
                object.Equals(_cryptClientSettings, rhs._cryptClientSettings) &&
                _directConnection.Equals(rhs._directConnection) &&
                _heartbeatInterval == rhs._heartbeatInterval &&
                _heartbeatTimeout == rhs._heartbeatTimeout &&
                _ipv6 == rhs._ipv6 &&
                object.Equals(_libraryInfo, rhs.LibraryInfo) &&
                _loadBalanced == rhs._loadBalanced &&
                _localThreshold == rhs._localThreshold &&
                _loggingSettings == rhs._loggingSettings &&
                _maxConnecting == rhs._maxConnecting &&
                _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                _receiveBufferSize == rhs._receiveBufferSize &&
                _replicaSetName == rhs._replicaSetName &&
                _scheme == rhs._scheme &&
                _sendBufferSize == rhs._sendBufferSize &&
                _serverApi == rhs._serverApi &&
                _servers.SequenceEqual(rhs._servers) &&
                _serverMonitoringMode == rhs._serverMonitoringMode &&
                _serverSelectionTimeout == rhs._serverSelectionTimeout &&
                _socketTimeout == rhs._socketTimeout &&
                object.Equals(_socks5ProxySettings, rhs._socks5ProxySettings) &&
                _srvMaxHosts == rhs._srvMaxHosts &&
                _srvServiceName == rhs.SrvServiceName &&
                object.Equals(_sslSettings, rhs._sslSettings) &&
                _tracingOptions == rhs._tracingOptions &&
                _useTls == rhs._useTls &&
                _waitQueueSize == rhs._waitQueueSize &&
                _waitQueueTimeout == rhs._waitQueueTimeout;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
