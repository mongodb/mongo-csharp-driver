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

using System.Collections.Generic;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures the endpoints a <see cref="MongoClient"/> connects to and the topology it expects.
/// </summary>
public sealed class ConnectivityBuilder
{
    internal ConnectivityBuilder()
    {
        Scheme = ConnectionStringScheme.MongoDB;
        Servers = new[] { new MongoServerAddress("localhost") };
        SrvServiceName = MongoInternalDefaults.MongoClientSettings.SrvServiceName;
    }

    /// <summary>
    /// Gets or sets whether the client connects directly to a single server rather than discovering
    /// and monitoring the topology it belongs to. The default value is <c>false</c>.
    /// </summary>
    public bool DirectConnection { get; set; }

    /// <summary>
    /// Gets or sets whether load balanced mode is used. The default value is <c>false</c>.
    /// </summary>
    public bool LoadBalanced { get; set; }

    /// <summary>
    /// Gets or sets the name of the replica set. The default value is <c>null</c>.
    /// </summary>
    public string ReplicaSetName { get; set; }

    /// <summary>
    /// Gets or sets the connection string scheme. The default value is
    /// <see cref="ConnectionStringScheme.MongoDB"/>.
    /// </summary>
    public ConnectionStringScheme Scheme { get; set; }

    /// <summary>
    /// Gets or sets the list of server addresses. The default value is a single
    /// <c>localhost:27017</c> address.
    /// </summary>
    public IEnumerable<MongoServerAddress> Servers
    {
        get;
        set => field = new List<MongoServerAddress>(Ensure.IsNotNull(value, nameof(Servers))).AsReadOnly();
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
}
