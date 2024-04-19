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
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.WireProtocol;
using MongoDB.Driver.Core.WireProtocol.Messages;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerMonitorTests : LoggableTestClass
    {
        #region static
        private static readonly EndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), __endPoint);
        private static readonly ServerMonitorSettings __serverMonitorSettings = new ServerMonitorSettings(TimeSpan.FromSeconds(30), Timeout.InfiniteTimeSpan);
        #endregion

        public ServerMonitorTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ServerMonitor(null, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer(), serverApi: null, loggerFactory: null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, null, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer(), serverApi: null, loggerFactory: null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, __endPoint, null, __serverMonitorSettings, new EventCapturer(), serverApi: null, loggerFactory: null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, eventSubscriber: null, serverApi: null, loggerFactory: null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_roundTripTimeMonitor_is_null()
        {
            var exception = Record.Exception(() => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), __serverMonitorSettings, new EventCapturer(), roundTripTimeMonitor: null, serverApi: null, loggerFactory: null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_serverMonitorSettings_is_null()
        {
            var exception = Record.Exception(() => new ServerMonitor(__serverId, __endPoint, Mock.Of<IConnectionFactory>(), serverMonitorSettings: null, new EventCapturer(), serverApi: null, loggerFactory: null));

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

#if WINDOWS
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

            // ServerHeartbeatStartedEvent and ServerHeartbeatSucceededEvent events should be emitted during initial handshake
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
        }
#endif

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

            // ServerHeartbeatStartedEvent and ServerHeartbeatSucceededEvent events should be emitted during initial handshake
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
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
                    mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(moreToCome.Value), null);
                    break;
                case "MongoConnectionException":
                    // previousDescription type is "Known" for this case
                    mockConnection.EnqueueCommandResponseMessage(
                        exception = CoreExceptionHelper.CreateException(exceptionType));
                    break;
            }

            // 10 seconds delay. Not expected to be processed
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), TimeSpan.FromSeconds(10));

            subject.Initialize();

            var expectedServerDescriptionChangedEventCount = exception != null
                ? 3 // +1 event because a connection initialized event doesn't have waiting
                : 2;
            capturedEvents.WaitForOrThrowIfTimeout(
                events =>
                    events.Count(e => e is ServerDescriptionChangedEvent) >= expectedServerDescriptionChangedEventCount,  // the connection has been initialized and the first heartbeat event has been fired
                TimeSpan.FromSeconds(10));

            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>(); // heartbeat succeeded before connection initialized
            capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>(); // connection initialized
            AssertHeartbeatAttempt();
            capturedEvents.Any().Should().BeFalse(); // the next attempt will be in 10 seconds because the second streamable response has 10 seconds delay

            void AssertHeartbeatAttempt()
            {
                if (exception != null)
                {
                    mockRoundTimeTripMonitor.Verify(c => c.Reset(), Times.Once);

                    var serverHeartbeatFailedEvent = capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>().Subject; // updating the server based on the heartbeat
                    serverHeartbeatFailedEvent.Exception.Should().Be(exception);

                    var serverDescriptionChangedEvent = capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
                    serverDescriptionChangedEvent.NewDescription.HeartbeatException.Should().Be(exception);

                    // when we catch exceptions, we close the current connection,
                    // so opening connection will trigger one more ServerHeartbeatSucceededEvent and ServerDescriptionChangedEvent
                    capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
                    serverDescriptionChangedEvent = capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
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
        [InlineData(false, false, OppressiveLanguageConstants.LegacyHelloCommandName)]
        [InlineData(true, false, OppressiveLanguageConstants.LegacyHelloCommandName)]
        [InlineData(false, true, "hello")]
        [InlineData(true, true, "hello")]
        public void InitializeHelloProtocol_should_use_streaming_protocol_when_available(bool isStreamable, bool helloOk, string expectedCommand)
        {
            var subject = CreateSubject(out var mockConnection, out _, out _);
            SetupHeartbeatConnection(mockConnection, isStreamable, autoFillStreamingResponses: true);

            mockConnection.WasReadTimeoutChanged.Should().Be(null);
            var resultProtocol = subject.InitializeHelloProtocol(mockConnection, helloOk);
            if (isStreamable)
            {
                mockConnection.WasReadTimeoutChanged.Should().BeTrue();
                resultProtocol._command().Should().Contain(expectedCommand);
                resultProtocol._command().Should().Contain("topologyVersion");
                resultProtocol._command().Should().Contain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.ExhaustAllowed);
            }
            else
            {
                mockConnection.WasReadTimeoutChanged.Should().Be(null);
                resultProtocol._command().Should().Contain(expectedCommand);
                resultProtocol._command().Should().NotContain("topologyVersion");
                resultProtocol._command().Should().NotContain("maxAwaitTimeMS");
                resultProtocol._responseHandling().Should().Be(CommandResponseHandling.Return);
            }
        }

        [Fact]
        public void RoundTripTimeMonitor_should_be_started_only_once_if_using_streaming_protocol()
        {
            var capturedEvents = new EventCapturer().Capture<ServerHeartbeatSucceededEvent>();
            var serverMonitorSettings = new ServerMonitorSettings(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10));
            var subject = CreateSubject(out var mockConnection, out _, out var mockRoundTripTimeMonitor, capturedEvents, serverMonitorSettings: serverMonitorSettings);

            SetupHeartbeatConnection(mockConnection, isStreamable: true, autoFillStreamingResponses: false);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), null);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), null);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), null);

            subject.Initialize();

            SpinWait.SpinUntil(() => capturedEvents.Count >= 4, TimeSpan.FromSeconds(5)).Should().BeTrue();
            mockRoundTripTimeMonitor.Verify(m => m.Start(), Times.Once);
            mockRoundTripTimeMonitor.Verify(m => m.IsStarted, Times.AtLeast(4));
            subject.Dispose();
        }

        [Fact]
        public void RoundTripTimeMonitor_should_not_be_started_if_using_polling_protocol()
        {
            var serverMonitorSettings = new ServerMonitorSettings(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(10),
                serverMonitoringMode: ServerMonitoringMode.Poll);

            var capturedEvents = new EventCapturer().Capture<ServerHeartbeatSucceededEvent>();
            var subject = CreateSubject(out var mockConnection, out _, out var mockRoundTripTimeMonitor, capturedEvents, serverMonitorSettings: serverMonitorSettings);

            SetupHeartbeatConnection(mockConnection, isStreamable: true, autoFillStreamingResponses: false);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());

            subject.Initialize();
            SpinWait.SpinUntil(() => capturedEvents.Count >= 4, TimeSpan.FromSeconds(5)).Should().BeTrue();

            mockRoundTripTimeMonitor.Verify(m => m.Start(), Times.Never);
            subject.Dispose();
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

        [Fact]
        public void ServerHeartBeatEvents_should_not_be_awaited_if_using_polling_protocol()
        {
            var capturedEvents = new EventCapturer()
                .Capture<ServerHeartbeatStartedEvent>()
                .Capture<ServerHeartbeatSucceededEvent>();

            var serverMonitorSettings = new ServerMonitorSettings(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10), serverMonitoringMode: ServerMonitoringMode.Poll);
            var subject = CreateSubject(out var mockConnection, out _, out _, capturedEvents, serverMonitorSettings: serverMonitorSettings);

            SetupHeartbeatConnection(mockConnection, isStreamable: true, autoFillStreamingResponses: false);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());

            subject.Initialize();
            SpinWait.SpinUntil(() => capturedEvents.Count >= 6, TimeSpan.FromSeconds(5)).Should().BeTrue();
            subject.Dispose();

            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
        }

        [Fact]
        public void ServerMonitor_should_use_serverApi()
        {
            var serverApi = new ServerApi(ServerApiVersion.V1);

            MockConnection connection;

            using (var subject = CreateSubject(out connection, out _, out _, serverApi: serverApi))
            {
                SetupHeartbeatConnection(connection, isStreamable: true, autoFillStreamingResponses: false);
                connection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(true), null);

                subject.Initialize();

                SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();
            }

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            var requestId = sentMessages[0]["requestId"].AsInt32;
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {requestId}, responseTo : 0, exhaustAllowed : true, sections : [ {{ payloadType : 0, document : {{ hello : 1, helloOk: true, topologyVersion : {{ processId : ObjectId(\"000000000000000000000000\"), counter : NumberLong(0) }}, maxAwaitTimeMS : NumberLong(86400000), $db : \"admin\", apiVersion : \"1\" }} }} ] }}");
        }

        [Fact]
        public void ServerMonitor_without_serverApi_should_use_legacy_hello_to_set_up_streamable_monitoring()
        {
            MockConnection connection;

            using (var subject = CreateSubject(out connection, out _, out _, serverApi: null))
            {
                SetupHeartbeatConnection(connection, isStreamable: true, autoFillStreamingResponses: false);
                connection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(true), null);

                subject.Initialize();

                SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();
            }

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            var requestId = sentMessages[0]["requestId"].AsInt32;
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {requestId}, responseTo : 0, exhaustAllowed : true, sections : [ {{ payloadType : 0, document : {{ {OppressiveLanguageConstants.LegacyHelloCommandName} : 1, helloOk : true, topologyVersion : {{ processId : ObjectId(\"000000000000000000000000\"), counter : NumberLong(0) }}, maxAwaitTimeMS : NumberLong(86400000), $db : \"admin\" }} }} ] }}");
        }

        [Fact]
        public void ServerMonitor_without_serverApi_but_with_loadBalancedConnection_should_use_hello_command_to_set_up_streamable_monitoring()
        {
            MockConnection connection;

            using (var subject = CreateSubject(out connection, out _, out _, loadBalanced: true))
            {
                SetupHeartbeatConnection(connection, isStreamable: true, autoFillStreamingResponses: false);
                connection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(true), null);

                subject.Initialize();

                SpinWait.SpinUntil(() => connection.GetSentMessages().Count >= 1, TimeSpan.FromSeconds(4)).Should().BeTrue();
            }

            var sentMessages = MessageHelper.TranslateMessagesToBsonDocuments(connection.GetSentMessages());
            var requestId = sentMessages[0]["requestId"].AsInt32;
            sentMessages[0].Should().Be($"{{ opcode : \"opmsg\", requestId : {requestId}, responseTo : 0, exhaustAllowed : true, sections : [ {{ payloadType : 0, document : {{ hello : 1, helloOk: true, topologyVersion : {{ processId : ObjectId(\"000000000000000000000000\"), counter : NumberLong(0) }}, maxAwaitTimeMS : NumberLong(86400000), loadBalanced: true, $db : \"admin\" }} }} ] }}");
        }

        [Theory]
        [InlineData("AWS_EXECUTION_ENV=AWS_Lambda_java8")]
        [InlineData("AWS_LAMBDA_RUNTIME_API")]
        [InlineData("FUNCTIONS_WORKER_RUNTIME")]
        [InlineData("K_SERVICE")]
        [InlineData("FUNCTION_NAME")]
        [InlineData("VERCEL")]
        public void Should_use_polling_protocol_if_running_in_FaaS_platform(string environmentVariable)
        {
            var environmentVariableParts = environmentVariable.Split('=');

            var environmentVariableProviderMock = new Mock<IEnvironmentVariableProvider>();
            environmentVariableProviderMock
                .Setup(env => env.GetEnvironmentVariable(environmentVariableParts[0]))
                .Returns(environmentVariableParts.Length > 1 ? environmentVariableParts[1] : "dummy");

            var capturedEvents = new EventCapturer()
                .Capture<ServerHeartbeatStartedEvent>()
                .Capture<ServerHeartbeatSucceededEvent>();

            var serverMonitorSettings = new ServerMonitorSettings(TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(10));
            var subject = CreateSubject(out var mockConnection, out _, out _, capturedEvents, serverMonitorSettings: serverMonitorSettings, environmentVariableProviderMock: environmentVariableProviderMock);

            SetupHeartbeatConnection(mockConnection, isStreamable: true, autoFillStreamingResponses: false);
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());
            mockConnection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage());

            subject.Initialize();
            SpinWait.SpinUntil(() => capturedEvents.Count >= 6, TimeSpan.FromSeconds(5)).Should().BeTrue();
            subject.Dispose();

            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>().Subject.Awaited.Should().Be(false);
            capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>().Subject.Awaited.Should().Be(false);
        }

        // private methods
        private ServerMonitor CreateSubject(
            out MockConnection connection,
            out Mock<IConnectionFactory> mockConnectionFactory,
            out Mock<IRoundTripTimeMonitor> mockRoundTripTimeMonitor,
            EventCapturer eventCapturer = null,
            bool captureConnectionEvents = false,
            ServerApi serverApi = null,
            bool loadBalanced = false,
            ServerMonitorSettings serverMonitorSettings = null,
            Mock<IEnvironmentVariableProvider> environmentVariableProviderMock = null)
        {
            mockRoundTripTimeMonitor = new Mock<IRoundTripTimeMonitor>();

            var isRttStarted = false;
            mockRoundTripTimeMonitor.Setup(m => m.Start()).Callback(() => { isRttStarted = true; }) ;
            mockRoundTripTimeMonitor.SetupGet(m => m.IsStarted).Returns(() => isRttStarted);

            if (captureConnectionEvents)
            {
                connection = new MockConnection(__serverId, new ConnectionSettings(), eventCapturer);
            }
            else
            {
                connection = new MockConnection(__serverId, new ConnectionSettings(loadBalanced: loadBalanced), null);
            }
            mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(f => f.CreateConnection(__serverId, __endPoint))
                .Returns(connection);

            return new ServerMonitor(
                __serverId,
                __endPoint,
                mockConnectionFactory.Object,
                serverMonitorSettings ?? __serverMonitorSettings,
                eventCapturer ?? new EventCapturer(),
                mockRoundTripTimeMonitor.Object,
                serverApi,
                LoggerFactory,
                environmentVariableProviderMock?.Object);
        }

        private void SetupHeartbeatConnection(MockConnection connection, bool isStreamable = false, bool autoFillStreamingResponses = true)
        {
            var streamingHello = Feature.StreamingHello;
            var maxWireVersion = isStreamable ? streamingHello.FirstSupportedWireVersion : streamingHello.LastNotSupportedWireVersion;
            var helloDocument = new BsonDocument
            {
                { "ok", 1 },
                { "topologyVersion", new TopologyVersion(new ObjectId(), 0).ToBsonDocument(), isStreamable },
                { "maxAwaitTimeMS", 5000, isStreamable },
                { "maxWireVersion", maxWireVersion }
            };

            connection.Description = new ConnectionDescription(
                connection.ConnectionId,
                new HelloResult(helloDocument));

            if (autoFillStreamingResponses && isStreamable)
            {
                // immediate attempt
                connection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), null);

                // 10 seconds delay. Won't expected to be processed
                connection.EnqueueCommandResponseMessage(CreateHeartbeatCommandResponseMessage(), TimeSpan.FromSeconds(10));
            }
        }

        private CommandResponseMessage CreateHeartbeatCommandResponseMessage(bool moreToCome = false)
        {
            var section0BsonDocument = new BsonDocument
            {
                { OppressiveLanguageConstants.LegacyHelloResponseIsWritablePrimaryFieldName, true },
                {
                    "topologyVersion",
                    new BsonDocument
                    {
                        { "processId", ObjectId.Parse("5ee3f0963109d4fe5e71dd28") },
                        { "counter", new BsonInt64(0) }
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

        public static CommandWireProtocol<BsonDocument> InitializeHelloProtocol(this ServerMonitor serverMonitor, IConnection connection, bool helloOk)
        {
            return (CommandWireProtocol<BsonDocument>)Reflector.Invoke(serverMonitor, nameof(InitializeHelloProtocol), connection, helloOk);
        }
    }
}
