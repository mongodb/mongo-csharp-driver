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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using MongoDB.Bson.Serialization;
using MongoDB.Driver.Core.Compression;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Encryption;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// The settings for a MongoDB client.
    /// </summary>
    public class MongoClientSettings : IEquatable<MongoClientSettings>, IInheritableMongoClientSettings
    {
        /// <summary>
        /// Extension Manager provides a way to configure extensions for the driver.
        /// </summary>
        public static readonly IExtensionManager Extensions = new ExtensionManager();

        // private fields
        private bool _allowInsecureTls;
        private string _applicationName;
        private AutoEncryptionOptions _autoEncryptionOptions;
        private Action<ClusterBuilder> _clusterConfigurator;
        private IClusterSource _clusterSource;
        private IReadOnlyList<CompressorConfiguration> _compressors;
        private TimeSpan _connectTimeout;
        private MongoCredential _credential;
        private bool _directConnection;
        private TimeSpan _heartbeatInterval;
        private TimeSpan _heartbeatTimeout;
        private bool _ipv6;
        private LibraryInfo _libraryInfo;
        private bool _loadBalanced;
        private TimeSpan _localThreshold;
        private LoggingSettings _loggingSettings;
        private int _maxConnecting;
        private TimeSpan _maxConnectionIdleTime;
        private TimeSpan _maxConnectionLifeTime;
        private int _maxConnectionPoolSize;
        private int _minConnectionPoolSize;
        private ReadConcern _readConcern;
        private UTF8Encoding _readEncoding;
        private ReadPreference _readPreference;
        private string _replicaSetName;
        private bool _retryReads;
        private bool _retryWrites;
        private ConnectionStringScheme _scheme;
        private IBsonSerializationDomain _serializationDomain;
        private ServerApi _serverApi;
        private List<MongoServerAddress> _servers;
        private ServerMonitoringMode _serverMonitoringMode;
        private TimeSpan _serverSelectionTimeout;
        private TimeSpan _socketTimeout;
        private int _srvMaxHosts;
        private string _srvServiceName;
        private SslSettings _sslSettings;
        private ExpressionTranslationOptions _translationOptions;
        private bool _useTls;
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
        /// Creates a new instance of MongoClientSettings. Usually you would use a connection string instead.
        /// </summary>
        public MongoClientSettings()
        {
            _allowInsecureTls = false;
            _applicationName = null;
            _autoEncryptionOptions = null;
            _clusterSource = DefaultClusterSource.Instance;
            _compressors = new CompressorConfiguration[0];
            _connectTimeout = MongoDefaults.ConnectTimeout;
            _directConnection = false;
            _heartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
            _heartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
            _ipv6 = false;
            _libraryInfo = null;
            _loadBalanced = false;
            _localThreshold = MongoDefaults.LocalThreshold;
            _maxConnecting = MongoInternalDefaults.ConnectionPool.MaxConnecting;
            _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
            _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
            _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
            _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
            _readConcern = ReadConcern.Default;
            _readEncoding = null;
            _readPreference = ReadPreference.Primary;
            _replicaSetName = null;
            _retryReads = true;
            _retryWrites = true;
            _scheme = ConnectionStringScheme.MongoDB;
            _serializationDomain = BsonSerializer.DefaultSerializationDomain;
            _serverApi = null;
            _servers = new List<MongoServerAddress> { new MongoServerAddress("localhost") };
            _serverMonitoringMode = ServerMonitoringMode.Auto;
            _serverSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
            _socketTimeout = MongoDefaults.SocketTimeout;
            _srvMaxHosts = 0;
            _srvServiceName = MongoInternalDefaults.MongoClientSettings.SrvServiceName;
            _sslSettings = null;
            _translationOptions = null;
            _useTls = false;
#pragma warning disable 618
            _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
#pragma warning restore 618
            _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
            _writeConcern = WriteConcern.Acknowledged;
            _writeEncoding = null;
        }

        // internal properties
        internal IClusterSource ClusterSource
        {
            get => _clusterSource;
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _clusterSource = Ensure.IsNotNull(value, nameof(value));
            }
        }

        // public properties
        /// <summary>
        /// Gets or sets whether to relax TLS constraints as much as possible.
        /// Setting this variable to true will also set SslSettings.CheckCertificateRevocation to false.
        /// </summary>
        public bool AllowInsecureTls
        {
            get { return _allowInsecureTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value)
                {
                    _sslSettings = _sslSettings ?? new SslSettings();
                    // Otherwise, the user will have to manually set CheckCertificateRevocation to false
                    _sslSettings.CheckCertificateRevocation = false;
                }
                _allowInsecureTls = value;
            }
        }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string ApplicationName
        {
            get { return _applicationName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _applicationName = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the auto encryption options.
        /// </summary>
        public AutoEncryptionOptions AutoEncryptionOptions
        {
            get { return _autoEncryptionOptions; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _autoEncryptionOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the compressors.
        /// </summary>
        public IReadOnlyList<CompressorConfiguration> Compressors
        {
            get { return _compressors; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _compressors = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _clusterConfigurator = value;
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
        /// Gets or sets the credential.
        /// </summary>
        public MongoCredential Credential
        {
            get
            {
                return _credential;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _credential = value;
            }
        }

        /// <summary>
        /// Gets or sets the direct connection.
        /// </summary>
        public bool DirectConnection
        {
            get
            {
                return _directConnection;
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _directConnection = value;
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
        /// Gets or sets the heartbeat interval.
        /// </summary>
        public TimeSpan HeartbeatInterval
        {
            get { return _heartbeatInterval; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _heartbeatInterval = Ensure.IsGreaterThanZero(value, nameof(value));
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _heartbeatTimeout = Ensure.IsInfiniteOrGreaterThanZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use IPv6.
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
        /// Gets or sets information about a library using the .NET Driver.
        /// </summary>
        public LibraryInfo LibraryInfo
        {
            get { return _libraryInfo; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _libraryInfo = value;
            }
        }

        /// <summary>
        /// Gets or sets whether load balanced mode is used.
        /// </summary>
        public bool LoadBalanced
        {
            get { return _loadBalanced; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _loadBalanced = value;
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _localThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the logging settings
        /// </summary>
        public LoggingSettings LoggingSettings
        {
            get { return _loggingSettings; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _loggingSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum concurrently connecting connections.
        /// </summary>
        public int MaxConnecting
        {
            get { return _maxConnecting; }
            set
            {
                ThrowIfFrozen();
                _maxConnecting = Ensure.IsGreaterThanZero(value, nameof(MaxConnecting));
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
                _maxConnectionPoolSize = Ensure.IsGreaterThanZero(value, nameof(MaxConnectionPoolSize));
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
        /// Gets or sets the read concern.
        /// </summary>
        public ReadConcern ReadConcern
        {
            get { return _readConcern; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _readPreference = value;
            }
        }

        /// <summary>
        /// //TODO
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public IBsonSerializationDomain SerializationDomain
        {
            get => _serializationDomain;
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _serializationDomain = value ?? throw new ArgumentNullException(nameof(value));
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
        /// Gets or sets whether to retry reads.
        /// </summary>
        public bool RetryReads
        {
            get { return _retryReads; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _retryReads = value;
            }
        }

        /// <summary>
        /// Gets or sets whether to retry writes.
        /// </summary>
        /// <value>
        /// The default value is <c>true</c>.
        /// </value>
        public bool RetryWrites
        {
            get { return _retryWrites; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _retryWrites = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string scheme.
        /// </summary>
        public ConnectionStringScheme Scheme
        {
            get { return _scheme; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _scheme = value;
            }
        }

        /// <summary>
        /// Gets or sets the server API.
        /// </summary>
        public ServerApi ServerApi
        {
            get { return _serverApi; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _serverApi = value;
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
                    throw new ArgumentNullException(nameof(value));
                }
                _servers = new List<MongoServerAddress> { value };
            }
        }

        /// <summary>
        /// Gets or sets the list of server addresses (see also Server if using only one address).
        /// </summary>
        public IEnumerable<MongoServerAddress> Servers
        {
            get
            {
                var servers = _srvMaxHosts > 0 ? _servers.Take(_srvMaxHosts).ToList() : _servers;
                return new ReadOnlyCollection<MongoServerAddress>(servers);
            }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                var servers = new List<MongoServerAddress>(value);
                if (_srvMaxHosts > 0)
                {
                    FisherYatesShuffle.Shuffle(servers);
                }

                _servers = servers;
            }
        }

        /// <summary>
        /// Gets or sets the server monitoring mode to use.
        /// </summary>
        public ServerMonitoringMode ServerMonitoringMode
        {
            get { return _serverMonitoringMode; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _serverMonitoringMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the server selection timeout.
        /// </summary>
        public TimeSpan ServerSelectionTimeout
        {
            get { return _serverSelectionTimeout; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _socketTimeout = value;
            }
        }

        /// <summary>
        /// Limits the number of SRV records used to populate the seedlist
        /// during initial discovery, as well as the number of additional hosts
        /// that may be added during SRV polling.
        /// </summary>
        public int SrvMaxHosts
        {
            get { return _srvMaxHosts; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _srvMaxHosts = Ensure.IsGreaterThanOrEqualToZero(value, nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the SRV service name which modifies the srv URI to look like:
        /// <code>_{srvServiceName}._tcp.{hostname}.{domainname}</code>
        /// The default value is "mongodb".
        /// </summary>
        public string SrvServiceName
        {
            get { return _srvServiceName; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _srvServiceName = Ensure.IsNotNullOrEmpty(value, nameof(SrvServiceName));
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
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _sslSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the translation options.
        /// </summary>
        public ExpressionTranslationOptions TranslationOptions
        {
            get { return _translationOptions; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _translationOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL.
        /// </summary>
        [Obsolete("Use UseTls instead.")]
        public bool UseSsl
        {
            get { return _useTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _useTls = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use TLS.
        /// </summary>
        public bool UseTls
        {
            get { return _useTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _useTls = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to verify an SSL certificate.
        /// </summary>
        [Obsolete("Use AllowInsecureTls instead.")]
        public bool VerifySslCertificate
        {
            get { return !_allowInsecureTls; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                // use property instead of private field because setter has additional side effects
                AllowInsecureTls = !value;
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

        /// <summary>
        /// Gets or sets the Write Encoding.
        /// </summary>
        public UTF8Encoding WriteEncoding
        {
            get { return _writeEncoding; }
            set
            {
                if (_isFrozen) { throw new InvalidOperationException("MongoClientSettings is frozen."); }
                _writeEncoding = value;
            }
        }

        // public operators
        /// <summary>
        /// Determines whether two <see cref="MongoClientSettings"/> instances are equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator ==(MongoClientSettings lhs, MongoClientSettings rhs)
        {
            return object.Equals(lhs, rhs); // handles lhs == null correctly
        }

        /// <summary>
        /// Determines whether two <see cref="MongoClientSettings"/> instances are not equal.
        /// </summary>
        /// <param name="lhs">The LHS.</param>
        /// <param name="rhs">The RHS.</param>
        /// <returns>
        ///   <c>true</c> if the left hand side is not equal to the right hand side; otherwise, <c>false</c>.
        /// </returns>
        public static bool operator !=(MongoClientSettings lhs, MongoClientSettings rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Gets a MongoClientSettings object intialized with values from a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromConnectionString(string connectionString)
        {
            return FromUrl(new MongoUrl(connectionString));
        }

        /// <summary>
        /// Gets a MongoClientSettings object initialized with values from a MongoURL.
        /// </summary>
        /// <param name="url">The MongoURL.</param>
        /// <returns>A MongoClientSettings.</returns>
        public static MongoClientSettings FromUrl(MongoUrl url)
        {
            if (!url.IsResolved)
            {
                url = url.Resolve(url.DirectConnection);
            }

            var credential = url.GetCredential();

            var clientSettings = new MongoClientSettings();
            clientSettings.AllowInsecureTls = url.AllowInsecureTls;
            clientSettings.ApplicationName = url.ApplicationName;
            clientSettings.AutoEncryptionOptions = null; // must be configured via code
            clientSettings.Compressors = url.Compressors;
            clientSettings.ConnectTimeout = url.ConnectTimeout;
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
                clientSettings.Credential = credential;
            }
            clientSettings.DirectConnection = url.DirectConnection;
            clientSettings.HeartbeatInterval = url.HeartbeatInterval;
            clientSettings.HeartbeatTimeout = url.HeartbeatTimeout;
            clientSettings.IPv6 = url.IPv6;
            clientSettings.LibraryInfo = null;
            clientSettings.LoadBalanced = url.LoadBalanced;
            clientSettings.LocalThreshold = url.LocalThreshold;
            clientSettings.MaxConnecting = url.MaxConnecting;
            clientSettings.MaxConnectionIdleTime = url.MaxConnectionIdleTime;
            clientSettings.MaxConnectionLifeTime = url.MaxConnectionLifeTime;
            clientSettings.MaxConnectionPoolSize = ConnectionStringConversions.GetEffectiveMaxConnections(url.MaxConnectionPoolSize);
            clientSettings.MinConnectionPoolSize = url.MinConnectionPoolSize;
            clientSettings.ReadConcern = new ReadConcern(url.ReadConcernLevel);
            clientSettings.ReadEncoding = null; // ReadEncoding must be provided in code
            clientSettings.ReadPreference = (url.ReadPreference == null) ? ReadPreference.Primary : url.ReadPreference;
            clientSettings.ReplicaSetName = url.ReplicaSetName;
            clientSettings.RetryReads = url.RetryReads.GetValueOrDefault(true);
            clientSettings.RetryWrites = url.RetryWrites.GetValueOrDefault(true);
            clientSettings.Scheme = url.Scheme;
            clientSettings.Servers = new List<MongoServerAddress>(url.Servers);
            clientSettings.ServerMonitoringMode = url.ServerMonitoringMode ?? ServerMonitoringMode.Auto;
            clientSettings.ServerSelectionTimeout = url.ServerSelectionTimeout;
            clientSettings.SocketTimeout = url.SocketTimeout;
            clientSettings.SrvMaxHosts = url.SrvMaxHosts.GetValueOrDefault(0);
            clientSettings.SrvServiceName = url.SrvServiceName;
            clientSettings.SslSettings = null;
            if (url.TlsDisableCertificateRevocationCheck)
            {
                clientSettings.SslSettings = new SslSettings { CheckCertificateRevocation = false };
            }
            clientSettings.UseTls = url.UseTls;
#pragma warning disable 618
            clientSettings.WaitQueueSize = url.ComputedWaitQueueSize;
#pragma warning restore 618
            clientSettings.WaitQueueTimeout = url.WaitQueueTimeout;
            clientSettings.WriteConcern = url.GetWriteConcern(true); // WriteConcern is enabled by default for MongoClient
            clientSettings.WriteEncoding = null; // WriteEncoding must be provided in code
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
            clone._allowInsecureTls = _allowInsecureTls;
            clone._applicationName = _applicationName;
            clone._autoEncryptionOptions = _autoEncryptionOptions;
            clone._compressors = _compressors;
            clone._clusterConfigurator = _clusterConfigurator;
            clone._clusterSource = _clusterSource;
            clone._connectTimeout = _connectTimeout;
            clone._credential = _credential;
            clone._directConnection = _directConnection;
            clone._heartbeatInterval = _heartbeatInterval;
            clone._heartbeatTimeout = _heartbeatTimeout;
            clone._ipv6 = _ipv6;
            clone._libraryInfo = _libraryInfo;
            clone._loadBalanced = _loadBalanced;
            clone._localThreshold = _localThreshold;
            clone._loggingSettings = _loggingSettings;
            clone._maxConnecting = _maxConnecting;
            clone._maxConnectionIdleTime = _maxConnectionIdleTime;
            clone._maxConnectionLifeTime = _maxConnectionLifeTime;
            clone._maxConnectionPoolSize = _maxConnectionPoolSize;
            clone._minConnectionPoolSize = _minConnectionPoolSize;
            clone._readConcern = _readConcern;
            clone._readEncoding = _readEncoding;
            clone._readPreference = _readPreference;
            clone._replicaSetName = _replicaSetName;
            clone._retryReads = _retryReads;
            clone._retryWrites = _retryWrites;
            clone._scheme = _scheme;
            clone._serializationDomain = _serializationDomain;
            clone._serverApi = _serverApi;
            clone._servers = new List<MongoServerAddress>(_servers);
            clone._serverMonitoringMode = _serverMonitoringMode;
            clone._serverSelectionTimeout = _serverSelectionTimeout;
            clone._socketTimeout = _socketTimeout;
            clone._srvMaxHosts = _srvMaxHosts;
            clone._srvServiceName = _srvServiceName;
            clone._sslSettings = (_sslSettings == null) ? null : _sslSettings.Clone();
            clone._translationOptions = _translationOptions;
            clone._useTls = _useTls;
            clone._waitQueueSize = _waitQueueSize;
            clone._waitQueueTimeout = _waitQueueTimeout;
            clone._writeConcern = _writeConcern;
            clone._writeEncoding = _writeEncoding;
            return clone;
        }

        /// <summary>
        /// Determines whether the specified <see cref="MongoClientSettings" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="MongoClientSettings" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="MongoClientSettings" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(MongoClientSettings obj)
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
            var rhs = (MongoClientSettings)obj;
            return
                _allowInsecureTls == rhs._allowInsecureTls &&
                _applicationName == rhs._applicationName &&
                object.Equals(_autoEncryptionOptions, rhs._autoEncryptionOptions) &&
                object.ReferenceEquals(_clusterConfigurator, rhs._clusterConfigurator) &&
                object.Equals(_clusterSource, rhs._clusterSource) &&
                _compressors.SequenceEqual(rhs._compressors) &&
                _connectTimeout == rhs._connectTimeout &&
                _credential == rhs._credential &&
                _directConnection.Equals(rhs._directConnection) &&
                _heartbeatInterval == rhs._heartbeatInterval &&
                _heartbeatTimeout == rhs._heartbeatTimeout &&
                _ipv6 == rhs._ipv6 &&
                object.Equals(_libraryInfo, rhs._libraryInfo) &&
                _loadBalanced == rhs._loadBalanced &&
                _localThreshold == rhs._localThreshold &&
                _loggingSettings == rhs._loggingSettings &&
                _maxConnecting == rhs._maxConnecting &&
                _maxConnectionIdleTime == rhs._maxConnectionIdleTime &&
                _maxConnectionLifeTime == rhs._maxConnectionLifeTime &&
                _maxConnectionPoolSize == rhs._maxConnectionPoolSize &&
                _minConnectionPoolSize == rhs._minConnectionPoolSize &&
                object.Equals(_readEncoding, rhs._readEncoding) &&
                object.Equals(_readConcern, rhs._readConcern) &&
                object.Equals(_readPreference, rhs._readPreference) &&
                _replicaSetName == rhs._replicaSetName &&
                _retryReads == rhs._retryReads &&
                _retryWrites == rhs._retryWrites &&
                _scheme == rhs._scheme &&
                _serverApi == rhs._serverApi &&
                _servers.SequenceEqual(rhs._servers) &&
                _serverMonitoringMode == rhs._serverMonitoringMode &&
                _serverSelectionTimeout == rhs._serverSelectionTimeout &&
                _socketTimeout == rhs._socketTimeout &&
                _srvMaxHosts == rhs._srvMaxHosts &&
                _srvServiceName == rhs._srvServiceName &&
                _sslSettings == rhs._sslSettings &&
                object.Equals(_translationOptions, rhs._translationOptions) &&
                _useTls == rhs._useTls &&
                _waitQueueSize == rhs._waitQueueSize &&
                _waitQueueTimeout == rhs._waitQueueTimeout &&
                object.Equals(_writeConcern, rhs._writeConcern) &&
                object.Equals(_writeEncoding, rhs._writeEncoding);
        }

        /// <summary>
        /// Freezes the settings.
        /// </summary>
        /// <returns>The frozen settings.</returns>
        public MongoClientSettings Freeze()
        {
            if (!_isFrozen)
            {
                ThrowIfSettingsAreInvalid();
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

            return new Hasher()
                .Hash(_allowInsecureTls)
                .Hash(_applicationName)
                .Hash(_autoEncryptionOptions)
                .Hash(_clusterConfigurator)
                .Hash(_clusterSource)
                .HashElements(_compressors)
                .Hash(_connectTimeout)
                .Hash(_credential)
                .Hash(_directConnection)
                .Hash(_heartbeatInterval)
                .Hash(_heartbeatTimeout)
                .Hash(_ipv6)
                .Hash(_libraryInfo)
                .Hash(_loadBalanced)
                .Hash(_localThreshold)
                .Hash(_maxConnecting)
                .Hash(_maxConnectionIdleTime)
                .Hash(_maxConnectionLifeTime)
                .Hash(_maxConnectionPoolSize)
                .Hash(_minConnectionPoolSize)
                .Hash(_readConcern)
                .Hash(_readEncoding)
                .Hash(_readPreference)
                .Hash(_replicaSetName)
                .Hash(_retryReads)
                .Hash(_retryWrites)
                .Hash(_scheme)
                .Hash(_serverApi)
                .HashElements(_servers)
                .Hash(_serverMonitoringMode)
                .Hash(_serverSelectionTimeout)
                .Hash(_socketTimeout)
                .Hash(_srvMaxHosts)
                .Hash(_srvServiceName)
                .Hash(_sslSettings)
                .Hash(_translationOptions)
                .Hash(_useTls)
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

            var sb = new StringBuilder();
            if (_applicationName != null)
            {
                sb.AppendFormat("ApplicationName={0};", _applicationName);
            }
            if (_autoEncryptionOptions != null)
            {
                sb.AppendFormat("AutoEncryptionOptions={0};", _autoEncryptionOptions);
            }
            if (_compressors?.Any() ?? false)
            {
                sb.AppendFormat("Compressors=[{0}];", string.Join(",", _compressors.Select(x => CompressorTypeMapper.ToServerName(x.Type))));
            }
            sb.AppendFormat("ConnectTimeout={0};", _connectTimeout);
            sb.AppendFormat("Credential={{{0}}};", _credential);
            sb.AppendFormat("DirectConnection={0};", _directConnection);

            sb.AppendFormat("HeartbeatInterval={0};", _heartbeatInterval);
            sb.AppendFormat("HeartbeatTimeout={0};", _heartbeatTimeout);
            sb.AppendFormat("IPv6={0};", _ipv6);
            if (_libraryInfo != null)
            {
                sb.AppendFormat("libraryInfo={0};", _libraryInfo);
            }
            if (_loadBalanced)
            {
                sb.AppendFormat("LoadBalanced={0};", _loadBalanced);
            }
            sb.AppendFormat("LocalThreshold={0};", _localThreshold);
            sb.AppendFormat("MaxConnecting={0};", _maxConnecting);
            sb.AppendFormat("MaxConnectionIdleTime={0};", _maxConnectionIdleTime);
            sb.AppendFormat("MaxConnectionLifeTime={0};", _maxConnectionLifeTime);
            sb.AppendFormat("MaxConnectionPoolSize={0};", _maxConnectionPoolSize);
            sb.AppendFormat("MinConnectionPoolSize={0};", _minConnectionPoolSize);
            if (_readEncoding != null)
            {
                sb.Append("ReadEncoding=UTF8Encoding;");
            }
            sb.AppendFormat("ReadConcern={0};", _readConcern);
            sb.AppendFormat("ReadPreference={0};", _readPreference);
            sb.AppendFormat("ReplicaSetName={0};", _replicaSetName);
            sb.AppendFormat("RetryReads={0};", _retryReads);
            sb.AppendFormat("RetryWrites={0};", _retryWrites);
            if (_scheme != ConnectionStringScheme.MongoDB)
            {
                sb.AppendFormat("Scheme={0};", _scheme);
            }
            if (_serverApi != null)
            {
                sb.AppendFormat("ServerApi={0};", _serverApi);
            }
            sb.AppendFormat("Servers={0};", string.Join(",", _servers.Select(s => s.ToString()).ToArray()));
            sb.AppendFormat("serverMonitoringMode={0};", _serverMonitoringMode);
            sb.AppendFormat("ServerSelectionTimeout={0};", _serverSelectionTimeout);
            sb.AppendFormat("SocketTimeout={0};", _socketTimeout);
            sb.AppendFormat("SrvMaxHosts={0};", _srvMaxHosts);
            sb.AppendFormat("SrvServiceName={0};", _srvServiceName);
            if (_sslSettings != null)
            {
                sb.AppendFormat("SslSettings={0};", _sslSettings);
            }
            sb.AppendFormat("Tls={0};", _useTls);
            sb.AppendFormat("TlsInsecure={0};", _allowInsecureTls);
            if (_translationOptions != null)
            {
                sb.AppendFormat("TranslationOptions={0};", _translationOptions);
            }
            sb.AppendFormat("WaitQueueSize={0};", _waitQueueSize);
            sb.AppendFormat("WaitQueueTimeout={0}", _waitQueueTimeout);
            sb.AppendFormat("WriteConcern={0};", _writeConcern);
            if (_writeEncoding != null)
            {
                sb.Append("WriteEncoding=UTF8Encoding;");
            }
            return sb.ToString();
        }

        // internal methods
        internal ClusterKey ToClusterKey()
        {
            return new ClusterKey(
                _allowInsecureTls,
                _applicationName,
                _clusterConfigurator,
                _compressors,
                _connectTimeout,
                _credential,
                _autoEncryptionOptions?.ToCryptClientSettings(),
                _directConnection,
                _heartbeatInterval,
                _heartbeatTimeout,
                _ipv6,
                _libraryInfo,
                _loadBalanced,
                _localThreshold,
                _loggingSettings,
                _maxConnecting,
                _maxConnectionIdleTime,
                _maxConnectionLifeTime,
                _maxConnectionPoolSize,
                _minConnectionPoolSize,
                MongoDefaults.TcpReceiveBufferSize, // TODO: add ReceiveBufferSize to MongoClientSettings?
                _replicaSetName,
                _scheme,
                MongoDefaults.TcpSendBufferSize, // TODO: add SendBufferSize to MongoClientSettings?
                _serverApi,
                _servers.ToList(),
                _serverMonitoringMode,
                _serverSelectionTimeout,
                _socketTimeout,
                _srvMaxHosts,
                _srvServiceName,
                _sslSettings,
                _useTls,
                _waitQueueSize,
                _waitQueueTimeout);
        }

        // private methods
        private void ThrowIfFrozen()
        {
            if (_isFrozen)
            {
                throw new InvalidOperationException($"{nameof(MongoClientSettings)} is frozen.");
            }
        }

        private void ThrowIfSettingsAreInvalid()
        {
            if (_allowInsecureTls && _sslSettings != null && _sslSettings.CheckCertificateRevocation)
            {
                throw new InvalidOperationException(
                        $"{nameof(AllowInsecureTls)} and {nameof(SslSettings)}" +
                        $".{nameof(_sslSettings.CheckCertificateRevocation)} cannot both be true.");
            }

            if (_directConnection)
            {
                if (_scheme == ConnectionStringScheme.MongoDBPlusSrv)
                {
                    throw new InvalidOperationException($"SRV cannot be used with direct connections.");
                }

                if (_servers.Count > 1)
                {
                    throw new InvalidOperationException($"Multiple host names cannot be used with direct connections.");
                }
            }

            if (_srvMaxHosts > 0 && _scheme != ConnectionStringScheme.MongoDBPlusSrv)
            {
                throw new InvalidOperationException("srvMaxHosts can only be used with the mongodb+srv scheme.");
            }

            if (_replicaSetName != null && _srvMaxHosts > 0)
            {
                throw new InvalidOperationException("Specifying srvMaxHosts when connecting to a replica set is invalid.");
            }

            if (_srvServiceName != MongoInternalDefaults.MongoClientSettings.SrvServiceName && _scheme != ConnectionStringScheme.MongoDBPlusSrv)
            {
                throw new InvalidOperationException("Specifying srvServiceName is only allowed with the mongodb+srv scheme.");
            }

            if (_loadBalanced)
            {
                if (_servers.Count > 1)
                {
                    throw new InvalidOperationException("Load balanced mode cannot be used with multiple host names.");
                }

                if (_replicaSetName != null)
                {
                    throw new InvalidOperationException("ReplicaSetName cannot be used with load balanced mode.");
                }

                if (_srvMaxHosts > 0)
                {
                    throw new InvalidOperationException("srvMaxHosts cannot be used with load balanced mode.");
                }

                if (_directConnection)
                {
                    throw new InvalidOperationException("Load balanced mode cannot be used with direct connection.");
                }
            }
        }
    }
}
