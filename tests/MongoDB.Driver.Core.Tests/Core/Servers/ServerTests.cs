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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
        private ClusterConnectionMode _clusterConnectionMode;
        private Mock<IConnectionPool> _mockConnectionPool;
        private Mock<IConnectionPoolFactory> _mockConnectionPoolFactory;
        private EndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private Mock<IServerMonitor> _mockServerMonitor;
        private Mock<IServerMonitorFactory> _mockServerMonitorFactory;
        private ServerSettings _settings;
        private Server _subject;

        public ServerTests()
        {
            _clusterId = new ClusterId();
            _endPoint = new DnsEndPoint("localhost", 27017);

            _clusterClock = new Mock<IClusterClock>().Object;
            _clusterConnectionMode = ClusterConnectionMode.Standalone;
            _mockConnectionPool = new Mock<IConnectionPool>();
            _mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>())).Returns(new Mock<IConnectionHandle>().Object);
            _mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new Mock<IConnectionHandle>().Object));
            _mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            _mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(_mockConnectionPool.Object);

            _mockServerMonitor = new Mock<IServerMonitor>();
            _mockServerMonitor.Setup(m => m.Description).Returns(new ServerDescription(new ServerId(_clusterId, _endPoint), _endPoint));
            _mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            _mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(_mockServerMonitor.Object);

            _capturedEvents = new EventCapturer();
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents);
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterClock, _clusterConnectionMode, null, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            Action act = () => new Server(null, _clusterClock, _clusterConnectionMode, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_clusterClock_is_null()
        {
            Action act = () => new Server(_clusterId, null, _clusterConnectionMode, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, null, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, null, _mockServerMonitorFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_serverMonitorFactory_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, _mockConnectionPoolFactory.Object, null, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, _mockConnectionPoolFactory.Object, _mockServerMonitorFactory.Object, null);

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
            mockConnectionHandle
                .Setup(c => c.Open(It.IsAny<CancellationToken>()))
                .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));
            mockConnectionHandle
                .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
                .Throws(new MongoAuthenticationException(connectionId, "Invalid login."));

            var mockConnectionPool = new Mock<IConnectionPool>();
            mockConnectionPool
                .Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>()))
                .Returns(mockConnectionHandle.Object);
            mockConnectionPool
                .Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(mockConnectionHandle.Object));
            mockConnectionPool.Setup(p => p.Clear());

            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);

            var server = new Server(
                _clusterId,
                _clusterClock,
                _clusterConnectionMode,
                _settings,
                _endPoint,
                mockConnectionPoolFactory.Object,
                _mockServerMonitorFactory.Object,
                _capturedEvents);
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
        public void GetChannel_should_throw_when_not_initialized(
            [Values(false, true)]
            bool async)
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
            var mockConnectionPool = new Mock<IConnectionPool>();
            mockConnectionPool.Setup(p => p.AcquireConnection(It.IsAny<CancellationToken>())).Returns(mockConnection.Object);
            mockConnectionPool.Setup(p => p.AcquireConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockConnection.Object);
            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();
            mockConnectionPoolFactory
                .Setup(f => f.CreateConnectionPool(It.IsAny<ServerId>(), _endPoint))
                .Returns(mockConnectionPool.Object);
            var mockMonitorServerDescription = new ServerDescription(serverId, _endPoint);
            var mockServerMonitor = new Mock<IServerMonitor>();
            mockServerMonitor.SetupGet(m => m.Description).Returns(mockMonitorServerDescription);
            mockServerMonitor.SetupGet(m => m.Lock).Returns(new object());
            var mockServerMonitorFactory = new Mock<IServerMonitorFactory>();
            mockServerMonitorFactory.Setup(f => f.Create(It.IsAny<ServerId>(), _endPoint)).Returns(mockServerMonitor.Object);

            var subject = new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, mockConnectionPoolFactory.Object, mockServerMonitorFactory.Object, _capturedEvents);
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
            mockConnectionPool.Verify(p => p.Clear(), Times.Once);
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
            var subject = new Server(_clusterId, _clusterClock, _clusterConnectionMode, _settings, _endPoint, mockConnectionPoolFactory.Object, mockServerMonitorFactory.Object, _capturedEvents);
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
        [InlineData((ServerErrorCode)(-1), false)]
        [InlineData(ServerErrorCode.NotMaster, true)]
        [InlineData(ServerErrorCode.NotMasterNoSlaveOk, true)]
        [InlineData(ServerErrorCode.NotMasterOrSecondary, false)]
        internal void IsNotMaster_should_return_expected_result_for_code(ServerErrorCode code, bool expectedResult)
        {
            _subject.Initialize();

            var result = _subject.IsNotMaster(code, null);

            result.Should().Be(expectedResult);
            if (result)
            {
                _subject.IsRecovering(code, null).Should().BeFalse();
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("abc", false)]
        [InlineData("not master", true)]
        [InlineData("not master or secondary", false)]
        internal void IsNotMaster_should_return_expected_result_for_message(string message, bool expectedResult)
        {
            _subject.Initialize();

            var result = _subject.IsNotMaster((ServerErrorCode)(-1), message);

            result.Should().Be(expectedResult);
            if (result)
            {
                _subject.IsRecovering((ServerErrorCode)(-1), message).Should().BeFalse();
            }
        }

        [Theory]
        [InlineData((ServerErrorCode)(-1), false)]
        [InlineData(ServerErrorCode.NotMaster, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        internal void IsStateChangeError_should_return_expected_result(ServerErrorCode code, bool expectedResult)
        {
            _subject.Initialize();

            var result = _subject.IsStateChangeError(code, null);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData((ServerErrorCode)(-1), false)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, true)]
        [InlineData(ServerErrorCode.InterruptedDueToReplStateChange, true)]
        [InlineData(ServerErrorCode.NotMasterOrSecondary, true)]
        [InlineData(ServerErrorCode.PrimarySteppedDown, true)]
        [InlineData(ServerErrorCode.ShutdownInProgress, true)]
        internal void IsRecovering_should_return_expected_result_for_code(ServerErrorCode code, bool expectedResult)
        {
            _subject.Initialize();

            var result = _subject.IsRecovering(code, null);

            result.Should().Be(expectedResult);
            if (result)
            {
                _subject.IsNotMaster(code, null).Should().BeFalse();
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("abc", false)]
        [InlineData("node is recovering", true)]
        [InlineData("not master or secondary", true)]
        internal void IsRecovering_should_return_expected_result_for_message(string message, bool expectedResult)
        {
            _subject.Initialize();

            var result = _subject.IsRecovering((ServerErrorCode)(-1), message);

            result.Should().Be(expectedResult);
            if (result)
            {
                _subject.IsNotMaster((ServerErrorCode)(-1), message).Should().BeFalse();
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
            var notMasterResult = new BsonDocument { { "code", ServerErrorCode.NotMaster } };
            var nodeIsRecoveringResult = new BsonDocument("code", ServerErrorCode.InterruptedAtShutdown);

            switch (exceptionTypeName)
            {
                case nameof(EndOfStreamException): exception = new EndOfStreamException(); break;
                case nameof(Exception): exception = new Exception(); break;
                case nameof(IOException): exception = new IOException(); break;
                case nameof(MongoConnectionException): exception = new MongoConnectionException(connectionId, "message"); break;
                case nameof(MongoNodeIsRecoveringException): exception = new MongoNodeIsRecoveringException(connectionId, command, notMasterResult); break;
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

            var result = _subject.ShouldInvalidateServer(new Mock<IConnectionHandle>().Object, exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData((ServerErrorCode)(-1), null, false)]
        [InlineData(ServerErrorCode.NotMaster, null, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, null, true)]
        [InlineData(null, "abc", false)]
        [InlineData(null, "not master", true)]
        [InlineData(null, "not master or secondary", true)]
        [InlineData(null, "node is recovering", true)]
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
            var exception = new MongoCommandException(connectionId, "message", command, commandResult);

            var result = _subject.ShouldInvalidateServer(new Mock<IConnectionHandle>().Object, exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData((ServerErrorCode)(-1), null, false)]
        [InlineData(ServerErrorCode.NotMaster, null, true)]
        [InlineData(ServerErrorCode.InterruptedAtShutdown, null, true)]
        [InlineData(null, "abc", false)]
        [InlineData(null, "not master", true)]
        [InlineData(null, "not master or secondary", true)]
        [InlineData(null, "node is recovering", true)]
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

            var result = _subject.ShouldInvalidateServer(new Mock<IConnectionHandle>().Object, exception, new ServerDescription(_subject.ServerId, _subject.EndPoint), out _);

            result.Should().Be(expectedResult);
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
    }

    internal static class ServerReflector
    {
        public static void HandleChannelException(this Server server, IConnection connection, Exception ex)
        {
            Reflector.Invoke(server, nameof(HandleChannelException), connection, ex);
        }

        public static bool IsNotMaster(this Server server, ServerErrorCode code, string message)
        {
            var methodInfo = typeof(Server).GetMethod(nameof(IsNotMaster), BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(server, new object[] { code, message });
        }

        public static bool IsStateChangeError(this Server server, ServerErrorCode code, string message)
        {
            var methodInfo = typeof(Server).GetMethod(nameof(IsStateChangeError), BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(server, new object[] { code, message });
        }

        public static bool IsRecovering(this Server server, ServerErrorCode code, string message)
        {
            var methodInfo = typeof(Server).GetMethod(nameof(IsRecovering), BindingFlags.NonPublic | BindingFlags.Instance);
            return (bool)methodInfo.Invoke(server, new object[] { code, message });
        }

        public static bool ShouldInvalidateServer(this Server server,
            IConnectionHandle connection,
            Exception exception,
            ServerDescription description,
            out TopologyVersion responseTopologyVersion)
        {
            var methodInfo = typeof(Server).GetMethod(nameof(ShouldInvalidateServer), BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { connection, exception, description, null };
            int outParameterIndex = Array.IndexOf(parameters, null);
            var shouldInvalidate = (bool)methodInfo.Invoke(server, parameters);
            responseTopologyVersion = (TopologyVersion)parameters[outParameterIndex];
            return shouldInvalidate;
        }
    }
}
