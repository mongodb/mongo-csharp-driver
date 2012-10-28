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
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using MongoDB.Bson;
using MongoDB.Driver.Internal;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings for a MongoDB client.
    /// </summary>
    public class MongoClientSettings
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
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private TimeSpan _secondaryAcceptableLatency;
        private List<MongoServerAddress> _servers;
        private TimeSpan _socketTimeout;
        private bool _useSsl;
        private bool _verifySslCertificate;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private WriteConcern _writeConcern;

        // the following fields are set when Freeze is called
        private bool _isFrozen;
        private int _frozenHashCode;
        private string _frozenStringRepresentation;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoClientSettings. Usually you would use a connection string instead.
        /// </summary>
        public MongoClientSettings()
        {
            _connectionMode = ConnectionMode.Automatic;
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _credentialsStore = new MongoCredentialsStore();
            _defaultCredentials = null;
            _guidRepresentation = MongoDefaults.GuidRepresentation;
            _ipv6 = false;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _readPreference = ReadPreference.Primary;
            _replicaSetName = null;
            _secondaryAcceptableLatency = MongoDefaults.SecondaryAcceptableLatency;
            _servers = new List<MongoServerAddress> { new MongoServerAddress("localhost") };
            _socketTimeout = MongoDefaults.SocketTimeout;
            _useSsl = false;
            _verifySslCertificate = true;
            _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _writeConcern = WriteConcern.Errors;
        }

        // public properties
        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
        public ConnectionMode ConnectionMode
        {
            get { return _connectionMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the read preferences.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get { return _readPreference; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _readPreference = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _replicaSetName = value;
            }
        }

        /// <summary>
        /// Gets or sets the acceptable latency for considering a replica set member for inclusion in load balancing
        /// when using a read preference of Secondary, SecondaryPreferred, and Nearest.
        /// </summary>
        public TimeSpan SecondaryAcceptableLatency
        {
            get { return _secondaryAcceptableLatency; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _secondaryAcceptableLatency = value;
            }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return _servers.Single(); }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _servers = new List<MongoServerAddress> { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return new ReadOnlyCollection<MongoServerAddress>(_servers); }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _servers = new List<MongoServerAddress>(value);
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _useSsl = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _verifySslCertificate = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _waitQueueTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the WriteConcern to use.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern = value;
            }
        }

        // public static methods
        /// <summary>
        /// Gets a MongoClientSettings object intialized with values from a connection string builder.
        /// </summary>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromConnectionStringBuilder(MongoConnectionStringBuilder builder)
        {
            MongoCredentials defaultCredentials = null;
            if (builder.Username != null && builder.Password != null)
            {
                defaultCredentials = new MongoCredentials(builder.Username, builder.Password);
            }

            var clientSettings = new MongoClientSettings();
            clientSettings.ConnectionMode = builder.ConnectionMode;
            clientSettings.ConnectTimeout = builder.ConnectTimeout;
            clientSettings.CredentialsStore = new MongoCredentialsStore();
            clientSettings.DefaultCredentials = defaultCredentials;
            clientSettings.GuidRepresentation = builder.GuidRepresentation;
            clientSettings.IPv6 = builder.IPv6;
            clientSettings.MaxConnectionIdleTime = builder.MaxConnectionIdleTime;
            clientSettings.MaxConnectionLifeTime = builder.MaxConnectionLifeTime;
            clientSettings.MaxConnectionPoolSize = builder.MaxConnectionPoolSize;
            clientSettings.MinConnectionPoolSize = builder.MinConnectionPoolSize;
            clientSettings.ReadPreference = (builder.ReadPreference == null) ? ReadPreference.Primary : builder.ReadPreference.Clone();
            clientSettings.ReplicaSetName = builder.ReplicaSetName;
            clientSettings.SecondaryAcceptableLatency = builder.SecondaryAcceptableLatency;
            clientSettings.Servers = new List<MongoServerAddress>(builder.Servers);
            clientSettings.SocketTimeout = builder.SocketTimeout;
            clientSettings.UseSsl = builder.UseSsl;
            clientSettings.VerifySslCertificate = builder.VerifySslCertificate;
            clientSettings.WaitQueueSize = builder.ComputedWaitQueueSize;
            clientSettings.WaitQueueTimeout = builder.WaitQueueTimeout;
            clientSettings.WriteConcern = builder.GetWriteConcern(false); // fireAndForget default for MongoClientSettings is false
            return clientSettings;
        }

        /// <summary>
        /// Gets a MongoClientSettings object intialized with values from a URL.
        /// </summary>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromUrl(MongoUrl url)
        {
            var clientSettings = new MongoClientSettings();
            clientSettings.ConnectionMode = url.ConnectionMode;
            clientSettings.ConnectTimeout = url.ConnectTimeout;
            clientSettings.CredentialsStore = new MongoCredentialsStore();
            clientSettings.DefaultCredentials = url.DefaultCredentials;
            clientSettings.GuidRepresentation = url.GuidRepresentation;
            clientSettings.IPv6 = url.IPv6;
            clientSettings.MaxConnectionIdleTime = url.MaxConnectionIdleTime;
            clientSettings.MaxConnectionLifeTime = url.MaxConnectionLifeTime;
            clientSettings.MaxConnectionPoolSize = url.MaxConnectionPoolSize;
            clientSettings.MinConnectionPoolSize = url.MinConnectionPoolSize;
            clientSettings.ReadPreference = (url.ReadPreference == null) ? ReadPreference.Primary : url.ReadPreference;
            clientSettings.ReplicaSetName = url.ReplicaSetName;
            clientSettings.SecondaryAcceptableLatency = url.SecondaryAcceptableLatency;
            clientSettings.Servers = new List<MongoServerAddress>(url.Servers);
            clientSettings.SocketTimeout = url.SocketTimeout;
            clientSettings.UseSsl = url.UseSsl;
            clientSettings.VerifySslCertificate = url.VerifySslCertificate;
            clientSettings.WaitQueueSize = url.ComputedWaitQueueSize;
            clientSettings.WaitQueueTimeout = url.WaitQueueTimeout;
            clientSettings.WriteConcern = url.GetWriteConcern(false); // fireAndForget default for MongoClientSettings is false
            return clientSettings;
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoClientSettings Clone()
        {
            var clone = new MongoClientSettings();
            clone._connectionMode = _connectionMode;
            clone._connectTimeout = _connectTimeout;
            clone._credentialsStore = _credentialsStore.Clone();
            clone._defaultCredentials = _defaultCredentials;
            clone._guidRepresentation = _guidRepresentation;
            clone._ipv6 = _ipv6;
            clone._maxConnectionIdleTime = _maxConnectionIdleTime;
            clone._maxConnectionLifeTime = _maxConnectionLifeTime;
            clone._maxConnectionPoolSize = _maxConnectionPoolSize;
            clone._minConnectionPoolSize = _minConnectionPoolSize;
            clone._readPreference = _readPreference.Clone();
            clone._replicaSetName = _replicaSetName;
            clone._secondaryAcceptableLatency = _secondaryAcceptableLatency;
            clone._servers = new List<MongoServerAddress>(_servers);
            clone._socketTimeout = _socketTimeout;
            clone._useSsl = _useSsl;
            clone._verifySslCertificate = _verifySslCertificate;
            clone._waitQueueSize = _waitQueueSize;
            clone._waitQueueTimeout = _waitQueueTimeout;
            clone._writeConcern = _writeConcern.Clone();
            return clone;
        }

        /// <summary>
        /// Compares two MongoClientSettings instances.
        /// </summary>
        /// <param name="obj">The other instance.</param>
        /// <returns>True if the two instances are equal.</returns>
        public override bool Equals(object obj)
        {
            var rhs = obj as MongoClientSettings;
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
                        _readPreference == rhs._readPreference &&
                        _replicaSetName == rhs._replicaSetName &&
                        _secondaryAcceptableLatency == rhs._secondaryAcceptableLatency &&
                        _servers.SequenceEqual(rhs._servers) &&
                        _socketTimeout == rhs._socketTimeout &&
                        _useSsl == rhs._useSsl &&
                        _verifySslCertificate == rhs._verifySslCertificate &&
                        _waitQueueSize == rhs._waitQueueSize &&
                        _waitQueueTimeout == rhs._waitQueueTimeout &&
                        _writeConcern == rhs._writeConcern;
                }
            }
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoClientSettings Freeze()
        {
            if (!_isFrozen)
            {
                _credentialsStore.Freeze();
                _readPreference = _readPreference.FrozenCopy();
                _writeConcern = _writeConcern.FrozenCopy();
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
        public MongoClientSettings FrozenCopy()
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
            hash = 37 * hash + ((_defaultCredentials == null) ? 0 : _defaultCredentials.GetHashCode());
            hash = 37 * hash + _guidRepresentation.GetHashCode();
            hash = 37 * hash + _ipv6.GetHashCode();
            hash = 37 * hash + _maxConnectionIdleTime.GetHashCode();
            hash = 37 * hash + _maxConnectionLifeTime.GetHashCode();
            hash = 37 * hash + _maxConnectionPoolSize.GetHashCode();
            hash = 37 * hash + _minConnectionPoolSize.GetHashCode();
            hash = 37 * hash + _readPreference.GetHashCode();
            hash = 37 * hash + ((_replicaSetName == null) ? 0 : _replicaSetName.GetHashCode());
            hash = 37 * hash + _secondaryAcceptableLatency.GetHashCode();
            foreach (var server in _servers)
            {
                hash = 37 * hash + server.GetHashCode();
            }
            hash = 37 * hash + _socketTimeout.GetHashCode();
            hash = 37 * hash + _useSsl.GetHashCode();
            hash = 37 * hash + _verifySslCertificate.GetHashCode();
            hash = 37 * hash + _waitQueueSize.GetHashCode();
            hash = 37 * hash + _waitQueueTimeout.GetHashCode();
            hash = 37 * hash + _writeConcern.GetHashCode();
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
            sb.AppendFormat("ReadPreference={0};", _readPreference);
            sb.AppendFormat("ReplicaSetName={0};", _replicaSetName);
            sb.AppendFormat("SecondaryAcceptableLatency={0};", _secondaryAcceptableLatency);
            sb.AppendFormat("Servers={0};", string.Join(",", _servers.Select(s => s.ToString()).ToArray()));
            sb.AppendFormat("SocketTimeout={0};", _socketTimeout);
            sb.AppendFormat("Ssl={0};", _useSsl);
            sb.AppendFormat("SslVerifyCertificate={0};", _verifySslCertificate);
            sb.AppendFormat("WaitQueueSize={0};", _waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", _waitQueueTimeout);
            sb.AppendFormat("WriteConcern={0};", _writeConcern);
            return sb.ToString();
        }
    }
}
