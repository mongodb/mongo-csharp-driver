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

using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Shared;

namespace MongoDB.Driver.Communication
{
    internal class ClusterKey
    {
        #region static
        // static fields
        private static readonly TimeSpan __defaultHeartbeatInterval;
        private static readonly TimeSpan __defaultHeartbeatTimeout;
        private static readonly int __defaultReceiveBufferSize;
        private static readonly int __defaultSendBufferSize;

        // static constructor
        static ClusterKey()
        {
            var defaultServerSettings = new ServerSettings();
            __defaultHeartbeatInterval = defaultServerSettings.HeartbeatTimeout;
            __defaultHeartbeatTimeout = defaultServerSettings.HeartbeatTimeout;

            var defaultTcpStreamSettings = new TcpStreamSettings();
            __defaultReceiveBufferSize = defaultTcpStreamSettings.ReceiveBufferSize;
            __defaultSendBufferSize = defaultTcpStreamSettings.SendBufferSize;
        }
        #endregion

        // fields
        private readonly ConnectionMode _connectionMode;
        private readonly TimeSpan _connectTimeout;
        private readonly IReadOnlyList<MongoCredential> _credentials;
        private readonly int _hashCode;
        private readonly TimeSpan _heartbeatInterval;
        private readonly TimeSpan _heartbeatTimeout;
        private readonly bool _ipv6;
        private readonly TimeSpan _maxConnectionIdleTime;
        private readonly TimeSpan _maxConnectionLifeTime;
        private readonly int _maxConnectionPoolSize;
        private readonly int _minConnectionPoolSize;
        private readonly int _receiveBufferSize;
        private readonly string _replicaSetName;
        private readonly int _sendBufferSize;
        private readonly IReadOnlyList<MongoServerAddress> _servers;
        private readonly TimeSpan _socketTimeout;
        private readonly SslSettings _sslSettings;
        private readonly bool _useSsl;
        private readonly bool _verifySslCertificate;
        private readonly int _waitQueueSize;
        private readonly TimeSpan _waitQueueTimeout;

        // constructors
        public ClusterKey(MongoClientSettings clientSettings)
        {
            _connectionMode = clientSettings.ConnectionMode;
            _connectTimeout = clientSettings.ConnectTimeout;
            _credentials = clientSettings.Credentials.ToList();
            _heartbeatInterval = __defaultHeartbeatInterval; // TODO: add HeartbeatInterval to MongoClientSettings?
            _heartbeatTimeout = __defaultHeartbeatTimeout; // TODO: add HeartbeatTimeout to MongoClientSettings?
            _ipv6 = clientSettings.IPv6;
            _maxConnectionIdleTime = clientSettings.MaxConnectionIdleTime;
            _maxConnectionLifeTime = clientSettings.MaxConnectionLifeTime;
            _maxConnectionPoolSize = clientSettings.MaxConnectionPoolSize;
            _minConnectionPoolSize = clientSettings.MinConnectionPoolSize;
            _receiveBufferSize = __defaultReceiveBufferSize; // TODO: add ReceiveBufferSize to MongoClientSettings?
            _replicaSetName = clientSettings.ReplicaSetName;
            _sendBufferSize = __defaultSendBufferSize; // TODO: add SendBufferSize to MongoClientSettings?
            _servers = clientSettings.Servers.ToList();
            _socketTimeout = clientSettings.SocketTimeout;
            _sslSettings = clientSettings.SslSettings;
            _useSsl = clientSettings.UseSsl;
            _verifySslCertificate = clientSettings.VerifySslCertificate;
            _waitQueueSize = clientSettings.WaitQueueSize;
            _waitQueueTimeout = clientSettings.WaitQueueTimeout;
            _hashCode = CalculateHashCode();
        }

        public ClusterKey(MongoServerSettings serverSettings)
        {
            _connectionMode = serverSettings.ConnectionMode;
            _connectTimeout = serverSettings.ConnectTimeout;
            _credentials = serverSettings.Credentials.ToList();
            _heartbeatInterval = __defaultHeartbeatInterval; // TODO: add HeartbeatInterval to MongoServerSettings?
            _heartbeatTimeout = __defaultHeartbeatTimeout; // TODO: add HeartbeatTimeout to MongoServerSettings?
            _ipv6 = serverSettings.IPv6;
            _maxConnectionIdleTime = serverSettings.MaxConnectionIdleTime;
            _maxConnectionLifeTime = serverSettings.MaxConnectionLifeTime;
            _maxConnectionPoolSize = serverSettings.MaxConnectionPoolSize;
            _minConnectionPoolSize = serverSettings.MinConnectionPoolSize;
            _receiveBufferSize = __defaultReceiveBufferSize; // TODO: add ReceiveBufferSize to MongoServerSettings?
            _replicaSetName = serverSettings.ReplicaSetName;
            _sendBufferSize = __defaultSendBufferSize; // TODO: add SendBufferSize to MongoServerSettings?
            _servers = serverSettings.Servers.ToList();
            _socketTimeout = serverSettings.SocketTimeout;
            _sslSettings = serverSettings.SslSettings;
            _useSsl = serverSettings.UseSsl;
            _verifySslCertificate = serverSettings.VerifySslCertificate;
            _waitQueueSize = serverSettings.WaitQueueSize;
            _waitQueueTimeout = serverSettings.WaitQueueTimeout;
            _hashCode = CalculateHashCode();
        }

        // properties
        public ConnectionMode ConnectionMode { get { return _connectionMode; } }
        public TimeSpan ConnectTimeout { get { return _connectTimeout; } }
        public IReadOnlyList<MongoCredential> Credentials { get { return _credentials; } }
        public TimeSpan HeartbeatInterval { get { return _heartbeatInterval; } }
        public TimeSpan HeartbeatTimeout { get { return _heartbeatTimeout; } }
        public bool IPv6 { get { return _ipv6; } }
        public TimeSpan MaxConnectionIdleTime { get { return _maxConnectionIdleTime; } }
        public TimeSpan MaxConnectionLifeTime { get { return _maxConnectionLifeTime; } }
        public int MaxConnectionPoolSize { get { return _maxConnectionPoolSize; } }
        public int MinConnectionPoolSize { get { return _minConnectionPoolSize; } }
        public int ReceiveBufferSize { get { return _receiveBufferSize; } }
        public string ReplicaSetName { get { return _replicaSetName; } }
        public int SendBufferSize { get { return _sendBufferSize; } }
        public IReadOnlyList<MongoServerAddress> Servers { get { return _servers; } }
        public TimeSpan SocketTimeout { get { return _socketTimeout; } }
        public SslSettings SslSettings { get { return _sslSettings; } }
        public bool UseSsl { get { return _useSsl; } }
        public bool VerifySslCertificate { get { return _verifySslCertificate; } }
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
                _connectionMode == rhs._connectionMode &&
                _connectTimeout == rhs._connectTimeout &&
                _credentials.SequenceEqual(rhs._credentials) &&
                _heartbeatInterval == rhs._heartbeatInterval &&
                _heartbeatTimeout == rhs._heartbeatTimeout &&
                _ipv6 == rhs._ipv6 &&
                _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                _receiveBufferSize == rhs._receiveBufferSize &&
                _replicaSetName == rhs._replicaSetName &&
                _sendBufferSize == rhs._sendBufferSize &&
                _servers.SequenceEqual(rhs._servers) &&
                _socketTimeout == rhs._socketTimeout &&
                object.Equals(_sslSettings, rhs._sslSettings) &&
                _useSsl == rhs._useSsl &&
                _verifySslCertificate == rhs._verifySslCertificate &&
                _waitQueueSize == rhs._waitQueueSize &&
                _waitQueueTimeout == rhs._waitQueueTimeout;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
