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
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Describes the endpoints a <see cref="MongoClient"/> connects to and the topology it expects.
/// </summary>
public sealed class ClusterBuilder
{
    internal ClusterBuilder(ConnectionStringScheme scheme, IReadOnlyList<EndPoint> servers)
    {
        Scheme = scheme;
        Servers = Ensure.IsNotNull(servers, nameof(servers));
        SrvServiceName = MongoInternalDefaults.MongoClientSettings.SrvServiceName;
    }

    /// <summary>
    /// Gets or sets whether the client connects directly to a single server rather than discovering
    /// and monitoring the topology it belongs to. The default value is <c>false</c>.
    /// </summary>
    public bool DirectConnection
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets whether load balanced mode is used. The default value is <c>false</c>.
    /// </summary>
    public bool LoadBalanced
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the name of the replica set the servers are expected to belong to. The default value is <c>null</c>.
    /// </summary>
    public string ReplicaSetName
    {
        get;
        set => field = Ensure.IsNotNullOrEmpty(value, nameof(ReplicaSetName));
    }

    /// <summary>
    /// Gets the connection string scheme. It is determined by the <see cref="MongoClientBuilder"/> factory method
    /// that created the builder.
    /// </summary>
    public ConnectionStringScheme Scheme
    {
        get;
    }

    /// <summary>
    /// Gets the server end points. Only <see cref="DnsEndPoint"/> and <see cref="IPEndPoint"/> are supported.
    /// With the <see cref="ConnectionStringScheme.MongoDBPlusSrv"/> scheme this is the single SRV host name.
    /// They are determined by the <see cref="MongoClientBuilder"/> factory method that created the builder.
    /// </summary>
    public IReadOnlyList<EndPoint> Servers
    {
        get;
    }

    /// <summary>
    /// Gets or sets the hostname suffix that hosts returned by an SRV lookup are validated against, replacing
    /// the domain name that would otherwise be inferred from the SRV hostname. Only valid with
    /// <see cref="ConnectionStringScheme.MongoDBPlusSrv"/>. The default value is <c>null</c>.
    /// <para>
    /// WARNING: Modifying the default SRV domain name validation can create vulnerabilities.
    /// Prefer the narrowest suffix that covers the deployment: the broader it is, the more
    /// hosts a forged SRV response could direct the driver to.
    /// </para>
    /// </summary>
    public string SrvAllowedHostsSuffix
    {
        get;
        set
        {
            if (value != null && !ConnectionString.TryNormalizeSrvAllowedHostsSuffix(value, out _, out var errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(SrvAllowedHostsSuffix));
            }

            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the limit on the number of SRV records used to populate the seedlist during initial
    /// discovery, as well as the number of additional hosts that may be added during SRV polling.
    /// Zero means no limit. Must be greater than or equal to zero. The default value is 0.
    /// </summary>
    public int SrvMaxHosts
    {
        get;
        set => field = Ensure.IsGreaterThanOrEqualToZero(value, nameof(SrvMaxHosts));
    }

    /// <summary>
    /// Gets or sets the SRV service name, which modifies the SRV URI to look like:
    /// <code>_{srvServiceName}._tcp.{hostname}.{domainname}</code>
    /// Must not be null or empty. The default value is "mongodb".
    /// </summary>
    public string SrvServiceName
    {
        get;
        set => field = Ensure.IsNotNullOrEmpty(value, nameof(SrvServiceName));
    }

    internal void Validate()
    {
        var servers = Servers;
        var isSrv = Scheme == ConnectionStringScheme.MongoDBPlusSrv;

        if (servers.Count == 0)
        {
            throw new InvalidOperationException("At least one server must be specified.");
        }

        if (servers.Any(server => server is not (DnsEndPoint or IPEndPoint)))
        {
            throw new InvalidOperationException("Servers must be specified as DnsEndPoint or IPEndPoint.");
        }

        if (isSrv)
        {
            if (servers.Count != 1)
            {
                throw new InvalidOperationException("Only 1 host is allowed when using the mongodb+srv scheme.");
            }

            if (servers[0] is not DnsEndPoint)
            {
                throw new InvalidOperationException("The host for the mongodb+srv scheme must be a DNS name, not an IP address.");
            }
        }

        if (DirectConnection)
        {
            if (isSrv)
            {
                throw new InvalidOperationException("SRV cannot be used with direct connections.");
            }

            if (servers.Count > 1)
            {
                throw new InvalidOperationException("Multiple host names cannot be used with direct connections.");
            }
        }

        if (SrvMaxHosts > 0)
        {
            if (!isSrv)
            {
                throw new InvalidOperationException("SrvMaxHosts can only be used with the mongodb+srv scheme.");
            }

            if (ReplicaSetName != null)
            {
                throw new InvalidOperationException("Specifying SrvMaxHosts when connecting to a replica set is invalid.");
            }
        }

        if (!isSrv && SrvServiceName != MongoInternalDefaults.MongoClientSettings.SrvServiceName)
        {
            throw new InvalidOperationException("Specifying SrvServiceName is only allowed with the mongodb+srv scheme.");
        }

        if (!isSrv && SrvAllowedHostsSuffix != null)
        {
            throw new InvalidOperationException("Specifying SrvAllowedHostsSuffix is only allowed with the mongodb+srv scheme.");
        }

        if (LoadBalanced)
        {
            if (servers.Count > 1)
            {
                throw new InvalidOperationException("Load balanced mode cannot be used with multiple host names.");
            }

            if (ReplicaSetName != null)
            {
                throw new InvalidOperationException("ReplicaSetName cannot be used with load balanced mode.");
            }

            if (SrvMaxHosts > 0)
            {
                throw new InvalidOperationException("SrvMaxHosts cannot be used with load balanced mode.");
            }

            if (DirectConnection)
            {
                throw new InvalidOperationException("Load balanced mode cannot be used with direct connection.");
            }
        }
    }

    internal void ApplyConnectionString(ConnectionString connectionString)
    {
        DirectConnection = connectionString.DirectConnection;
        LoadBalanced = connectionString.LoadBalanced;
        if (connectionString.ReplicaSet != null)
        {
            ReplicaSetName = connectionString.ReplicaSet;
        }
        if (connectionString.SrvAllowedHostsSuffix != null)
        {
            SrvAllowedHostsSuffix = connectionString.SrvAllowedHostsSuffix;
        }
        if (connectionString.SrvMaxHosts.HasValue)
        {
            SrvMaxHosts = connectionString.SrvMaxHosts.Value;
        }
        if (connectionString.SrvServiceName != null)
        {
            SrvServiceName = connectionString.SrvServiceName;
        }
    }
}
