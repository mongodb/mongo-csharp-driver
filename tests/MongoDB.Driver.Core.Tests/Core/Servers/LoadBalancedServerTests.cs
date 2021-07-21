﻿/* Copyright 2021-present MongoDB Inc.
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class LoadBalancedTests
    {
        private IClusterClock _clusterClock;
        private ClusterId _clusterId;
        private ConnectionId _connectionId;
        private Mock<IConnectionPool> _mockConnectionPool;
        private Mock<IConnectionPoolFactory> _mockConnectionPoolFactory;
        private EndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private ServerApi _serverApi;
        private ServerSettings _settings;
        private LoadBalancedServer _subject;

        public LoadBalancedTests()
        {
            _clusterId = new ClusterId();
            _endPoint = new DnsEndPoint("localhost", 27017);

            _clusterClock = new Mock<IClusterClock>().Object;
            _mockConnectionPool = new Mock<IConnectionPool>();
            _mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>())).Returns(new Mock<IConnectionHandle>().Object);
            _mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Mock<IConnectionHandle>().Object));
            _mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            _mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(_mockConnectionPool.Object);

            _capturedEvents = new EventCapturer();
            _serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, _mockConnectionPoolFactory.Object, _capturedEvents, _serverApi);
            _connectionId = new ConnectionId(_subject.ServerId);
        }

        [Theory]
        [ParameterAttributeData]
        public void ChannelFork_should_not_affect_operations_count([Values(false, true)] bool async)
        {
            IClusterableServer server = SetupServer(false, false);

            var channel = async ?
                server.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult() :
                server.GetChannel(CancellationToken.None);

            server.OutstandingOperationsCount.Should().Be(1);

            var forkedChannel = channel.Fork();
            server.OutstandingOperationsCount.Should().Be(1);

            forkedChannel.Dispose();
            server.OutstandingOperationsCount.Should().Be(1);

            channel.Dispose();
            server.OutstandingOperationsCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_should_not_throw_when_serverApi_is_null()
        {
            _ = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, _mockConnectionPoolFactory.Object, _capturedEvents, serverApi: null);
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: null, _endPoint, _mockConnectionPoolFactory.Object, _capturedEvents, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(clusterId: null, _clusterClock, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, _capturedEvents, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterClock_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, clusterClock: null, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, _capturedEvents, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, endPoint: null, _mockConnectionPoolFactory.Object, _capturedEvents, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, _endPoint, connectionPoolFactory: null, _capturedEvents, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, eventSubscriber: null, _serverApi));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_should_dispose_the_server()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Dispose();
            _mockConnectionPool.Verify(p => p.Dispose(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ServerClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_clear_connection_pool_when_opening_connection_throws_MongoAuthenticationException(
            [Values(false, true)] bool async)
        {
            var connectionId = new ConnectionId(new ServerId(_clusterId, _endPoint));
            var mockConnectionHandle = new Mock<IConnectionHandle>();

            var mockConnectionPool = new Mock<IConnectionPool>();
            var authenticationException = new MongoAuthenticationException(connectionId, "Invalid login.") { ServiceId = ObjectId.GenerateNewId() };
            mockConnectionPool
                .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                .Throws(authenticationException);
            mockConnectionPool
                .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                .Throws(authenticationException);
            mockConnectionPool.Setup(p => p.Clear(It.IsAny<ObjectId>()));

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);

            var server = new LoadBalancedServer(
                _clusterId,
                _clusterClock,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _capturedEvents,
                _serverApi);
            server.Initialize();

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    server.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    server.GetChannel(CancellationToken.None);
                }
            });

            exception.Should().BeOfType<MongoAuthenticationException>();
            mockConnectionPool.Verify(p => p.Clear(It.IsAny<ObjectId>()), Times.Once());
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_get_a_connection([Values(false, true)] bool async)
        {
            _subject.Initialize();

            IChannelHandle channel;
            if (async)
            {
                channel = _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                channel = _subject.GetChannel(CancellationToken.None);
            }

            channel.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_not_increase_operations_count_on_exception(
            [Values(false, true)] bool async,
            [Values(false, true)] bool connectionOpenException)
        {
            IClusterableServer server = SetupServer(connectionOpenException, !connectionOpenException);

            var exception = Record.Exception(() =>
            {
                if (async)
                {
                    server.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    server.GetChannel(CancellationToken.None);
                }
            });

            exception.Should().NotBeNull();
            server.OutstandingOperationsCount.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_set_operations_count_correctly(
            [Values(false, true)] bool async,
            [Values(0, 1, 2, 10)] int operationsCount)
        {
            IClusterableServer server = SetupServer(false, false);

            var channels = new List<IChannel>();
            for (int i = 0; i < operationsCount; i++)
            {
                if (async)
                {
                    channels.Add(server.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult());
                }
                else
                {
                    channels.Add(server.GetChannel(CancellationToken.None));
                }
            }

            server.OutstandingOperationsCount.Should().Be(operationsCount);

            foreach (var channel in channels)
            {
                channel.Dispose();
                server.OutstandingOperationsCount.Should().Be(--operationsCount);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_throw_when_not_initialized(
            [Values(false, true)] bool async)
        {
            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => _subject.GetChannel(CancellationToken.None));
            }

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_throw_when_disposed([Values(false, true)] bool async)
        {
            _subject.Dispose();

            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => _subject.GetChannel(CancellationToken.None));
            }

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_not_update_topology_and_clear_connection_pool_on_MongoConnectionException(
            [Values("TimedOutSocketException", "NetworkUnreachableSocketException")] string errorType,
            [Values(false, true)] bool async)
        {
            var serverId = new ServerId(_clusterId, _endPoint);
            var connectionId = new ConnectionId(serverId);
            var innerMostException = CoreExceptionHelper.CreateException(errorType);

            var openConnectionException = new MongoConnectionException(connectionId, "Oops", new IOException("Cry", innerMostException));
            var mockConnection = new Mock<IConnectionHandle>();
            mockConnection.Setup(c => c.Open(It.IsAny<CancellationToken>())).Throws(openConnectionException);
            mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).ThrowsAsync(openConnectionException);

            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(cf => cf.CreateConnection(serverId, _endPoint)).Returns(mockConnection.Object);

            var connectionPoolSettings = new ConnectionPoolSettings();
            var connectionPool = new ExclusiveConnectionPool(serverId, _endPoint, connectionPoolSettings, connectionFactory.Object, new EventAggregator());

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(connectionPool);

            var subject = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, mockConnectionPoolFactory.Object, _capturedEvents, _serverApi);
            subject.Initialize();

            IChannelHandle channel = null;
            Exception exception;
            if (async)
            {
                exception = Record.Exception(() => channel = subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult());
            }
            else
            {
                exception = Record.Exception(() => channel = subject.GetChannel(CancellationToken.None));
            }

            channel.Should().BeNull();
            exception.Should().Be(openConnectionException);
            subject.Description.Type.Should().Be(ServerType.LoadBalanced);
            subject.Description.ReasonChanged.Should().Be("Initialized");
            subject.Description.State.Should().Be(ServerState.Connected);

            _mockConnectionPool.Verify(c => c.Clear(), Times.Never);
        }

        [Fact]
        public void Initialize_should_initialize_the_server()
        {
            _subject.Initialize();
            _mockConnectionPool.Verify(p => p.Initialize(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ServerOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Invalidate_should_not_clear_the_connection_pool()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Invalidate("Test", responseTopologyDescription: null);
            _mockConnectionPool.Verify(p => p.Clear(), Times.Never);
            _mockConnectionPool.Verify(p => p.Clear(It.IsAny<ObjectId>()), Times.Never);
        }

        // private methods
        private Server SetupServer(bool exceptionOnConnectionOpen, bool exceptionOnConnectionAcquire)
        {
            var connectionId = new ConnectionId(new ServerId(_clusterId, _endPoint));
            var mockConnectionHandle = new Mock<IConnectionHandle>();

            mockConnectionHandle
                .Setup(c => c.Fork())
                .Returns(mockConnectionHandle.Object);

            var mockConnectionPool = new Mock<IConnectionPool>();
            if (exceptionOnConnectionAcquire)
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                    .Throws(new TimeoutException("Timeout"));
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                    .Throws(new TimeoutException("Timeout"));
                mockConnectionPool.Setup(p => p.Clear());
            }
            else if (exceptionOnConnectionOpen)
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                    .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                    .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
                mockConnectionPool.Setup(p => p.Clear());
            }
            else
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                    .Returns(mockConnectionHandle.Object);
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(mockConnectionHandle.Object));
                mockConnectionPool.Setup(p => p.Clear());
            }

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);

            var server = new LoadBalancedServer(
                _clusterId,
                _clusterClock,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _capturedEvents,
                _serverApi);
            server.Initialize();

            return server;
        }
    }

    internal static class LoadBalancedServerReflector
    {
        public static void HandleChannelException(this LoadBalancedServer server, IConnection connection, Exception ex)
        {
            Reflector.Invoke(server, nameof(HandleChannelException), connection, ex, checkBaseClass: true);
        }
    }
}
