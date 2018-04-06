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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
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
        public void Invalidate_should_tell_the_monitor_to_invalidate_and_clear_the_connection_pool()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Invalidate();
            _mockConnectionPool.Verify(p => p.Clear(), Times.Once);
            _mockServerMonitor.Verify(m => m.Invalidate(), Times.Once);
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
}