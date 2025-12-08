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
    internal sealed class TcpStreamFactory : IStreamFactory
    {
        private static readonly byte[] __ensureConnectedBuffer = new byte[1];

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
#if NET472
            Socket socket = null;
            NetworkStream stream = null;

            try
            {
                socket = CreateSocket(endPoint);
                Connect(socket, endPoint, cancellationToken);
                stream = CreateNetworkStream(socket);

                return stream;
            }
            catch
            {
                socket?.Dispose();
                stream?.Dispose();

                throw;
            }
#else
            var resolved = ResolveEndPoints(endPoint);
            for (var i = 0; i < resolved.Length; i++)
            {
                Socket socket = null;
                NetworkStream stream = null;

                try
                {
                    socket = CreateSocket(resolved[i]);
                    Connect(socket, resolved[i], cancellationToken);
                    stream = CreateNetworkStream(socket);
                    return stream;
                }
                catch
                {
                    socket?.Dispose();
                    stream?.Dispose();

                    // if we have tried all of them and still failed,
                    // then blow up.
                    if (i == resolved.Length - 1)
                    {
                        throw;
                    }
                }
            }

            // we should never get here...
            throw new InvalidOperationException("Unable to resolve endpoint.");
#endif
        }

        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
#if NET472
            Socket socket = null;
            NetworkStream stream = null;

            try
            {
                socket = CreateSocket(endPoint);
                await ConnectAsync(socket, endPoint, cancellationToken).ConfigureAwait(false);
                stream = CreateNetworkStream(socket);
                return stream;
            }
            catch
            {
                socket?.Dispose();
                stream?.Dispose();

                throw;
            }
#else
            var resolved = await ResolveEndPointsAsync(endPoint).ConfigureAwait(false);
            for (int i = 0; i < resolved.Length; i++)
            {
                Socket socket = null;
                NetworkStream stream = null;

                try
                {
                    socket = CreateSocket(resolved[i]);
                    await ConnectAsync(socket, resolved[i], cancellationToken).ConfigureAwait(false);
                    stream = CreateNetworkStream(socket);
                    return stream;
                }
                catch
                {
                    socket?.Dispose();
                    stream?.Dispose();

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
            var callbackState = new ConnectOperationState(socket);
            using var timeoutCancellationTokenSource = new CancellationTokenSource(_settings.ConnectTimeout);
            using var combinedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
            using var cancellationSubscription = combinedCancellationTokenSource.Token.Register(state =>
            {
                var operationState = (ConnectOperationState)state;
                if (operationState.IsSucceeded)
                {
                    return;
                }
                DisposeSocket(operationState.Socket);
            }, callbackState);

            try
            {
#if NET472
                if (endPoint is DnsEndPoint dnsEndPoint)
                {
                    // mono doesn't support DnsEndPoint in its Connect method.
                    socket.Connect(dnsEndPoint.Host, dnsEndPoint.Port);
                }
                else
                {
                    socket.Connect(endPoint);
                }
#else
                socket.Connect(endPoint);
#endif
                EnsureConnected(socket);
                callbackState.IsSucceeded = true;
            }
            catch
            {
                DisposeSocket(socket);

                cancellationToken.ThrowIfCancellationRequested();
                if (timeoutCancellationTokenSource.IsCancellationRequested)
                {
                    throw new TimeoutException($"Timed out connecting to {endPoint}. Timeout was {_settings.ConnectTimeout}.");
                }

                throw;
            }

            static void DisposeSocket(Socket socket)
            {
                try
                {
                    socket.Dispose();
                }
                catch
                {
                    // Ignore any exceptions.
                }
            }

            static void EnsureConnected(Socket socket)
            {
                bool originalBlockingState = socket.Blocking;

                try
                {
                    socket.Blocking = false;
                    // Try to use the socket to ensure it's connected. On MacOS with net6.0 sometimes Connect is completed successfully even after the socket disposal.
                    socket.Send(__ensureConnectedBuffer, 0, 0);
                }
                finally
                {
                    // Restore original blocking state
                    socket.Blocking = originalBlockingState;
                }
            }
        }

        private async Task ConnectAsync(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            Task connectTask;
#if NET472
            if (endPoint is DnsEndPoint dnsEndPoint)
            {
                // mono doesn't support DnsEndPoint in its ConnectAsync method.
                connectTask = socket.ConnectAsync(dnsEndPoint.Host, dnsEndPoint.Port);
            }
            else
            {
                connectTask = socket.ConnectAsync(endPoint);
            }
#else
            connectTask = socket.ConnectAsync(endPoint);
#endif
            try
            {
                await connectTask.WaitAsync(_settings.ConnectTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    connectTask.IgnoreExceptions();
                    socket.Dispose();
                }
                catch { }

                if (ex is TimeoutException)
                {
                    throw new TimeoutException($"Timed out connecting to {endPoint}. Timeout was {_settings.ConnectTimeout}.");
                }

                throw;
            }
        }

        private NetworkStream CreateNetworkStream(Socket socket)
        {
            ConfigureConnectedSocket(socket);
            return new NetworkStream(socket, true);
        }

        private Socket CreateSocket(EndPoint endPoint)
        {
            var addressFamily = endPoint.AddressFamily;
            if (addressFamily == AddressFamily.Unspecified || addressFamily == AddressFamily.Unknown)
            {
                addressFamily = _settings.AddressFamily;
            }

            var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                if (OperatingSystemHelper.CurrentOperatingSystem == OperatingSystemPlatform.Windows)
                {
                    // Reviewing the .NET source, Socket.IOControl for IOControlCode.KeepAlivesValue will
                    // throw PlatformNotSupportedException on all platforms except for Windows.
                    // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SocketPal.Unix.cs#L1346
                    var keepAliveValues = new KeepAliveValues
                    {
                        OnOff = 1,
                        KeepAliveTime = 120000, // 120 seconds in milliseconds
                        KeepAliveInterval = 10000 // 10 seconds in milliseconds
                    };
#pragma warning disable CA1416 //  Validate platform compatibility
                    socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues.ToBytes(), null);
#pragma warning restore CA1416 //  Validate platform compatibility
                }
                else
                {
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                }
            }
            catch (PlatformNotSupportedException)
            {
                // ignore PlatformNotSupportedException
            }

            return socket;
        }

        private EndPoint[] ResolveEndPoints(EndPoint initial)
        {
            if (initial is not DnsEndPoint dnsInitial)
            {
                return [initial];
            }

            if (IPAddress.TryParse(dnsInitial.Host, out var address))
            {
                return [new IPEndPoint(address, dnsInitial.Port)];
            }

            var preferred = initial.AddressFamily;
            if (preferred is AddressFamily.Unspecified or AddressFamily.Unknown)
            {
                preferred = _settings.AddressFamily;
            }

            var hostAddresses = Dns.GetHostAddresses(dnsInitial.Host);
            return hostAddresses
                .Select(x => new IPEndPoint(x, dnsInitial.Port))
                .OrderBy(x => x, new PreferredAddressFamilyComparer(preferred))
                .ToArray();
        }

        private async Task<EndPoint[]> ResolveEndPointsAsync(EndPoint initial)
        {
            if (initial is not DnsEndPoint dnsInitial)
            {
                return [initial];
            }

            if (IPAddress.TryParse(dnsInitial.Host, out var address))
            {
                return [new IPEndPoint(address, dnsInitial.Port)];
            }

            var preferred = initial.AddressFamily;
            if (preferred is AddressFamily.Unspecified or AddressFamily.Unknown)
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

        private sealed record ConnectOperationState(Socket Socket)
        {
            public bool IsSucceeded { get; set; }
        }
    }
}
