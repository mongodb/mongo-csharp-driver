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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerTests
    {
        private IClusterClock _clusterClock;
        private ClusterId _clusterId;
#pragma warning disable CS0618 // Type or member is obsolete
        private ClusterConnectionMode _clusterConnectionMode;
        private ConnectionId _connectionId;
        private ConnectionModeSwitch _connectionModeSwitch;
#pragma warning restore CS0618 // Type or member is obsolete
        private bool? _directConnection;
        private Mock<IConnectionPool> _mockConnectionPool;
        private Mock<IConnectionPoolFactory> _mockConnectionPoolFactory;
        private EndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private Mock<IServerMonitor> _mockServerMonitor;
        private Mock<IServerMonitorFactory> _mockServerMonitorFactory;
        private ServerApi _serverApi;
        private ServerSettings _settings;
        private DefaultServer _subject;

        public ServerTests()
        {
            _clusterId = new ClusterId();
            _endPoint = new DnsEndPoint("localhost", 27017);

            _clusterClock = new Mock<IClusterClock>().Object;
#pragma warning disable CS0618 // Type or member is obsolete
            _clusterConnectionMode = ClusterConnectionMode.Standalone;
            _connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
            _directConnection = null;
            _mockConnectionPool = new Mock<IConnectionPool>();
            _mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>())).Returns(new Mock<IConnectionHandle>().Object);
            _mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Mock<IConnectionHandle>().Object));
            _mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            _mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(_mockConnectionPool.Object);

            _mockServerMonitor = new Mock<IServerMonitor>();
            _mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            _mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(_mockServerMonitor.Object);

            _capturedEvents = new EventCapturer();
            _serverApi = new ServerApi(ServerApiVersion.V1, true, true);
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);
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
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, null);

            act.ShouldNotThrow();
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, null, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            Action act = () => new DefaultServer(null, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterClock_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, null, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, null, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, null, _mockServerMonitorFactory.Object, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_serverMonitorFactory_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, null, _capturedEvents, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, null, _serverApi);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_should_dispose_the_server()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Dispose();
            _mockConnectionPool.Verify(p => p.Dispose(), Times.Once);
            _mockServerMonitor.Verify(m => m.Dispose(), Times.Once);

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
            mockConnectionPool
                .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
            mockConnectionPool
                .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
            mockConnectionPool.Setup(p => p.Clear());

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);

            var server = new DefaultServer(
                _clusterId,
                _clusterClock,
                _clusterConnectionMode,
                _connectionModeSwitch,
                _directConnection,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _mockServerMonitorFactory.Object,
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
            mockConnectionPool.Verify(p => p.Clear(), Times.Once());
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_get_a_connection(
            [Values(false, true)]
            bool async)
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

            _ = Record.Exception(() =>
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
            Action act;
            if (async)
            {
                act = () => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_throw_when_disposed(
            [Values(false, true)]
            bool async)
        {
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void GetChannel_should_update_topology_and_clear_connection_pool_on_network_error_or_timeout(
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
            var mockMonitorServerDescription = new ServerDescription(serverId, _endPoint);
            var mockServerMonitor = new Mock<IServerMonitor>();
            mockServerMonitor.SetupGet(m => m.Description).Returns(mockMonitorServerDescription);
            mockServerMonitor.SetupGet(m => m.Lock).Returns(new object());
            var mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(mockServerMonitor.Object);

            var subject = new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, mockConnectionPoolFactory.Object, mockServerMonitorFactory.Object, _capturedEvents, _serverApi);
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
            subject.Description.Type.Should().Be(ServerType.Unknown);
            subject.Description.ReasonChanged.Should().Contain("ChannelException during handshake");
        }

        [Theory]
        [InlineData(nameof(MongoConnectionException), true)]
        [InlineData("MongoConnectionExceptionWithSocketTimeout", false)]
        public void HandleChannelException_should_update_topology_as_expected_on_network_error_or_timeout(
            string errorType, bool shouldUpdateTopology)
        {
            var serverId = new ServerId(_clusterId, _endPoint);
            var connectionId = new ConnectionId(serverId);
            Exception innerMostException;
            switch (errorType)
            {
                case "MongoConnectionExceptionWithSocketTimeout":
                    innerMostException = new SocketException((int)SocketError.TimedOut);
                    break;
                case nameof(MongoConnectionException):
                    innerMostException = new SocketException((int)SocketError.NetworkUnreachable);
                    break;
                default: throw new ArgumentException("Unknown error type.");
            }

            var operationUsingChannelException = new MongoConnectionException(connectionId, "Oops", new IOException("Cry", innerMostException));
            var mockConnection = new Mock<IConnectionHandle>();
            var isMasterResult = new IsMasterResult(new BsonDocument { { "compressors", new BsonArray() } });
            // the server version doesn't matter when we're not testing MongoNotPrimaryExceptions, but is needed when
            // Server calls ShouldClearConnectionPoolForException
            var buildInfoResult = new BuildInfoResult(new BsonDocument { { "version", "4.4.0" } });
            mockConnection.SetupGet(c => c.Description)
                .Returns(new ConnectionDescription(new ConnectionId(serverId, 0), isMasterResult, buildInfoResult));
            var mockConnectionPool = new Mock<IConnectionPool>();
            mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>())).Returns(mockConnection.Object);
            mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockConnection.Object);
            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);
            var mockMonitorServerInitialDescription = new ServerDescription(serverId, _endPoint).With(reasonChanged: "Initial D", type: ServerType.Unknown);
            var mockServerMonitor = new Mock<IServerMonitor>();
            mockServerMonitor.SetupGet(m => m.Description).Returns(mockMonitorServerInitialDescription);
            mockServerMonitor.SetupGet(m => m.Lock).Returns(new object());
            var mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(mockServerMonitor.Object);
            var subject = new DefaultServer(_clusterId, _clusterClock, _clusterConnectionMode, _connectionModeSwitch, _directConnection, _settings, _endPoint, mockConnectionPoolFactory.Object, mockServerMonitorFactory.Object, _capturedEvents, _serverApi);
            subject.Initialize();
            var heartbeatDescription = mockMonitorServerInitialDescription.With(reasonChanged: "Heartbeat", type: ServerType.Standalone);
            mockServerMonitor.Setup(m => m.Description).Returns(heartbeatDescription);
            mockServerMonitor.Raise(
                m => m.DescriptionChanged += null,
                new ServerDescriptionChangedEventArgs(mockMonitorServerInitialDescription, heartbeatDescription));
            subject.Description.Should().Be(heartbeatDescription);

            subject.HandleChannelException(mockConnection.Object, operationUsingChannelException);

            if (shouldUpdateTopology)
            {
                subject.Description.Type.Should().Be(ServerType.Unknown);
                subject.Description.ReasonChanged.Should().Contain("ChannelException");
            }
            else
            {
                subject.Description.Should().Be(heartbeatDescription);
            }
        }

        [Fact]
        public void Initialize_should_initialize_the_server()
        {
            _subject.Initialize();
            _mockConnectionPool.Verify(p => p.Initialize(), Times.Once);
            _mockServerMonitor.Verify(m => m.Initialize(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ServerOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Invalidate_should_clear_the_connection_pool()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Invalidate("Test", responseTopologyDescription: null);
            _mockConnectionPool.Verify(p => p.Clear(), Times.Once);
        }

        [Fact]
        public void RequestHeartbeat_should_tell_the_monitor_to_request_a_heartbeat()
        {
            _subject.Initialize();
            _capturedEvents.Clear();
            _subject.RequestHeartbeat();
            _mockServerMonitor.Verify(m => m.RequestHeartbeat(), Times.Once);

            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void A_description_changed_event_with_a_heartbeat_exception_should_clear_the_connection_pool()
        {
            _subject.Initialize();
            var description = new ServerDescription(_subject.ServerId, _subject.EndPoint)
                .With(heartbeatException: new Exception("ughhh"));
            _mockServerMonitor.Raise(m => m.DescriptionChanged += null, new ServerDescriptionChangedEventArgs(description, description));

            _mockConnectionPool.Verify(p => p.Clear(), Times.Once);
        }

        [Theory]
        [InlineData(null, false, null)]
        [InlineData((ServerErrorCode)1, false, null)]
        [InlineData(ServerErrorCode.LegacyNotPrimary, true, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotWritablePrimary, true, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotPrimaryNoSecondaryOk, true, typeof(MongoNotPrimaryException))]
        [InlineData(ServerErrorCode.NotPrimaryOrSecondary, false, typeof(MongoNodeIsRecoveringException))]
        internal void IsNotWritablePrimary_should_return_expected_result_for_code(ServerErrorCode? code, bool expectedResult, Type expectedExceptionType)
        {
            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "code", code, code != null }
            };

            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            if (expectedExceptionType != null)
            {
                exception.Should().BeOfType(expectedExceptionType);
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        internal void IsNotWritablePrimary_should_return_expected_result_for_code_with_conflicting_message()
        {
            var code = (ServerErrorCode)1;
            var message = "not master";

            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "code", code }, // code takes precedence over errmsg
                { "errmsg", message }
            };
            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(null, false, null)]
        [InlineData("abc", false, null)]
        [InlineData("not master", true, typeof(MongoNotPrimaryException))]
        [InlineData("not master or secondary", false, typeof(MongoNodeIsRecoveringException))]
        internal void IsNotWritablePrimary_should_return_expected_result_for_message(string message, bool expectedResult, Type expectedException)
        {
            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "errmsg", message, message != null }
            };

            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            if (expectedException != null)
            {
                exception.Should().BeOfType(expectedException);
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData((ServerErrorCode)1, false)]
        [InlineData(ServerErrorCode.NotWritablePrimary, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        internal void IsStateChangeException_should_return_expected_result(ServerErrorCode? code, bool expectedResult)
        {
            _subject.Initialize();

            var mappedException = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), new BsonDocument("code", code), "dummy");

            var result = _subject.IsStateChangeException(mappedException);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData((ServerErrorCode)1, false)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, true)]
        [InlineData(ServerErrorCode.NotPrimaryOrSecondary, true)]
        [InlineData(ServerErrorCode.PrimarySteppedDown, true)]
        [InlineData(ServerErrorCode.ShutdownInProgress, true)]
        internal void IsRecovering_should_return_expected_result_for_code(ServerErrorCode? code, bool expectedResult)
        {
            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "code", code }
            };

            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            if (expectedResult)
            {
                exception.Should().BeOfType<MongoNodeIsRecoveringException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Fact]
        internal void IsRecovering_should_return_expected_result_for_code_with_conflicting_message()
        {
            var code = (ServerErrorCode)1;
            var message = "not master or secondary";

            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "code", code }, // code takes precedence over errmsg
                { "errmsg", message }
            };
            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            exception.Should().BeNull();
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("abc", false)]
        [InlineData("node is recovering", true)]
        [InlineData("not master or secondary", true)]
        internal void IsRecovering_should_return_expected_result_for_message(string message, bool expectedResult)
        {
            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "errmsg", message, message != null }
            };
            var exception = ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(_connectionId, new BsonDocument(), commandResult, "errmsg");

            if (expectedResult)
            {
                exception.Should().BeOfType<MongoNodeIsRecoveringException>();
            }
            else
            {
                exception.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(nameof(EndOfStreamException), true)]
        [InlineData(nameof(Exception), false)]
        [InlineData(nameof(IOException), true)]
        [InlineData(nameof(MongoConnectionException), true)]
        [InlineData(nameof(MongoNodeIsRecoveringException), true)]
        [InlineData(nameof(MongoNotPrimaryException), true)]
        [InlineData(nameof(SocketException), true)]
        [InlineData(nameof(TimeoutException), false)]
        [InlineData("MongoConnectionExceptionWithSocketTimeout", false)]
        [InlineData(nameof(MongoExecutionTimeoutException), false)]
        internal void ShouldInvalidateServer_should_return_expected_result_for_exceptionType(string exceptionTypeName, bool expectedResult)
        {
            _subject.Initialize();
            Exception exception;
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId);
            var command = new BsonDocument("command", 1);
            var notWritablePrimaryResult = new BsonDocument { { "code", ServerErrorCode.NotWritablePrimary } };
            var nodeIsRecoveringResult = new BsonDocument("code", ServerErrorCode.InterruptedAtShutdown);

            switch (exceptionTypeName)
            {
                case nameof(EndOfStreamException): exception = new EndOfStreamException(); break;
                case nameof(Exception): exception = new Exception(); break;
                case nameof(IOException): exception = new IOException(); break;
                case nameof(MongoConnectionException): exception = new MongoConnectionException(connectionId, "message"); break;
                case nameof(MongoNodeIsRecoveringException): exception = new MongoNodeIsRecoveringException(connectionId, command, notWritablePrimaryResult); break;
                case nameof(MongoNotPrimaryException): exception = new MongoNotPrimaryException(connectionId, command, nodeIsRecoveringResult); break;
                case nameof(SocketException): exception = new SocketException(); break;
                case "MongoConnectionExceptionWithSocketTimeout":
                    var innermostException = new SocketException((int)SocketError.TimedOut);
                    var innerException = new IOException("Execute Order 66", innermostException);
                    exception = new MongoConnectionException(connectionId, "Yes, Lord Sidious", innerException);
                    break;
                case nameof(TimeoutException): exception = new TimeoutException(); break;
                case nameof(MongoExecutionTimeoutException): exception = new MongoExecutionTimeoutException(connectionId, "message"); break;
                default: throw new Exception($"Invalid exceptionTypeName: {exceptionTypeName}.");
            }

            var result = _subject.ShouldInvalidateServer(Mock.Of<IConnection>(), exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData((ServerErrorCode)1, null, false)]
        [InlineData(ServerErrorCode.NotWritablePrimary, null, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, null, true)]
        [InlineData(null, "abc", false)]
        [InlineData(null, "not master", true)]
        [InlineData(null, "not master or secondary", true)]
        [InlineData(null, "node is recovering", true)]
        [InlineData((ServerErrorCode)1, "not master", false)]
        internal void ShouldInvalidateServer_should_return_expected_result_for_MongoCommandException(ServerErrorCode? code, string message, bool expectedResult)
        {
            _subject.Initialize();
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId);
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument
            {
                { "ok", 0 },
                { "code", () => (int)code.Value, code.HasValue },
                { "errmsg", message, message != null }
            };
            var exception =
                ExceptionMapper.MapNotPrimaryOrNodeIsRecovering(connectionId, command, commandResult, "errmsg") ??
                new MongoCommandException(connectionId, "message", command, commandResult); // this needs for cases when we have just a random exception

            var result = _subject.ShouldInvalidateServer(Mock.Of<IConnection>(), exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData((ServerErrorCode)1, null, false)]
        [InlineData(ServerErrorCode.NotWritablePrimary, null, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, null, true)]
        [InlineData(null, "abc", false)]
        [InlineData(null, "not master", true)]
        [InlineData(null, "not master or secondary", true)]
        [InlineData(null, "node is recovering", true)]
        [InlineData((ServerErrorCode)1, "not master", false)]
        internal void ShouldInvalidateServer_should_return_expected_result_for_MongoWriteConcernException(ServerErrorCode? code, string message, bool expectedResult)
        {
            _subject.Initialize();
            var clusterId = new ClusterId(1);
            var serverId = new ServerId(clusterId, new DnsEndPoint("localhost", 27017));
            var connectionId = new ConnectionId(serverId);
            var command = new BsonDocument("command", 1);
            var commandResult = new BsonDocument
            {
                { "ok", 1 },
                { "writeConcernError", new BsonDocument
                    {
                        { "code", () => (int)code.Value, code.HasValue },
                        { "errmsg", message, message != null }
                    }
                }
            };
            var writeConcernResult = new WriteConcernResult(commandResult);
            var exception = new MongoWriteConcernException(connectionId, "message", writeConcernResult);

            var result = _subject.ShouldInvalidateServer(Mock.Of<IConnection>(), exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
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

            var server = new DefaultServer(
                _clusterId,
                _clusterClock,
                _clusterConnectionMode,
                _connectionModeSwitch,
                _directConnection,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _mockServerMonitorFactory.Object,
                _capturedEvents,
                _serverApi);
            server.Initialize();

            return server;
        }
    }

    public class ServerChannelTests
    {
        [SkippableTheory]
        [InlineData(1, 2, 2)]
        [InlineData(2, 1, 2)]
        public void Command_should_send_the_greater_of_the_session_and_cluster_cluster_times(long sessionTimestamp, long clusterTimestamp, long expectedTimestamp)
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.6").ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);
            var sessionClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(sessionTimestamp));
            var clusterClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(clusterTimestamp));
            var expectedClusterTime = new BsonDocument("clusterTime", new BsonTimestamp(expectedTimestamp));

            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "ping");
            using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
            using (var session = cluster.StartSession())
            {
                var cancellationToken = CancellationToken.None;
                var server = (Server)cluster.SelectServer(WritableServerSelector.Instance, cancellationToken);
                using (var channel = server.GetChannel(cancellationToken))
                {
                    session.AdvanceClusterTime(sessionClusterTime);
                    server.ClusterClock.AdvanceClusterTime(clusterClusterTime);

                    var command = BsonDocument.Parse("{ ping : 1 }");
                    try
                    {
                        channel.Command<BsonDocument>(
                            session,
                            ReadPreference.Primary,
                            DatabaseNamespace.Admin,
                            command,
                            null, // payloads
                            NoOpElementNameValidator.Instance,
                            null, // additionalOptions
                            null, // postWriteAction
                            CommandResponseHandling.Return,
                            BsonDocumentSerializer.Instance,
                            new MessageEncoderSettings(),
                            cancellationToken);
                    }
                    catch (MongoCommandException ex)
                    {
                        // we're expecting the command to fail because the $clusterTime we sent is not properly signed
                        // the point of this test is just to assert that the driver sent the higher of the session and cluster clusterTimes
                        ex.Message.Should().Contain("Missing expected field \"signature\"");
                    }
                }
            }

            var commandStartedEvent = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
            var actualCommand = commandStartedEvent.Command;
            var actualClusterTime = actualCommand["$clusterTime"].AsBsonDocument;
            actualClusterTime.Should().Be(expectedClusterTime);
        }

        [SkippableFact]
        public void Command_should_update_the_session_and_cluster_cluster_times()
        {
            RequireServer.Check().VersionGreaterThanOrEqualTo("3.6").ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            var eventCapturer = new EventCapturer().Capture<CommandSucceededEvent>(e => e.CommandName == "ping");
            using (var cluster = CoreTestConfiguration.CreateCluster(b => b.Subscribe(eventCapturer)))
            using (var session = cluster.StartSession())
            {
                var cancellationToken = CancellationToken.None;
                var server = (Server)cluster.SelectServer(WritableServerSelector.Instance, cancellationToken);
                using (var channel = server.GetChannel(cancellationToken))
                {
                    var command = BsonDocument.Parse("{ ping : 1 }");
                    channel.Command<BsonDocument>(
                        session,
                        ReadPreference.Primary,
                        DatabaseNamespace.Admin,
                        command,
                        null, // payloads
                        NoOpElementNameValidator.Instance,
                        null, // additionalOptions
                        null, // postWriteAction
                        CommandResponseHandling.Return,
                        BsonDocumentSerializer.Instance,
                        new MessageEncoderSettings(),
                        cancellationToken);
                }

                var commandSucceededEvent = eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>().Subject;
                var actualReply = commandSucceededEvent.Reply;
                var actualClusterTime = actualReply["$clusterTime"].AsBsonDocument;
                session.ClusterTime.Should().Be(actualClusterTime);
                server.ClusterClock.ClusterTime.Should().Be(actualClusterTime);
            }
        }

        [SkippableTheory]
        [ParameterAttributeData]
        public void Command_should_use_serverApi([Values(false, true)] bool async)
        {
            RequireServer.Check().Supports(Feature.CommandMessage);

            var serverApi = new ServerApi(ServerApiVersion.V1);
            var eventCapturer = new EventCapturer().Capture<CommandStartedEvent>(e => e.CommandName == "ping");
            var builder = CoreTestConfiguration
                .ConfigureCluster(new ClusterBuilder())
                .Subscribe(eventCapturer)
                .ConfigureCluster(x => x.With(serverApi: serverApi));

            using (var cluster = CoreTestConfiguration.CreateCluster(builder))
            using (var session = cluster.StartSession())
            {
                var cancellationToken = CancellationToken.None;
                var server = (Server)cluster.SelectServer(WritableServerSelector.Instance, cancellationToken);
                using (var channel = server.GetChannel(cancellationToken))
                {
                    var command = BsonDocument.Parse("{ ping : 1 }");
                    if (async)
                    {
                        channel
                            .CommandAsync(
                                session,
                                ReadPreference.Primary,
                                DatabaseNamespace.Admin,
                                command,
                                null, // payloads
                                NoOpElementNameValidator.Instance,
                                null, // additionalOptions
                                null, // postWriteAction
                                CommandResponseHandling.Return,
                                BsonDocumentSerializer.Instance,
                                new MessageEncoderSettings(),
                                cancellationToken)
                            .GetAwaiter()
                            .GetResult();
                    }
                    else
                    {
                        channel.Command(
                            session,
                            ReadPreference.Primary,
                            DatabaseNamespace.Admin,
                            command,
                            null, // payloads
                            NoOpElementNameValidator.Instance,
                            null, // additionalOptions
                            null, // postWriteAction
                            CommandResponseHandling.Return,
                            BsonDocumentSerializer.Instance,
                            new MessageEncoderSettings(),
                            cancellationToken);
                    }
                }
            }

            var commandStartedEvent = eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject;
            commandStartedEvent.Command["apiVersion"].AsString.Should().Be("1");
        }
    }

    internal static class ServerReflector
    {
        public static void HandleChannelException(this DefaultServer server, IConnection connection, Exception ex)
        {
            Reflector.Invoke(server, nameof(HandleChannelException), connection, ex, checkBaseClass: true);
        }

        public static bool IsStateChangeException(this DefaultServer server, Exception exception)
        {
            return (bool)Reflector.Invoke(server, nameof(IsStateChangeException), exception);
        }

        public static bool ShouldInvalidateServer(this DefaultServer server,
            IConnection connection,
            Exception exception,
            ServerDescription description,
            out TopologyVersion responseTopologyVersion)
        {
            return (bool)Reflector.Invoke(server, nameof(ShouldInvalidateServer), connection, exception, description, out responseTopologyVersion);
        }
    }
}
