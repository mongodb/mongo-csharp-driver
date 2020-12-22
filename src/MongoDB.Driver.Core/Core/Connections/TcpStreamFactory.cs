/* Copyright 2013-present MongoDB Inc.
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Connections
{
    /// <summary>
    /// Represents a factory for a binary stream over a TCP/IP connection.
    /// </summary>
    internal class TcpStreamFactory : IStreamFactory
    {
        // fields
        private readonly TcpStreamSettings _settings;

        // constructors
        public TcpStreamFactory()
        {
            _settings = new TcpStreamSettings();
        }

        public TcpStreamFactory(TcpStreamSettings settings)
        {
            _settings = Ensure.IsNotNull(settings, nameof(settings));
        }

        // methods
        public Stream CreateStream(EndPoint endPoint, CancellationToken cancellationToken)
        {
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1
            // ugh... I know... but there isn't a non-async version of dns resolution
            // in .NET Core
            var resolved = ResolveEndPointsAsync(endPoint).GetAwaiter().GetResult();
            for (int i = 0; i < resolved.Length; i++)
            {
                try
                {
                    var socket = CreateSocket(resolved[i]);
                    Connect(socket, resolved[i], cancellationToken);
                    return CreateNetworkStream(socket);
                }
                catch
                {
                    // if we have tried all of them and still failed,
                    // then blow up.
                    if (i == resolved.Length - 1)
                    {
                        throw;
                    }
                }
            }

            // we should never get here...
            throw new InvalidOperationException("Unabled to resolve endpoint.");
#else
            var socket = CreateSocket(endPoint);
            Connect(socket, endPoint, cancellationToken);
            return CreateNetworkStream(socket);
#endif
        }

        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
#if NETSTANDARD1_5 || NETSTANDARD2_0 || NETSTANDARD2_1
            var resolved = await ResolveEndPointsAsync(endPoint).ConfigureAwait(false);
            for (int i = 0; i < resolved.Length; i++)
            {
                try
                {
                    var socket = CreateSocket(resolved[i]);
                    await ConnectAsync(socket, resolved[i], cancellationToken).ConfigureAwait(false);
                    return CreateNetworkStream(socket);
                }
                catch
                {
                    // if we have tried all of them and still failed,
                    // then blow up.
                    if (i == resolved.Length - 1)
                    {
                        throw;
                    }
                }
            }

            // we should never get here...
            throw new InvalidOperationException("Unabled to resolve endpoint.");
#else
            var socket = CreateSocket(endPoint);
            await ConnectAsync(socket, endPoint, cancellationToken).ConfigureAwait(false);
            return CreateNetworkStream(socket);
#endif
        }

        // non-public methods
        private void ConfigureConnectedSocket(Socket socket)
        {
            socket.NoDelay = true;
            socket.ReceiveBufferSize = _settings.ReceiveBufferSize;
            socket.SendBufferSize = _settings.SendBufferSize;

            _settings.SocketConfigurator?.Invoke(socket);
        }

        private void Connect(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            var state = 1; // 1 == connecting, 2 == connected, 3 == timedout, 4 == cancelled

            using (new Timer(_ => ChangeState(3), null, _settings.ConnectTimeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(() => ChangeState(4)))
            {
                try
                {
                    var dnsEndPoint = endPoint as DnsEndPoint;
                    if (dnsEndPoint != null)
                    {
                        // mono doesn't support DnsEndPoint in its BeginConnect method.
                        socket.Connect(dnsEndPoint.Host, dnsEndPoint.Port);
                    }
                    else
                    {
                        socket.Connect(endPoint);
                    }
                    ChangeState(2); // note: might not actually go to state 2 if already in state 3 or 4
                }
                catch when (state == 1)
                {
                    try { socket.Dispose(); } catch { }
                    throw;
                }
                catch when (state >= 3)
                {
                    // a timeout or operation cancelled exception will be thrown instead
                }

                if (state == 3)
                {
                    var message = string.Format("Timed out connecting to {0}. Timeout was {1}.", endPoint, _settings.ConnectTimeout);
                    throw new TimeoutException(message);
                }
                if (state == 4) { throw new OperationCanceledException(); }
            }

            void ChangeState(int to)
            {
                var from = Interlocked.CompareExchange(ref state, to, 1);
                if (from == 1 && to >= 3)
                {
                    try { socket.Dispose(); } catch { } // disposing the socket aborts the connection attempt
                }
            }
        }

        private async Task ConnectAsync(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            var state = 1; // 1 == connecting, 2 == connected, 3 == timedout, 4 == cancelled

            using (new Timer(_ => ChangeState(3), null, _settings.ConnectTimeout, Timeout.InfiniteTimeSpan))
            using (cancellationToken.Register(() => ChangeState(4)))
            {
                try
                {
                    var dnsEndPoint = endPoint as DnsEndPoint;
#if !NET452
                    await socket.ConnectAsync(endPoint).ConfigureAwait(false);
#else
                    if (dnsEndPoint != null)
                    {
                        // mono doesn't support DnsEndPoint in its BeginConnect method.
                        await Task.Factory.FromAsync(socket.BeginConnect(dnsEndPoint.Host, dnsEndPoint.Port, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Factory.FromAsync(socket.BeginConnect(endPoint, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
#endif
                    ChangeState(2); // note: might not actually go to state 2 if already in state 3 or 4
                }
                catch when (state == 1)
                {
                    try { socket.Dispose(); } catch { }
                    throw;
                }
                catch when (state >= 3)
                {
                    // a timeout or operation cancelled exception will be thrown instead
                }

                if (state == 3)
                {
                    var message = string.Format("Timed out connecting to {0}. Timeout was {1}.", endPoint, _settings.ConnectTimeout);
                    throw new TimeoutException(message);
                }
                if (state == 4) { throw new OperationCanceledException(); }
            }

            void ChangeState(int to)
            {
                var from = Interlocked.CompareExchange(ref state, to, 1);
                if (from == 1 && to >= 3)
                {
                    try { socket.Dispose(); } catch { } // disposing the socket aborts the connection attempt
                }
            }
        }

        private NetworkStream CreateNetworkStream(Socket socket)
        {
            ConfigureConnectedSocket(socket);

            var stream = new NetworkStream(socket, true);

            if (_settings.ReadTimeout.HasValue)
            {
                var readTimeout = (int)_settings.ReadTimeout.Value.TotalMilliseconds;
                if (readTimeout != 0)
                {
                    stream.ReadTimeout = readTimeout;
                }
            }

            if (_settings.WriteTimeout.HasValue)
            {
                var writeTimeout = (int)_settings.WriteTimeout.Value.TotalMilliseconds;
                if (writeTimeout != 0)
                {
                    stream.WriteTimeout = writeTimeout;
                }
            }

            return stream;
        }

        private Socket CreateSocket(EndPoint endPoint)
        {
            var addressFamily = endPoint.AddressFamily;
            if (addressFamily == AddressFamily.Unspecified || addressFamily == AddressFamily.Unknown)
            {
                addressFamily = _settings.AddressFamily;
            }

            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            // not all platforms support IOControl
            try
            {
                var keepAliveValues = new KeepAliveValues
                {
                    OnOff = 1,
                    KeepAliveTime = 120000, // 120 seconds in milliseconds
                    KeepAliveInterval = 10000 // 10 seconds in milliseconds
                };
                socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues.ToBytes(), null);
            }
            catch (PlatformNotSupportedException)
            {
                // most platforms should support this call to SetSocketOption, but just in case call it in a try/catch also
                try
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                }
                catch (PlatformNotSupportedException)
                {
                    // ignore PlatformNotSupportedException
                }
            }

            return socket;
        }

        private async Task<EndPoint[]> ResolveEndPointsAsync(EndPoint initial)
        {
            var dnsInitial = initial as DnsEndPoint;
            if (dnsInitial == null)
            {
                return new[] { initial };
            }

            IPAddress address;
            if (IPAddress.TryParse(dnsInitial.Host, out address))
            {
                return new[] { new IPEndPoint(address, dnsInitial.Port) };
            }

            var preferred = initial.AddressFamily;
            if (preferred == AddressFamily.Unspecified || preferred == AddressFamily.Unknown)
            {
                preferred = _settings.AddressFamily;
            }

            return (await Dns.GetHostAddressesAsync(dnsInitial.Host).ConfigureAwait(false))
                .Select(x => new IPEndPoint(x, dnsInitial.Port))
                .OrderBy(x => x, new PreferredAddressFamilyComparer(preferred))
                .ToArray();
        }

        private class PreferredAddressFamilyComparer : IComparer<EndPoint>
        {
            private readonly AddressFamily _preferred;

            public PreferredAddressFamilyComparer(AddressFamily preferred)
            {
                _preferred = preferred;
            }

            public int Compare(EndPoint x, EndPoint y)
            {
                if (x.AddressFamily == y.AddressFamily)
                {
                    return 0;
                }
                if (x.AddressFamily == _preferred)
                {
                    return -1;
                }
                else if (y.AddressFamily == _preferred)
                {
                    return 1;
                }

                return 0;
            }
        }
    }
}
