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
using MongoDB.Driver.Configuration;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;
using ClusterBuilder = MongoDB.Driver.Configuration.ClusterBuilder;

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
    private readonly ClusterBuilder _clusterBuilder = new();
    private readonly ConnectionPoolBuilder _connectionPoolBuilder = new();
    private readonly DiagnosticsBuilder _diagnosticsBuilder = new();
    private readonly NetworkBuilder _networkBuilder = new();
    private readonly OperationsBuilder _operationsBuilder = new();
    private readonly ServerMonitoringBuilder _serverMonitoringBuilder = new();
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
    /// Configures the cluster: the endpoints the client connects to and the topology it expects.
    /// </summary>
    /// <returns>The <see cref="ClusterBuilder"/> of this <see cref="MongoClientBuilder"/>.</returns>
    public ClusterBuilder Cluster()
        => _clusterBuilder;

    /// <summary>
    /// Configures the cluster: the endpoints the client connects to and the topology it expects.
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

    /// <summary>
    /// Configures serialization.
    /// </summary>
    /// <returns>The same <see cref="MongoClientBuilder"/> instance so that calls can be chained.</returns>
    // TODO: should have SerializationBuilder parameter
    // TODO: WriteEncoding and ReadEncoding: should we either dropped them or moved under the serialization builder
    public MongoClientBuilder Serialization()
    {
        return this;
    }

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
        // TODO: validate combinations of settings before constructing the client. Setters on the inner builders
        // only validate their own value, so that the result never depends on the order the setters were called
        // in (or on whether a value came from FromConnectionString or was set in code). Every rule involving
        // more than one setting belongs here. Existing rules, ported from MongoClientSettings.ThrowIfSettingsAreInvalid
        // and ConnectionString validation:
        //   Tls:
        //     - AllowInsecure and CheckCertificateRevocation cannot both be true.
        //   Cluster:
        //     - DirectConnection cannot be used with the mongodb+srv scheme.
        //     - DirectConnection cannot be used with more than one server.
        //     - SrvMaxHosts > 0 requires the mongodb+srv scheme.
        //     - SrvMaxHosts > 0 cannot be used with ReplicaSetName.
        //     - A non-default SrvServiceName requires the mongodb+srv scheme.
        //     - A SrvAllowedHostsSuffix requires the mongodb+srv scheme.
        //     - The mongodb+srv scheme requires exactly one server, given without a port.
        //     - LoadBalanced cannot be used with more than one server, with ReplicaSetName, with SrvMaxHosts > 0,
        //       or with DirectConnection.
        //   ConnectionPool:
        //     - MaxSize must be greater than or equal to MinSize.
        //   AutoEncryption (when configured):
        //     - KeyVaultNamespace is required, as is at least one registered KMS provider. Already enforced by
        //       AutoEncryptionBuilder.ToAutoEncryptionOptions().
        // New rules to consider (not enforced by MongoClientSettings today):
        //     - Tls options other than Enabled (client certificates, callbacks, AllowInsecure, AllowedProtocols) are
        //       configured while Tls.Enabled is false, which currently ignores them silently.
        //     - Tls.AllowInsecure together with Tls.ServerCertificateValidationCallback; the callback silently wins
        //       today (see ClusterRegistry.ConfigureSsl).
        //     - A MONGODB-X509 credential while Tls.Enabled is false.

        // TODO: implement constructing of the MongoClient. The builder stays usable after Build, so the client must
        // receive a snapshot rather than references to the builder's mutable state: use
        // AutoEncryptionBuilder.ToAutoEncryptionOptions(), and create a new EventAggregator from a copy of the
        // DiagnosticsBuilder subscribers.
        return null;
    }
}
