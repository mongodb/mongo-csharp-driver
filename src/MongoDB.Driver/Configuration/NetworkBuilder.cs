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
using System.Net.Sockets;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Configuration;

/// <summary>
/// Configures the socket level settings and wire compression a <see cref="MongoClient"/> uses to reach
/// the servers.
/// </summary>
public sealed class NetworkBuilder
{
    internal NetworkBuilder()
    {
        Compressors = new CompressorConfiguration[0];
        ConnectTimeout = MongoDefaults.ConnectTimeout;
        ReceiveBufferSize = MongoDefaults.TcpReceiveBufferSize;
        SendBufferSize = MongoDefaults.TcpSendBufferSize;
        SocketTimeout = MongoDefaults.SocketTimeout;
    }

    /// <summary>
    /// Gets or sets the compressors offered to the server, in preference order. The connection uses the
    /// first one the server also supports, or no compression if there is no overlap. Must not be null.
    /// The default value is an empty list, which disables compression.
    /// </summary>
    public IReadOnlyList<CompressorConfiguration> Compressors
    {
        get;
        set => field = Ensure.IsNotNull(value, nameof(Compressors));
    }

    /// <summary>
    /// Gets or sets how long to wait for a socket to connect. Must be infinite, or greater than or equal
    /// to zero. Defaults to <see cref="MongoDefaults.ConnectTimeout"/> (30 seconds).
    /// </summary>
    public TimeSpan ConnectTimeout
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(ConnectTimeout));
    }

    /// <summary>
    /// Gets or sets a value indicating whether IPv6 addresses are tried before IPv4 addresses when a server
    /// host name resolves to both. Every resolved address is still tried, so the other address family is used
    /// as a fallback. Has no effect on servers given as an IP address. The default value is <c>false</c>,
    /// which tries IPv4 addresses first.
    /// </summary>
    public bool IPv6 { get; set; }

    /// <summary>
    /// Gets or sets the size, in bytes, of each socket's receive buffer. Must be greater than zero.
    /// Defaults to <see cref="MongoDefaults.TcpReceiveBufferSize"/> (64 KiB).
    /// </summary>
    public int? ReceiveBufferSize
    {
        get;
        set => field = Ensure.IsNullOrGreaterThanZero(value, nameof(ReceiveBufferSize));
    }

    /// <summary>
    /// Gets or sets the size, in bytes, of each socket's send buffer. Must be greater than zero.
    /// Defaults to <see cref="MongoDefaults.TcpSendBufferSize"/> (64 KiB).
    /// </summary>
    public int? SendBufferSize
    {
        get;
        set => field = Ensure.IsNullOrGreaterThanZero(value, nameof(SendBufferSize));
    }

    /// <summary>
    /// Gets or sets a delegate that is called with every socket once it is connected, after the driver has
    /// applied its own socket settings, so that it can set additional socket options (for example keep-alive
    /// or linger). The default value is <c>null</c>.
    /// </summary>
    /// <remarks>
    /// The delegate is called for every connection the client opens, including the ones used to monitor
    /// servers, possibly from several threads at once, so it must be thread-safe.
    /// </remarks>
    public Action<Socket> SocketConfigurator { get; set; }

    /// <summary>
    /// Gets or sets the socket read and write timeout. Must be infinite, or greater than or equal to zero.
    /// Defaults to <see cref="MongoDefaults.SocketTimeout"/> (zero, which uses the operating system default).
    /// </summary>
    public TimeSpan SocketTimeout
    {
        get;
        set => field = Ensure.IsInfiniteOrGreaterThanOrEqualToZero(value, nameof(SocketTimeout));
    }

    /// <summary>
    /// Gets or sets the SOCKS5 proxy the client connects through. The default value is <c>null</c>,
    /// which connects directly.
    /// </summary>
    public Socks5ProxySettings Socks5Proxy { get; set; }
}
