/* Copyright 2010-2016 MongoDB Inc.
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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings used to access a MongoDB server.
    /// </summary>
    public class MongoServerSettings : IEquatable<MongoServerSettings>, IInheritableMongoClientSettings
    {
        // private fields
        private string _applicationName;
        private Action<ClusterBuilder> _clusterConfigurator;
        private ConnectionMode _connectionMode;
        private TimeSpan _connectTimeout;
        private MongoCredentialStore _credentials;
        private GuidRepresentation _guidRepresentation;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _heartbeatTimeout;
        private bool _ipv6;
        private TimeSpan _localThreshold;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private TimeSpan _operationTimeout;
        private ReadConcern _readConcern;
        private UTF8Encoding _readEncoding;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private List<MongoServerAddress> _servers;
        private TimeSpan _serverSelectionTimeout;
        private TimeSpan _socketTimeout;
        private SslSettings _sslSettings;
        private bool _useSsl;
        private bool _verifySslCertificate;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private WriteConcern _writeConcern;
        private UTF8Encoding _writeEncoding;

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
            _applicationName = null;
            _connectionMode = ConnectionMode.Automatic;
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _credentials = new MongoCredentialStore(new MongoCredential[0]);
            _guidRepresentation = MongoDefaults.GuidRepresentation;
            _heartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
            _heartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
            _ipv6 = false;
            _localThreshold = MongoDefaults.LocalThreshold;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _operationTimeout = MongoDefaults.OperationTimeout;
            _readConcern = ReadConcern.Default;
            _readEncoding = null;
            _readPreference = ReadPreference.Primary;
            _replicaSetName = null;
            _servers = new List<MongoServerAddress> { new MongoServerAddress("localhost") };
            _serverSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _sslSettings = null;
            _useSsl = false;
            _verifySslCertificate = true;
            _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _writeConcern = WriteConcern.Unacknowledged;
            _writeEncoding = null;
        }

        // public properties
        /// <summary>
        /// Gets the AddressFamily for the IPEndPoint (derived from the IPv6 setting).
        /// </summary>
        [Obsolete("Use IPv6 instead.")]
        public AddressFamily AddressFamily
        {
            get { return _ipv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork; }
        }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _applicationName = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the cluster configurator.
        /// </summary>
        public Action<ClusterBuilder> ClusterConfigurator
        {
            get { return _clusterConfigurator; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _clusterConfigurator = value;
            }
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
        /// Gets or sets the credentials.
        /// </summary>
        public IEnumerable<MongoCredential> Credentials
        {
            get { return _credentials; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _credentials = new MongoCredentialStore(value);
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
        /// Gets or sets the heartbeat interval.
        /// </summary>
        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _heartbeatInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the heartbeat timeout.
        /// </summary>
        public TimeSpan HeartbeatTimeout
        {
            get { return _heartbeatTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _heartbeatTimeout = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the settings have been frozen to prevent further changes.
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use IPv6.
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
        /// Gets or sets the local threshold.
        /// </summary>
        public TimeSpan LocalThreshold
        {
            get { return _localThreshold; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _localThreshold = value;
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
        /// Gets or sets the operation timeout.
        /// </summary>
        public TimeSpan OperationTimeout
        {
            get { return _operationTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _operationTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the read concern.
        /// </summary>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _readConcern = Ensure.IsNotNull(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the Read Encoding.
        /// </summary>
        public UTF8Encoding ReadEncoding
        {
            get { return _readEncoding; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _readEncoding = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _replicaSetName = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _servers = new List<MongoServerAddress>(value);
            }
        }

        /// <summary>
        /// Gets or sets the server selection timeout.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">MongoServerSettings is frozen.</exception>
        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _serverSelectionTimeout = value;
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
        /// Gets or sets the SSL settings.
        /// </summary>
        public SslSettings SslSettings
        {
            get { return _sslSettings; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _sslSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL.
        /// </summary>
        public bool UseSsl
        {
            get { return _useSsl; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _useSsl = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to verify an SSL certificate.
        /// </summary>
        public bool VerifySslCertificate
        {
            get { return _verifySslCertificate; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
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

        /// <summary>
        /// Gets or sets the WriteConcern to use.
        /// </summary>
        public WriteConcern WriteConcern
        {
            get { return _writeConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _writeConcern = value;
            }
        }

        /// <summary>
        /// Gets or sets the Write Encoding.
        /// </summary>
        public UTF8Encoding WriteEncoding
        {
            get { return _writeEncoding; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoServerSettings is frozen."); }
                _writeEncoding = value;
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="MongoServerSettings"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(MongoServerSettings lhs, MongoServerSettings rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="MongoServerSettings"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(MongoServerSettings lhs, MongoServerSettings rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a new MongoServerSettings object from a MongoClientSettings object.
        /// </summary>
        /// <param name="clientSettings">The MongoClientSettings.</param>
        /// <returns>A MongoServerSettings.</returns>
        public static MongoServerSettings FromClientSettings(MongoClientSettings clientSettings)
        {
            var serverSettings = new MongoServerSettings();
            serverSettings.ApplicationName = clientSettings.ApplicationName;
            serverSettings.ClusterConfigurator = clientSettings.ClusterConfigurator;
            serverSettings.ConnectionMode = clientSettings.ConnectionMode;
            serverSettings.ConnectTimeout = clientSettings.ConnectTimeout;
            serverSettings.Credentials = clientSettings.Credentials;
            serverSettings.GuidRepresentation = clientSettings.GuidRepresentation;
            serverSettings.HeartbeatInterval = clientSettings.HeartbeatInterval;
            serverSettings.HeartbeatTimeout = clientSettings.HeartbeatTimeout;
            serverSettings.IPv6 = clientSettings.IPv6;
            serverSettings.MaxConnectionIdleTime = clientSettings.MaxConnectionIdleTime;
            serverSettings.MaxConnectionLifeTime = clientSettings.MaxConnectionLifeTime;
            serverSettings.MaxConnectionPoolSize = clientSettings.MaxConnectionPoolSize;
            serverSettings.MinConnectionPoolSize = clientSettings.MinConnectionPoolSize;
            serverSettings.ReadConcern = clientSettings.ReadConcern;
            serverSettings.ReadEncoding = clientSettings.ReadEncoding;
            serverSettings.ReadPreference = clientSettings.ReadPreference;
            serverSettings.ReplicaSetName = clientSettings.ReplicaSetName;
            serverSettings.LocalThreshold = clientSettings.LocalThreshold;
            serverSettings.Servers = new List<MongoServerAddress>(clientSettings.Servers);
            serverSettings.ServerSelectionTimeout = clientSettings.ServerSelectionTimeout;
            serverSettings.SocketTimeout = clientSettings.SocketTimeout;
            serverSettings.SslSettings = (clientSettings.SslSettings == null) ? null : clientSettings.SslSettings.Clone();
            serverSettings.UseSsl = clientSettings.UseSsl;
            serverSettings.VerifySslCertificate = clientSettings.VerifySslCertificate;
            serverSettings.WaitQueueSize = clientSettings.WaitQueueSize;
            serverSettings.WaitQueueTimeout = clientSettings.WaitQueueTimeout;
            serverSettings.WriteConcern = clientSettings.WriteConcern;
            serverSettings.WriteEncoding = clientSettings.WriteEncoding;
            return serverSettings;
        }

        /// <summary>
        /// Gets a MongoServerSettings object intialized with values from a MongoUrl.
        /// </summary>
        /// <param name="url">The MongoUrl.</param>
        /// <returns>A MongoServerSettings.</returns>
        public static MongoServerSettings FromUrl(MongoUrl url)
        {
            var credential = url.GetCredential();

            var serverSettings = new MongoServerSettings();
            serverSettings.ApplicationName = url.ApplicationName;
            serverSettings.ConnectionMode = url.ConnectionMode;
            serverSettings.ConnectTimeout = url.ConnectTimeout;
            if (credential != null)
            {
                foreach (var property in url.AuthenticationMechanismProperties)
                {
                    if (property.Key.Equals("CANONICALIZE_HOST_NAME", StringComparison.OrdinalIgnoreCase))
                    {
                        credential = credential.WithMechanismProperty(property.Key, bool.Parse(property.Value));
                    }
                    else
                    {
                        credential = credential.WithMechanismProperty(property.Key, property.Value);
                    }
                }
                serverSettings.Credentials = new[] { credential };
            }
            serverSettings.GuidRepresentation = url.GuidRepresentation;
            serverSettings.HeartbeatInterval = url.HeartbeatInterval;
            serverSettings.HeartbeatTimeout = url.HeartbeatTimeout;
            serverSettings.IPv6 = url.IPv6;
            serverSettings.MaxConnectionIdleTime = url.MaxConnectionIdleTime;
            serverSettings.MaxConnectionLifeTime = url.MaxConnectionLifeTime;
            serverSettings.MaxConnectionPoolSize = url.MaxConnectionPoolSize;
            serverSettings.MinConnectionPoolSize = url.MinConnectionPoolSize;
            serverSettings.ReadConcern = new ReadConcern(url.ReadConcernLevel);
            serverSettings.ReadEncoding = null; // ReadEncoding must be provided in code
            serverSettings.ReadPreference = (url.ReadPreference == null) ? ReadPreference.Primary : url.ReadPreference;
            serverSettings.ReplicaSetName = url.ReplicaSetName;
            serverSettings.LocalThreshold = url.LocalThreshold;
            serverSettings.Servers = new List<MongoServerAddress>(url.Servers);
            serverSettings.ServerSelectionTimeout = url.ServerSelectionTimeout;
            serverSettings.SocketTimeout = url.SocketTimeout;
            serverSettings.SslSettings = null; // SSL settings must be provided in code
            serverSettings.UseSsl = url.UseSsl;
            serverSettings.VerifySslCertificate = url.VerifySslCertificate;
            serverSettings.WaitQueueSize = url.ComputedWaitQueueSize;
            serverSettings.WaitQueueTimeout = url.WaitQueueTimeout;
            serverSettings.WriteConcern = url.GetWriteConcern(false);
            serverSettings.WriteEncoding = null; // WriteEncoding must be provided in code
            return serverSettings;
        }

        // public methods
        /// <summary>
        /// Creates a clone of the settings.
        /// </summary>
        /// <returns>A clone of the settings.</returns>
        public MongoServerSettings Clone()
        {
            var clone = new MongoServerSettings();
            clone._applicationName = _applicationName;
            clone._clusterConfigurator = _clusterConfigurator;
            clone._connectionMode = _connectionMode;
            clone._connectTimeout = _connectTimeout;
            clone._credentials = _credentials;
            clone._guidRepresentation = _guidRepresentation;
            clone._heartbeatInterval = _heartbeatInterval;
            clone._heartbeatTimeout = _heartbeatTimeout;
            clone._ipv6 = _ipv6;
            clone._maxConnectionIdleTime = _maxConnectionIdleTime;
            clone._maxConnectionLifeTime = _maxConnectionLifeTime;
            clone._maxConnectionPoolSize = _maxConnectionPoolSize;
            clone._minConnectionPoolSize = _minConnectionPoolSize;
            clone._operationTimeout = _operationTimeout;
            clone._readConcern = _readConcern;
            clone._readEncoding = _readEncoding;
            clone._readPreference = _readPreference;
            clone._replicaSetName = _replicaSetName;
            clone._localThreshold = _localThreshold;
            clone._servers = new List<MongoServerAddress>(_servers);
            clone._serverSelectionTimeout = _serverSelectionTimeout;
            clone._socketTimeout = _socketTimeout;
            clone._sslSettings = (_sslSettings == null) ? null : _sslSettings.Clone();
            clone._useSsl = _useSsl;
            clone._verifySslCertificate = _verifySslCertificate;
            clone._waitQueueSize = _waitQueueSize;
            clone._waitQueueTimeout = _waitQueueTimeout;
            clone._writeConcern = _writeConcern;
            clone._writeEncoding = _writeEncoding;
            return clone;
        }

        /// <summary>
        /// Determines whether the specified <see cref="MongoServerSettings" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="MongoServerSettings" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="MongoServerSettings" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(MongoServerSettings obj)
        {
            return Equals((object)obj); // handles obj == null correctly
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || GetType() != obj.GetType()) { return false; }
            var rhs = (MongoServerSettings)obj;
            return
                _applicationName == rhs._applicationName &&
                object.ReferenceEquals(_clusterConfigurator, rhs._clusterConfigurator) &&
               _connectionMode == rhs._connectionMode &&
               _connectTimeout == rhs._connectTimeout &&
               _credentials == rhs._credentials &&
               _guidRepresentation == rhs._guidRepresentation &&
               _heartbeatInterval == rhs._heartbeatInterval &&
               _heartbeatTimeout == rhs._heartbeatTimeout &&
               _ipv6 == rhs._ipv6 &&
               _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
               _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
               _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
               _minConnectionPoolSize == rhs._minConnectionPoolSize &&
               _operationTimeout == rhs._operationTimeout &&
               object.Equals(_readConcern, rhs._readConcern) &&
               object.Equals(_readEncoding, rhs._readEncoding) &&
               _readPreference.Equals(rhs._readPreference) &&
               _replicaSetName == rhs._replicaSetName &&
               _localThreshold == rhs._localThreshold &&
               _servers.SequenceEqual(rhs._servers) &&
               _serverSelectionTimeout == rhs._serverSelectionTimeout &&
               _socketTimeout == rhs._socketTimeout &&
               _sslSettings == rhs._sslSettings &&
               _useSsl == rhs._useSsl &&
               _verifySslCertificate == rhs._verifySslCertificate &&
               _waitQueueSize == rhs._waitQueueSize &&
               _waitQueueTimeout == rhs._waitQueueTimeout &&
               _writeConcern.Equals(rhs._writeConcern) &&
                object.Equals(_writeEncoding, rhs._writeEncoding);
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoServerSettings Freeze()
        {
            if (!_isFrozen)
            {
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
        /// Gets the hash code.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (_isFrozen)
            {
                return _frozenHashCode;
            }

            return new Hasher()
                .Hash(_applicationName)
                .Hash(_clusterConfigurator)
                .Hash(_connectionMode)
                .Hash(_connectTimeout)
                .Hash(_credentials)
                .Hash(_guidRepresentation)
                .Hash(_heartbeatInterval)
                .Hash(_heartbeatTimeout)
                .Hash(_ipv6)
                .Hash(_maxConnectionIdleTime)
                .Hash(_maxConnectionLifeTime)
                .Hash(_maxConnectionPoolSize)
                .Hash(_minConnectionPoolSize)
                .Hash(_operationTimeout)
                .Hash(_readConcern)
                .Hash(_readEncoding)
                .Hash(_readPreference)
                .Hash(_replicaSetName)
                .Hash(_localThreshold)
                .HashElements(_servers)
                .Hash(_serverSelectionTimeout)
                .Hash(_socketTimeout)
                .Hash(_sslSettings)
                .Hash(_useSsl)
                .Hash(_verifySslCertificate)
                .Hash(_waitQueueSize)
                .Hash(_waitQueueTimeout)
                .Hash(_writeConcern)
                .Hash(_writeEncoding)
                .GetHashCode();
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

            var parts = new List<string>();
            if (_applicationName != null)
            {
                parts.Add(string.Format("ApplicationName={0}", _applicationName));
            }
            parts.Add(string.Format("ConnectionMode={0}", _connectionMode));
            parts.Add(string.Format("ConnectTimeout={0}", _connectTimeout));
            parts.Add(string.Format("Credentials={{{0}}}", _credentials));
            parts.Add(string.Format("GuidRepresentation={0}", _guidRepresentation));
            parts.Add(string.Format("HeartbeatInterval={0}", _heartbeatInterval));
            parts.Add(string.Format("HeartbeatTimeout={0}", _heartbeatTimeout));
            parts.Add(string.Format("IPv6={0}", _ipv6));
            parts.Add(string.Format("MaxConnectionIdleTime={0}", _maxConnectionIdleTime));
            parts.Add(string.Format("MaxConnectionLifeTime={0}", _maxConnectionLifeTime));
            parts.Add(string.Format("MaxConnectionPoolSize={0}", _maxConnectionPoolSize));
            parts.Add(string.Format("MinConnectionPoolSize={0}", _minConnectionPoolSize));
            parts.Add(string.Format("OperationTimeout={0}", _operationTimeout));
            parts.Add(string.Format("ReadConcern={0}", _readConcern));
            if (_readEncoding != null)
            {
                parts.Add("ReadEncoding=UTF8Encoding");
            }
            parts.Add(string.Format("ReadPreference={0}", _readPreference));
            parts.Add(string.Format("ReplicaSetName={0}", _replicaSetName));
            parts.Add(string.Format("LocalThreshold={0}", _localThreshold));
            parts.Add(string.Format("Servers={0}", string.Join(",", _servers.Select(s => s.ToString()).ToArray())));
            parts.Add(string.Format("ServerSelectionTimeout={0}", _serverSelectionTimeout));
            parts.Add(string.Format("SocketTimeout={0}", _socketTimeout));
            if (_sslSettings != null)
            {
                parts.Add(string.Format("SslSettings={0}", _sslSettings));
            }
            parts.Add(string.Format("Ssl={0}", _useSsl));
            parts.Add(string.Format("SslVerifyCertificate={0}", _verifySslCertificate));
            parts.Add(string.Format("WaitQueueSize={0}", _waitQueueSize));
            parts.Add(string.Format("WaitQueueTimeout={0}", _waitQueueTimeout));
            parts.Add(string.Format("WriteConcern={0}", _writeConcern));
            if (_writeEncoding != null)
            {
                parts.Add("WriteEncoding=UTF8Encoding");
            }
            return string.Join(",", parts.ToArray());
        }

        // internal methods
        internal ClusterKey ToClusterKey()
        {
            return new ClusterKey(
                _applicationName,
                _clusterConfigurator,
                _connectionMode,
                _connectTimeout,
                _credentials.ToList(),
                _heartbeatInterval,
                _heartbeatTimeout,
                _ipv6,
                _localThreshold,
                _maxConnectionIdleTime,
                _maxConnectionLifeTime,
                _maxConnectionPoolSize,
                _minConnectionPoolSize,
                _replicaSetName,
                _servers.ToList(),
                _serverSelectionTimeout,
                _socketTimeout,
                _sslSettings,
                _useSsl,
                _verifySslCertificate,
                _waitQueueSize,
                _waitQueueTimeout);
        }
    }
}
