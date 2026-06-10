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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Connections
{
    public class BinaryConnectionFactoryTests
    {
        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_settings_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;

            var exception = Record.Exception(() => new BinaryConnectionFactory(
                settings: null,
                streamFactory,
                eventSubscriber,
                serverApi: null,
                loggerFactory: null,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("settings");
        }

        [Fact]
        public void Constructor_should_throw_an_ArgumentNullException_when_streamFactory_is_null()
        {
            var eventSubscriber = new Mock<IEventSubscriber>().Object;

            var exception = Record.Exception(() => new BinaryConnectionFactory(
                settings: new ConnectionSettings(),
                streamFactory: null,
                eventSubscriber,
                serverApi: null,
                loggerFactory: null,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("streamFactory");
        }

        [Fact]
        public void CreateConnection_should_throw_an_ArgumentNullException_when_serverId_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber,
                serverApi: null,
                loggerFactory: null,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            var exception = Record.Exception(() => subject.CreateConnection(null, new DnsEndPoint("localhost", 27017)));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("serverId");
        }

        [Fact]
        public void CreateConnection_should_throw_an_ArgumentNullException_when_endPoint_is_null()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber,
                serverApi: null,
                loggerFactory: null,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));
            var exception = Record.Exception(() => subject.CreateConnection(serverId, null));

            exception.Should().BeOfType<ArgumentNullException>().Subject
                .ParamName.Should().Be("endPoint");
        }

        [Fact]
        public void CreateConnection_should_return_a_BinaryConnection()
        {
            var streamFactory = new Mock<IStreamFactory>().Object;
            var eventSubscriber = new Mock<IEventSubscriber>().Object;
            var serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            var subject = new BinaryConnectionFactory(
                new ConnectionSettings(),
                streamFactory,
                eventSubscriber,
                serverApi,
                loggerFactory: null,
                tracingOptions: null,
                socketReadTimeout: Timeout.InfiniteTimeSpan,
                socketWriteTimeout: Timeout.InfiniteTimeSpan);

            var serverId = new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017));

            var connection = subject.CreateConnection(serverId, serverId.EndPoint);
            connection.Should().NotBeNull();
            connection.Should().BeOfType<BinaryConnection>();
        }
    }

    public static class BinaryConnectionFactoryReflector
    {
        internal static ConnectionSettings _settings(this BinaryConnectionFactory obj) => (ConnectionSettings)Reflector.GetFieldValue(obj, nameof(_settings));
        internal static IStreamFactory _streamFactory(this BinaryConnectionFactory obj) => (IStreamFactory)Reflector.GetFieldValue(obj, nameof(_streamFactory));
    }
}
