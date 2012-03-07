/* Copyright 2010-2012 10gen Inc.
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
using System.Net.Sockets;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a MongoDB server.
    /// </summary>
    public class MongoServerSettings
    {
        // private fields
        private ConnectionMode _connectionMode;
        private TimeSpan _connectTimeout;
        private MongoCredentialsStore _credentialsStore;
        private MongoCredentials _defaultCredentials;
        private GuidRepresentation _guidRepresentation;
        private bool _ipv6;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private string _replicaSetName;
        private SafeMode _safeMode;
        private IEnumerable<MongoServerAddress> _servers;
        private bool _slaveOk;
        private TimeSpan _socketTimeout;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoServerSettings. Usually you would use a connection string instead.
        /// </summary>
        public MongoServerSettings()
        {
            _connectionMode = ConnectionMode.Direct;
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _credentialsStore = new MongoCredentialsStore();
            _defaultCredentials = null;
            _guidRepresentation = MongoDefaults.GuidRepresentation;
            _ipv6 = false;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _replicaSetName = null;
            _safeMode = MongoDefaults.SafeMode;
            _servers = null;
            _slaveOk = false;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
        }

        /// <summary>
        /// Creates a new instance of MongoServerSettings. Usually you would use a connection string instead.
        /// </summary>
        /// <param name="connectionMode">The connection mode (Direct or ReplicaSet).</param>
        /// <param name="connectTimeout">The connect timeout.</param>
        /// <param name="credentialsStore">The credentials store.</param>
        /// <param name="defaultCredentials">The default credentials.</param>
        /// <param name="guidRepresentation">The representation for Guids.</param>
        /// <param name="ipv6">Whether to use IPv6.</param>
        /// <param name="maxConnectionIdleTime">The max connection idle time.</param>
        /// <param name="maxConnectionLifeTime">The max connection life time.</param>
        /// <param name="maxConnectionPoolSize">The max connection pool size.</param>
        /// <param name="minConnectionPoolSize">The min connection pool size.</param>
        /// <param name="replicaSetName">The name of the replica set.</param>
        /// <param name="safeMode">The safe mode.</param>
        /// <param name="servers">The server addresses (normally one unless it is the seed list for connecting to a replica set).</param>
        /// <param name="slaveOk">Whether queries should be sent to secondary servers.</param>
        /// <param name="socketTimeout">The socket timeout.</param>
        /// <param name="waitQueueSize">The wait queue size.</param>
        /// <param name="waitQueueTimeout">The wait queue timeout.</param>
        public MongoServerSettings(
            ConnectionMode connectionMode,
            TimeSpan connectTimeout,
            MongoCredentialsStore credentialsStore,
            MongoCredentials defaultCredentials,
            GuidRepresentation guidRepresentation,
            bool ipv6,
            TimeSpan maxConnectionIdleTime,
            TimeSpan maxConnectionLifeTime,
            int maxConnectionPoolSize,
            int minConnectionPoolSize,
            string replicaSetName,
            SafeMode safeMode,
            IEnumerable<MongoServerAddress> servers,
            bool slaveOk,
            TimeSpan socketTimeout,
            int waitQueueSize,
            TimeSpan waitQueueTimeout)
        {
            _connectionMode = connectionMode;
            _connectTimeout = connectTimeout;
            _credentialsStore = credentialsStore ?? new MongoCredentialsStore();
            _defaultCredentials = defaultCredentials;
            _guidRepresentation = guidRepresentation;
            _ipv6 = ipv6;
            _maxConnectionIdleTime = maxConnectionIdleTime;
            _maxConnectionLifeTime = maxConnectionLifeTime;
            _maxConnectionPoolSize = maxConnectionPoolSize;
            _minConnectionPoolSize = minConnectionPoolSize;
            _replicaSetName = replicaSetName;
            _safeMode = safeMode;
            _servers = servers;
            _slaveOk = slaveOk;
            _socketTimeout = socketTimeout;
            _waitQueueSize = waitQueueSize;
            _waitQueueTimeout = waitQueueTimeout;
        }

        // public properties
        /// <summary>
        /// Gets the AddressFamily for the IPEndPoint (derived from the IPv6 setting).
        /// </summary>
        public AddressFamily AddressFamily
        {
            get { return _ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork; }
        }

        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _connectionMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the credentials store.
        /// </summary>
        public MongoCredentialsStore CredentialsStore
        {
            get { return _credentialsStore; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _credentialsStore = value;
            }
        }

        /// <summary>
        /// Gets or sets the default credentials.
        /// </summary>
        public MongoCredentials DefaultCredentials
        {
            get { return _defaultCredentials; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _defaultCredentials = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        public GuidRepresentation GuidRepresentation
        {
            get { return _guidRepresentation; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _guidRepresentation = value;
            }
        }

        /// <summary>
        /// Gets whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _ipv6 = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection idle time.
        /// </summary>
        public TimeSpan MaxConnectionIdleTime
        {
            get { return _maxConnectionIdleTime; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _maxConnectionIdleTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection life time.
        /// </summary>
        public TimeSpan MaxConnectionLifeTime
        {
            get { return _maxConnectionLifeTime; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _maxConnectionLifeTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the max connection pool size.
        /// </summary>
        public int MaxConnectionPoolSize
        {
            get { return _maxConnectionPoolSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _maxConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the min connection pool size.
        /// </summary>
        public int MinConnectionPoolSize
        {
            get { return _minConnectionPoolSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the SafeMode to use.
        /// </summary>
        public SafeMode SafeMode
        {
            get { return _safeMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (_servers == null) ? null : _servers.Single(); }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _servers = new MongoServerAddress[] { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return _servers; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _servers = value;
            }
        }

        /// <summary>
        /// Gets or sets whether queries should be sent to secondary servers.
        /// </summary>
        public bool SlaveOk
        {
            get { return _slaveOk; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _slaveOk = value;
            }
        }

        /// <summary>
        /// Gets or sets the socket timeout.
        /// </summary>
        public TimeSpan SocketTimeout
        {
            get { return _socketTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _waitQueueSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue timeout.
        /// </summary>
        public TimeSpan WaitQueueTimeout
        {
            get { return _waitQueueTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _waitQueueTimeout = value;
            }
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoServerSettings Clone()
        {
            return new MongoServerSettings(_connectionMode, _connectTimeout, _credentialsStore.Clone(), _defaultCredentials,
                _guidRepresentation, _ipv6, _maxConnectionIdleTime, _maxConnectionLifeTime, _maxConnectionPoolSize,
                _minConnectionPoolSize, _replicaSetName, _safeMode, _servers, _slaveOk, _socketTimeout,
                _waitQueueSize, _waitQueueTimeout);
        }

        /// <summary>
        /// Compares two MongoServerSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as MongoServerSettings;
            if (rhs == null)
            {
                return false;
            }
            else
            {
                if (_isFrozen && rhs._isFrozen)
                {
                    return _frozenStringRepresentation == rhs._frozenStringRepresentation;
                }
                else
                {
                    return
                        _connectionMode == rhs._connectionMode &&
                        _connectTimeout == rhs._connectTimeout &&
                        _credentialsStore.Equals(rhs._credentialsStore) &&
                        _defaultCredentials == rhs._defaultCredentials &&
                        _guidRepresentation == rhs._guidRepresentation &&
                        _ipv6 == rhs._ipv6 &&
                        _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                        _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                        _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                        _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                        _replicaSetName == rhs._replicaSetName &&
                        _safeMode == rhs._safeMode &&
                        (_servers == null && rhs._servers == null || _servers.SequenceEqual(rhs._servers)) &&
                        _slaveOk == rhs._slaveOk &&
                        _socketTimeout == rhs._socketTimeout &&
                        _waitQueueSize == rhs._waitQueueSize &&
                        _waitQueueTimeout == rhs._waitQueueTimeout;
                }
            }
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoServerSettings Freeze()
        {
            if (!_isFrozen)
            {
                _credentialsStore.Freeze();
                _safeMode = _safeMode.FrozenCopy();
                _frozenHashCode = GetHashCode();
                _frozenStringRepresentation = ToString();
                _isFrozen = true;
            }
            return this;
        }

        /// <summary>
        /// Returns a frozen copy of the settings.
        /// </summary>
        /// <returns>A frozen copy of the settings.</returns>
        public MongoServerSettings FrozenCopy()
        {
            if (_isFrozen)
            {
                return this;
            }
            else
            {
                return Clone().Freeze();
            }
        }

        /// <summary>
        /// Gets credentials for a particular database.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        /// <returns>The credentials for that database (or null).</returns>
        public MongoCredentials GetCredentials(string databaseName)
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }

            MongoCredentials credentials;
            if (_credentialsStore.TryGetCredentials(databaseName, out credentials))
            {
                return credentials;
            }

            if (databaseName == "admin" && _defaultCredentials != null && _defaultCredentials.Admin)
            {
                return _defaultCredentials;
            }
            if (databaseName != "admin")
            {
                return _defaultCredentials;
            }

            return null;
        }

        /// <summary>
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            // see Effective Java by Joshua Bloch
            int hash = 17;
            hash = 37 * hash + _connectionMode.GetHashCode();
            hash = 37 * hash + _connectTimeout.GetHashCode();
            hash = 37 * hash + _credentialsStore.GetHashCode();
            hash = 37 * hash + (_defaultCredentials == null ? 0 : _defaultCredentials.GetHashCode());
            hash = 37 * hash + _guidRepresentation.GetHashCode();
            hash = 37 * hash + _ipv6.GetHashCode();
            hash = 37 * hash + _maxConnectionIdleTime.GetHashCode();
            hash = 37 * hash + _maxConnectionLifeTime.GetHashCode();
            hash = 37 * hash + _maxConnectionPoolSize.GetHashCode();
            hash = 37 * hash + _minConnectionPoolSize.GetHashCode();
            hash = 37 * hash + (_replicaSetName == null ? 0 : _replicaSetName.GetHashCode());
            hash = 37 * hash + (_safeMode == null ? 0 : _safeMode.GetHashCode());
            if (_servers != null)
            {
                foreach (var server in _servers)
                {
                    hash = 37 * hash + server.GetHashCode();
                }
            }
            hash = 37 * hash + _slaveOk.GetHashCode();
            hash = 37 * hash + _socketTimeout.GetHashCode();
            hash = 37 * hash + _waitQueueSize.GetHashCode();
            hash = 37 * hash + _waitQueueTimeout.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string representation of the settings.
        /// </summary>
        /// <returns>A string representation of the settings.</returns>
        public override string ToString()
        {
            if (_isFrozen)
            {
                return _frozenStringRepresentation;
            }

            var sb = new StringBuilder();
            string serversString = null;
            if (_servers != null)
            {
                serversString = string.Join(",", _servers.Select(s => s.ToString()).ToArray());
            }
            sb.AppendFormat("ConnectionMode={0};", _connectionMode);
            sb.AppendFormat("ConnectTimeout={0};", _connectTimeout);
            sb.AppendFormat("Credentials={{{0}}};", _credentialsStore);
            sb.AppendFormat("DefaultCredentials={0};", _defaultCredentials);
            sb.AppendFormat("GuidRepresentation={0};", _guidRepresentation);
            sb.AppendFormat("IPv6={0};", _ipv6);
            sb.AppendFormat("MaxConnectionIdleTime={0};", _maxConnectionIdleTime);
            sb.AppendFormat("MaxConnectionLifeTime={0};", _maxConnectionLifeTime);
            sb.AppendFormat("MaxConnectionPoolSize={0};", _maxConnectionPoolSize);
            sb.AppendFormat("MinConnectionPoolSize={0};", _minConnectionPoolSize);
            sb.AppendFormat("ReplicaSetName={0};", _replicaSetName);
            sb.AppendFormat("SafeMode={0};", _safeMode);
            sb.AppendFormat("Servers={0};", serversString);
            sb.AppendFormat("SlaveOk={0};", _slaveOk);
            sb.AppendFormat("SocketTimeout={0};", _socketTimeout);
            sb.AppendFormat("WaitQueueSize={0};", _waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", _waitQueueTimeout);
            return sb.ToString();
        }
    }
}
