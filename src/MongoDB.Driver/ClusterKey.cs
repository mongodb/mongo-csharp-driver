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
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Encryption;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    internal class ClusterKey
    {
        // fields
        private readonly bool _allowInsecureTls;
        private readonly string _applicationName;
        private readonly Action<ClusterBuilder> _clusterConfigurator;
        private readonly IReadOnlyList<CompressorConfiguration> _compressors;
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly ConnectionMode _connectionMode;
        private readonly ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private readonly TimeSpan _connectTimeout;
        private readonly IReadOnlyList<MongoCredential> _credentials;
        private readonly bool? _directConnection;
        private readonly int _hashCode;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _heartbeatTimeout;
        private readonly bool _ipv6;
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders;
        private readonly TimeSpan _localThreshold;
        private readonly TimeSpan _maxConnectionIdleTime;
        private readonly TimeSpan _maxConnectionLifeTime;
        private readonly int _maxConnectionPoolSize;
        private readonly int _minConnectionPoolSize;
        private readonly int _receiveBufferSize;
        private readonly string _replicaSetName;
        private readonly IReadOnlyDictionary<string, BsonDocument> _schemaMap;
        private readonly ConnectionStringScheme _scheme;
        private readonly string _sdamLogFilename;
        private readonly int _sendBufferSize;
        private readonly ServerApi _serverApi;
        private readonly IReadOnlyList<MongoServerAddress> _servers;
        private readonly TimeSpan _serverSelectionTimeout;
        private readonly TimeSpan _socketTimeout;
        private readonly SslSettings _sslSettings;
        private readonly bool _useTls;
        private readonly int _waitQueueSize;
        private readonly TimeSpan _waitQueueTimeout;

        // constructors
        public ClusterKey(
            bool allowInsecureTls,
            string applicationName,
            Action<ClusterBuilder> clusterConfigurator,
            IReadOnlyList<CompressorConfiguration> compressors,
#pragma warning disable CS0618 // Type or member is obsolete
            ConnectionMode connectionMode,
            ConnectionModeSwitch connectionModeSwitch,
#pragma warning restore CS0618 // Type or member is obsolete
            TimeSpan connectTimeout,
            IReadOnlyList<MongoCredential> credentials,
            bool? directConnection,
            TimeSpan heartbeatInterval,
            TimeSpan heartbeatTimeout,
            bool ipv6,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> kmsProviders,
            TimeSpan localThreshold,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            int receiveBufferSize,
            string replicaSetName,
            IReadOnlyDictionary<string, BsonDocument> schemaMap,
            ConnectionStringScheme scheme,
            string sdamLogFilename,
            int sendBufferSize,
            ServerApi serverApi,
            IReadOnlyList<MongoServerAddress> servers,
            TimeSpan serverSelectionTimeout,
            TimeSpan socketTimeout,
            SslSettings sslSettings,
            bool useTls,
            int waitQueueSize,
            TimeSpan waitQueueTimeout)
        {
            ConnectionModeHelper.EnsureConnectionModeValuesAreValid(connectionMode, connectionModeSwitch, directConnection);

            _allowInsecureTls = allowInsecureTls;
            _applicationName = applicationName;
            _clusterConfigurator = clusterConfigurator;
            _compressors = compressors;
            _connectionMode = connectionMode;
            _connectionModeSwitch = connectionModeSwitch;
            _connectTimeout = connectTimeout;
            _credentials = credentials;
            _directConnection = directConnection;
            _heartbeatInterval = heartbeatInterval;
            _heartbeatTimeout = heartbeatTimeout;
            _ipv6 = ipv6;
            _kmsProviders = kmsProviders;
            _localThreshold = localThreshold;
            _maxConnectionIdleTime = maxConnectionIdleTime;
            _maxConnectionLifeTime = maxConnectionLifeTime;
            _maxConnectionPoolSize = maxConnectionPoolSize;
            _minConnectionPoolSize = minConnectionPoolSize;
            _receiveBufferSize = receiveBufferSize;
            _replicaSetName = replicaSetName;
            _schemaMap = schemaMap;
            _scheme = scheme;
            _sdamLogFilename = sdamLogFilename;
            _sendBufferSize = sendBufferSize;
            _serverApi = serverApi;
            _servers = servers;
            _serverSelectionTimeout = serverSelectionTimeout;
            _socketTimeout = socketTimeout;
            _sslSettings = sslSettings;
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
        [Obsolete("Use DirectConnection instead.")]
        public ConnectionMode ConnectionMode
        {
            get
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }
                return _connectionMode;
            }
        }
        [Obsolete("This property will be removed in a later release.")]
        public ConnectionModeSwitch ConnectionModeSwitch => _connectionModeSwitch;
        public TimeSpan ConnectTimeout { get { return _connectTimeout; } }
        public IReadOnlyList<MongoCredential> Credentials { get { return _credentials; } }
        public bool? DirectConnection
        {
            get
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }
                return _directConnection;
            }
        }
        public TimeSpan HeartbeatInterval { get { return _heartbeatInterval; } }
        public TimeSpan HeartbeatTimeout { get { return _heartbeatTimeout; } }
        public bool IPv6 { get { return _ipv6; } }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, object>> KmsProviders { get { return _kmsProviders; } }
        public TimeSpan LocalThreshold { get { return _localThreshold; } }
        public TimeSpan MaxConnectionIdleTime { get { return _maxConnectionIdleTime; } }
        public TimeSpan MaxConnectionLifeTime { get { return _maxConnectionLifeTime; } }
        public int MaxConnectionPoolSize { get { return _maxConnectionPoolSize; } }
        public int MinConnectionPoolSize { get { return _minConnectionPoolSize; } }
        public int ReceiveBufferSize { get { return _receiveBufferSize; } }
        public string ReplicaSetName { get { return _replicaSetName; } }
        public IReadOnlyDictionary<string, BsonDocument> SchemaMap { get { return _schemaMap; } }
        public ConnectionStringScheme Scheme { get { return _scheme; } }
        public string SdamLogFilename { get { return _sdamLogFilename; } }
        public int SendBufferSize { get { return _sendBufferSize; } }
        public ServerApi ServerApi { get { return _serverApi; } }
        public IReadOnlyList<MongoServerAddress> Servers { get { return _servers; } }
        public TimeSpan ServerSelectionTimeout { get { return _serverSelectionTimeout; } }
        public TimeSpan SocketTimeout { get { return _socketTimeout; } }
        public SslSettings SslSettings { get { return _sslSettings; } }
        public bool UseTls => _useTls;
        public int WaitQueueSize { get { return _waitQueueSize; } }
        public TimeSpan WaitQueueTimeout { get { return _waitQueueTimeout; } }

        // methods
        private int CalculateHashCode()
        {
            // keep calculation simple (leave out fields that are rarely used)
            return new Hasher()
                .HashElements(_credentials)
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
                _connectionMode == rhs._connectionMode &&
                _connectionModeSwitch == rhs._connectionModeSwitch &&
                _connectTimeout == rhs._connectTimeout &&
                _credentials.SequenceEqual(rhs._credentials) &&
                _directConnection.Equals(rhs._directConnection) &&
                _heartbeatInterval == rhs._heartbeatInterval &&
                _heartbeatTimeout == rhs._heartbeatTimeout &&
                _ipv6 == rhs._ipv6 &&
                KmsProvidersHelper.Equals(_kmsProviders, rhs.KmsProviders) &&
                _localThreshold == rhs._localThreshold &&
                _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                _receiveBufferSize == rhs._receiveBufferSize &&
                _replicaSetName == rhs._replicaSetName &&
                _schemaMap.IsEquivalentTo(rhs._schemaMap, object.Equals) &&
                _scheme == rhs._scheme &&
                _sdamLogFilename == rhs._sdamLogFilename &&
                _sendBufferSize == rhs._sendBufferSize &&
                _serverApi == rhs._serverApi &&
                _servers.SequenceEqual(rhs._servers) &&
                _serverSelectionTimeout == rhs._serverSelectionTimeout &&
                _socketTimeout == rhs._socketTimeout &&
                object.Equals(_sslSettings, rhs._sslSettings) &&
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
