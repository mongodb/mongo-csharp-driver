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
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver;

/// <summary>
/// A builder for configuring and creating a <see cref="IMongoClient"/>.
/// </summary>
public sealed class MongoClientBuilder
{
    // TODO: ClusterBuilder (existing class used in MongoClientSettings.ClusterConfigurator) contains duplicated settings
    // to the new builder proposed in this file. I believe we should obsolete the ClusterBuilder and
    // make sure we moved all knobs to the new builders infrastructure.

    // TODO: static property MongoClientSettings.Extensions probably need to be moved into the MongoClientBuilder.

    // TODO: ClusterSource property is not implemented yet, need to decide how and where it goes. In scope of
    // MongoClient disposability work. It is internal on MongoClientSettings, so it is not part of the public
    // surface this builder has to replace and does not block shipping.

    private readonly ConnectionPoolBuilder _connectionPoolBuilder = new();
    private MongoCredential _credential;
    private readonly AutoEncryptionBuilder _autoEncryptionBuilder = new();
    private readonly ClientMetadataBuilder _clientMetadataBuilder = new();
    private readonly ConnectivityBuilder _connectivityBuilder = new();
    private readonly DiagnosticsBuilder _diagnosticsBuilder = new();
    private readonly ServerMonitoringBuilder _serverMonitoringBuilder = new();
    private readonly NetworkBuilder _networkBuilder = new();
    private readonly OperationExecutionBuilder _operationExecutionBuilder = new();
    private readonly ServerSelectionBuilder _serverSelectionBuilder = new();
    private readonly TlsBuilder _tlsBuilder = new();
    private readonly TranslationBuilder _translationBuilder = new();

    /// <summary>
    /// Creates a builder initialized from a MongoDB connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromConnectionString(string connectionString)
        => FromConnectionString(new ConnectionString(connectionString));

    /// <summary>
    /// Creates a builder initialized from a MongoDB connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromConnectionString(ConnectionString connectionString)
    {
        // TODO: Discuss if it should be ConnectionString or MongoUrl here, and deprecate another.
        throw new NotImplementedException("Implement mapping from the connection string.");
    }

    /// <summary>
    /// Configures serialization.
    /// </summary>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    // TODO: should have SerializationBuilder parameter
    // TODO: WriteEncoding and ReadEncoding: should we either dropped them or moved under the serialization builder
    public MongoClientBuilder ConfigureSerialization()
    {
        return this;
    }

    /// <summary>
    /// Configures the connection pool.
    /// </summary>
    /// <param name="configure">A delegate that configures the connection pool.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureConnectionPool(Action<ConnectionPoolBuilder> configure)
    {
        configure?.Invoke(_connectionPoolBuilder);
        return this;
    }

    /// <summary>
    /// Configures connectivity: the endpoints the client connects to and the topology it expects.
    /// </summary>
    /// <param name="configure">A delegate that configures connectivity.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureConnectivity(Action<ConnectivityBuilder> configure)
    {
        configure?.Invoke(_connectivityBuilder);
        return this;
    }

    /// <summary>
    /// Configures automatic client-side field level encryption.
    /// </summary>
    /// <param name="configure">A delegate that configures automatic encryption.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureAutoEncryption(Action<AutoEncryptionBuilder> configure)
    {
        configure?.Invoke(_autoEncryptionBuilder);
        return this;
    }

    /// <summary>
    /// Configures the client metadata: the application name and library information the client reports
    /// about itself to the server during the handshake.
    /// </summary>
    /// <param name="configure">A delegate that configures the client metadata.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureClientMetadata(Action<ClientMetadataBuilder> configure)
    {
        configure?.Invoke(_clientMetadataBuilder);
        return this;
    }

    /// <summary>
    /// Configures diagnostics: what the client logs and traces.
    /// </summary>
    /// <param name="configure">A delegate that configures diagnostics.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureDiagnostics(Action<DiagnosticsBuilder> configure)
    {
        configure?.Invoke(_diagnosticsBuilder);
        return this;
    }

    /// <summary>
    /// Configures server monitoring: how the client discovers and tracks the state of each server.
    /// </summary>
    /// <param name="configure">A delegate that configures server monitoring.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureServerMonitoring(Action<ServerMonitoringBuilder> configure)
    {
        configure?.Invoke(_serverMonitoringBuilder);
        return this;
    }

    /// <summary>
    /// Configures server selection: which server in the topology each operation is routed to.
    /// </summary>
    /// <param name="configure">A delegate that configures server selection.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureServerSelection(Action<ServerSelectionBuilder> configure)
    {
        configure?.Invoke(_serverSelectionBuilder);
        return this;
    }

    /// <summary>
    /// Configures the network: the socket level settings and wire compression used to reach the servers.
    /// </summary>
    /// <param name="configure">A delegate that configures the network.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureNetwork(Action<NetworkBuilder> configure)
    {
        configure?.Invoke(_networkBuilder);
        return this;
    }

    /// <summary>
    /// Configures operation execution: the concerns, retry behavior and API version applied to operations
    /// that do not specify their own.
    /// </summary>
    /// <param name="configure">A delegate that configures operation execution.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureOperationExecution(Action<OperationExecutionBuilder> configure)
    {
        configure?.Invoke(_operationExecutionBuilder);
        return this;
    }

    /// <summary>
    /// Configures TLS: whether connections are encrypted and how server and client certificates are handled.
    /// </summary>
    /// <param name="configure">A delegate that configures TLS.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureTls(Action<TlsBuilder> configure)
    {
        configure?.Invoke(_tlsBuilder);
        return this;
    }

    /// <summary>
    /// Configures translation of .NET expression trees into MongoDB expressions.
    /// </summary>
    /// <param name="configure">A delegate that configures translation.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConfigureTranslation(Action<TranslationBuilder> configure)
    {
        configure?.Invoke(_translationBuilder);
        return this;
    }

    /// <summary>
    /// Sets the credential used to authenticate with the server.
    /// </summary>
    /// <param name="credential">The credential, or <c>null</c> to connect without authenticating.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder UseCredentials(MongoCredential credential)
    {
        _credential = credential;
        return this;
    }

    /// <summary>
    /// Builds a <see cref="IMongoClient"/> from the configuration accumulated on this builder.
    /// </summary>
    /// <remarks>
    /// The builder is not consumed by this call; it can be further configured and used to build additional clients.
    /// Each call returns a new client, and the caller is responsible for disposing it.
    /// </remarks>
    /// <returns>A new <see cref="IMongoClient"/>.</returns>
    public IMongoClient Build()
    {
        // TODO: implement constructing of the MongoClient
        return null;
    }
}

/// <summary>
/// Configures the connection pool of a <see cref="MongoClient"/>.
/// </summary>
public sealed class ConnectionPoolBuilder
{
    private TimeSpan _maintenanceInterval;
    private int _maxConnecting;
    private TimeSpan _maxConnectionIdleTime;
    private TimeSpan _maxConnectionLifeTime;
    private int _maxConnectionPoolSize;
    private int _minConnectionPoolSize;
    private int _waitQueueSize;
    private TimeSpan _waitQueueTimeout;

    internal ConnectionPoolBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings and MongoUrlBuilder,
        // which snapshot the same MongoDefaults statics in their constructors
        _maintenanceInterval = TimeSpan.FromMinutes(1);
        _maxConnecting = MongoInternalDefaults.ConnectionPool.MaxConnecting;
        _maxConnectionIdleTime = MongoDefaults.MaxConnectionIdleTime;
        _maxConnectionLifeTime = MongoDefaults.MaxConnectionLifeTime;
        _maxConnectionPoolSize = MongoDefaults.MaxConnectionPoolSize;
        _minConnectionPoolSize = MongoDefaults.MinConnectionPoolSize;
#pragma warning disable 618
        _waitQueueSize = MongoDefaults.ComputedWaitQueueSize;
#pragma warning restore 618
        _waitQueueTimeout = MongoDefaults.WaitQueueTimeout;
    }

    /// <summary>
    /// Gets or sets the interval at which the pool prunes idle and expired connections and tops up
    /// to <see cref="MinConnectionPoolSize"/>. <see cref="Timeout.InfiniteTimeSpan"/> disables the
    /// maintenance thread. Must be infinite, or greater than or equal to zero. The default value is 1 minute.
    /// </summary>
    public TimeSpan MaintenanceInterval
    {
        get => _maintenanceInterval;
        set => _maintenanceInterval = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(MaintenanceInterval));
    }

    /// <summary>
    /// Gets or sets the maximum number of connections the pool may be establishing concurrently.
    /// Must be greater than zero. Defaults to <c>MongoInternalDefaults.ConnectionPool.MaxConnecting</c> (2).
    /// </summary>
    public int MaxConnecting
    {
        get => _maxConnecting;
        set => _maxConnecting = Ensure.IsGreaterThanZero(value, nameof(MaxConnecting));
    }

    /// <summary>
    /// Gets or sets the maximum time a connection may remain idle in the pool before it is closed.
    /// Must be greater than or equal to zero. Defaults to <see cref="MongoDefaults.MaxConnectionIdleTime"/> (10 minutes).
    /// </summary>
    public TimeSpan MaxConnectionIdleTime
    {
        get => _maxConnectionIdleTime;
        set => _maxConnectionIdleTime = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MaxConnectionIdleTime));
    }

    /// <summary>
    /// Gets or sets the maximum time a connection may remain in the pool before it is closed.
    /// Must be greater than zero. Defaults to <see cref="MongoDefaults.MaxConnectionLifeTime"/> (30 minutes).
    /// </summary>
    public TimeSpan MaxConnectionLifeTime
    {
        get => _maxConnectionLifeTime;
        set => _maxConnectionLifeTime = Ensure.IsGreaterThanZero(value, nameof(MaxConnectionLifeTime));
    }

    /// <summary>
    /// Gets or sets the maximum number of connections the pool may contain.
    /// Must be greater than zero. Defaults to <see cref="MongoDefaults.MaxConnectionPoolSize"/> (100).
    /// </summary>
    public int MaxConnectionPoolSize
    {
        get => _maxConnectionPoolSize;
        set => _maxConnectionPoolSize = Ensure.IsGreaterThanZero(value, nameof(MaxConnectionPoolSize));
    }

    /// <summary>
    /// Gets or sets the minimum number of connections the pool maintains.
    /// Must be greater than or equal to zero. Defaults to <see cref="MongoDefaults.MinConnectionPoolSize"/> (0).
    /// </summary>
    public int MinConnectionPoolSize
    {
        get => _minConnectionPoolSize;
        set => _minConnectionPoolSize = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MinConnectionPoolSize));
    }

    /// <summary>
    /// Gets or sets the maximum number of threads that may be waiting for a connection to become available.
    /// Must be greater than or equal to zero. Defaults to <c>MongoDefaults.ComputedWaitQueueSize</c>
    /// (<see cref="MongoDefaults.MaxConnectionPoolSize"/> x <c>MongoDefaults.WaitQueueMultiple</c>, i.e. 500).
    /// </summary>
    [Obsolete("This property will be removed in a later release.")]
    public int WaitQueueSize
    {
        get => _waitQueueSize;
        set => _waitQueueSize = Ensure.IsGreaterThanOrEqualToZero(value, nameof(WaitQueueSize));
    }

    /// <summary>
    /// Gets or sets the maximum time a thread waits for a connection to become available.
    /// Must be infinite, or greater than or equal to zero. Defaults to <see cref="MongoDefaults.WaitQueueTimeout"/> (2 minutes).
    /// </summary>
    public TimeSpan WaitQueueTimeout
    {
        get => _waitQueueTimeout;
        set => _waitQueueTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(WaitQueueTimeout));
    }
}

/// <summary>
/// Configures the endpoints a <see cref="MongoClient"/> connects to and the topology it expects.
/// </summary>
public sealed class ConnectivityBuilder
{
    private bool _directConnection;
    private bool _ipv6;
    private bool _loadBalanced;
    private string _replicaSetName;
    private ConnectionStringScheme _scheme;
    private List<MongoServerAddress> _servers;
    private int _srvMaxHosts;
    private string _srvServiceName;

    internal ConnectivityBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _directConnection = false;
        _ipv6 = false;
        _loadBalanced = false;
        _replicaSetName = null;
        _scheme = ConnectionStringScheme.MongoDB;
        _servers = new List<MongoServerAddress> { new MongoServerAddress("localhost") };
        _srvMaxHosts = 0;
        _srvServiceName = MongoInternalDefaults.MongoClientSettings.SrvServiceName;
    }

    /// <summary>
    /// Gets or sets whether the client connects directly to a single server rather than discovering
    /// and monitoring the topology it belongs to. The default value is <c>false</c>.
    /// </summary>
    public bool DirectConnection
    {
        get => _directConnection;
        set => _directConnection = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to use IPv6. The default value is <c>false</c>.
    /// </summary>
    public bool IPv6
    {
        get => _ipv6;
        set => _ipv6 = value;
    }

    /// <summary>
    /// Gets or sets whether load balanced mode is used. The default value is <c>false</c>.
    /// </summary>
    public bool LoadBalanced
    {
        get => _loadBalanced;
        set => _loadBalanced = value;
    }

    /// <summary>
    /// Gets or sets the name of the replica set. The default value is <c>null</c>.
    /// </summary>
    public string ReplicaSetName
    {
        get => _replicaSetName;
        set => _replicaSetName = value;
    }

    /// <summary>
    /// Gets or sets the connection string scheme. The default value is
    /// <see cref="ConnectionStringScheme.MongoDB"/>.
    /// </summary>
    public ConnectionStringScheme Scheme
    {
        get => _scheme;
        set => _scheme = value;
    }

    /// <summary>
    /// Gets or sets the list of server addresses. The default value is a single
    /// <c>localhost:27017</c> address.
    /// </summary>
    public IEnumerable<MongoServerAddress> Servers
    {
        get => new ReadOnlyCollection<MongoServerAddress>(_servers);
        set => _servers = new List<MongoServerAddress>(Ensure.IsNotNull(value, nameof(Servers)));
    }

    /// <summary>
    /// Gets or sets the limit on the number of SRV records used to populate the seedlist during initial
    /// discovery, as well as the number of additional hosts that may be added during SRV polling.
    /// Zero means no limit. Must be greater than or equal to zero. The default value is 0.
    /// </summary>
    public int SrvMaxHosts
    {
        get => _srvMaxHosts;
        set => _srvMaxHosts = Ensure.IsGreaterThanOrEqualToZero(value, nameof(SrvMaxHosts));
    }

    /// <summary>
    /// Gets or sets the SRV service name, which modifies the SRV URI to look like:
    /// <code>_{srvServiceName}._tcp.{hostname}.{domainname}</code>
    /// Must not be null or empty. The default value is "mongodb".
    /// </summary>
    public string SrvServiceName
    {
        get => _srvServiceName;
        set => _srvServiceName = Ensure.IsNotNullOrEmpty(value, nameof(SrvServiceName));
    }
}

/// <summary>
/// Configures what a <see cref="MongoClient"/> logs and traces.
/// </summary>
public sealed class DiagnosticsBuilder
{
    private readonly EventAggregator _eventAggregator = new();

    private LoggingSettings _loggingSettings;
    private TracingOptions _tracingOptions;

    internal DiagnosticsBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _loggingSettings = null;
        _tracingOptions = null;
    }

    /// <summary>
    /// Gets or sets the logging settings. The default value is <c>null</c>, which disables logging.
    /// </summary>
    public LoggingSettings LoggingSettings
    {
        get => _loggingSettings;
        set => _loggingSettings = value;
    }

    /// <summary>
    /// Gets or sets the tracing options for OpenTelemetry instrumentation. The default value is
    /// <c>null</c>, which disables tracing.
    /// </summary>
    public TracingOptions TracingOptions
    {
        get => _tracingOptions;
        set => _tracingOptions = value;
    }

    /// <summary>
    /// Subscribes a handler to events of type <typeparamref name="TEvent"/>. Subscriptions accumulate:
    /// calling this more than once, including more than once for the same event type, adds handlers
    /// rather than replacing them.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="handler">The handler.</param>
    /// <returns>The same <see cref="DiagnosticsBuilder"/> instance so that calls can be chained.</returns>
    /// <remarks>
    /// Command events carry the full command and reply, apart from a closed allowlist of
    /// authentication-bearing commands whose bodies the driver redacts. Bulk operations can therefore
    /// deliver multi-megabyte payloads; filter or truncate in the handler.
    /// </remarks>
    public DiagnosticsBuilder Subscribe<TEvent>(Action<TEvent> handler)
    {
        Ensure.IsNotNull(handler, nameof(handler));

        _eventAggregator.Subscribe(handler);
        return this;
    }

    /// <summary>
    /// Subscribes the specified subscriber. Subscriptions accumulate: calling this more than once adds
    /// subscribers rather than replacing them.
    /// </summary>
    /// <param name="subscriber">The subscriber.</param>
    /// <returns>The same <see cref="DiagnosticsBuilder"/> instance so that calls can be chained.</returns>
    /// <remarks>
    /// Command events carry the full command and reply, apart from a closed allowlist of
    /// authentication-bearing commands whose bodies the driver redacts. Bulk operations can therefore
    /// deliver multi-megabyte payloads; filter or truncate in the subscriber.
    /// </remarks>
    public DiagnosticsBuilder Subscribe(IEventSubscriber subscriber)
    {
        Ensure.IsNotNull(subscriber, nameof(subscriber));

        _eventAggregator.Subscribe(subscriber);
        return this;
    }
}

/// <summary>
/// Configures the metadata a <see cref="MongoClient"/> reports about itself to the server during the
/// handshake. Most applications only set <see cref="ApplicationName"/>, which identifies the application
/// in server logs, in profiler output, and in the Atlas UI.
/// </summary>
public sealed class ClientMetadataBuilder
{
    private string _applicationName;
    private LibraryInfo _libraryInfo;

    internal ClientMetadataBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _applicationName = null;
        _libraryInfo = null;
    }

    /// <summary>
    /// Gets or sets the application name reported to the server during the handshake and surfaced in
    /// server logs and profiler output. Must be at most 128 bytes when encoded as UTF-8.
    /// The default value is <c>null</c>.
    /// </summary>
    public string ApplicationName
    {
        get => _applicationName;
        set => _applicationName = ApplicationNameHelper.EnsureApplicationNameIsValid(value, nameof(ApplicationName));
    }

    /// <summary>
    /// Gets or sets information about a library built on top of the .NET driver, appended to the driver
    /// metadata sent during the handshake. Intended for ODMs and framework integrations rather than
    /// applications. The default value is <c>null</c>.
    /// </summary>
    public LibraryInfo LibraryInfo
    {
        get => _libraryInfo;
        set => _libraryInfo = value;
    }
}

/// <summary>
/// Configures how a <see cref="MongoClient"/> discovers and tracks the state of each server in the
/// topology (SDAM monitoring).
/// </summary>
public sealed class ServerMonitoringBuilder
{
    private TimeSpan _heartbeatInterval;
    private TimeSpan _heartbeatTimeout;
    private ServerMonitoringMode _serverMonitoringMode;

    internal ServerMonitoringBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _heartbeatInterval = ServerSettings.DefaultHeartbeatInterval;
        _heartbeatTimeout = ServerSettings.DefaultHeartbeatTimeout;
        _serverMonitoringMode = ServerSettings.DefaultServerMonitoringMode;
    }

    /// <summary>
    /// Gets or sets the interval between checks of each server's state. Must be greater than zero.
    /// Defaults to <see cref="ServerSettings.DefaultHeartbeatInterval"/> (10 seconds).
    /// </summary>
    public TimeSpan HeartbeatInterval
    {
        get => _heartbeatInterval;
        set => _heartbeatInterval = Ensure.IsGreaterThanZero(value, nameof(HeartbeatInterval));
    }

    /// <summary>
    /// Gets or sets how long a single server check may take before it is considered to have failed.
    /// Must be infinite, or greater than zero. Defaults to
    /// <see cref="ServerSettings.DefaultHeartbeatTimeout"/> (infinite, meaning the connect timeout applies).
    /// </summary>
    public TimeSpan HeartbeatTimeout
    {
        get => _heartbeatTimeout;
        set => _heartbeatTimeout = Ensure.IsInfiniteOrGreaterThanZero(value, nameof(HeartbeatTimeout));
    }

    /// <summary>
    /// Gets or sets whether servers are monitored with the streaming or polling protocol. Defaults to
    /// <see cref="ServerMonitoringMode.Auto"/>, which uses streaming except when the client detects that
    /// it is running inside a FaaS environment.
    /// </summary>
    public ServerMonitoringMode ServerMonitoringMode
    {
        get => _serverMonitoringMode;
        set => _serverMonitoringMode = value;
    }
}

/// <summary>
/// Configures which server in the topology a <see cref="MongoClient"/> routes each operation to.
/// </summary>
public sealed class ServerSelectionBuilder
{
    private TimeSpan _localThreshold;
    private ReadPreference _readPreference;
    private TimeSpan _serverSelectionTimeout;

    internal ServerSelectionBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _localThreshold = MongoDefaults.LocalThreshold;
        _readPreference = ReadPreference.Primary;
        _serverSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
    }

    /// <summary>
    /// Gets or sets the window, measured from the round trip time of the fastest suitable server, within
    /// which other servers are still considered eligible for selection. Must be infinite, or greater than
    /// or equal to zero. Defaults to <see cref="MongoDefaults.LocalThreshold"/> (15 milliseconds).
    /// </summary>
    public TimeSpan LocalThreshold
    {
        get => _localThreshold;
        set => _localThreshold = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(LocalThreshold));
    }

    /// <summary>
    /// Gets or sets the default read preference used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="ReadPreference.Primary"/>.
    /// </summary>
    public ReadPreference ReadPreference
    {
        get => _readPreference;
        set => _readPreference = Ensure.IsNotNull(value, nameof(ReadPreference));
    }

    /// <summary>
    /// Gets or sets how long the client waits for a suitable server to become available before failing
    /// the operation. Must be greater than or equal to zero. Defaults to
    /// <see cref="MongoDefaults.ServerSelectionTimeout"/> (30 seconds).
    /// </summary>
    public TimeSpan ServerSelectionTimeout
    {
        get => _serverSelectionTimeout;
        set => _serverSelectionTimeout = Ensure.IsGreaterThanOrEqualToZero(value, nameof(ServerSelectionTimeout));
    }
}

/// <summary>
/// Configures whether a <see cref="MongoClient"/> encrypts its connections with TLS, and how server and
/// client certificates are handled.
/// </summary>
public sealed class TlsBuilder
{
    private bool _allowInsecureTls;
    private bool _checkCertificateRevocation;
    private X509Certificate[] _clientCertificates;
    private LocalCertificateSelectionCallback _clientCertificateSelectionCallback;
    private SslProtocols _enabledProtocols;
    private RemoteCertificateValidationCallback _serverCertificateValidationCallback;
    private bool _useTls;

    internal TlsBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings and SslSettings,
        // which snapshot the same values in their constructors
        _allowInsecureTls = false;
        _checkCertificateRevocation = false;
        _clientCertificates = null;
        _clientCertificateSelectionCallback = null;
        _enabledProtocols = SslStreamSettings.SslProtocolsTls13 | SslProtocols.Tls12;
        _serverCertificateValidationCallback = null;
        _useTls = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to relax TLS constraints as much as possible. Setting this
    /// to <c>true</c> disables both certificate and host name validation, and forces
    /// <see cref="CheckCertificateRevocation"/> to <c>false</c>. Use with care; it is intended for
    /// testing against servers with self-signed certificates. The default value is <c>false</c>.
    /// </summary>
    public bool AllowInsecureTls
    {
        get => _allowInsecureTls;
        set
        {
            if (value)
            {
                // this is the only way to get the desired behavior in the underlying stream
                _checkCertificateRevocation = false;
            }
            _allowInsecureTls = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to check for certificate revocation. Cannot be set to
    /// <c>true</c> while <see cref="AllowInsecureTls"/> is <c>true</c>. The default value is <c>false</c>.
    /// </summary>
    public bool CheckCertificateRevocation
    {
        get => _checkCertificateRevocation;
        set
        {
            if (value && _allowInsecureTls)
            {
                throw new InvalidOperationException(
                    $"{nameof(AllowInsecureTls)} and {nameof(CheckCertificateRevocation)} cannot both be true.");
            }
            _checkCertificateRevocation = value;
        }
    }

    /// <summary>
    /// Gets or sets the client certificates presented to the server. The default value is <c>null</c>.
    /// </summary>
    public IEnumerable<X509Certificate> ClientCertificates
    {
        get => _clientCertificates;
        set => _clientCertificates = value?.ToArray();
    }

    /// <summary>
    /// Gets or sets the callback used to select the client certificate presented to the server.
    /// The default value is <c>null</c>.
    /// </summary>
    public LocalCertificateSelectionCallback ClientCertificateSelectionCallback
    {
        get => _clientCertificateSelectionCallback;
        set => _clientCertificateSelectionCallback = value;
    }

    /// <summary>
    /// Gets or sets the enabled TLS protocol versions. The default value is TLS 1.2 and TLS 1.3.
    /// </summary>
    public SslProtocols EnabledProtocols
    {
        get => _enabledProtocols;
        set => _enabledProtocols = value;
    }

    /// <summary>
    /// Gets or sets the callback used to validate the certificate presented by the server.
    /// The default value is <c>null</c>, which uses the default validation.
    /// </summary>
    public RemoteCertificateValidationCallback ServerCertificateValidationCallback
    {
        get => _serverCertificateValidationCallback;
        set => _serverCertificateValidationCallback = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to connect using TLS. The default value is <c>false</c>.
    /// </summary>
    public bool UseTls
    {
        get => _useTls;
        set => _useTls = value;
    }
}

/// <summary>
/// Configures the concerns, retry behavior and API version a <see cref="MongoClient"/> applies to
/// operations that do not specify their own.
/// </summary>
public sealed class OperationExecutionBuilder
{
    private bool _enableOverloadRetargeting;
    private int _maxAdaptiveRetries;
    private ReadConcern _readConcern;
    private bool _retryReads;
    private bool _retryWrites;
    private ServerApi _serverApi;
    private WriteConcern _writeConcern;

    internal OperationExecutionBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _enableOverloadRetargeting = false;
        _maxAdaptiveRetries = RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries;
        _readConcern = ReadConcern.Default;
        _retryReads = true;
        _retryWrites = true;
        _serverApi = null;
        _writeConcern = WriteConcern.Acknowledged;
    }

    /// <summary>
    /// Gets or sets whether overload retargeting is enabled. The default value is <c>false</c>.
    /// </summary>
    /// <remarks>This option requires MongoDB Atlas Server Version 9.0 and above.</remarks>
    public bool EnableOverloadRetargeting
    {
        get => _enableOverloadRetargeting;
        set => _enableOverloadRetargeting = value;
    }

    /// <summary>
    /// Gets or sets the maximum number of adaptive retries for overload errors. Must be greater than or
    /// equal to zero. The default value is 2.
    /// </summary>
    /// <remarks>This option requires MongoDB Atlas Server Version 9.0 and above.</remarks>
    public int MaxAdaptiveRetries
    {
        get => _maxAdaptiveRetries;
        set => _maxAdaptiveRetries = Ensure.IsGreaterThanOrEqualToZero(value, nameof(MaxAdaptiveRetries));
    }

    /// <summary>
    /// Gets or sets the default read concern used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="ReadConcern.Default"/>.
    /// </summary>
    public ReadConcern ReadConcern
    {
        get => _readConcern;
        set => _readConcern = Ensure.IsNotNull(value, nameof(ReadConcern));
    }

    /// <summary>
    /// Gets or sets whether reads are retried once after a retryable error. The default value is <c>true</c>.
    /// </summary>
    public bool RetryReads
    {
        get => _retryReads;
        set => _retryReads = value;
    }

    /// <summary>
    /// Gets or sets whether writes are retried once after a retryable error. The default value is <c>true</c>.
    /// </summary>
    public bool RetryWrites
    {
        get => _retryWrites;
        set => _retryWrites = value;
    }

    /// <summary>
    /// Gets or sets the server API version to declare on every command. The default value is <c>null</c>,
    /// which does not declare an API version.
    /// </summary>
    public ServerApi ServerApi
    {
        get => _serverApi;
        set => _serverApi = value;
    }

    /// <summary>
    /// Gets or sets the default write concern used by operations that do not specify their own.
    /// Must not be null. Defaults to <see cref="WriteConcern.Acknowledged"/>.
    /// </summary>
    public WriteConcern WriteConcern
    {
        get => _writeConcern;
        set => _writeConcern = Ensure.IsNotNull(value, nameof(WriteConcern));
    }
}

/// <summary>
/// Configures how a <see cref="MongoClient"/> translates .NET expression trees into MongoDB expressions.
/// These are the defaults for operations that do not specify their own translation options.
/// </summary>
public sealed class TranslationBuilder
{
    private ServerVersion? _compatibilityLevel;
    private bool? _enableClientSideProjections;

    internal TranslationBuilder()
    {
        // null means "not specified", so that per-operation translation options can supply a value
        _compatibilityLevel = null;
        _enableClientSideProjections = null;
    }

    /// <summary>
    /// Gets or sets the server version to target when translating expressions. The default value is
    /// <c>null</c>, which targets the newest supported behavior.
    /// </summary>
    public ServerVersion? CompatibilityLevel
    {
        get => _compatibilityLevel;
        set => _compatibilityLevel = value;
    }

    /// <summary>
    /// Gets or sets whether client side projections are enabled. The default value is <c>null</c>,
    /// which leaves the provider default in effect.
    /// </summary>
    public bool? EnableClientSideProjections
    {
        get => _enableClientSideProjections;
        set => _enableClientSideProjections = value;
    }
}

/// <summary>
/// Configures automatic client-side field level encryption for a <see cref="MongoClient"/>.
/// </summary>
/// <remarks>
/// <see cref="KeyVaultNamespace"/> is required, as is at least one KMS provider registered with
/// <see cref="RegisterKmsProvider(string, IReadOnlyDictionary{string, object}, SslSettings)"/>. Both are
/// verified when the client is built.
/// </remarks>
public sealed class AutoEncryptionBuilder
{
    private readonly Dictionary<string, BsonDocument> _encryptedFieldsMap = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, object>> _kmsProviders = new();
    private readonly Dictionary<string, BsonDocument> _schemaMap = new();
    private readonly Dictionary<string, SslSettings> _tlsOptions = new();

    private bool _bypassAutoEncryption;
    private bool? _bypassQueryAnalysis;
    private IReadOnlyDictionary<string, object> _extraOptions;
    private TimeSpan? _keyExpiration;
    private IMongoClient _keyVaultClient;
    private CollectionNamespace _keyVaultNamespace;
    private IKmsConnector _kmsConnector;

    internal AutoEncryptionBuilder()
    {
        // defaults are read here so that this builder agrees with AutoEncryptionOptions,
        // which applies the same defaults in its constructor
        _bypassAutoEncryption = false;
        _bypassQueryAnalysis = null;
        _extraOptions = null;
        _keyExpiration = null;
        _keyVaultClient = null;
        _keyVaultNamespace = null;
        _kmsConnector = null;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass automatic encryption. Decryption still occurs.
    /// The default value is <c>false</c>.
    /// </summary>
    public bool BypassAutoEncryption
    {
        get => _bypassAutoEncryption;
        set => _bypassAutoEncryption = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to bypass query analysis, which removes the need for
    /// mongocryptd or the crypt shared library. The default value is <c>null</c>.
    /// </summary>
    public bool? BypassQueryAnalysis
    {
        get => _bypassQueryAnalysis;
        set => _bypassQueryAnalysis = value;
    }

    /// <summary>
    /// Gets or sets options passed through to libmongocrypt, such as the mongocryptd spawn arguments.
    /// The default value is <c>null</c>.
    /// </summary>
    public IReadOnlyDictionary<string, object> ExtraOptions
    {
        get => _extraOptions;
        set => _extraOptions = value;
    }

    /// <summary>
    /// Gets or sets how long a data key may be cached before it is fetched from the key vault again.
    /// The default value is <c>null</c>, which uses the libmongocrypt default of 60 seconds.
    /// </summary>
    public TimeSpan? KeyExpiration
    {
        get => _keyExpiration;
        set => _keyExpiration = value;
    }

    /// <summary>
    /// Gets or sets the client used to access the key vault. The default value is <c>null</c>, which
    /// uses the client being built.
    /// </summary>
    public IMongoClient KeyVaultClient
    {
        get => _keyVaultClient;
        set => _keyVaultClient = value;
    }

    /// <summary>
    /// Gets or sets the collection holding the data keys. Required.
    /// </summary>
    public CollectionNamespace KeyVaultNamespace
    {
        get => _keyVaultNamespace;
        set => _keyVaultNamespace = Ensure.IsNotNull(value, nameof(KeyVaultNamespace));
    }

    /// <summary>
    /// Gets or sets the connector used to open connections to KMS hosts. The default value is
    /// <c>null</c>, which connects directly.
    /// </summary>
    public IKmsConnector KmsConnector
    {
        get => _kmsConnector;
        set => _kmsConnector = value;
    }

    // TODO: reevaluate taking SslSettings here. Cluster TLS is flattened into TlsBuilder, so this is the
    // only remaining place SslSettings appears on the new builder surface, which keeps the type alive.
    // Options: keep it (per-KMS-host TLS is a genuinely different concern from cluster TLS), or introduce
    // a small dedicated per-provider TLS type.

    // NOTE: do not merge these overloads into one method with optional parameters. Optional parameter
    // defaults are compiled into the caller's assembly, so adding a parameter later would break already
    // compiled callers, and adding an overload would make existing call sites ambiguous. Keeping every
    // parameter required leaves room to add one more overload without breaking anything.
    /// <summary>
    /// Registers a KMS provider.
    /// </summary>
    /// <param name="name">
    /// The provider name, either a bare provider such as <c>"aws"</c> or a named provider such as
    /// <c>"aws:name1"</c>. The name is opaque to the driver and is interpreted by libmongocrypt.
    /// </param>
    /// <param name="options">
    /// The provider options. Every value must be a <see cref="string"/> or a <c>byte[]</c>.
    /// An empty dictionary requests on-demand credentials for providers that support them.
    /// </param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options)
        => RegisterKmsProvider(name, options, tlsSettings: null);

    /// <summary>
    /// Registers a KMS provider and the TLS settings used to reach it.
    /// </summary>
    /// <param name="name">
    /// The provider name, either a bare provider such as <c>"aws"</c> or a named provider such as
    /// <c>"aws:name1"</c>. The name is opaque to the driver and is interpreted by libmongocrypt.
    /// </param>
    /// <param name="options">
    /// The provider options. Every value must be a <see cref="string"/> or a <c>byte[]</c>.
    /// An empty dictionary requests on-demand credentials for providers that support them.
    /// </param>
    /// <param name="tlsSettings">The TLS settings used to reach the provider, or <c>null</c> for the defaults.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterKmsProvider(
        string name,
        IReadOnlyDictionary<string, object> options,
        SslSettings tlsSettings)
    {
        Ensure.IsNotNullOrEmpty(name, nameof(name));
        Ensure.IsNotNull(options, nameof(options));

        if (_kmsProviders.ContainsKey(name))
        {
            throw new InvalidOperationException($"KMS provider \"{name}\" is already registered.");
        }

        foreach (var option in options)
        {
            var value = Ensure.IsNotNull(option.Value, $"{nameof(options)}[\"{option.Key}\"]");
            if (!(value is string || value is byte[]))
            {
                throw new ArgumentException(
                    $"Invalid KMS provider option type: {value.GetType().Name}. Must be a string or a byte[].",
                    nameof(options));
            }
        }

        if (tlsSettings != null && tlsSettings.ServerCertificateValidationCallback != null)
        {
            throw new ArgumentException("Insecure TLS options prohibited.", nameof(tlsSettings));
        }

        _kmsProviders.Add(name, options);
        if (tlsSettings != null)
        {
            _tlsOptions.Add(name, tlsSettings);
        }

        return this;
    }

    /// <summary>
    /// Registers the encrypted fields of a Queryable Encryption collection. A collection cannot have both
    /// encrypted fields and a schema.
    /// </summary>
    /// <param name="collectionNamespace">The collection.</param>
    /// <param name="encryptedFields">The encryptedFields document.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterEncryptedFields(CollectionNamespace collectionNamespace, BsonDocument encryptedFields)
    {
        Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        Ensure.IsNotNull(encryptedFields, nameof(encryptedFields));

        var key = collectionNamespace.FullName;
        if (_schemaMap.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Collection \"{key}\" already has a schema registered; a collection cannot have both a schema and encrypted fields.");
        }
        if (_encryptedFieldsMap.ContainsKey(key))
        {
            throw new InvalidOperationException($"Collection \"{key}\" already has encrypted fields registered.");
        }

        _encryptedFieldsMap.Add(key, encryptedFields);
        return this;
    }

    /// <summary>
    /// Registers the JSON schema of a client-side field level encryption collection. A collection cannot
    /// have both a schema and encrypted fields.
    /// </summary>
    /// <param name="collectionNamespace">The collection.</param>
    /// <param name="schema">The JSON schema document.</param>
    /// <returns>The same <see cref="AutoEncryptionBuilder"/> instance so that calls can be chained.</returns>
    public AutoEncryptionBuilder RegisterSchema(CollectionNamespace collectionNamespace, BsonDocument schema)
    {
        Ensure.IsNotNull(collectionNamespace, nameof(collectionNamespace));
        Ensure.IsNotNull(schema, nameof(schema));

        var key = collectionNamespace.FullName;
        if (_encryptedFieldsMap.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Collection \"{key}\" already has encrypted fields registered; a collection cannot have both a schema and encrypted fields.");
        }
        if (_schemaMap.ContainsKey(key))
        {
            throw new InvalidOperationException($"Collection \"{key}\" already has a schema registered.");
        }

        _schemaMap.Add(key, schema);
        return this;
    }
}

/// <summary>
/// Configures the socket level settings and wire compression a <see cref="MongoClient"/> uses to reach
/// the servers.
/// </summary>
public sealed class NetworkBuilder
{
    private IReadOnlyList<CompressorConfiguration> _compressors;
    private TimeSpan _connectTimeout;
    private TimeSpan _socketTimeout;
    private Socks5ProxySettings _socks5ProxySettings;

    internal NetworkBuilder()
    {
        // defaults are read here so that this builder agrees with MongoClientSettings,
        // which snapshots the same values in its constructor
        _compressors = new CompressorConfiguration[0];
        _connectTimeout = MongoDefaults.ConnectTimeout;
        _socketTimeout = MongoDefaults.SocketTimeout;
        _socks5ProxySettings = null;
    }

    /// <summary>
    /// Gets or sets the compressors offered to the server, in preference order. The connection uses the
    /// first one the server also supports, or no compression if there is no overlap. Must not be null.
    /// The default value is an empty list, which disables compression.
    /// </summary>
    public IReadOnlyList<CompressorConfiguration> Compressors
    {
        get => _compressors;
        set => _compressors = Ensure.IsNotNull(value, nameof(Compressors));
    }

    /// <summary>
    /// Gets or sets how long to wait for a socket to connect. Must be infinite, or greater than or equal
    /// to zero. Defaults to <see cref="MongoDefaults.ConnectTimeout"/> (30 seconds).
    /// </summary>
    public TimeSpan ConnectTimeout
    {
        get => _connectTimeout;
        set => _connectTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(ConnectTimeout));
    }

    /// <summary>
    /// Gets or sets the socket read and write timeout. Must be infinite, or greater than or equal to zero.
    /// Defaults to <see cref="MongoDefaults.SocketTimeout"/> (zero, which uses the operating system default).
    /// </summary>
    public TimeSpan SocketTimeout
    {
        get => _socketTimeout;
        set => _socketTimeout = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(SocketTimeout));
    }

    /// <summary>
    /// Gets or sets the SOCKS5 proxy the client connects through. The default value is <c>null</c>,
    /// which connects directly.
    /// </summary>
    public Socks5ProxySettings Socks5ProxySettings
    {
        get => _socks5ProxySettings;
        set => _socks5ProxySettings = value;
    }
}
