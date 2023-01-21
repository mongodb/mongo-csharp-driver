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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using Xunit;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Bson.TestHelpers;

namespace MongoDB.Driver.Core.Connections
{
    public class TcpStreamFactoryTests
    {
        [Theory]
        [ParameterAttributeData]
        public void Connect_should_dispose_socket_if_socket_fails([Values(false, true)] bool async)
        {
            RequireServer.Check();

            var subject = new TcpStreamFactory();
            var endpoint = new DnsEndPoint("test", 80); // not existed endpoint which will fail when we call socket.Connect

            using (var testSocket = new TestSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Exception exception;
                if (async)
                {
                    exception = Record.Exception(
                        () =>
                            subject
                                .ConnectAsync(testSocket, endpoint, CancellationToken.None)
                                .GetAwaiter()
                                .GetResult());
                }
                else
                {
                    exception = Record.Exception(() => subject.Connect(testSocket, endpoint, CancellationToken.None));
                }

                exception.Should().NotBeNull();
                testSocket.DisposeAttempts.Should().Be(1);
            }
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_tcpStreamSettings_is_null()
        {
            Action act = () => new TcpStreamFactory(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateStream_should_throw_a_SocketException_when_the_endpoint_could_not_be_resolved(
            [Values(false, true)]
            bool async)
        {
            var subject = new TcpStreamFactory();

            Action act;
            if (async)
            {
                act = () => subject.CreateStreamAsync(new DnsEndPoint("not-gonna-exist-i-hope", 27017), CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.CreateStream(new DnsEndPoint("not-gonna-exist-i-hope", 27017), CancellationToken.None);
            }

            act.ShouldThrow<SocketException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateStream_should_throw_when_cancellation_is_requested(
            [Values(false, true)]
            bool async)
        {
            var subject = new TcpStreamFactory();
            var endPoint = new IPEndPoint(new IPAddress(0x01010101), 12345); // a non-existent host and port
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

            Action action;
            if (async)
            {
                action = () => subject.CreateStreamAsync(endPoint, cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            else
            {
                action = () => subject.CreateStream(endPoint, cancellationTokenSource.Token);
            }

            action.ShouldThrow<OperationCanceledException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateStream_should_throw_when_connect_timeout_has_expired(
            [Values(false, true)]
            bool async)
        {
            var settings = new TcpStreamSettings(connectTimeout: TimeSpan.FromMilliseconds(20));
            var subject = new TcpStreamFactory(settings);
            var endPoint = new IPEndPoint(new IPAddress(0x01010101), 12345); // a non-existent host and port

            Action action;
            if (async)
            {
                action = () => subject.CreateStreamAsync(endPoint, CancellationToken.None).GetAwaiter().GetResult(); ;
            }
            else
            {
                action = () => subject.CreateStream(endPoint, CancellationToken.None);
            }

            action.ShouldThrow<TimeoutException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateStream_should_call_the_socketConfigurator(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            var socketConfiguratorWasCalled = false;
            Action<Socket> socketConfigurator = s => socketConfiguratorWasCalled = true;
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            if (async)
            {
                subject.CreateStreamAsync(endPoint, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                subject.CreateStream(endPoint, CancellationToken.None);
            }

            socketConfiguratorWasCalled.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public void CreateStream_should_connect_to_a_running_server_and_return_a_non_null_stream(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            var subject = new TcpStreamFactory();
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            Stream stream;
            if (async)
            {
                stream = subject.CreateStreamAsync(endPoint, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                stream = subject.CreateStream(endPoint, CancellationToken.None);
            }

            stream.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void SocketConfigurator_can_be_used_to_set_keepAlive(
            [Values(false, true)]
            bool async)
        {
            RequireServer.Check();
            Action<Socket> socketConfigurator = s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            Stream stream;
            if (async)
            {
                stream = subject.CreateStreamAsync(endPoint, CancellationToken.None).GetAwaiter().GetResult();
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
