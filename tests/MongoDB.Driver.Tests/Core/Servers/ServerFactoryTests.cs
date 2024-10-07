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
        private IConnectionPoolFactory _connectionPoolFactory;
        private bool _directConnection;
        private EndPoint _endPoint;
        private IEventSubscriber _eventSubscriber;
        private ServerApi _serverApi;
        private IServerMonitorFactory _serverMonitorFactory;
        private ServerSettings _settings;

        public ServerFactoryTests()
        {
            _clusterId = new ClusterId();
            _connectionPoolFactory = new Mock<IConnectionPoolFactory>().Object;
            _directConnection = false;
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
            Action act = () => new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, null, null);

            act.ShouldNotThrow();
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ServerFactory(_directConnection, null, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new ServerFactory(_directConnection, _settings, null, _serverMonitorFactory, _eventSubscriber, _serverApi, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_heartbeatConnectionFactory_is_null()
        {
            Action act = () => new ServerFactory(_directConnection, _settings, _connectionPoolFactory, null, _eventSubscriber, _serverApi, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, null, _serverApi, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_clusterId_is_null()
        {
            var subject = new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi, null);
            var clusterClock = new Mock<IClusterClock>().Object;

            Action act = () => subject.CreateServer(ClusterType.Unknown, null, clusterClock, _endPoint);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_clusterClock_is_null()
        {
            var subject = new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi, null);
            var clusterId = new ClusterId();

            Action act = () => subject.CreateServer(ClusterType.Unknown, clusterId, null, _endPoint);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void CreateServer_should_throw_if_endPoint_is_null()
        {
            var subject = new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi, null);
            var clusterClock = new Mock<IClusterClock>().Object;

            Action act = () => subject.CreateServer(ClusterType.Unknown, _clusterId, clusterClock, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [InlineData(ClusterType.LoadBalanced, typeof(LoadBalancedServer))]
        [InlineData(ClusterType.Unknown, typeof(DefaultServer))]
        public void CreateServer_should_return_correct_Server(ClusterType clusterType, Type expectedServerType)
        {
            var subject = new ServerFactory(_directConnection, _settings, _connectionPoolFactory, _serverMonitorFactory, _eventSubscriber, _serverApi, null);
            var clusterClock = new Mock<IClusterClock>().Object;
            

            var result = subject.CreateServer(clusterType, _clusterId, clusterClock, _endPoint);

            result.Should().NotBeNull();
            result.Should().BeOfType(expectedServerType);
        }
    }
}
