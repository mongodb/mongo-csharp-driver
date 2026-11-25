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

    private readonly AuthenticationBuilder _authenticationBuilder = new();
    private readonly AutoEncryptionBuilder _autoEncryptionBuilder = new();
    private readonly ClientMetadataBuilder _clientMetadataBuilder = new();
    private readonly TopologyBuilder _topologyBuilder;
    private readonly ConnectionPoolBuilder _connectionPoolBuilder = new();
    private readonly DiagnosticsBuilder _diagnosticsBuilder = new();
    private readonly NetworkBuilder _networkBuilder = new();
    private readonly OperationsBuilder _operationsBuilder = new();
    private readonly ServerMonitoringBuilder _serverMonitoringBuilder = new();
    private readonly TlsBuilder _tlsBuilder = new();
    private readonly TranslationBuilder _translationBuilder = new();

    private MongoClientBuilder(TopologyBuilder topologyBuilder)
    {
        _topologyBuilder = topologyBuilder;
    }

    /// <summary>
    /// Creates a builder initialized from a MongoDB connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    public static MongoClientBuilder FromConnectionString(string connectionString)
    {
        var parsedConnectionString = new ConnectionString(connectionString);

        parsedConnectionString = parsedConnectionString.Resolve(resolveHosts: false);

        var builder = new MongoClientBuilder(new TopologyBuilder(parsedConnectionString.Scheme, parsedConnectionString.Hosts));
        builder._authenticationBuilder.ApplyConnectionString(parsedConnectionString);
        builder._clientMetadataBuilder.ApplyConnectionString(parsedConnectionString);
        builder._topologyBuilder.ApplyConnectionString(parsedConnectionString);
        builder._connectionPoolBuilder.ApplyConnectionString(parsedConnectionString);
        builder._networkBuilder.ApplyConnectionString(parsedConnectionString);
        builder._operationsBuilder.ApplyConnectionString(parsedConnectionString);
        builder._serverMonitoringBuilder.ApplyConnectionString(parsedConnectionString);
        builder._tlsBuilder.ApplyConnectionString(parsedConnectionString);
        return builder;
    }

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoint">The seed addresses as <c>host</c> or <c>host:port</c>. The default port is 27017.</param>
    public static MongoClientBuilder FromEndPoint(string endPoint)
        => FromEndPoint(ParseEndPoint(endPoint, nameof(endPoint)));

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoint">The seed end point.</param>
    public static MongoClientBuilder FromEndPoint(EndPoint endPoint)
    {
        Ensure.IsNotNull(endPoint, nameof(endPoint));
        return new MongoClientBuilder(new TopologyBuilder(ConnectionStringScheme.MongoDB, [endPoint]));
    }

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoints">The seed addresses as <c>host</c> or <c>host:port</c>. The default port is 27017.</param>
    public static MongoClientBuilder FromEndPoints(IEnumerable<string> endPoints)
        => FromEndPoints(ParseEndPoints(endPoints, nameof(endPoints)));

    /// <summary>
    /// Creates a builder that discovers the topology starting from the given seed servers. Use it for
    /// standalone servers, sharded clusters (the mongos routers) and replica sets alike.
    /// </summary>
    /// <param name="endPoints">The seed end points.</param>
    public static MongoClientBuilder FromEndPoints(IEnumerable<EndPoint> endPoints)
    {
        Ensure.IsNotNull(endPoints, nameof(endPoints));

        return new MongoClientBuilder(new TopologyBuilder(ConnectionStringScheme.MongoDB, endPoints.ToArray()));
    }

    /// <summary>
    /// Creates a builder that connects to a mongodb+srv:// deployment: the servers are found by looking up
    /// the SRV records of the given host name.
    /// </summary>
    /// <param name="host">The SRV host name, without a port, for example <c>cluster0.example.mongodb.net</c>.</param>
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
        return new(new TopologyBuilder(
            ConnectionStringScheme.MongoDBPlusSrv,
            [new DnsEndPoint(dnsEndPoint.Host, ConnectionString.DefaultSrvPort)]));
    }

    /// <summary>
    /// Configures authentication: the credential the client uses to authenticate with the server.
    /// </summary>
    public AuthenticationBuilder Authentication()
        => _authenticationBuilder;

    /// <summary>
    /// Configures authentication: the credential the client uses to authenticate with the server.
    /// </summary>
    /// <param name="configure">A delegate that configures authentication.</param>
    public MongoClientBuilder Authentication(Action<AuthenticationBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_authenticationBuilder);
        return this;
    }

    /// <summary>
    /// Configures automatic client-side field level encryption.
    /// </summary>
    public AutoEncryptionBuilder AutoEncryption()
        => _autoEncryptionBuilder;

    /// <summary>
    /// Configures automatic client-side field level encryption.
    /// </summary>
    /// <param name="configure">A delegate that configures automatic encryption.</param>
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
    public ClientMetadataBuilder ClientMetadata()
        => _clientMetadataBuilder;

    /// <summary>
    /// Configures the client metadata: the application name and library information the client reports
    /// about itself to the server during the handshake.
    /// </summary>
    /// <param name="configure">A delegate that configures the client metadata.</param>
    public MongoClientBuilder ClientMetadata(Action<ClientMetadataBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_clientMetadataBuilder);
        return this;
    }

    /// <summary>
    /// Configures the topology. The endpoints and the scheme are fixed by the factory method that created this
    /// builder, so only the remaining topology options (such as the replica set name, direct connection, load
    /// balancing and the SRV options) can be modified here.
    /// </summary>
    public TopologyBuilder Topology()
        => _topologyBuilder;

    /// <summary>
    /// Configures the topology. The endpoints and the scheme are fixed by the factory method that created this
    /// builder, so only the remaining topology options (such as the replica set name, direct connection, load
    /// balancing and the SRV options) can be modified here.
    /// </summary>
    /// <param name="configure">A delegate that configures the topology.</param>
    public MongoClientBuilder Topology(Action<TopologyBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_topologyBuilder);
        return this;
    }

    /// <summary>
    /// Configures the connection pool.
    /// </summary>
    public ConnectionPoolBuilder ConnectionPool()
        => _connectionPoolBuilder;

    /// <summary>
    /// Configures the connection pool.
    /// </summary>
    /// <param name="configure">A delegate that configures the connection pool.</param>
    public MongoClientBuilder ConnectionPool(Action<ConnectionPoolBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_connectionPoolBuilder);
        return this;
    }

    /// <summary>
    /// Configures diagnostics: what the client logs and traces.
    /// </summary>
    public DiagnosticsBuilder Diagnostics()
        => _diagnosticsBuilder;

    /// <summary>
    /// Configures diagnostics: what the client logs and traces.
    /// </summary>
    /// <param name="configure">A delegate that configures diagnostics.</param>
    public MongoClientBuilder Diagnostics(Action<DiagnosticsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_diagnosticsBuilder);
        return this;
    }

    /// <summary>
    /// Configures the network: the socket level settings and wire compression used to reach the servers.
    /// </summary>
    public NetworkBuilder Network()
        => _networkBuilder;

    /// <summary>
    /// Configures the network: the socket level settings and wire compression used to reach the servers.
    /// </summary>
    /// <param name="configure">A delegate that configures the network.</param>
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
    public OperationsBuilder Operations()
        => _operationsBuilder;

    /// <summary>
    /// Configures operations: server selection, and the read preference, concerns, retry behavior and API
    /// version applied to operations that do not specify their own.
    /// </summary>
    /// <param name="configure">A delegate that configures operations.</param>
    public MongoClientBuilder Operations(Action<OperationsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_operationsBuilder);
        return this;
    }

    /// <summary>
    /// Configures server monitoring: how the client discovers and tracks the state of each server.
    /// </summary>
    public ServerMonitoringBuilder ServerMonitoring()
        => _serverMonitoringBuilder;

    /// <summary>
    /// Configures server monitoring: how the client discovers and tracks the state of each server.
    /// </summary>
    /// <param name="configure">A delegate that configures server monitoring.</param>
    public MongoClientBuilder ServerMonitoring(Action<ServerMonitoringBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_serverMonitoringBuilder);
        return this;
    }

    /// <summary>
    /// Configures TLS: whether connections are encrypted and how server and client certificates are handled.
    /// </summary>
    public TlsBuilder Tls()
        => _tlsBuilder;

    /// <summary>
    /// Configures TLS: whether connections are encrypted and how server and client certificates are handled.
    /// </summary>
    /// <param name="configure">A delegate that configures TLS.</param>
    public MongoClientBuilder Tls(Action<TlsBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_tlsBuilder);
        return this;
    }

    /// <summary>
    /// Configures translation of .NET expression trees into MongoDB expressions.
    /// </summary>
    public TranslationBuilder Translation()
        => _translationBuilder;

    /// <summary>
    /// Configures translation of .NET expression trees into MongoDB expressions.
    /// </summary>
    /// <param name="configure">A delegate that configures translation.</param>
    public MongoClientBuilder Translation(Action<TranslationBuilder> configure)
    {
        Ensure.IsNotNull(configure, nameof(configure));

        configure(_translationBuilder);
        return this;
    }

    /// <summary>
    /// Builds a <see cref="IMongoClient"/> from the configuration accumulated on this builder.
    /// </summary>
    public IMongoClient Build()
    {
        Validate();

        var settings = ToMongoClientSettings();
        settings.Freeze();
        return new MongoClient(settings);
    }

    internal void Validate()
    {
        _tlsBuilder.Validate();
        _topologyBuilder.Validate();
        _connectionPoolBuilder.Validate();
        ValidateAuthentication();
    }

    internal MongoClientSettings ToMongoClientSettings()
    {
        var settings = new MongoClientSettings();

        // Authentication
        settings.Credential = _authenticationBuilder.Credential;

        // AutoEncryption
        settings.AutoEncryptionOptions = _autoEncryptionBuilder.ToAutoEncryptionOptions();

        // ClientMetadata
        settings.ApplicationName = _clientMetadataBuilder.ApplicationName;
        settings.LibraryInfo = _clientMetadataBuilder.LibraryInfo;

        // Topology.SrvMaxHosts goes before Servers: the Servers setter shuffles the list when SrvMaxHosts > 0.
        settings.DirectConnection = _topologyBuilder.DirectConnection;
        settings.LoadBalanced = _topologyBuilder.LoadBalanced;
        settings.ReplicaSetName = _topologyBuilder.ReplicaSetName;
        settings.Scheme = _topologyBuilder.Scheme;
        settings.SrvAllowedHostsSuffix = _topologyBuilder.SrvAllowedHostsSuffix;
        settings.SrvMaxHosts = _topologyBuilder.SrvMaxHosts;
        settings.SrvServiceName = _topologyBuilder.SrvServiceName;
        settings.Servers = _topologyBuilder.Servers.Select(ToServerAddress).ToList();

        // ConnectionPool
        settings.MaxConnecting = _connectionPoolBuilder.MaxConnecting;
        settings.MaxConnectionIdleTime = _connectionPoolBuilder.MaxConnectionIdleTime;
        settings.MaxConnectionLifeTime = _connectionPoolBuilder.MaxConnectionLifeTime;
        settings.MaxConnectionPoolSize = _connectionPoolBuilder.MaxSize;
        settings.MinConnectionPoolSize = _connectionPoolBuilder.MinSize;
        settings.WaitQueueTimeout = _connectionPoolBuilder.WaitQueueTimeout;

        // Diagnostics
        settings.LoggingSettings = _diagnosticsBuilder.CreateLoggingSettings();
        settings.TracingOptions = _diagnosticsBuilder.CreateTracingOptions();

        // Network
        settings.Compressors = _networkBuilder.Compressors.ToArray();
        settings.ConnectTimeout = _networkBuilder.ConnectTimeout;
        settings.IPv6 = _networkBuilder.IPv6;
        settings.SocketTimeout = _networkBuilder.SocketTimeout;
        settings.Socks5ProxySettings = _networkBuilder.Socks5Proxy;
        // TODO: set MongoClientSettings.UseOperatingSystemBufferSizes = true, once it merged into the main

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

    private Action<ClusterBuilder> CreateClusterConfigurator()
    {
        var configurator = new BuilderClusterConfigurator(
            _diagnosticsBuilder.GetSubscribers(),
            _operationsBuilder.PreServerSelector,
            _operationsBuilder.PostServerSelector,
            _networkBuilder.SocketConfigurator,
            _connectionPoolBuilder.MaintenanceInterval);

        // Left null when there is nothing to apply, so that equal settings keep sharing a cluster
        return configurator.IsEmpty ? null : configurator.ToAction();
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
