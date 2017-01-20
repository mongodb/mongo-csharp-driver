/* Copyright 2013-2016 MongoDB Inc.
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
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnectionFactoryTests
    {
        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_connectionSettings_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;

            Action act = () => new BinaryConnectionFactory(
                null,
                streamFactory,
                eventSubscriber);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_streamFactory_is_null()
        {
            var eventSubscriber = new Mock<IEventSubscriber>().Object;

            Action act = () => new BinaryConnectionFactory(
                new ConnectionSettings(),
                null,
                eventSubscriber);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateConnection_should_throw_an_ArgumentNullException_when_serverId_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber);

            Action act = () => subject.CreateConnection(null, new DnsEndPoint("localhost", 27017));
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateConnection_should_throw_an_ArgumentNullException_when_endPoint_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber);

            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

            Action act = () => subject.CreateConnection(serverId, null);
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateConnection_should_return_a_BinaryConnection()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber);

            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

            var connection = subject.CreateConnection(serverId, serverId.EndPoint);
            connection.Should().NotBeNull();
            connection.Should().BeOfType<BinaryConnection>();
        }
    }
}