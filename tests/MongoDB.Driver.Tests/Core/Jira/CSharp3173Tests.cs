/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Tests.Jira
{
    public class CSharp3173Tests : LoggableTestClass
    {
        private readonly static ClusterId __clusterId = new ClusterId();
        private readonly static bool __directConnection = false;
        private readonly static EndPoint __endPoint1 = new DnsEndPoint("localhost", 27017);
        private readonly static EndPoint __endPoint2 = new DnsEndPoint("localhost", 27018);
        private readonly static TimeSpan __heartbeatInterval = TimeSpan.FromMilliseconds(200);
        private readonly static ServerId __serverId1 = new ServerId(__clusterId, __endPoint1);
        private readonly static ServerId __serverId2 = new ServerId(__clusterId, __endPoint2);

        public CSharp3173Tests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [ParameterAttributeData]
        public void Ensure_command_network_error_before_handshake_is_correctly_handled([Values(false, true)] bool async, [Values(false, true)] bool streamable)
        {
            var eventCapturer = new EventCapturer().Capture<ServerDescriptionChangedEvent>();

            // ensure that hello or legacy hello check response is finished only after network error
            var hasNetworkErrorBeenTriggered = new TaskCompletionSource<bool>();
            // ensure that there are no unexpected events between test ending and cluster disposing
            var hasClusterBeenDisposed = new TaskCompletionSource<bool>();

            EndPoint initialSelectedEndpoint = null;
            using (var cluster = CreateAndSetupCluster(hasNetworkErrorBeenTriggered, hasClusterBeenDisposed, eventCapturer, streamable))
            {
                ForceClusterId(cluster, __clusterId);

                // 0. Initial heartbeat via `connection.Open`
                // The next hello or legacy hello response will be delayed because the waiting in the mock.Callbacks
                cluster.Initialize();

                var (selectedServer, _) = cluster.SelectServer(OperationContext.NoTimeout, CreateWritableServerAndEndPointSelector(__endPoint1));
                initialSelectedEndpoint = selectedServer.EndPoint;
                initialSelectedEndpoint.Should().Be(__endPoint1);

                // make sure the next hello or legacy hello check has been called
                Thread.Sleep(__heartbeatInterval + TimeSpan.FromMilliseconds(50));

                // 1. Trigger the command network error BEFORE handshake. At this time hello or legacy hello response is already delayed until `hasNetworkErrorBeenTriggered.SetResult`
                Exception exception;
                if (async)
                {
                    exception = Record.Exception(() => selectedServer.GetConnectionAsync(OperationContext.NoTimeout).GetAwaiter().GetResult());
                }
                else
                {
                    exception = Record.Exception(() => selectedServer.GetConnection(OperationContext.NoTimeout));
                }

                var e = exception.Should().BeOfType<MongoConnectionException>().Subject;
                e.Message.Should().Be("DnsException:pool");

                // 2. Waiting for the hello or legacy hello check
                hasNetworkErrorBeenTriggered.SetResult(true); // unlock the in-progress hello or legacy hello response

                Thread.Sleep(100); // make sure the delayed hello or legacy hello check had time to change description if there is a bug
                var knownServers = cluster.Description.Servers.Where(s => s.Type != ServerType.Unknown);
                if (knownServers.Select(s => s.EndPoint).Contains(initialSelectedEndpoint))
                {
                    throw new Exception($"The type of failed server {initialSelectedEndpoint} has not been changed to Unknown.");
                }

                // ensure that a new server can be selected
                (selectedServer, _) = cluster.SelectServer(OperationContext.NoTimeout, WritableServerSelector.Instance);

                // ensure that the selected server is not the same as the initial
                selectedServer.EndPoint.Should().Be(__endPoint2);

                // the 4th event is MongoConnectionException which will trigger the next hello or legacy hello check immediately
                eventCapturer.WaitForOrThrowIfTimeout(events => events.Count() >= 4, TimeSpan.FromSeconds(5));
            }
            hasClusterBeenDisposed.SetCanceled(); // Cut off not related events. Stop waiting in the latest mock.Callbacks for connection.Open

            // Events asserting
            var initialHeartbeatEvents = new[]
            {
                // endpoints can be in random order
                eventCapturer.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject,
                eventCapturer.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject
            }
            .OrderBy(c => GetPort(c.NewDescription.EndPoint))
            .ToList();
            AssertEvent(initialHeartbeatEvents[0], __endPoint1, ServerType.ShardRouter, "Heartbeat");
            AssertEvent(initialHeartbeatEvents[1], __endPoint2, ServerType.ShardRouter, "Heartbeat"); // the next 27018 events will be suppressed

            AssertNextEvent(eventCapturer, initialSelectedEndpoint, ServerType.Unknown, "InvalidatedBecause:ChannelException during handshake: MongoDB.Driver.MongoConnectionException: DnsException");
            AssertNextEvent(eventCapturer, initialSelectedEndpoint, ServerType.Unknown, "Heartbeat", (typeof(MongoConnectionException), "DnsException:sdam"));
            eventCapturer.Any().Should().BeFalse();

            int GetPort(EndPoint endpoint) => ((DnsEndPoint)endpoint).Port;
        }

        // private method
        private void AssertEvent(ServerDescriptionChangedEvent @event, EndPoint expectedEndPoint, ServerType expectedServerType, string expectedReasonStart, (Type ExceptionType, string ExceptionMessage)? exceptionInfo = null)
        {
            @event.ServerId.ClusterId.Should().Be(__clusterId);
            @event.NewDescription.EndPoint.Should().Be(expectedEndPoint);
            @event.NewDescription.Type.Should().Be(expectedServerType);
            @event.NewDescription.State.Should().Be(expectedServerType == ServerType.Unknown ? ServerState.Disconnected : ServerState.Connected);
            if (exceptionInfo.HasValue)
            {
                @event.NewDescription.HeartbeatException.Should().BeOfType(exceptionInfo.Value.ExceptionType);
                @event.NewDescription.HeartbeatException.Message.Should().Be(exceptionInfo.Value.ExceptionMessage);
            }
            else
            {
                @event.NewDescription.HeartbeatException.Should().BeNull();
            }
            @event.NewDescription.ReasonChanged.Should().StartWith(expectedReasonStart);
        }

        private void AssertNextEvent(EventCapturer eventCapturer, EndPoint expectedEndPoint, ServerType expectedServerType, string expectedReasonStart, (Type ExceptionType, string ExceptionMessage)? exceptionInfo = null)
        {
            var @event = eventCapturer.Next().Should().BeOfType<ServerDescriptionChangedEvent>().Subject;
            AssertEvent(@event, expectedEndPoint, expectedServerType, expectedReasonStart, exceptionInfo);
        }

        private IConnectionPoolFactory CreateAndSetupConnectionPoolFactory(Func<ServerId, IConnectionExceptionHandler> exceptionHandlerProvider, params (ServerId ServerId, EndPoint Endpoint, bool IsHealthy)[] serverInfoCollection)
        {
            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();

            foreach (var serverInfo in serverInfoCollection)
            {
                var mockConnectionPool = new Mock<IConnectionPool>();
                SetupConnectionPoolFactory(mockConnectionPoolFactory, mockConnectionPool.Object, serverInfo.ServerId, serverInfo.Endpoint);

                var mockServerConnection = new Mock<IConnectionHandle>();
                SetupConnection(mockServerConnection, serverInfo.ServerId);

                SetupConnectionPool(mockConnectionPool, mockServerConnection.Object, () => exceptionHandlerProvider(serverInfo.ServerId));
            }

            return mockConnectionPoolFactory.Object;

            void SetupConnection(Mock<IConnectionHandle> mockConnectionHandle, ServerId serverId)
            {
                mockConnectionHandle.SetupGet(c => c.ConnectionId).Returns(new ConnectionId(serverId));
            }

            void SetupConnectionPool(Mock<IConnectionPool> mockConnectionPool, IConnectionHandle connection, Func<IConnectionExceptionHandler> exceptionHandlerProvider)
            {
                var dnsException = CreateDnsException(connection.ConnectionId, from: "pool");
                mockConnectionPool
                    .Setup(c => c.AcquireConnection(It.IsAny<OperationContext>()))
                    .Callback(() => exceptionHandlerProvider().HandleExceptionOnOpen(dnsException))
                    .Throws(dnsException); // throw command dns exception
                mockConnectionPool
                    .Setup(c => c.AcquireConnectionAsync(It.IsAny<OperationContext>()))
                    .Callback(() => exceptionHandlerProvider().HandleExceptionOnOpen(dnsException))
                    .Throws(dnsException); // throw command dns exception
            }

            void SetupConnectionPoolFactory(Mock<IConnectionPoolFactory> mockFactory, IConnectionPool connectionPool, ServerId serverId, EndPoint endPoint)
            {
                mockFactory.Setup(c => c.CreateConnectionPool(serverId, endPoint, It.IsAny<IConnectionExceptionHandler>())).Returns(connectionPool);
            }
        }

        private IConnectionFactory CreateAndSetupServerMonitorConnectionFactory(
            TaskCompletionSource<bool> hasNetworkErrorBeenTriggered,
            TaskCompletionSource<bool> hasClusterBeenDisposed,
            bool streamable,
            params (ServerId ServerId, EndPoint Endpoint, bool IsHealthy)[] serverInfoCollection)
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());

            foreach (var serverInfo in serverInfoCollection)
            {
                // configure ServerMonitor connections
                var mockServerMonitorConnection = new Mock<IConnection>();
                SetupServerMonitorConnection(mockServerMonitorConnection, serverInfo.ServerId, serverInfo.IsHealthy, hasNetworkErrorBeenTriggered, hasClusterBeenDisposed, streamable);
                mockConnectionFactory
                    .When(() => !Environment.StackTrace.Contains(nameof(RoundTripTimeMonitor)))
                    .Setup(c => c.CreateConnection(serverInfo.ServerId, serverInfo.Endpoint))
                    .Returns(mockServerMonitorConnection.Object);

                // configure healthy RTT connections
                var mockRttConnection = new Mock<IConnection>();
                SetupServerMonitorConnection(
                    mockRttConnection,
                    serverInfo.ServerId,
                    isHealthy: true,
                    hasNetworkErrorBeenTriggered: null, // has no role for RTT
                    hasClusterBeenDisposed,
                    streamable);
                mockConnectionFactory
                    .When(() => Environment.StackTrace.Contains(nameof(RoundTripTimeMonitor)))
                    .Setup(c => c.CreateConnection(serverInfo.ServerId, serverInfo.Endpoint))
                    .Returns(mockRttConnection.Object);
            }

            return mockConnectionFactory.Object;
        }

        private MultiServerCluster CreateAndSetupCluster(TaskCompletionSource<bool> hasNetworkErrorBeenTriggered, TaskCompletionSource<bool> hasClusterBeenDisposed, IEventSubscriber eventCapturer, bool streamable)
        {
            (ServerId ServerId, EndPoint Endpoint, bool IsHealthy)[] serverInfoCollection = new[]
            {
                (__serverId1, __endPoint1, false),
                (__serverId2, __endPoint2, true),
            };

            var clusterSettings = new ClusterSettings(
                serverSelectionTimeout: TimeSpan.FromSeconds(30),
                endPoints: serverInfoCollection.Select(c => c.Endpoint).ToArray(),
                serverApi: null);

            var serverMonitorSettings = new ServerMonitorSettings(
                connectTimeout: TimeSpan.FromMilliseconds(1),
                heartbeatInterval: __heartbeatInterval);
            var serverSettings = new ServerSettings(serverMonitorSettings.HeartbeatInterval);

            MultiServerCluster cluster = null;

            var connectionPoolFactory = CreateAndSetupConnectionPoolFactory(
                serverId => (IConnectionExceptionHandler)cluster._servers().Single(s => s.ServerId.Equals(serverId)),
                serverInfoCollection);
            var serverMonitorConnectionFactory = CreateAndSetupServerMonitorConnectionFactory(hasNetworkErrorBeenTriggered, hasClusterBeenDisposed, streamable, serverInfoCollection);
            var serverMonitorFactory = new ServerMonitorFactory(serverMonitorSettings, serverMonitorConnectionFactory, eventCapturer, serverApi: null, LoggerFactory);

            var serverFactory = new ServerFactory(__directConnection, serverSettings, connectionPoolFactory, serverMonitorFactory, eventCapturer, serverApi: null, loggerFactory: null);

            return cluster = new MultiServerCluster(clusterSettings, serverFactory, eventCapturer, LoggerFactory);
        }

        private Exception CreateDnsException(ConnectionId connectionId, string from)
        {
            return new MongoConnectionException(connectionId, $"DnsException:{from}");
        }

        private IServerSelector CreateWritableServerAndEndPointSelector(EndPoint endPoint)
        {
            IServerSelector endPointServerSelector = new EndPointServerSelector(endPoint);
            return new CompositeServerSelector(
                new[]
                {
                    WritableServerSelector.Instance,
                    endPointServerSelector
                });
        }

        private void ForceClusterId(MultiServerCluster cluster, ClusterId clusterId)
        {
            Reflector.SetFieldValue(cluster, "_clusterId", clusterId);
            Reflector.SetFieldValue(cluster, "_expirableClusterDescription", new Cluster.ExpirableClusterDescription(cluster, ClusterDescription.CreateInitial(clusterId, __directConnection)));
        }

        private void SetupServerMonitorConnection(
            Mock<IConnection> mockConnection,
            ServerId serverId,
            bool isHealthy,
            TaskCompletionSource<bool> hasNetworkErrorBeenTriggered,
            TaskCompletionSource<bool> hasClusterBeenDisposed,
            bool streamable)
        {
            var connectionId = new ConnectionId(serverId);
            var maxWireVersion = streamable ? WireVersion.Server44 : WireVersion.Server40;
            var helloDocument = new BsonDocument
            {
                { "ok", 1 },
                { "minWireVersion", 0 },
                { "maxWireVersion", maxWireVersion },
                { "msg", "isdbgrid" },
                { "topologyVersion", new TopologyVersion(ObjectId.Empty, 1).ToBsonDocument(), streamable }
            };

            mockConnection.SetupGet(c => c.ConnectionId).Returns(connectionId);
            mockConnection.SetupGet(c => c.EndPoint).Returns(serverId.EndPoint);
            mockConnection.Setup(c => c.Settings).Returns(() => new ConnectionSettings());

            mockConnection
                .SetupGet(c => c.Description)
                .Returns(
                    new ConnectionDescription(
                        mockConnection.Object.ConnectionId,
                        new HelloResult(helloDocument)));

            Func<ResponseMessage> commandResponseAction;
            if (streamable)
            {
                commandResponseAction = () => { return MessageHelper.BuildCommandResponse(new RawBsonDocument(helloDocument.ToBson()), moreToCome: true); };
            }
            else
            {
                commandResponseAction = () => { return MessageHelper.BuildCommandResponse(new RawBsonDocument(helloDocument.ToBson())); };
            }

            if (isHealthy)
            {
                SetupHealthyConnection(mockConnection);
            }
            else
            {
                SetupFailedConnection(mockConnection);
            }

            void SetupFailedConnection(Mock<IConnection> mockFaultyConnection)
            {
                Ensure.IsNotNull(hasNetworkErrorBeenTriggered, nameof(hasNetworkErrorBeenTriggered));

                // async path is not used in serverMonitor
                var faultyConnectionResponses = new Queue<Action>(new Action[]
                {
                    () => { }, // the first hello or legacy hello configuration passes
                    () => throw CreateDnsException(mockConnection.Object.ConnectionId, from: "sdam"), // the dns exception. Should be triggered after Invalidate
                    () => WaitForTaskOrTimeout(hasClusterBeenDisposed.Task, TimeSpan.FromMinutes(1), "cluster dispose")
                });
                mockFaultyConnection
                    .Setup(c => c.Open(It.IsAny<OperationContext>()))
                    .Callback(() =>
                    {
                        var responseAction = faultyConnectionResponses.Dequeue();
                        responseAction();
                    });

                mockFaultyConnection
                    .Setup(c => c.ReceiveMessage(It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>()))
                    .Returns(() =>
                    {
                        WaitForTaskOrTimeout(
                            hasNetworkErrorBeenTriggered.Task,
                            TimeSpan.FromMinutes(1),
                            testTarget: "network error");
                        return commandResponseAction();
                    });
            }

            void SetupHealthyConnection(Mock<IConnection> mockHealthyConnection)
            {
                mockHealthyConnection.Setup(c => c.Open(It.IsAny<OperationContext>())); // no action is required
                mockHealthyConnection.Setup(c => c.OpenAsync(It.IsAny<OperationContext>())).Returns(Task.FromResult(true)); // no action is required
                mockHealthyConnection
                    .Setup(c => c.ReceiveMessage(It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>()))
                    .Returns(commandResponseAction);
                mockConnection
                    .Setup(c => c.ReceiveMessageAsync(It.IsAny<OperationContext>(), It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>()))
                    .ReturnsAsync(commandResponseAction);
            }
        }

        private void WaitForTaskOrTimeout(Task task, TimeSpan timeout, string testTarget)
        {
            var resultedTask = Task.WaitAny(task, Task.Delay(timeout));
            if (resultedTask != 0)
            {
                throw new Exception($"The waiting for {testTarget} is exceeded timeout {timeout}.");
            }
        }
    }
}
