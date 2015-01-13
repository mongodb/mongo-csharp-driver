/* Copyright 2010-2014 MongoDB Inc.
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
using System.Net.Security;
using System.Net.Sockets;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Configuration
{
    public static class ClusterBuilderExtensionMethods
    {
        public static ClusterBuilder ConfigureWithConnectionString(this ClusterBuilder configuration, string connectionString)
        {
            Ensure.IsNotNull(configuration, "configuration");
            Ensure.IsNotNullOrEmpty(connectionString, "connectionString");

            var parsedConnectionString = new ConnectionString(connectionString);
            return ConfigureWithConnectionString(configuration, parsedConnectionString);
        }

        public static ClusterBuilder ConfigureWithConnectionString(this ClusterBuilder configuration, ConnectionString connectionString)
        {
            Ensure.IsNotNull(configuration, "configuration");
            Ensure.IsNotNull(connectionString, "connectionString");

            // TCP
            if (connectionString.ConnectTimeout != null)
            {
                configuration.ConfigureTcp(s => s.With(connectTimeout: connectionString.ConnectTimeout.Value));
            }
            if (connectionString.Ipv6.HasValue && connectionString.Ipv6.Value)
            {
                configuration.ConfigureTcp(s => s.With(addressFamily: AddressFamily.InterNetworkV6));
            }

            if (connectionString.SocketTimeout != null)
            {
                configuration.ConfigureTcp(s => s.With(
                    readTimeout: connectionString.SocketTimeout.Value,
                    writeTimeout: connectionString.SocketTimeout.Value));
            }

            if (connectionString.Ssl != null)
            {
                configuration.ConfigureSsl(ssl =>
                {
                    if (connectionString.SslVerifyCertificate.GetValueOrDefault(true))
                    {
                        ssl = ssl.With(
                            serverCertificateValidationCallback: new RemoteCertificateValidationCallback((obj, cert, chain, errors) => true));
                    }

                    return ssl;
                });
            }

            // Connection
            if (connectionString.Username != null)
            {
                var authenticator = CreateAuthenticator(connectionString);
                configuration.ConfigureConnection(s => s.With(authenticators: new[] { authenticator }));
            }

            if (connectionString.MaxIdleTime != null)
            {
                configuration.ConfigureConnection(s => s.With(maxIdleTime: connectionString.MaxIdleTime.Value));
            }
            if (connectionString.MaxLifeTime != null)
            {
                configuration.ConfigureConnection(s => s.With(maxLifeTime: connectionString.MaxLifeTime.Value));
            }

            // Connection Pool
            if (connectionString.MaxPoolSize != null)
            {
                configuration.ConfigureConnectionPool(s => s.With(maxConnections: connectionString.MaxPoolSize.Value));
            }
            if (connectionString.MinPoolSize != null)
            {
                configuration.ConfigureConnectionPool(s => s.With(minConnections: connectionString.MinPoolSize.Value));
            }
            if (connectionString.WaitQueueSize != null)
            {
                configuration.ConfigureConnectionPool(s => s.With(waitQueueSize: connectionString.WaitQueueSize.Value));
            }
            else if (connectionString.WaitQueueMultiple != null)
            {
                var maxConnections = connectionString.MaxPoolSize ?? new ConnectionPoolSettings().MaxConnections;
                var waitQueueSize = (int)Math.Round(maxConnections * connectionString.WaitQueueMultiple.Value);
                configuration.ConfigureConnectionPool(s => s.With(waitQueueSize: waitQueueSize));
            }
            if (connectionString.WaitQueueTimeout != null)
            {
                configuration.ConfigureConnectionPool(s => s.With(waitQueueTimeout: connectionString.WaitQueueTimeout.Value));
            }

            // Server

            // Cluster
            if (connectionString.Hosts.Count > 0)
            {
                configuration.ConfigureCluster(s => s.With(endPoints: Optional.Create<IEnumerable<EndPoint>>(connectionString.Hosts)));
            }
            if (connectionString.ReplicaSet != null)
            {
                configuration.ConfigureCluster(s => s.With(
                    replicaSetName: connectionString.ReplicaSet));
            }

            return configuration;
        }

        private static IAuthenticator CreateAuthenticator(ConnectionString connectionString)
        {
            if (connectionString.Password != null)
            {
                var defaultSource = GetDefaultSource(connectionString);

                var credential = new UsernamePasswordCredential(
                        connectionString.AuthSource ?? connectionString.DatabaseName ?? defaultSource,
                        connectionString.Username,
                        connectionString.Password);

                if (connectionString.AuthMechanism == null)
                {
                    return new DefaultAuthenticator(credential);
                }
                else if (connectionString.AuthMechanism == MongoDBCRAuthenticator.MechanismName)
                {
                    return new MongoDBCRAuthenticator(credential);
                }
                else if (connectionString.AuthMechanism == ScramSha1Authenticator.MechanismName)
                {
                    return new ScramSha1Authenticator(credential);
                }
                else if (connectionString.AuthMechanism == PlainAuthenticator.MechanismName)
                {
                    return new PlainAuthenticator(credential);
                }
                else if (connectionString.AuthMechanism == GssapiAuthenticator.MechanismName)
                {
                    return new GssapiAuthenticator(credential, connectionString.AuthMechanismProperties);
                }
            }
            else
            {
                if (connectionString.AuthMechanism == MongoDBX509Authenticator.MechanismName)
                {
                    return new MongoDBX509Authenticator(connectionString.Username);
                }
                else if (connectionString.AuthMechanism == GssapiAuthenticator.MechanismName)
                {
                    return new GssapiAuthenticator(connectionString.Username, connectionString.AuthMechanismProperties);
                }
            }

            throw new NotSupportedException("Unable to create an authenticator.");
        }

        private static string GetDefaultSource(ConnectionString connectionString)
        {
            if (connectionString.AuthMechanism != null && connectionString.AuthMechanism.Equals("GSSAPI", StringComparison.InvariantCultureIgnoreCase))
            {
                return "$external";
            }

            return "admin";
        }

        public static ClusterBuilder UsePerformanceCounters(this ClusterBuilder configuration, string applicationName, bool install = false)
        {
            Ensure.IsNotNull(configuration, "configuration");

            if (install)
            {
                PerformanceCounterListener.Install();
            }

            return configuration.AddListener(new PerformanceCounterListener(applicationName));
        }
    }
}