/* Copyright 2021-present MongoDB Inc.
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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Servers
{
    public class LoadBalancedTests : LoggableTestClass
    {
        private IClusterClock _clusterClock;
        private ClusterId _clusterId;
        private ConnectionId _connectionId;
        private Mock<IConnectionPool> _mockConnectionPool;
        private Mock<IConnectionPoolFactory> _mockConnectionPoolFactory;
        private EndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private EventLogger<LogCategories.SDAM> _eventLogger;
        private ServerApi _serverApi;
        private ServerSettings _settings;
        private LoadBalancedServer _subject;

        public LoadBalancedTests(ITestOutputHelper output) : base(output)
        {
            _clusterId = new ClusterId();
            _endPoint = new DnsEndPoint("localhost", 27017);

            _clusterClock = new Mock<IClusterClock>().Object;
            _mockConnectionPool = new Mock<IConnectionPool>();
            _mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<OperationContext>())).Returns(new Mock<IConnectionHandle>().Object);
            _mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<OperationContext>())).Returns(Task.FromResult(new Mock<IConnectionHandle>().Object));
            _mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            _mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint, It.IsAny<IConnectionExceptionHandler>()))
                .Returns(_mockConnectionPool.Object);

            _capturedEvents = new EventCapturer();
            _eventLogger = _capturedEvents.ToEventLogger<LogCategories.SDAM>();
            _serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, _mockConnectionPoolFactory.Object, _serverApi, _eventLogger);
            _connectionId = new ConnectionId(_subject.ServerId);
        }

        [Fact]
        public void Constructor_should_not_throw_when_serverApi_is_null()
        {
            _ = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, _mockConnectionPoolFactory.Object, serverApi: null, _capturedEvents.ToEventLogger<LogCategories.SDAM>());
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: null, _endPoint, _mockConnectionPoolFactory.Object, _serverApi, _eventLogger));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(clusterId: null, _clusterClock, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, _serverApi, _eventLogger));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterClock_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, clusterClock: null, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, _serverApi, _eventLogger));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, endPoint: null, _mockConnectionPoolFactory.Object, _serverApi, _eventLogger));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, _endPoint, connectionPoolFactory: null, _serverApi, _eventLogger));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventLogger_is_null()
        {
            var exception = Record.Exception(() => new LoadBalancedServer(_clusterId, _clusterClock, serverSettings: _settings, _endPoint, _mockConnectionPoolFactory.Object, _serverApi, eventLogger: null));

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
        public async Task GetConnection_should_clear_connection_pool_when_opening_connection_throws_MongoAuthenticationException(
            [Values(false, true)] bool async)
        {
            var connectionId = new ConnectionId(new ServerId(_clusterId, _endPoint));
            var mockConnectionHandle = new Mock<IConnectionHandle>();
            var mockConnectionExceptionHandler = new Mock<IConnectionExceptionHandler>();

            LoadBalancedServer server = null;

            var mockConnectionPool = new Mock<IConnectionPool>();
            var authenticationException = new MongoAuthenticationException(connectionId, "Invalid login.") { ServiceId = ObjectId.GenerateNewId() };
            mockConnectionPool
                .Setup(p => p.AcquireConnection(It.IsAny<OperationContext>()))
                .Callback(() => server.HandleExceptionOnOpen(authenticationException))
                .Throws(authenticationException);
            mockConnectionPool
                .Setup(p => p.AcquireConnectionAsync(It.IsAny<OperationContext>()))
                .Callback(() => server.HandleExceptionOnOpen(authenticationException))
                .Throws(authenticationException);
            mockConnectionPool.Setup(p => p.Clear(It.IsAny<ObjectId>()));

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint, It.IsAny<IConnectionExceptionHandler>()))
                .Returns(mockConnectionPool.Object);

            server = new LoadBalancedServer(
                _clusterId,
                _clusterClock,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _serverApi,
                _eventLogger);
            server.Initialize();

            var exception = async ?
                await Record.ExceptionAsync(() => server.GetConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => server.GetConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<MongoAuthenticationException>();
            mockConnectionPool.Verify(p => p.Clear(It.IsAny<ObjectId>()), Times.Once());
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_get_a_connection([Values(false, true)] bool async)
        {
            _subject.Initialize();

            var channel = async ?
                await _subject.GetConnectionAsync(OperationContext.NoTimeout) :
                _subject.GetConnection(OperationContext.NoTimeout);

            channel.Should().NotBeNull();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_not_increase_operations_count_on_exception(
            [Values(false, true)] bool async,
            [Values(false, true)] bool connectionOpenException)
        {
            IClusterableServer server = SetupServer(connectionOpenException, !connectionOpenException);

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.GetConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.GetConnection(OperationContext.NoTimeout));

            exception.Should().NotBeNull();
            server.OutstandingOperationsCount.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_set_operations_count_correctly(
            [Values(false, true)] bool async,
            [Values(0, 1, 2, 10)] int operationsCount)
        {
            IClusterableServer server = SetupServer(false, false);

            var connections = new List<IConnectionHandle>();
            for (int i = 0; i < operationsCount; i++)
            {
                var connection = async ?
                    await server.GetConnectionAsync(OperationContext.NoTimeout) :
                    server.GetConnection(OperationContext.NoTimeout);
                connections.Add(connection);
            }

            server.OutstandingOperationsCount.Should().Be(operationsCount);

            foreach (var connection in connections)
            {
                server.ReturnConnection(connection);
                server.OutstandingOperationsCount.Should().Be(--operationsCount);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_throw_when_not_initialized(
            [Values(false, true)] bool async)
        {
            var exception = async ?
                await Record.ExceptionAsync(() => _subject.GetConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.GetConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_throw_when_disposed([Values(false, true)] bool async)
        {
            _subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.GetConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.GetConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task GetConnection_should_not_update_topology_and_clear_connection_pool_on_MongoConnectionException(
            [Values("TimedOutSocketException", "NetworkUnreachableSocketException")] string errorType,
            [Values(false, true)] bool async)
        {
            var serverId = new ServerId(_clusterId, _endPoint);
            var connectionId = new ConnectionId(serverId);
            var innerMostException = CoreExceptionHelper.CreateException(errorType);
            var mockConnectionExceptionHandler = new Mock<IConnectionExceptionHandler>();

            var openConnectionException = new MongoConnectionException(connectionId, "Oops", new IOException("Cry", innerMostException));
            var mockConnection = new Mock<IConnectionHandle>();
            mockConnection.Setup(c => c.ConnectionId).Returns(connectionId);
            mockConnection.Setup(c => c.Open(It.IsAny<OperationContext>())).Throws(openConnectionException);
            mockConnection.Setup(c => c.OpenAsync(It.IsAny<OperationContext>())).ThrowsAsync(openConnectionException);

            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory.Setup(cf => cf.CreateConnection(serverId, _endPoint)).Returns(mockConnection.Object);
            connectionFactory.Setup(cf => cf.ConnectionSettings).Returns(new ConnectionSettings());

            var connectionPoolSettings = new ConnectionPoolSettings();
            var connectionPool = new ExclusiveConnectionPool(serverId, _endPoint, connectionPoolSettings, connectionFactory.Object, mockConnectionExceptionHandler.Object, CreateLogger<LogCategories.Connection>().ToEventLogger(null));

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint, It.IsAny<IConnectionExceptionHandler>()))
                .Returns(connectionPool);

            var subject = new LoadBalancedServer(_clusterId, _clusterClock, _settings, _endPoint, mockConnectionPoolFactory.Object, _serverApi, _eventLogger);
            subject.Initialize();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.GetConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.GetConnection(OperationContext.NoTimeout));

            exception.Should().Be(openConnectionException);
            subject.Description.Type.Should().Be(ServerType.LoadBalanced);
            subject.Description.ReasonChanged.Should().Be("Initialized");
            subject.Description.State.Should().Be(ServerState.Connected);

            _mockConnectionPool.Verify(c => c.Clear(It.IsAny<bool>()), Times.Never);
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
            _mockConnectionPool.Verify(p => p.Clear(It.IsAny<bool>()), Times.Never);
            _mockConnectionPool.Verify(p => p.Clear(It.IsAny<ObjectId>()), Times.Never);
        }

        [Fact]
        public void InitializeSubClass_should_initialize_ConnectionPool_before_cluster_updating()
        {
            bool setReadyHasBeenCalled = false;
            _subject.DescriptionChanged += (s, args) =>
            {
                _mockConnectionPool.Verify(c => c.SetReady(), Times.Once());
                setReadyHasBeenCalled = true; // can be called only if the above check passed
            };
            _subject.Initialize();
            setReadyHasBeenCalled.Should().BeTrue();
        }

        // private methods
        private Server SetupServer(bool exceptionOnConnectionOpen, bool exceptionOnConnectionAcquire)
        {
            var connectionId = new ConnectionId(new ServerId(_clusterId, _endPoint));
            var mockConnectionHandle = new Mock<IConnectionHandle>();
            var mockConnectionExceptionHandler = new Mock<IConnectionExceptionHandler>();

            mockConnectionHandle
                .Setup(c => c.Fork())
                .Returns(mockConnectionHandle.Object);

            var mockConnectionPool = new Mock<IConnectionPool>();
            if (exceptionOnConnectionAcquire)
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<OperationContext>()))
                    .Throws(new TimeoutException("Timeout"));
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<OperationContext>()))
                    .Throws(new TimeoutException("Timeout"));
                mockConnectionPool.Setup(p => p.Clear(It.IsAny<bool>()));
            }
            else if (exceptionOnConnectionOpen)
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<OperationContext>()))
                    .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<OperationContext>()))
                    .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
                mockConnectionPool.Setup(p => p.Clear(It.IsAny<bool>()));
            }
            else
            {
                mockConnectionPool
                    .Setup(p => p.AcquireConnection(It.IsAny<OperationContext>()))
                    .Returns(mockConnectionHandle.Object);
                mockConnectionPool
                    .Setup(p => p.AcquireConnectionAsync(It.IsAny<OperationContext>()))
                    .Returns(Task.FromResult(mockConnectionHandle.Object));
                mockConnectionPool.Setup(p => p.Clear(It.IsAny<bool>()));
            }

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint, It.IsAny<IConnectionExceptionHandler>()))
                .Returns(mockConnectionPool.Object);

            var server = new LoadBalancedServer(
                _clusterId,
                _clusterClock,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _serverApi,
                _eventLogger);
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
