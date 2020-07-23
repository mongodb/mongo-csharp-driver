/* Copyright 2016-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing,.Setup software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerMonitorTests
    {
        #region static
        private static readonly EndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), __endPoint);
        private static readonly ServerMonitorSettings __serverMonitorSettings = new ServerMonitorSettings(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        #endregion

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ServerMonitor(null, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, null, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, __endPoint, null, __serverMonitorSettings, new EventCapturer());

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_roundTripTimeMonitor_is_null()
        {
            var exception = Record.Exception(() => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer(), roundTripTimeMonitor: null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_serverMonitorSettings_is_null()
        {
            var exception = Record.Exception(() => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), null, new EventCapturer()));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Description_should_return_default_when_uninitialized()
        {
            var subject = CreateSubject(out _, out _, out _);

            var description = subject.Description;

            description.EndPoint.Should().Be(__endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void Description_should_return_default_when_disposed()
        {
            var subject = CreateSubject(out _, out _, out _);

            subject.Dispose();

            var description = subject.Description;
            description.EndPoint.Should().Be(__endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void DescriptionChanged_should_be_raised_during_initial_handshake()
        {
            var capturedEvents = new EventCapturer();

            var changes = new List<ServerDescriptionChangedEventArgs>();
            var subject = CreateSubject(out var mockConnection, out _, out _, capturedEvents);
            subject.DescriptionChanged += (o, e) => changes.Add(e);

            SetupHeartbeatConnection(mockConnection);
            subject.Initialize();
            SpinWait.SpinUntil(
                () =>
                    subject.Description.State == ServerState.Connected &&
                    changes.Count > 0, // there is a small possible delay between triggering an event and the actual description changing
                    TimeSpan.FromSeconds(5))
                .Should()
                .BeTrue();

            changes.Count.Should().Be(1);
            changes[0].OldServerDescription.State.Should().Be(ServerState.Disconnected);
            changes[0].NewServerDescription.State.Should().Be(ServerState.Connected);

            capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Description_should_be_connected_after_successful_heartbeat()
        {
            var capturedEvents = new EventCapturer();

            var subject = CreateSubject(out var mockConnection, out _, out _, capturedEvents);
            SetupHeartbeatConnection(mockConnection);
            subject.Initialize();
            SpinWait.SpinUntil(() => subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            subject.Description.State.Should().Be(ServerState.Connected);
            subject.Description.Type.Should().Be(ServerType.Standalone);

            // no ServerHeartbeat events should be triggered during initial handshake
            capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_clear_all_resources_only_once()
        {
            var capturedEvents = new EventCapturer();

            var subject = CreateSubject(out var mockConnection, out _, out var mockRoundTripTimeMonitor, capturedEvents, captureConnectionEvents: true);

            SetupHeartbeatConnection(mockConnection);
            subject.Initialize();
            SpinWait.SpinUntil(() => subject._connection() != null, TimeSpan.FromSeconds(5)).Should().BeTrue();

            subject.Dispose();
            subject.Dispose();

            capturedEvents.Events.Count(e => e is ConnectionClosingEvent).Should().Be(1);
            mockRoundTripTimeMonitor.Verify(m => m.Dispose(), Times.Once);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(null, true)]
        [InlineData("MongoConnectionException", null)]
        public void Heartbeat_should_make_immediate_next_attempt_for_streaming_protocol(string exceptionType, bool? moreToCome)
        {
            var capturedEvents = new EventCapturer()
                    .Capture<ServerHeartbeatSucceededEvent>()
                    .Capture<ServerHeartbeatFailedEvent>()
                    .Capture<ServerDescriptionChangedEvent>();
            var subject = CreateSubject(out var mockConnection, out _, out var mockRoundTimeTripMonitor, capturedEvents);

            subject.DescriptionChanged +=
                (o, e) =>
                {
                    capturedEvents.TryGetEventHandler<ServerDescriptionChangedEvent>(out var eventHandler);
                    eventHandler(new ServerDescriptionChangedEvent(e.OldServerDescription, e.NewServerDescription));
                };

            SetupHeartbeatConnection(mockConnection, isStreamable: true, autoFillStreamingResponses: false);

            Exception exception = null;
            switch (exceptionType)
            {
                case null:
                    mockConnection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(moreToCome.Value), null);
                    break;
                case "MongoConnectionException":
                    // previousDescription type is "Known" for this case
                    mockConnection.EnqueueCommandResponseMessage(
                        exception = CoreExceptionHelper.CreateException(exceptionType));
                    break;
            }

            // 10 seconds delay. Not expected to be processed
            mockConnection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), TimeSpan.FromSeconds(10));

            subject.Initialize();

            var expectedServerDescriptionChangedEventCount = exception != null
                ? 3 // +1 event because a connection initialized event doesn't have waiting
                : 2;
            capturedEvents.WaitForOrThrowIfTimeout(
                events =>
                    events.Count(e => e is ServerDescriptionChangedEvent) >= expectedServerDescriptionChangedEventCount,  // the connection has been initialized and the first heatbeat event has been fired
                TimeSpan.FromSeconds(10));

            capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>(); // connection initialized
            AssertHeartbeatAttempt();
            capturedEvents.Any().Should().BeFalse(); // the next attempt will be in 10 seconds because the second stremable respone has 10 seconds delay

            void AssertHeartbeatAttempt()
            {
                if (exception != null)
                {
                    mockRoundTimeTripMonitor.Verify(c => c.Reset(), Times.Once);

                    var serverHeartbeatFailedEvent = capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>().Subject; // updating the server based on the heartbeat
                    serverHeartbeatFailedEvent.Exception.Should().Be(exception);

                    var serverDescriptionChangedEvent = capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().Be(exception);

                    serverDescriptionChangedEvent = capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;  // when we catch exceptions, we close the current connection, so opening connection will trigger one more ServerDescriptionChangedEvent
                    serverDescriptionChangedEvent.OldDescription.HeartbeatException.Should().Be(exception);
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().BeNull();
                }
                else
                {
                    mockRoundTimeTripMonitor.Verify(c => c.Reset(), Times.Never);
                    capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
                    var serverDescriptionChangedEvent = capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject; // updating the server based on the heartbeat
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().BeNull();
                }
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void InitializeIsMasterProtocol_should_use_streaming_protocol_when_available([Values(false, true)] bool isStreamable)
        {
            var subject = CreateSubject(out var mockConnection, out _, out _);
            SetupHeartbeatConnection(mockConnection, isStreamable, autoFillStreamingResponses: true);

            mockConnection.WasReadTimeoutChanged.Should().Be(null);
            var resultProtocol = subject.InitializeIsMasterProtocol(mockConnection);
            if (isStreamable)
            {
                mockConnection.WasReadTimeoutChanged.Should().BeTrue();
                resultProtocol._command().Should().Contain("isMaster");
                resultProtocol._command().Should().Contain("topologyVersion");
                resultProtocol._command().Should().Contain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.ExhaustAllowed);
            }
            else
            {
                mockConnection.WasReadTimeoutChanged.Should().Be(null);
                resultProtocol._command().Should().Contain("isMaster");
                resultProtocol._command().Should().NotContain("topologyVersion");
                resultProtocol._command().Should().NotContain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.Return);
            }
        }

        [Fact]
        public void Initialize_should_run_round_time_trip_monitor_only_once()
        {
            var subject = CreateSubject(out var mockConnection, out _, out var mockRoundTripTimeMonitor);

            SetupHeartbeatConnection(mockConnection);

            subject.Initialize();
            subject.Initialize();

            mockRoundTripTimeMonitor.Verify(m => m.RunAsync(), Times.Once);
        }

        [Fact]
        public void RequestHeartbeat_should_force_another_heartbeat()
        {
            var capturedEvents = new EventCapturer();
            var subject = CreateSubject(out var mockConnection, out _, out _, capturedEvents);

            SetupHeartbeatConnection(mockConnection);
            subject.Initialize();
            SpinWait.SpinUntil(() => subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();
            capturedEvents.Clear();

            subject.RequestHeartbeat();

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            capturedEvents.Any().Should().BeFalse();
        }

        // private methods
        private ServerMonitor CreateSubject(out MockConnection connection, out Mock<IConnectionFactory> mockConnectionFactory, out Mock<IRoundTripTimeMonitor> mockRoundTripTimeMonitor, EventCapturer eventCapturer = null, bool captureConnectionEvents = false)
        {
            mockRoundTripTimeMonitor = new Mock<IRoundTripTimeMonitor>();
            mockRoundTripTimeMonitor.Setup(m => m.RunAsync()).Returns(Task.FromResult(true));

            if (captureConnectionEvents)
            {
                connection = new MockConnection(__serverId, new ConnectionSettings(), eventCapturer);
            }
            else
            {
                connection = new MockConnection();
            }
            mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(connection);

            return new ServerMonitor(
                __serverId,
                __endPoint,
                mockConnectionFactory.Object,
                __serverMonitorSettings,
                eventCapturer ?? new EventCapturer(),
                mockRoundTripTimeMonitor.Object);
        }

        private void SetupHeartbeatConnection(MockConnection connection, bool isStreamable = false, bool autoFillStreamingResponses = true)
        {
            var isMasterDocument = new BsonDocument
            {
                { "ok", 1 },
                { "topologyVersion", new TopologyVersion(new ObjectId(), 0).ToBsonDocument(), isStreamable },
                { "maxAwaitTimeMS", 5000, isStreamable }
            };

            var streamingIsMaster = Feature.StreamingIsMaster;
            var version = isStreamable ? streamingIsMaster.FirstSupportedVersion : streamingIsMaster.LastNotSupportedVersion;
            connection.Description = new ConnectionDescription(
                connection.ConnectionId,
                new IsMasterResult(isMasterDocument),
                new BuildInfoResult(BsonDocument.Parse($"{{ ok : 1, version : '{version}' }}")));

            if (autoFillStreamingResponses && isStreamable)
            {
                // immediate attempt
                connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), null);

                // 10 seconds delay. Won't expected to be processed
                connection.EnqueueCommandResponseMessage(CreateStreamableCommandResponseMessage(), TimeSpan.FromSeconds(10));
            }
        }

        private CommandResponseMessage CreateStreamableCommandResponseMessage(bool moreToCome = false)
        {
            var section0BsonDocument = new BsonDocument
            {
                { "ismaster", true },
                {
                    "topologyVersion",
                    new BsonDocument
                    {
                        { "processId", ObjectId.Parse("5ee3f0963109d4fe5e71dd28") },
                        { "counter", 0 }
                    },
                    !moreToCome // needs only for tests reasons
                },
                { "ok", 1 }
            };
            return MessageHelper.BuildCommandResponse(new RawBsonDocument(section0BsonDocument.ToBson()), moreToCome: moreToCome);
        }
    }

    internal static class ServerMonitorReflector
    {
        public static IConnection _connection(this ServerMonitor serverMonitor)
        {
            return (IConnection)Reflector.GetFieldValue(serverMonitor, nameof(_connection));
        }

        public static CommandWireProtocol<BsonDocument> InitializeIsMasterProtocol(this ServerMonitor serverMonitor, IConnection connection)
        {
            return (CommandWireProtocol<BsonDocument>)Reflector.Invoke(serverMonitor, nameof(InitializeIsMasterProtocol), connection);
        }
    }
}
