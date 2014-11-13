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
        public void CreateStreamAsync_should_connect_to_a_running_server_and_return_a_non_null_stream()
        {
            var subject = new TcpStreamFactory();

            var stream = subject.CreateStreamAsync(new DnsEndPoint("localhost", 27017), CancellationToken.None);
            stream.Should().NotBeNull();
        }
    }
}