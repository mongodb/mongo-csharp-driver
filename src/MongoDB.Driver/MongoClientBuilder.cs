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
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using ClusterBuilder = MongoDB.Driver.Configuration.ClusterBuilder;
using CoreClusterBuilder = MongoDB.Driver.Core.Configuration.ClusterBuilder;

namespace MongoDB.Driver;

/// <summary>
/// A builder for configuring and creating a <see cref="IMongoClient"/>.
/// </summary>
public sealed class MongoClientBuilder
{
    /// <summary>
    /// Extension Manager provides a way to configure extensions for the driver.
    /// </summary>
    public static readonly IExtensionManager Extensions = new ExtensionManager();

    // TODO: ClusterSource property is not implemented yet, need to decide how and where it goes. In scope of
    // MongoClient disposability work. It is internal on MongoClientSettings, so it is not part of the public
    // surface this builder has to replace and does not block shipping.

    private readonly AuthenticationBuilder _authenticationBuilder = new();
    private readonly AutoEncryptionBuilder _autoEncryptionBuilder = new();
    private readonly ClientMetadataBuilder _clientMetadataBuilder = new();
    private readonly ClusterBuilder _clusterBuilder;
    private readonly ConnectionPoolBuilder _connectionPoolBuilder = new();
    private readonly DiagnosticsBuilder _diagnosticsBuilder = new();
    private readonly NetworkBuilder _networkBuilder = new();
    private readonly OperationsBuilder _operationsBuilder = new();
    private readonly ServerMonitoringBuilder _serverMonitoringBuilder = new();
    private readonly TlsBuilder _tlsBuilder = new();
    private readonly TranslationBuilder _translationBuilder = new();

    private MongoClientBuilder(ClusterBuilder clusterBuilder)
    {
        _clusterBuilder = clusterBuilder;
    }

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
        Ensure.IsNotNull(connectionString, nameof(connectionString));
        // TODO: Discuss if it should be ConnectionString or MongoUrl here, and deprecate another.

        connectionString = connectionString.Resolve(resolveHosts: false);

        var builder = new MongoClientBuilder(new ClusterBuilder(connectionString.Scheme, connectionString.Hosts));
        builder._authenticationBuilder.ApplyConnectionString(connectionString);
        builder._clientMetadataBuilder.ApplyConnectionString(connectionString);
        builder._clusterBuilder.ApplyConnectionString(connectionString);
        builder._connectionPoolBuilder.ApplyConnectionString(connectionString);
        builder._networkBuilder.ApplyConnectionString(connectionString);
        builder._operationsBuilder.ApplyConnectionString(connectionString);
        builder._serverMonitoringBuilder.ApplyConnectionString(connectionString);
        builder._tlsBuilder.ApplyConnectionString(connectionString);
        return builder;
    }

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoint">The seed addresses as <c>host</c> or <c>host:port</c>. The default port is 27017.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromEndPoint(string endPoint)
        => FromEndPoint(ParseEndPoint(endPoint, nameof(endPoint)));

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoint">The seed end point.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromEndPoint(EndPoint endPoint)
        => new(new ClusterBuilder(ConnectionStringScheme.MongoDB, [endPoint]));

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoints">The seed addresses as <c>host</c> or <c>host:port</c>. The default port is 27017.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromEndPoints(IEnumerable<string> endPoints)
        => FromEndPoints(ParseEndPoints(endPoints, nameof(endPoints)));

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoints">The seed end points.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromEndPoints(IEnumerable<EndPoint> endPoints)
        => new(new ClusterBuilder(ConnectionStringScheme.MongoDB, endPoints.ToArray()));

    /// <summary>
    /// Creates a builder that connects to a mongodb+srv:// deployment: the servers are found by looking up
    /// the SRV records of the given host name.
    /// </summary>
    /// <param name="host">The SRV host name, without a port, for example <c>cluster0.example.mongodb.net</c>.</param>
    /// <returns>A new <see cref="MongoClientBuilder"/>.</returns>
    public static MongoClientBuilder FromSrv(string host)
    {
        Ensure.IsNotNullOrEmpty(host, nameof(host));
        if (host.IndexOf(':') >= 0)
        {
            throw new ArgumentException("The SRV host name cannot specify a port.", nameof(host));
        }

        if (ParseEndPoint(host, nameof(host)) is not DnsEndPoint dnsEndPoint)
        {
            throw new ArgumentException("The SRV host must be a DNS name, not an IP address.", nameof(host));
        }

        // The port is not used by SRV, ConnectionString uses the same placeholder
        return new(new ClusterBuilder(
            ConnectionStringScheme.MongoDBPlusSrv,
            [new DnsEndPoint(dnsEndPoint.Host, ConnectionString.DefaultSrvPort)]));
    }

    /// <summary>
    /// Configures authentication: the credential the client uses to authenticate with the server.
    /// </summary>
    /// <returns>The <see cref="AuthenticationBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public AuthenticationBuilder Authentication()
        => _authenticationBuilder;

    /// <summary>
    /// Configures authentication: the credential the client uses to authenticate with the server.
    /// </summary>
    /// <param name="configure">A delegate that configures authentication.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Authentication(Action<AuthenticationBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_authenticationBuilder);
        return this;
    }

    /// <summary>
    /// Configures automatic client-side field level encryption.
    /// </summary>
    /// <returns>The <see cref="AutoEncryptionBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public AutoEncryptionBuilder AutoEncryption()
        => _autoEncryptionBuilder;

    /// <summary>
    /// Configures automatic client-side field level encryption.
    /// </summary>
    /// <param name="configure">A delegate that configures automatic encryption.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder AutoEncryption(Action<AutoEncryptionBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_autoEncryptionBuilder);
        return this;
    }

    /// <summary>
    /// Configures the client metadata: the application name and library information the client reports
    /// about itself to the server during the handshake.
    /// </summary>
    /// <returns>The <see cref="ClientMetadataBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public ClientMetadataBuilder ClientMetadata()
        => _clientMetadataBuilder;

    /// <summary>
    /// Configures the client metadata: the application name and library information the client reports
    /// about itself to the server during the handshake.
    /// </summary>
    /// <param name="configure">A delegate that configures the client metadata.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ClientMetadata(Action<ClientMetadataBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_clientMetadataBuilder);
        return this;
    }

    /// <summary>
    /// Configures the cluster. The endpoints and the topology are fixed by the factory method that created this
    /// builder, so only the SRV options can be modified here.
    /// </summary>
    /// <returns>The <see cref="ClusterBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public ClusterBuilder Cluster()
        => _clusterBuilder;

    /// <summary>
    /// Configures the cluster. The endpoints and the topology are fixed by the factory method that created this
    /// builder, so only the SRV options can be modified here.
    /// </summary>
    /// <param name="configure">A delegate that configures the cluster.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Cluster(Action<ClusterBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_clusterBuilder);
        return this;
    }

    /// <summary>
    /// Configures the connection pool.
    /// </summary>
    /// <returns>The <see cref="ConnectionPoolBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public ConnectionPoolBuilder ConnectionPool()
        => _connectionPoolBuilder;

    /// <summary>
    /// Configures the connection pool.
    /// </summary>
    /// <param name="configure">A delegate that configures the connection pool.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ConnectionPool(Action<ConnectionPoolBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_connectionPoolBuilder);
        return this;
    }

    /// <summary>
    /// Configures diagnostics: what the client logs and traces.
    /// </summary>
    /// <returns>The <see cref="DiagnosticsBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public DiagnosticsBuilder Diagnostics()
        => _diagnosticsBuilder;

    /// <summary>
    /// Configures diagnostics: what the client logs and traces.
    /// </summary>
    /// <param name="configure">A delegate that configures diagnostics.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Diagnostics(Action<DiagnosticsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_diagnosticsBuilder);
        return this;
    }

    /// <summary>
    /// Configures the network: the socket level settings and wire compression used to reach the servers.
    /// </summary>
    /// <returns>The <see cref="NetworkBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public NetworkBuilder Network()
        => _networkBuilder;

    /// <summary>
    /// Configures the network: the socket level settings and wire compression used to reach the servers.
    /// </summary>
    /// <param name="configure">A delegate that configures the network.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Network(Action<NetworkBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_networkBuilder);
        return this;
    }

    /// <summary>
    /// Configures operations: server selection, and the read preference, concerns, retry behavior and API
    /// version applied to operations that do not specify their own.
    /// </summary>
    /// <returns>The <see cref="OperationsBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public OperationsBuilder Operations()
        => _operationsBuilder;

    /// <summary>
    /// Configures operations: server selection, and the read preference, concerns, retry behavior and API
    /// version applied to operations that do not specify their own.
    /// </summary>
    /// <param name="configure">A delegate that configures operations.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Operations(Action<OperationsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_operationsBuilder);
        return this;
    }

    // TODO: should have SerializationBuilder parameter
    // TODO: WriteEncoding and ReadEncoding: should we either dropped them or moved under the serialization builder
    // public MongoClientBuilder Serialization()
    // {
    //     return this;
    // }

    /// <summary>
    /// Configures server monitoring: how the client discovers and tracks the state of each server.
    /// </summary>
    /// <returns>The <see cref="ServerMonitoringBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public ServerMonitoringBuilder ServerMonitoring()
        => _serverMonitoringBuilder;

    /// <summary>
    /// Configures server monitoring: how the client discovers and tracks the state of each server.
    /// </summary>
    /// <param name="configure">A delegate that configures server monitoring.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder ServerMonitoring(Action<ServerMonitoringBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_serverMonitoringBuilder);
        return this;
    }

    /// <summary>
    /// Configures TLS: whether connections are encrypted and how server and client certificates are handled.
    /// </summary>
    /// <returns>The <see cref="TlsBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public TlsBuilder Tls()
        => _tlsBuilder;

    /// <summary>
    /// Configures TLS: whether connections are encrypted and how server and client certificates are handled.
    /// </summary>
    /// <param name="configure">A delegate that configures TLS.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Tls(Action<TlsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_tlsBuilder);
        return this;
    }

    /// <summary>
    /// Configures translation of .NET expression trees into MongoDB expressions.
    /// </summary>
    /// <returns>The <see cref="TranslationBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public TranslationBuilder Translation()
        => _translationBuilder;

    /// <summary>
    /// Configures translation of .NET expression trees into MongoDB expressions.
    /// </summary>
    /// <param name="configure">A delegate that configures translation.</param>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    public MongoClientBuilder Translation(Action<TranslationBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_translationBuilder);
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
        Validate();

        var settings = ToMongoClientSettings();
        settings.Freeze();
        return new MongoClient(settings);
    }

    // Setters on the inner builders only validate their own value, so that the result never depends on the order
    // the setters were called in. Rules involving more than one setting are checked in Build instead: the ones
    // that concern a single builder by that builder's Validate, the ones spanning builders here. Most are ported
    // from MongoClientSettings.ThrowIfSettingsAreInvalid and ConnectionString validation; the rest are rules that
    // MongoClientSettings does not enforce, where the combination is silently ignored today.
    internal void Validate()
    {
        _tlsBuilder.Validate();
        _clusterBuilder.Validate();
        _connectionPoolBuilder.Validate();
        ValidateAuthentication();
    }

    internal MongoClientSettings ToMongoClientSettings()
    {
        // The settings must not reference any mutable state of the builder, because the builder stays usable
        // after Build (see the remarks on Build).
        var settings = new MongoClientSettings();

        // Authentication
        settings.Credential = _authenticationBuilder.Credential;

        // AutoEncryption
        settings.AutoEncryptionOptions = _autoEncryptionBuilder.ToAutoEncryptionOptions();

        // ClientMetadata
        settings.ApplicationName = _clientMetadataBuilder.ApplicationName;
        settings.LibraryInfo = _clientMetadataBuilder.LibraryInfo;

        // Cluster. SrvMaxHosts goes before Servers: the Servers setter shuffles the list when SrvMaxHosts > 0.
        settings.DirectConnection = _clusterBuilder.DirectConnection;
        settings.LoadBalanced = _clusterBuilder.LoadBalanced;
        settings.ReplicaSetName = _clusterBuilder.ReplicaSetName;
        settings.Scheme = _clusterBuilder.Scheme;
        settings.SrvAllowedHostsSuffix = _clusterBuilder.SrvAllowedHostsSuffix;
        settings.SrvMaxHosts = _clusterBuilder.SrvMaxHosts;
        settings.SrvServiceName = _clusterBuilder.SrvServiceName;
        settings.Servers = _clusterBuilder.Servers.Select(ToServerAddress).ToList();

        // ConnectionPool
        settings.MaxConnecting = _connectionPoolBuilder.MaxConnecting;
        settings.MaxConnectionIdleTime = _connectionPoolBuilder.MaxConnectionIdleTime;
        settings.MaxConnectionLifeTime = _connectionPoolBuilder.MaxConnectionLifeTime;
        settings.MaxConnectionPoolSize = _connectionPoolBuilder.MaxSize;
        settings.MinConnectionPoolSize = _connectionPoolBuilder.MinSize;
        settings.WaitQueueTimeout = _connectionPoolBuilder.WaitQueueTimeout;

        // Diagnostics
        settings.LoggingSettings = _diagnosticsBuilder.Logging;
        settings.TracingOptions = _diagnosticsBuilder.Tracing;

        // Network
        settings.Compressors = _networkBuilder.Compressors.ToArray();
        settings.ConnectTimeout = _networkBuilder.ConnectTimeout;
        settings.IPv6 = _networkBuilder.IPv6;
        settings.SocketTimeout = _networkBuilder.SocketTimeout;
        settings.Socks5ProxySettings = _networkBuilder.Socks5Proxy;

        // Operations
        settings.EnableOverloadRetargeting = _operationsBuilder.EnableOverloadRetargeting;
        settings.LocalThreshold = _operationsBuilder.LocalThreshold;
        settings.MaxAdaptiveRetries = _operationsBuilder.MaxAdaptiveRetries;
        settings.ReadConcern = _operationsBuilder.ReadConcern;
        settings.ReadPreference = _operationsBuilder.ReadPreference;
        settings.RetryReads = _operationsBuilder.RetryReads;
        settings.RetryWrites = _operationsBuilder.RetryWrites;
        settings.ServerApi = _operationsBuilder.ServerApi;
        settings.ServerSelectionTimeout = _operationsBuilder.ServerSelectionTimeout;
        settings.WriteConcern = _operationsBuilder.WriteConcern;

        // ServerMonitoring
        settings.HeartbeatInterval = _serverMonitoringBuilder.HeartbeatInterval;
        settings.HeartbeatTimeout = _serverMonitoringBuilder.HeartbeatTimeout;
        settings.ServerMonitoringMode = _serverMonitoringBuilder.Mode;

        // Tls. The SslSettings are created only when TLS is enabled: TlsBuilder.Validate rejects TLS options otherwise.
        settings.UseTls = _tlsBuilder.Enabled;
        if (_tlsBuilder.Enabled)
        {
            settings.SslSettings = new SslSettings
            {
                CheckCertificateRevocation = _tlsBuilder.CheckCertificateRevocation,
                ClientCertificates = _tlsBuilder.ClientCertificates,
                ClientCertificateSelectionCallback = _tlsBuilder.ClientCertificateSelectionCallback,
                EnabledSslProtocols = _tlsBuilder.AllowedProtocols,
                ServerCertificateValidationCallback = _tlsBuilder.ServerCertificateValidationCallback
            };
            settings.AllowInsecureTls = _tlsBuilder.AllowInsecure;
        }

        // Translation
        if (_translationBuilder.CompatibilityLevel != null || _translationBuilder.EnableClientSideProjections != null)
        {
            settings.TranslationOptions = new ExpressionTranslationOptions
            {
                CompatibilityLevel = _translationBuilder.CompatibilityLevel,
                EnableClientSideProjections = _translationBuilder.EnableClientSideProjections
            };
        }

        // MongoClientSettings has no counterpart for the following, they are applied to the cluster instead
        settings.ClusterConfigurator = CreateClusterConfigurator();

        return settings;
    }

    private Action<CoreClusterBuilder> CreateClusterConfigurator()
    {
        var configurators = new List<Action<CoreClusterBuilder>>();

        var subscribers = _diagnosticsBuilder.GetSubscribers();
        if (subscribers.Count > 0)
        {
            configurators.Add(builder =>
            {
                foreach (var subscriber in subscribers)
                {
                    builder.Subscribe(subscriber);
                }
            });
        }

        var preServerSelector = _operationsBuilder.PreServerSelector;
        var postServerSelector = _operationsBuilder.PostServerSelector;
        if (preServerSelector != null || postServerSelector != null)
        {
            configurators.Add(builder => builder.ConfigureCluster(clusterSettings => clusterSettings.With(
                preServerSelector: preServerSelector != null ? Optional.Create(preServerSelector) : default,
                postServerSelector: postServerSelector != null ? Optional.Create(postServerSelector) : default)));
        }

        // A null buffer size keeps the driver default.
        var receiveBufferSize = _networkBuilder.ReceiveBufferSize;
        var sendBufferSize = _networkBuilder.SendBufferSize;
        var socketConfigurator = _networkBuilder.SocketConfigurator;
        var isReceiveBufferSizeCustom = receiveBufferSize.HasValue && receiveBufferSize.Value != MongoDefaults.TcpReceiveBufferSize;
        var isSendBufferSizeCustom = sendBufferSize.HasValue && sendBufferSize.Value != MongoDefaults.TcpSendBufferSize;
        if (isReceiveBufferSizeCustom || isSendBufferSizeCustom || socketConfigurator != null)
        {
            configurators.Add(builder => builder.ConfigureTcp(tcpSettings => tcpSettings.With(
                receiveBufferSize: isReceiveBufferSizeCustom ? Optional.Create(receiveBufferSize.Value) : default,
                sendBufferSize: isSendBufferSizeCustom ? Optional.Create(sendBufferSize.Value) : default,
                socketConfigurator: socketConfigurator != null ? Optional.Create(socketConfigurator) : default)));
        }

        var maintenanceInterval = _connectionPoolBuilder.MaintenanceInterval;
        if (maintenanceInterval != new ConnectionPoolSettings().MaintenanceInterval)
        {
            configurators.Add(builder => builder.ConfigureConnectionPool(poolSettings => poolSettings.With(
                maintenanceInterval: maintenanceInterval)));
        }

        // Left null when there is nothing to apply, so that equal settings keep sharing a cluster
        if (configurators.Count == 0)
        {
            return null;
        }

        return builder =>
        {
            foreach (var configurator in configurators)
            {
                configurator(builder);
            }
        };
    }

    private void ValidateAuthentication()
    {
        var credential = _authenticationBuilder.Credential;
        if (credential?.Mechanism == MongoDBX509Authenticator.MechanismName && !_tlsBuilder.Enabled)
        {
            throw new InvalidOperationException("The MONGODB-X509 authentication mechanism requires TLS to be enabled.");
        }
    }

    private static MongoServerAddress ToServerAddress(EndPoint endPoint)
    {
        var (host, port) = endPoint.GetHostAndPort();
        return new MongoServerAddress(host, port);
    }

    private static EndPoint ParseEndPoint(string value, string paramName)
    {
        Ensure.IsNotNullOrEmpty(value, paramName);

        if (!EndPointHelper.TryParse(value, out var endPoint))
        {
            throw new ArgumentException($"'{value}' is not a valid end point.", paramName);
        }

        return endPoint;
    }

    private static EndPoint[] ParseEndPoints(IEnumerable<string> values, string paramName)
    {
        Ensure.IsNotNull(values, paramName);

        return values.Select(value => ParseEndPoint(value, paramName)).ToArray();
    }
}
