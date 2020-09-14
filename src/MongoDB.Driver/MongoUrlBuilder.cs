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
using System.Net;
using System.Text;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// Represents URL-style connection strings.
    /// </summary>
#if NET452
    [Serializable]
#endif
    public class MongoUrlBuilder
    {
        // private fields
        private bool _allowInsecureTls;
        private string _applicationName;
        private string _authenticationMechanism;
        private Dictionary<string, string> _authenticationMechanismProperties;
        private string _authenticationSource;
        private IReadOnlyList<CompressorConfiguration> _compressors;
#pragma warning disable CS0618 // Type or member is obsolete
        private ConnectionMode _connectionMode;
        private ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private TimeSpan _connectTimeout;
        private string _databaseName;
        private bool? _directConnection;
        private bool? _fsync;
        private GuidRepresentation _guidRepresentation;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _heartbeatTimeout;
        private bool _ipv6;
        private bool? _journal;
        private TimeSpan _localThreshold;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private string _password;
        private ReadConcernLevel? _readConcernLevel;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private bool? _retryReads;
        private bool? _retryWrites;
        private ConnectionStringScheme _scheme;
        private IEnumerable<MongoServerAddress> _servers;
        private TimeSpan _serverSelectionTimeout;
        private TimeSpan _socketTimeout;
        private bool? _tlsDisableCertificateRevocationCheck;
        private string _username;
        private bool _useTls;
        private WriteConcern.WValue _w;
        private double _waitQueueMultiple;
        private int _waitQueueSize;
        private TimeSpan _waitQueueTimeout;
        private TimeSpan? _wTimeout;

        // constructors
        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        public MongoUrlBuilder()
        {
            _allowInsecureTls = false;
            _applicationName = null;
            _authenticationMechanism = MongoDefaults.AuthenticationMechanism;
            _authenticationMechanismProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _authenticationSource = null;
            _compressors = new CompressorConfiguration[0];
            _connectionMode = ConnectionMode.Automatic;
#pragma warning disable CS0618 // Type or member is obsolete
            _connectionModeSwitch = ConnectionModeSwitch.NotSet;
#pragma warning restore CS0618 // Type or member is obsolete
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _databaseName = null;
            _directConnection = null;
            _fsync = null;
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                _guidRepresentation = MongoDefaults.GuidRepresentation;
            }
#pragma warning restore 618
            _heartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
            _heartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
            _ipv6 = false;
            _journal = null;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _password = null;
            _readConcernLevel = null;
            _readPreference = null;
            _replicaSetName = null;
            _retryReads = null;
            _retryWrites = null;
            _localThreshold = MongoDefaults.LocalThreshold;
            _scheme = ConnectionStringScheme.MongoDB;
            _servers = new[] { new MongoServerAddress("localhost", 27017) };
            _serverSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _username = null;
            _useTls = false;
            _w = null;
#pragma warning disable 618
            _waitQueueMultiple = MongoDefaults.WaitQueueMultiple;
            _waitQueueSize = MongoDefaults.WaitQueueSize;
#pragma warning restore 618
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _wTimeout = null;
        }

        /// <summary>
        /// Creates a new instance of MongoUrlBuilder.
        /// </summary>
        /// <param name="url">The initial settings.</param>
        public MongoUrlBuilder(string url)
            : this()
        {
            Parse(url);
        }

        // public properties
        /// <summary>
        /// Gets or sets whether to relax TLS constraints as much as possible.
        /// </summary>
        public bool AllowInsecureTls
        {
            get => _allowInsecureTls;
            set => _allowInsecureTls = value;
        }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set { _applicationName = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(value)); }
        }

        /// <summary>
        /// Gets or sets the authentication mechanism.
        /// </summary>
        public string AuthenticationMechanism
        {
            get { return _authenticationMechanism; }
            set { _authenticationMechanism = value; }
        }

        /// <summary>
        /// Gets or sets the authentication mechanism properties.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> AuthenticationMechanismProperties
        {
            get { return _authenticationMechanismProperties; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                _authenticationMechanismProperties = value.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the authentication source.
        /// </summary>
        public string AuthenticationSource
        {
            get { return _authenticationSource; }
            set { _authenticationSource = value; }
        }

        /// <summary>
        /// Gets or sets the compressors.
        /// </summary>
        public IReadOnlyList<CompressorConfiguration> Compressors
        {
            get { return _compressors; }
            set { _compressors = value; }
        }

        /// <summary>
        /// Gets the actual wait queue size (either WaitQueueSize or WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public int ComputedWaitQueueSize
        {
            get
            {
                if (_waitQueueMultiple == 0.0)
                {
                    return _waitQueueSize;
                }
                else
                {
                    return (int)(_waitQueueMultiple * _maxConnectionPoolSize);
                }
            }
        }

        /// <summary>
        /// Gets or sets the connection mode.
        /// </summary>
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
            set
            {
                if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
                {
                    throw new InvalidOperationException("ConnectionMode cannot be used when ConnectionModeSwitch is set to UseDirectConnection.");
                }
                _connectionMode = value;
                _connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode; // _directConnection is always null here
            }
        }

        /// <summary>
        /// Gets the connection mode switch.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public ConnectionModeSwitch ConnectionModeSwitch
        {
            get { return _connectionModeSwitch; }
        }

        /// <summary>
        /// Gets or sets the connect timeout.
        /// </summary>
        public TimeSpan ConnectTimeout
        {
            get { return _connectTimeout; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "ConnectTimeout must be greater than or equal to zero.");
                }
                _connectTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the optional database name.
        /// </summary>
        public string DatabaseName
        {
            get { return _databaseName; }
            set { _databaseName = value; }
        }

        /// <summary>
        /// Gets or sets the direct connection.
        /// </summary>
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
            set
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
                {
                    throw new InvalidOperationException("DirectConnection cannot be used when ConnectionModeSwitch is set to UseConnectionMode.");
                }
                _directConnection = value;
                _connectionModeSwitch = ConnectionModeSwitch.UseDirectConnection; // _connectionMode is always Automatic here
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Gets or sets the FSync component of the write concern.
        /// </summary>
        public bool? FSync
        {
            get { return _fsync; }
            set
            {
                _fsync = value;
            }
        }

        /// <summary>
        /// Gets or sets the representation to use for Guids.
        /// </summary>
        [Obsolete("Configure serializers instead.")]
        public GuidRepresentation GuidRepresentation
        {
            get
            {
                if (BsonDefaults.GuidRepresentationMode != GuidRepresentationMode.V2)
                {
                    throw new InvalidOperationException("MongoUrlBuilder.GuidRepresentation can only be used when BsonDefaults.GuidRepresentationMode is V2.");
                }
                return _guidRepresentation;
            }
            set
            {
                if (BsonDefaults.GuidRepresentationMode != GuidRepresentationMode.V2)
                {
                    throw new InvalidOperationException("MongoUrlBuilder.GuidRepresentation can only be used when BsonDefaults.GuidRepresentationMode is V2.");
                }
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "HeartbeatInterval must be greater than or equal to zero.");
                }
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
                if (value < TimeSpan.Zero && value != Timeout.InfiniteTimeSpan)
                {
                    throw new ArgumentOutOfRangeException("value", "HeartbeatTimeout must be greater than or equal to zero.");
                }
                _heartbeatTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use IPv6.
        /// </summary>
        public bool IPv6
        {
            get { return _ipv6; }
            set { _ipv6 = value; }
        }

        /// <summary>
        /// Gets or sets the Journal component of the write concern.
        /// </summary>
        public bool? Journal
        {
            get { return _journal; }
            set
            {
                _journal = value;
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "LocalThreshold must be greater than or equal to zero.");
                }
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionIdleTime must be greater than or equal to zero.");
                }
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionLifeTime must be greater than or equal to zero.");
                }
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
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "MaxConnectionPoolSize must be greater than zero.");
                }
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
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", "MinConnectionPoolSize must be greater than or equal to zero.");
                }
                _minConnectionPoolSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// Gets or sets the read concern level.
        /// </summary>
        public ReadConcernLevel? ReadConcernLevel
        {
            get { return _readConcernLevel; }
            set { _readConcernLevel = value; }
        }

        /// <summary>
        /// Gets or sets the read preference.
        /// </summary>
        public ReadPreference ReadPreference
        {
            get
            {
                if (_readPreference != null)
                {
                    return _readPreference;
                }
                else
                {
                    return null;
                }
            }
            set { _readPreference = value; }
        }

        /// <summary>
        /// Gets or sets the name of the replica set.
        /// </summary>
        public string ReplicaSetName
        {
            get { return _replicaSetName; }
            set { _replicaSetName = value; }
        }

        /// <summary>
        /// Gets or sets whether to retry reads.
        /// </summary>
        public bool? RetryReads
        {
            get { return _retryReads; }
            set { _retryReads = value; }
        }

        /// <summary>
        /// Gets or sets whether to retry writes.
        /// </summary>
        public bool? RetryWrites
        {
            get { return _retryWrites; }
            set { _retryWrites = value; }
        }

        /// <summary>
        /// The connection string scheme.
        /// </summary>
        public ConnectionStringScheme Scheme
        {
            get { return _scheme; }
            set { _scheme = value; }
        }

        /// <summary>
        /// Gets or sets the address of the server (see also Servers if using more than one address).
        /// </summary>
        public MongoServerAddress Server
        {
            get { return (_servers == null) ? null : _servers.Single(); }
            set { _servers = (value == null) ? null : new[] { value }; }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get { return _servers; }
            set { _servers = value; }
        }

        /// <summary>
        /// Gets or sets the server selection timeout.
        /// </summary>
        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "ServerSelectionTimeout must be greater than or equal to zero.");
                }
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "SocketTimeout must be greater than or equal to zero.");
                }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to disable certificate revocation checking during the TLS handshake.
        /// </summary>
        public bool TlsDisableCertificateRevocationCheck
        {
            get => _tlsDisableCertificateRevocationCheck.GetValueOrDefault(true);
            set => _tlsDisableCertificateRevocationCheck = value;
        }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL.
        /// </summary>
        [Obsolete("Use UseTls instead.")]
        public bool UseSsl
        {
            get { return _useTls; }
            set { _useTls = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use TLS.
        /// </summary>
        public bool UseTls
        {
            get => _useTls;
            set => _useTls = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to verify an SSL certificate.
        /// </summary>
        [Obsolete("Use AllowInsecureTls instead.")]
        public bool VerifySslCertificate
        {
            get => !_allowInsecureTls;
            set => _allowInsecureTls = !value;
        }

        /// <summary>
        /// Gets or sets the W component of the write concern.
        /// </summary>
        public WriteConcern.WValue W
        {
            get { return _w; }
            set
            {
                _w = value;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue multiple (the actual wait queue size will be WaitQueueMultiple x MaxConnectionPoolSize).
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public double WaitQueueMultiple
        {
            get { return _waitQueueMultiple; }
            set
            {
                if (value <= 0.0)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueMultiple must be greater than zero.");
                }
                _waitQueueMultiple = value;
                _waitQueueSize = 0;
            }
        }

        /// <summary>
        /// Gets or sets the wait queue size.
        /// </summary>
        [Obsolete("This property will be removed in a later release.")]
        public int WaitQueueSize
        {
            get { return _waitQueueSize; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueSize must be greater than zero.");
                }
                _waitQueueSize = value;
                _waitQueueMultiple = 0.0;
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
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "WaitQueueTimeout must be greater than or equal to zero.");
                }
                _waitQueueTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the WTimeout component of the write concern.
        /// </summary>
        public TimeSpan? WTimeout
        {
            get { return _wTimeout; }
            set
            {
                if (value != null && value.Value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException("value", "WTimeout must be greater than or equal to zero.");
                }
                _wTimeout = value;
            }
        }

        // public methods
        /// <summary>
        /// Returns a WriteConcern value based on this instance's settings and a default enabled value.
        /// </summary>
        /// <param name="enabledDefault">The default enabled value.</param>
        /// <returns>A WriteConcern.</returns>
        public WriteConcern GetWriteConcern(bool enabledDefault)
        {
            if (_w == null && !_wTimeout.HasValue && !_fsync.HasValue && !_journal.HasValue)
            {
                return enabledDefault ? WriteConcern.Acknowledged : WriteConcern.Unacknowledged;
            }

            return new WriteConcern(_w, _wTimeout, _fsync, _journal);
        }

        /// <summary>
        /// Parses a URL and sets all settings to match the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void Parse(string url)
        {
            var connectionString = new ConnectionString(url);
            _allowInsecureTls = connectionString.TlsInsecure.GetValueOrDefault(false);
            _applicationName = connectionString.ApplicationName;
            _authenticationMechanism = connectionString.AuthMechanism;
            _authenticationMechanismProperties = connectionString.AuthMechanismProperties.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            _authenticationSource = connectionString.AuthSource;
            _compressors = connectionString.Compressors;
#pragma warning disable CS0618
            if (connectionString.ConnectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
            {
                switch (connectionString.Connect)
                {
                    case ClusterConnectionMode.Direct:
                        _connectionMode = Driver.ConnectionMode.Direct;
                        break;
                    case ClusterConnectionMode.ReplicaSet:
                        _connectionMode = Driver.ConnectionMode.ReplicaSet;
                        break;
                    case ClusterConnectionMode.Sharded:
                        _connectionMode = Driver.ConnectionMode.ShardRouter;
                        break;
                    case ClusterConnectionMode.Standalone:
                        _connectionMode = Driver.ConnectionMode.Standalone;
                        break;
                    default:
                        _connectionMode = Driver.ConnectionMode.Automatic;
                        break;
                }
#pragma warning restore CS0618
            }
#pragma warning disable CS0618 // Type or member is obsolete
            _connectionModeSwitch = connectionString.ConnectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
            _connectTimeout = connectionString.ConnectTimeout.GetValueOrDefault(MongoDefaults.ConnectTimeout);
            _databaseName = connectionString.DatabaseName;
#pragma warning disable CS0618
            if (connectionString.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618
            {
                _directConnection = connectionString.DirectConnection;
            }
            _fsync = connectionString.FSync;
#pragma warning disable 618
            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
            {
                _guidRepresentation = connectionString.UuidRepresentation.GetValueOrDefault(MongoDefaults.GuidRepresentation);
            }
            else
            {
                if (connectionString.UuidRepresentation.HasValue)
                {
                    throw new InvalidOperationException("ConnectionString.UuidRepresentation can only be used when BsonDefaults.GuidRepresentationMode is V2.");
                }
            }
#pragma warning restore 618
            _heartbeatInterval = connectionString.HeartbeatInterval ?? ServerSettings.DefaultHeartbeatInterval;
            _heartbeatTimeout = connectionString.HeartbeatTimeout ?? ServerSettings.DefaultHeartbeatTimeout;
            _ipv6 = connectionString.Ipv6.GetValueOrDefault(false);
            _journal = connectionString.Journal;
            _maxConnectionIdleTime = connectionString.MaxIdleTime.GetValueOrDefault(MongoDefaults.MaxConnectionIdleTime);
            _maxConnectionLifeTime = connectionString.MaxLifeTime.GetValueOrDefault(MongoDefaults.MaxConnectionLifeTime);
            _maxConnectionPoolSize = connectionString.MaxPoolSize.GetValueOrDefault(MongoDefaults.MaxConnectionPoolSize);
            _minConnectionPoolSize = connectionString.MinPoolSize.GetValueOrDefault(MongoDefaults.MinConnectionPoolSize);
            _password = connectionString.Password;
            _readConcernLevel = connectionString.ReadConcernLevel;
            if (connectionString.ReadPreference.HasValue || connectionString.ReadPreferenceTags != null || connectionString.MaxStaleness.HasValue)
            {
                if (!connectionString.ReadPreference.HasValue)
                {
                    throw new MongoConfigurationException("readPreference mode is required when using tag sets or max staleness.");
                }
                _readPreference = new ReadPreference(connectionString.ReadPreference.Value, connectionString.ReadPreferenceTags, connectionString.MaxStaleness);
            }
            _replicaSetName = connectionString.ReplicaSet;
            _retryReads = connectionString.RetryReads;
            _retryWrites = connectionString.RetryWrites;
            _localThreshold = connectionString.LocalThreshold.GetValueOrDefault(MongoDefaults.LocalThreshold);
            _scheme = connectionString.Scheme;
            _servers = connectionString.Hosts.Select(endPoint =>
            {
                DnsEndPoint dnsEndPoint;
                IPEndPoint ipEndPoint;
                if ((dnsEndPoint = endPoint as DnsEndPoint) != null)
                {
                    return new MongoServerAddress(dnsEndPoint.Host, dnsEndPoint.Port);
                }
                else if ((ipEndPoint = endPoint as IPEndPoint) != null)
                {
                    var address = ipEndPoint.Address.ToString();
                    if (ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        address = "[" + address + "]";
                    }
                    return new MongoServerAddress(address, ipEndPoint.Port);
                }
                else
                {
                    throw new NotSupportedException("Only DnsEndPoint and IPEndPoints are supported in the connection string.");
                }
            });
            _serverSelectionTimeout = connectionString.ServerSelectionTimeout.GetValueOrDefault(MongoDefaults.ServerSelectionTimeout);
            _socketTimeout = connectionString.SocketTimeout.GetValueOrDefault(MongoDefaults.SocketTimeout);
            _tlsDisableCertificateRevocationCheck = connectionString.TlsDisableCertificateRevocationCheck;
            _username = connectionString.Username;
            _useTls = connectionString.Tls.GetValueOrDefault(false);
            _w = connectionString.W;
#pragma warning disable 618
            if (connectionString.WaitQueueSize != null)
            {
                _waitQueueSize = connectionString.WaitQueueSize.Value;
                _waitQueueMultiple = 0.0;
            }
            else if (connectionString.WaitQueueMultiple != null)
            {
                _waitQueueMultiple = connectionString.WaitQueueMultiple.Value;
                _waitQueueSize = 0;
            }
#pragma warning restore 618
            _waitQueueTimeout = connectionString.WaitQueueTimeout.GetValueOrDefault(MongoDefaults.WaitQueueTimeout);
            _wTimeout = connectionString.WTimeout;
        }

        /// <summary>
        /// Creates a new instance of MongoUrl based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>A new instance of MongoUrl.</returns>
        public MongoUrl ToMongoUrl()
        {
            return MongoUrl.Create(ToString());
        }

        /// <summary>
        /// Returns the canonical URL based on the settings in this MongoUrlBuilder.
        /// </summary>
        /// <returns>The canonical URL.</returns>
        public override string ToString()
        {
            StringBuilder url = new StringBuilder();
            if (_scheme == ConnectionStringScheme.MongoDB)
            {
                url.Append("mongodb://");
            }
            else
            {
                url.Append("mongodb+srv://");
            }
            if (!string.IsNullOrEmpty(_username))
            {
                url.Append(Uri.EscapeDataString(_username));
                if (_password != null)
                {
                    url.AppendFormat(":{0}", Uri.EscapeDataString(_password));
                }
                url.Append("@");
            }
            else if (_password != null)
            {
                // this would be weird and we really shouldn't be here...
                url.AppendFormat(":{0}@", _password);
            }
            if (_servers != null)
            {
                bool firstServer = true;
                foreach (MongoServerAddress server in _servers)
                {
                    if (!firstServer) { url.Append(","); }
                    if (server.Port == 27017 || _scheme == ConnectionStringScheme.MongoDBPlusSrv)
                    {
                        url.Append(server.Host);
                    }
                    else
                    {
                        url.AppendFormat("{0}:{1}", server.Host, server.Port);
                    }
                    firstServer = false;
                }
            }
            if (_databaseName != null)
            {
                url.Append("/");
                url.Append(_databaseName);
            }
            var query = new StringBuilder();
            if (_authenticationMechanism != null)
            {
                query.AppendFormat("authMechanism={0};", _authenticationMechanism);
            }
            if (_authenticationMechanismProperties.Any())
            {
                query.AppendFormat(
                    "authMechanismProperties={0};",
                    string.Join(",", _authenticationMechanismProperties
                        .Select(x => string.Format("{0}:{1}", x.Key, x.Value)).ToArray()));
            }
            if (_authenticationSource != null)
            {
                query.AppendFormat("authSource={0};", _authenticationSource);
            }
            if (_applicationName != null)
            {
                query.AppendFormat("appname={0};", _applicationName);
            }
            if (_ipv6)
            {
                query.AppendFormat("ipv6=true;");
            }
            if (_scheme == ConnectionStringScheme.MongoDBPlusSrv)
            {
                if (!_useTls)
                {
                    query.AppendFormat("tls=false;");
                }
            }
            else
            {
                if (_useTls)
                {
                    query.AppendFormat("tls=true;");
                }
            }
            if (_allowInsecureTls)
            {
                query.AppendFormat("tlsInsecure=true;");
            }

            if (_tlsDisableCertificateRevocationCheck != null)
            {
                query.AppendFormat("tlsDisableCertificateRevocationCheck={0};", JsonConvert.ToString(_tlsDisableCertificateRevocationCheck.Value));
            }

            if (_compressors?.Any() ?? false)
            {
                query.AppendFormat("compressors={0};", string.Join(",", _compressors.Select(x => x.Type.ToString().ToLowerInvariant())));
                foreach (var compressor in _compressors)
                {
                    ParseAndAppendCompressorOptions(query, compressor);
                }
            }
#pragma warning disable CS0618
            if (_connectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
            {
                if (_connectionMode != ConnectionMode.Automatic)
                {
                    query.AppendFormat("connect={0};", MongoUtils.ToCamelCase(_connectionMode.ToString()));
                }
            }
            else if (_connectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
#pragma warning restore CS0618
            {
                if (_directConnection.HasValue)
                {
                    query.AppendFormat("directConnection={0};", JsonConvert.ToString(_directConnection.Value));
                }
            }
            if (!string.IsNullOrEmpty(_replicaSetName))
            {
                query.AppendFormat("replicaSet={0};", _replicaSetName);
            }
            if (_readConcernLevel != null)
            {
                query.AppendFormat("readConcernLevel={0};", MongoUtils.ToCamelCase(_readConcernLevel.Value.ToString()));
            }
            if (_readPreference != null)
            {
                query.AppendFormat("readPreference={0};", MongoUtils.ToCamelCase(_readPreference.ReadPreferenceMode.ToString()));
                if (_readPreference.TagSets != null)
                {
                    foreach (var tagSet in _readPreference.TagSets)
                    {
                        query.AppendFormat("readPreferenceTags={0};", string.Join(",", tagSet.Tags.Select(t => string.Format("{0}:{1}", t.Name, t.Value)).ToArray()));
                    }
                }
                if (_readPreference.MaxStaleness.HasValue)
                {
                    query.AppendFormat("maxStaleness={0};", FormatTimeSpan(_readPreference.MaxStaleness.Value));
                }
            }
            if (_fsync != null)
            {
                query.AppendFormat("fsync={0};", JsonConvert.ToString(_fsync.Value));
            }
            if (_journal != null)
            {
                query.AppendFormat("journal={0};", JsonConvert.ToString(_journal.Value));
            }
            if (_w != null)
            {
                query.AppendFormat("w={0};", _w);
            }
            if (_wTimeout != null)
            {
                query.AppendFormat("wtimeout={0};", FormatTimeSpan(_wTimeout.Value));
            }
            if (_connectTimeout != MongoDefaults.ConnectTimeout)
            {
                query.AppendFormat("connectTimeout={0};", FormatTimeSpan(_connectTimeout));
            }
            if (_heartbeatInterval != ServerSettings.DefaultHeartbeatInterval)
            {
                query.AppendFormat("heartbeatInterval={0};", FormatTimeSpan(_heartbeatInterval));
            }
            if (_heartbeatTimeout != ServerSettings.DefaultHeartbeatTimeout)
            {
                query.AppendFormat("heartbeatTimeout={0};", FormatTimeSpan(_heartbeatTimeout));
            }
            if (_maxConnectionIdleTime != MongoDefaults.MaxConnectionIdleTime)
            {
                query.AppendFormat("maxIdleTime={0};", FormatTimeSpan(_maxConnectionIdleTime));
            }
            if (_maxConnectionLifeTime != MongoDefaults.MaxConnectionLifeTime)
            {
                query.AppendFormat("maxLifeTime={0};", FormatTimeSpan(_maxConnectionLifeTime));
            }
            if (_maxConnectionPoolSize != MongoDefaults.MaxConnectionPoolSize)
            {
                query.AppendFormat("maxPoolSize={0};", _maxConnectionPoolSize);
            }
            if (_minConnectionPoolSize != MongoDefaults.MinConnectionPoolSize)
            {
                query.AppendFormat("minPoolSize={0};", _minConnectionPoolSize);
            }
            if (_localThreshold != MongoDefaults.LocalThreshold)
            {
                query.AppendFormat("localThreshold={0};", FormatTimeSpan(_localThreshold));
            }
            if (_serverSelectionTimeout != MongoDefaults.ServerSelectionTimeout)
            {
                query.AppendFormat("serverSelectionTimeout={0};", FormatTimeSpan(_serverSelectionTimeout));
            }
            if (_socketTimeout != MongoDefaults.SocketTimeout)
            {
                query.AppendFormat("socketTimeout={0};", FormatTimeSpan(_socketTimeout));
            }
#pragma warning disable 618
            if (_waitQueueMultiple != 0.0 && _waitQueueMultiple != MongoDefaults.WaitQueueMultiple)
#pragma warning restore 618
            {
                query.AppendFormat("waitQueueMultiple={0};", _waitQueueMultiple);
            }
#pragma warning disable 618
            if (_waitQueueSize != 0 && _waitQueueSize != MongoDefaults.WaitQueueSize)
#pragma warning restore 618
            {
                query.AppendFormat("waitQueueSize={0};", _waitQueueSize);
            }
            if (_waitQueueTimeout != MongoDefaults.WaitQueueTimeout)
            {
                query.AppendFormat("waitQueueTimeout={0};", FormatTimeSpan(WaitQueueTimeout));
            }
#pragma warning disable 618
            var defaultGuidRepresentation = BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2 ? BsonDefaults.GuidRepresentation : GuidRepresentation.Unspecified;
            if (_guidRepresentation != defaultGuidRepresentation)
            {
                query.AppendFormat("uuidRepresentation={0};", (_guidRepresentation == GuidRepresentation.CSharpLegacy) ? "csharpLegacy" : MongoUtils.ToCamelCase(_guidRepresentation.ToString()));
            }
#pragma warning restore 618
            if (!_retryReads.GetValueOrDefault(true))
            {
                query.AppendFormat("retryReads=false;");
            }
            if (_retryWrites.HasValue)
            {
                query.AppendFormat("retryWrites={0};", JsonConvert.ToString(_retryWrites.Value));
            }
            if (query.Length != 0)
            {
                query.Length = query.Length - 1; // remove trailing ";"
                if (_databaseName == null)
                {
                    url.Append("/");
                }
                url.Append("?");
                url.Append(query.ToString());
            }
            return url.ToString();
        }

        // private methods
        private bool AnyWriteConcernSettingsAreSet()
        {
            return _fsync != null || _journal != null || _w != null || _wTimeout != null;
        }

        private string FormatTimeSpan(TimeSpan value)
        {
            const int msInOneSecond = 1000; // milliseconds
            const int msInOneMinute = 60 * msInOneSecond;
            const int msInOneHour = 60 * msInOneMinute;

            var ms = (int)value.TotalMilliseconds;
            if ((ms % msInOneHour) == 0)
            {
                return string.Format("{0}h", ms / msInOneHour);
            }
            else if ((ms % msInOneMinute) == 0 && ms < msInOneHour)
            {
                return string.Format("{0}m", ms / msInOneMinute);
            }
            else if ((ms % msInOneSecond) == 0 && ms < msInOneMinute)
            {
                return string.Format("{0}s", ms / msInOneSecond);
            }
            else if (ms < 1000)
            {
                return string.Format("{0}ms", ms);
            }
            else
            {
                return value.ToString();
            }
        }

        private static void ParseAndAppendCompressorOptions(StringBuilder builder, CompressorConfiguration compressorConfiguration)
        {
            switch (compressorConfiguration.Type)
            {
                case CompressorType.Zlib:
                    {
                        if (compressorConfiguration.Properties.TryGetValue("Level", out var zlibCompressionLevel))
                        {
                            builder.AppendFormat("zlibCompressionLevel={0};", zlibCompressionLevel);
                        }
                    }
                    break;
            }
        }
    }
}
