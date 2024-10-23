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
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver.Authentication;
using MongoDB.Driver.Authentication.Gssapi;
using MongoDB.Driver.Authentication.Oidc;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    /// <summary>
    /// Extension methods for a ClusterBuilder.
    /// </summary>
    public static class ClusterBuilderExtensions
    {
        /// <summary>
        /// Configures a cluster builder from a connection string.
        /// </summary>
        /// <param name="builder">The cluster builder.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder ConfigureWithConnectionString(this ClusterBuilder builder, string connectionString)
        {
            Ensure.IsNotNull(builder, nameof(builder));
            Ensure.IsNotNullOrEmpty(connectionString, nameof(connectionString));

            var parsedConnectionString = new ConnectionString(connectionString);

            return ConfigureWithConnectionString(builder, parsedConnectionString);
        }

        /// <summary>
        /// Configures a cluster builder from a connection string.
        /// </summary>
        /// <param name="builder">The cluster builder.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder ConfigureWithConnectionString(
            this ClusterBuilder builder,
            string connectionString,
            ServerApi serverApi)
        {
            Ensure.IsNotNull(builder, nameof(builder));
            Ensure.IsNotNullOrEmpty(connectionString, nameof(connectionString));

            var parsedConnectionString = new ConnectionString(connectionString);

            return ConfigureWithConnectionString(builder, parsedConnectionString, serverApi);
        }

        /// <summary>
        /// Configures a cluster builder from a connection string.
        /// </summary>
        /// <param name="builder">The cluster builder.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder ConfigureWithConnectionString(this ClusterBuilder builder, ConnectionString connectionString)
        {
            return ConfigureWithConnectionString(builder, connectionString, serverApi: null);
        }

        /// <summary>
        /// Configures a cluster builder from a connection string.
        /// </summary>
        /// <param name="builder">The cluster builder.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="serverApi">The server API.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder ConfigureWithConnectionString(
            this ClusterBuilder builder,
            ConnectionString connectionString,
            ServerApi serverApi)
        {
            Ensure.IsNotNull(builder, nameof(builder));
            Ensure.IsNotNull(connectionString, nameof(connectionString));

            if (!connectionString.IsResolved)
            {
                connectionString = connectionString.Resolve(connectionString.DirectConnection);
            }

            // TCP
            if (connectionString.ConnectTimeout != null)
            {
                builder = builder.ConfigureTcp(s => s.With(connectTimeout: connectionString.ConnectTimeout.Value));
            }
            if (connectionString.HeartbeatInterval.HasValue)
            {
                builder = builder.ConfigureServer(s => s.With(heartbeatInterval: connectionString.HeartbeatInterval.Value));
            }
            if (connectionString.HeartbeatTimeout.HasValue)
            {
                builder = builder.ConfigureServer(s => s.With(heartbeatTimeout: connectionString.HeartbeatTimeout.Value));
            }
            if (connectionString.Ipv6.HasValue && connectionString.Ipv6.Value)
            {
                builder = builder.ConfigureTcp(s => s.With(addressFamily: AddressFamily.InterNetworkV6));
            }

            if (connectionString.SocketTimeout != null)
            {
                builder = builder.ConfigureTcp(s => s.With(
                    readTimeout: connectionString.SocketTimeout.Value,
                    writeTimeout: connectionString.SocketTimeout.Value));
            }

            if (connectionString.Tls != null)
            {
                builder = builder.ConfigureSsl(ssl =>
                {
                    if (connectionString.TlsInsecure.GetValueOrDefault(false))
                    {
                        ssl = ssl.With(serverCertificateValidationCallback: new RemoteCertificateValidationCallback(AcceptAnySslCertificate));
                    }
                    if (connectionString.TlsDisableCertificateRevocationCheck.HasValue)
                    {
                        ssl = ssl.With(
                            checkCertificateRevocation: !connectionString.TlsDisableCertificateRevocationCheck.Value);
                    }

                    return ssl;
                });
            }

            // Connection
            if (connectionString.Username != null)
            {
                var authenticatorFactory = new AuthenticatorFactory(() => CreateAuthenticator(connectionString, serverApi));
                builder = builder.ConfigureConnection(s => s.With(authenticatorFactory: authenticatorFactory));
            }
            if (connectionString.ApplicationName != null)
            {
                builder = builder.ConfigureConnection(s => s.With(applicationName: connectionString.ApplicationName));
            }
            if (connectionString.LoadBalanced)
            {
                builder = builder.ConfigureConnection(s => s.With(loadBalanced: connectionString.LoadBalanced));
            }
            if (connectionString.MaxIdleTime != null)
            {
                builder = builder.ConfigureConnection(s => s.With(maxIdleTime: connectionString.MaxIdleTime.Value));
            }
            if (connectionString.MaxLifeTime != null)
            {
                builder = builder.ConfigureConnection(s => s.With(maxLifeTime: connectionString.MaxLifeTime.Value));
            }

            if (connectionString.Compressors != null)
            {
                builder = builder.ConfigureConnection(s => s.With(compressors: connectionString.Compressors.ToArray()));
            }

            // Connection Pool
            if (connectionString.MaxConnecting.HasValue)
            {
                builder = builder.ConfigureConnectionPool(s => s.With(maxConnecting: connectionString.MaxConnecting.Value));
            }
            if (connectionString.MaxPoolSize != null)
            {
                var effectiveMaxConnections = ConnectionStringConversions.GetEffectiveMaxConnections(connectionString.MaxPoolSize.Value);
                builder = builder.ConfigureConnectionPool(s => s.With(maxConnections: effectiveMaxConnections));
            }
            if (connectionString.MinPoolSize != null)
            {
                builder = builder.ConfigureConnectionPool(s => s.With(minConnections: connectionString.MinPoolSize.Value));
            }
#pragma warning disable 618
            if (connectionString.WaitQueueSize != null)
            {
                builder = builder.ConfigureConnectionPool(s => s.With(waitQueueSize: connectionString.WaitQueueSize.Value));
            }
            else if (connectionString.WaitQueueMultiple != null)
            {
                var effectiveMaxConnections = ConnectionStringConversions.GetEffectiveMaxConnections(connectionString.MaxPoolSize) ?? new ConnectionPoolSettings().MaxConnections;
                var computedWaitQueueSize = ConnectionStringConversions.GetComputedWaitQueueSize(effectiveMaxConnections, connectionString.WaitQueueMultiple.Value);
                builder = builder.ConfigureConnectionPool(s => s.With(waitQueueSize: computedWaitQueueSize));
            }
#pragma warning restore 618
            if (connectionString.WaitQueueTimeout != null)
            {
                builder = builder.ConfigureConnectionPool(s => s.With(waitQueueTimeout: connectionString.WaitQueueTimeout.Value));
            }

            // Server

            // Cluster
            var directConnection = connectionString.DirectConnection;
            builder = builder.ConfigureCluster(
                s =>
                    s.With(
                        directConnection: directConnection,
                        scheme: connectionString.Scheme,
                        loadBalanced: connectionString.LoadBalanced));
            if (connectionString.Hosts.Count > 0)
            {
                builder = builder.ConfigureCluster(s => s.With(endPoints: Optional.Enumerable(connectionString.Hosts)));
            }
            if (connectionString.ReplicaSet != null)
            {
                builder = builder.ConfigureCluster(s => s.With(replicaSetName: connectionString.ReplicaSet));
            }
            if (connectionString.ServerSelectionTimeout != null)
            {
                builder = builder.ConfigureCluster(s => s.With(serverSelectionTimeout: connectionString.ServerSelectionTimeout.Value));
            }
            if (serverApi != null)
            {
                builder = builder.ConfigureCluster(s => s.With(serverApi: serverApi));
            }

            return builder;
        }

        private static bool AcceptAnySslCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors
        )
        {
            return true;
        }

        private const string AwsAuthMechanismName = "MONGODB-AWS";

        private static string GetAuthSource(ConnectionString connectionString)
        {
            var defaultSource = GetDefaultAuthSource(connectionString);

            if (connectionString.AuthMechanism == AwsAuthMechanismName ||
                connectionString.AuthMechanism == OidcSaslMechanism.MechanismName)
            {
                return connectionString.AuthSource ?? defaultSource;
            }

            return connectionString.AuthSource ?? connectionString.DatabaseName ?? defaultSource;
        }

        private static string GetDefaultAuthSource(ConnectionString connectionString)
        {
            if (connectionString.AuthMechanism != null && (
                connectionString.AuthMechanism == GssapiSaslMechanism.MechanismName ||
                connectionString.AuthMechanism == AwsAuthMechanismName ||
                connectionString.AuthMechanism == OidcSaslMechanism.MechanismName))
            {
                return "$external";
            }

            return "admin";
        }

        private static IAuthenticator CreateAuthenticator(ConnectionString connectionString, ServerApi serverApi)
        {
            if (connectionString.AuthMechanism == MongoDBX509Authenticator.MechanismName)
            {
                return new MongoDBX509Authenticator(connectionString.Username, serverApi);
            }

            var authSource = GetAuthSource(connectionString);
            var identity = new MongoExternalIdentity(authSource, connectionString.Username);
            MongoIdentityEvidence evidence = null;
            if (connectionString.Password != null)
            {
                evidence = new PasswordEvidence(connectionString.Password);
            }

            if (connectionString.AuthMechanism == null)
            {
                return new DefaultAuthenticator(identity, evidence, connectionString.Hosts, serverApi);
            }

            if (SaslAuthenticator.TryCreate(
                    connectionString.AuthMechanism,
                    connectionString.Hosts,
                    identity,
                    evidence,
                    connectionString.AuthMechanismProperties?.Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value)),
                    serverApi,
                    out var authenticator))
            {
                return authenticator;
            }

            throw new NotSupportedException("Unable to create an authenticator.");
        }

#if NET472
        /// <summary>
        /// Configures the cluster to write performance counters.
        /// </summary>
        /// <param name="builder">The cluster builder.</param>
        /// <param name="applicationName">The name of the application.</param>
        /// <param name="install">if set to <c>true</c> install the performance counters first.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder UsePerformanceCounters(this ClusterBuilder builder, string applicationName, bool install = false)
        {
            Ensure.IsNotNull(builder, nameof(builder));

            if (install)
            {
                PerformanceCounterEventSubscriber.InstallPerformanceCounters();
            }

            var subscriber = new PerformanceCounterEventSubscriber(applicationName);
            return builder.Subscribe(subscriber);
        }
#endif

        /// <summary>
        /// Configures the cluster to trace events to the specified <paramref name="traceSource"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="traceSource">The trace source.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder TraceWith(this ClusterBuilder builder, TraceSource traceSource)
        {
            Ensure.IsNotNull(builder, nameof(builder));
            Ensure.IsNotNull(traceSource, nameof(traceSource));

            return builder.Subscribe(new TraceSourceEventSubscriber(traceSource));
        }

        /// <summary>
        /// Configures the cluster to trace command events to the specified <paramref name="traceSource"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="traceSource">The trace source.</param>
        /// <returns>A reconfigured cluster builder.</returns>
        public static ClusterBuilder TraceCommandsWith(this ClusterBuilder builder, TraceSource traceSource)
        {
            Ensure.IsNotNull(builder, nameof(builder));
            Ensure.IsNotNull(traceSource, nameof(traceSource));

            return builder.Subscribe(new TraceSourceCommandEventSubscriber(traceSource));
        }
    }
}
