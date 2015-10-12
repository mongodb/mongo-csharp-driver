/* Copyright 2013-2015 MongoDB Inc.
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
using System.IO;
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
            var socket = CreateSocket(endPoint);
            Connect(socket, endPoint, cancellationToken);
            return CreateNetworkStream(socket);
        }

        public async Task<Stream> CreateStreamAsync(EndPoint endPoint, CancellationToken cancellationToken)
        {
            var socket = CreateSocket(endPoint);
            await ConnectAsync(socket, endPoint, cancellationToken).ConfigureAwait(false);
            return CreateNetworkStream(socket);
        }

        // non-public methods
        private void ConfigureConnectedSocket(Socket socket)
        {
            socket.NoDelay = true;
            socket.ReceiveBufferSize = _settings.ReceiveBufferSize;
            socket.SendBufferSize = _settings.SendBufferSize;

            var socketConfigurator = _settings.SocketConfigurator;
            if (socketConfigurator != null)
            {
                socketConfigurator(socket);
            }
        }

        private void Connect(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            var connected = false;
            var cancelled = false;
            var timedOut = false;

            using (var registration = cancellationToken.Register(() => { if (!connected) { cancelled = true; try { socket.Close(); } catch { } } }))
            using (var timer = new Timer(_ => { if (!connected) { timedOut = true; try { socket.Close(); } catch { } } }, null, _settings.ConnectTimeout, Timeout.InfiniteTimeSpan))
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
                    connected = true;
                    return;
                }
                catch
                {
                    if (!cancelled && !timedOut)
                    {
                        throw;
                    }
                }
            }

            if (socket.Connected)
            {
                try { socket.Close(); } catch { }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (timedOut)
            {
                var message = string.Format("Timed out connecting to {0}. Timeout was {1}.", endPoint, _settings.ConnectTimeout);
                throw new TimeoutException(message);
            }
        }

        private async Task ConnectAsync(Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            var connected = false;
            var cancelled = false;
            var timedOut = false;

            using (var registration = cancellationToken.Register(() => { if (!connected) { cancelled = true; try { socket.Close(); } catch { } } }))
            using (var timer = new Timer(_ => { if (!connected) { timedOut = true; try { socket.Close(); } catch { } } }, null, _settings.ConnectTimeout, Timeout.InfiniteTimeSpan))
            {
                try
                {
                    var dnsEndPoint = endPoint as DnsEndPoint;
                    if (dnsEndPoint != null)
                    {
                        // mono doesn't support DnsEndPoint in its BeginConnect method.
                        await Task.Factory.FromAsync(socket.BeginConnect(dnsEndPoint.Host, dnsEndPoint.Port, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Factory.FromAsync(socket.BeginConnect(endPoint, null, null), socket.EndConnect).ConfigureAwait(false);
                    }
                    connected = true;
                    return;
                }
                catch
                {
                    if (!cancelled && !timedOut)
                    {
                        throw;
                    }
                }
            }

            if (socket.Connected)
            {
                try { socket.Close(); } catch { }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (timedOut)
            {
                var message = string.Format("Timed out connecting to {0}. Timeout was {1}.", endPoint, _settings.ConnectTimeout);
                throw new TimeoutException(message);
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
            return new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }
    }
}
