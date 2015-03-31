/* Copyright 2013-2014 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Reflection;

namespace MongoDB.Driver.Core.Connections
{
    [TestFixture]
    public class TcpStreamFactoryTests
    {
        [Test]
        public void Constructor_should_throw_an_ArgumentNullException_when_tcpStreamSettings_is_null()
        {
            Action act = () => new TcpStreamFactory(null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void CreateStreamAsync_should_throw_a_SocketException_when_the_endpoint_could_not_be_resolved()
        {
            var subject = new TcpStreamFactory();

            Action act = () => subject.CreateStreamAsync(new DnsEndPoint("not-gonna-exist-i-hope", 27017), CancellationToken.None).Wait();

            act.ShouldThrow<SocketException>();
        }

        [Test]
        [RequiresServer]
        public async Task CreateStreamAsync_should_call_the_socketConfigurator()
        {
            var socketConfiguratorWasCalled = false;
            Action<Socket> socketConfigurator = s => socketConfiguratorWasCalled = true;
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            await subject.CreateStreamAsync(endPoint, CancellationToken.None);

            socketConfiguratorWasCalled.Should().BeTrue();
        }

        [Test]
        [RequiresServer]
        public async Task CreateStreamAsync_should_connect_to_a_running_server_and_return_a_non_null_stream()
        {
            var subject = new TcpStreamFactory();
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            var stream = await subject.CreateStreamAsync(endPoint, CancellationToken.None);

            stream.Should().NotBeNull();
        }

        [Test]
        [RequiresServer]
        public async Task SocketConfigurator_can_be_used_to_set_keepAlive()
        {
            Action<Socket> socketConfigurator = s => s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var settings = new TcpStreamSettings(socketConfigurator: socketConfigurator);
            var subject = new TcpStreamFactory(settings);
            var endPoint = CoreTestConfiguration.ConnectionString.Hosts[0];

            var stream = await subject.CreateStreamAsync(endPoint, CancellationToken.None);

            var socketProperty = typeof(NetworkStream).GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
            var socket = (Socket)socketProperty.GetValue(stream);
            var keepAlive = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive);
            keepAlive.Should().NotBe(0); // .NET returns 1 but Mono returns 8
        }
    }
}