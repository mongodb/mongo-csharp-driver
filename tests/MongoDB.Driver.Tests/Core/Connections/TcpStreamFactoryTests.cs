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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    [Trait("Category", "Integration")]
    public class TcpStreamFactoryTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task Connect_should_dispose_socket_if_socket_fails([Values(false, true)] bool async)
        {
            RequireServer.Check();

            var subject = new TcpStreamFactory();
            var endpoint = new DnsEndPoint("test", 80); // not existed endpoint which will fail when we call socket.Connect

            using (var testSocket = new TestSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var exception = async ?
                    await Record.ExceptionAsync(() => subject.ConnectAsync(testSocket, endpoint, CancellationToken.None)) :
                    Record.Exception(() => subject.Connect(testSocket, endpoint, CancellationToken.None));

                exception.Should().NotBeNull();
                testSocket.DisposeAttempts.Should().Be(1);
            }
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_tcpStreamSettings_is_null()
        {
            var exception = Record.Exception(() => new TcpStreamFactory(null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("settings");
        }

        [Theory]
        [ParameterAttributeData]
        public async Task CreateStream_should_throw_a_SocketException_when_the_endpoint_could_not_be_resolved([Values(false, true)] bool async)
        {
            var subject = new TcpStreamFactory();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.CreateStreamAsync(new DnsEndPoint("not-gonna-exist-i-hope", 27017), CancellationToken.None)) :
                Record.Exception(() => subject.CreateStream(new DnsEndPoint("not-gonna-exist-i-hope", 27017), CancellationToken.None));

            exception.Should().BeAssignableTo<SocketException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task CreateStream_should_throw_when_cancellation_is_requested([Values(false, true)] bool async)
        {
            var subject = new TcpStreamFactory();
            var endPoint = new IPEndPoint(new IPAddress(0x01010101), 12345); // a non-existent host and port
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

            var exception = async ?
                await Record.ExceptionAsync(() => subject.CreateStreamAsync(endPoint, cancellationTokenSource.Token)) :
                Record.Exception(() => subject.CreateStream(endPoint, cancellationTokenSource.Token));
            if (async)
            {
                exception.Should().BeOfType<TaskCanceledException>();
            }
            else
            {
                exception.Should().BeOfType<OperationCanceledException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task CreateStream_should_throw_when_connect_timeout_has_expired([Values(false, true)] bool async)
        {
            var settings = new TcpStreamSettings(connectTimeout: TimeSpan.FromMilliseconds(20));
            var subject = new TcpStreamFactory(settings);
            var endPoint = new IPEndPoint(new IPAddress(0x01010101), 12345); // a non-existent host and port

            var exception = async ?
                await Record.ExceptionAsync(() => subject.CreateStreamAsync(endPoint, CancellationToken.None)) :
                Record.Exception(() => subject.CreateStream(endPoint, CancellationToken.None));

            exception.Should().BeOfType<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task CreateStream_should_call_the_socketConfigurator([Values(false, true)] bool async)
        {
            RequireServer.Check();
            var socketConfiguratorWasCalled = false;
            Action<Socket> socketConfigurator = s => socketConfiguratorWasCalled = true;
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            if (async)
            {
                await subject.CreateStreamAsync(endPoint, CancellationToken.None);
            }
            else
            {
                subject.CreateStream(endPoint, CancellationToken.None);
            }

            socketConfiguratorWasCalled.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task CreateStream_should_connect_to_a_running_server_and_return_a_non_null_stream([Values(false, true)] bool async)
        {
            RequireServer.Check();
            var subject = new TcpStreamFactory();
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            Stream stream;
            if (async)
            {
                stream = await subject.CreateStreamAsync(endPoint, CancellationToken.None);
            }
            else
            {
                stream = subject.CreateStream(endPoint, CancellationToken.None);
            }

            stream.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SocketConfigurator_can_be_used_to_set_keepAlive([Values(false, true)] bool async)
        {
            RequireServer.Check();
            Action<Socket> socketConfigurator = s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            Stream stream;
            if (async)
            {
                stream = await subject.CreateStreamAsync(endPoint, CancellationToken.None);
            }
            else
            {
                stream = subject.CreateStream(endPoint, CancellationToken.None);
            }

            var socketProperty = typeof(NetworkStream).GetProperty("Socket", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var socket = (Socket)socketProperty.GetValue(stream);
            var keepAlive = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive);
            keepAlive.Should().NotBe(0); // .NET returns 1 but Mono returns 8
        }

        [Fact]
        public void ConfigureConnectedSocket_should_set_the_buffer_sizes_when_specified()
        {
            // the OS may not report back the exact value that was set (e.g. Linux doubles it), so compare
            // against a reference socket that had the same value assigned directly instead of a literal
            using var referenceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            referenceSocket.ReceiveBufferSize = 131072;
            referenceSocket.SendBufferSize = 262144;
            var settings = new TcpStreamSettings(receiveBufferSize: 131072, sendBufferSize: 262144);
            var subject = new TcpStreamFactory(settings);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            subject.ConfigureConnectedSocket(socket);

            socket.ReceiveBufferSize.Should().Be(referenceSocket.ReceiveBufferSize);
            socket.SendBufferSize.Should().Be(referenceSocket.SendBufferSize);
        }

        [Fact]
        public void ConfigureConnectedSocket_should_leave_the_operating_system_default_when_OperatingSystemDefaultBufferSize_is_specified()
        {
            using var referenceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var osDefaultReceiveBufferSize = referenceSocket.ReceiveBufferSize;
            var osDefaultSendBufferSize = referenceSocket.SendBufferSize;
            var settings = new TcpStreamSettings(
                receiveBufferSize: TcpStreamSettings.OperatingSystemDefaultBufferSize,
                sendBufferSize: TcpStreamSettings.OperatingSystemDefaultBufferSize);
            var subject = new TcpStreamFactory(settings);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            subject.ConfigureConnectedSocket(socket);

            socket.ReceiveBufferSize.Should().Be(osDefaultReceiveBufferSize);
            socket.SendBufferSize.Should().Be(osDefaultSendBufferSize);
        }

        // nested types
        private class TestSocket : Socket
        {
            public int DisposeAttempts { get; set; } = 0;

            public TestSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType) : base(addressFamily, socketType, protocolType)
            {
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                DisposeAttempts++;
            }
        }
    }

    internal static class TcpStreamFactoryReflector
    {
        internal static TcpStreamSettings _settings(this TcpStreamFactory obj) => (TcpStreamSettings)Reflector.GetFieldValue(obj, nameof(_settings));

        internal static void ConfigureConnectedSocket(this TcpStreamFactory obj, Socket socket)
        {
            Reflector.Invoke(obj, nameof(ConfigureConnectedSocket), socket);
        }

        internal static void Connect(this TcpStreamFactory obj, Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            Reflector.Invoke(obj, nameof(Connect), socket, endPoint, cancellationToken);
        }

        internal static Task ConnectAsync(this TcpStreamFactory obj, Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
        {
            return (Task)Reflector.Invoke(obj, nameof(ConnectAsync), socket, endPoint, cancellationToken);
        }
    }
}
