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
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerFactoryTests
    {
        private ClusterId _clusterId;
#pragma warning disable CS0618 // Type or member is obsolete
        private ClusterConnectionMode _clusterConnectionMode;
        private ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private IConnectionPoolFactory _connectionPoolFactory;
        private bool? _directConnection;
        private EndPoint _endPoint;
        private IEventSubscriber _eventSubscriber;
        private ServerApi _serverApi;
        private IServerMonitorFactory _serverMonitorFactory;
        private ServerSettings _settings;

        public ServerFactoryTests()
        {
            _clusterId = new ClusterId();
#pragma warning disable CS0618 // Type or member is obsolete
            _clusterConnectionMode = ClusterConnectionMode.Standalone;
            _connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
            _connectionPoolFactory = new Mock<IConnectionPoolFactory>().Object;
            _directConnection = null;
            _endPoint = new DnsEndPoint("localhost", 27017);
            var mockServerMonitor = new Mock<IServerMonitor>();
            mockServerMonitor.Setup(m => m.Description).Returns(new ServerDescription(new ServerId(_clusterId, _endPoint), _endPoint));
            var mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(mockServerMonitor.Object);
            _serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            _serverMonitorFactory = mockServerMonitorFactory.Object;
            _eventSubscriber = new Mock<IEventSubscriber>().Object;
            _settings = new ServerSettings();
        }

        [Fact]
        public void Constructor_should_not_throw_when_serverApi_is_null()
        {
            Action act = () => new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, null);

            act.ShouldNotThrow();
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, null, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, null, _serverMonitorFactory, _eventSubscriber, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_heartbeatConnectionFactory_is_null()
        {
            Action act = () => new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, null, _eventSubscriber, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, null, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_clusterId_is_null()
        {
            var subject = new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi);
            var clusterClock = new Mock<IClusterClock>().Object;

            Action act = () => subject.CreateServer(null, clusterClock, _endPoint);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_clusterClock_is_null()
        {
            var subject = new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi);
            var clusterId = new ClusterId();

            Action act = () => subject.CreateServer(clusterId, null, _endPoint);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_endPoint_is_null()
        {
            var subject = new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi);
            var clusterClock = new Mock<IClusterClock>().Object;

            Action act = () => subject.CreateServer(_clusterId, clusterClock, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_return_Server()
        {
            var subject = new ServerFactory(_clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi);
            var clusterClock = new Mock<IClusterClock>().Object;

            var result = subject.CreateServer(_clusterId, clusterClock, _endPoint);

            result.Should().NotBeNull();
            result.Should().BeOfType<Server>();
        }
    }
}
