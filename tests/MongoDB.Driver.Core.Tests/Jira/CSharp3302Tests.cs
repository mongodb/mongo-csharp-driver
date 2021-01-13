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
using System.Diagnostics;
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
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.WireProtocol.Messages;
using MongoDB.Driver.Core.WireProtocol.Messages.Encoders;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Jira
{
    [CollectionDefinition("EndpointTests", DisableParallelization = true)]
    public class CSharp3302Tests
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly static ClusterConnectionMode __clusterConnectionMode = ClusterConnectionMode.ReplicaSet;
        private readonly static ConnectionModeSwitch __connectionModeSwitch = ConnectionModeSwitch.UseConnectionMode;
#pragma warning restore CS0618 // Type or member is obsolete
        private readonly static ClusterId __clusterId = new ClusterId();
        private readonly static bool? __directConnection = null;
        private readonly static EndPoint __endPoint1 = new DnsEndPoint("localhost", 27017);
        private readonly static EndPoint __endPoint2 = new DnsEndPoint("localhost", 27018);
        private readonly static TimeSpan __heartbeatInterval = TimeSpan.FromMilliseconds(200);
        private readonly static ServerId __serverId1 = new ServerId(__clusterId, __endPoint1);
        private readonly static ServerId __serverId2 = new ServerId(__clusterId, __endPoint2);

        private static HashSet<ServerId> s_primaries = new HashSet<ServerId>();

        [Fact]
        public async Task Ensure_hearbeat_timer_runs_synchroniosly()
        {
            var clusterSettings = new ClusterSettings(
                connectionMode: __clusterConnectionMode,
                connectionModeSwitch: __connectionModeSwitch,
                serverSelectionTimeout: TimeSpan.FromSeconds(30),
                endPoints: new[] { __endPoint1 });

            var allHeartbeatsRecieved = new TaskCompletionSource<bool>(false);
            const int heartbeatsExpectedMinCount = 3;
            int heartbeatsCount = 0, isInHearbeat = 0;
            var calledConcurrently = false;

            void BlockHeartbeatRequested()
            {
                // Validate BlockHeartbeatRequested is not running already
                calledConcurrently |= Interlocked.Exchange(ref isInHearbeat, 1) != 0;

                // Block Cluster._rapidHeartbeatTimer timer
                Thread.Sleep(40);

                Interlocked.Exchange(ref isInHearbeat, 0);

                if (Interlocked.Increment(ref heartbeatsCount) == heartbeatsExpectedMinCount)
                    allHeartbeatsRecieved.SetResult(true);
            }

            var serverDescriptionDisconnected = new ServerDescription(
                __serverId1,
                __endPoint1,
                type: ServerType.ReplicaSetPrimary,
                state: ServerState.Disconnected,
                replicaSetConfig: new ReplicaSetConfig(new[] { __endPoint1 }, "rs", __endPoint1, null));

            var serverDescriptionConnected = new ServerDescription(
              __serverId1,
              __endPoint1,
              type: ServerType.ReplicaSetPrimary,
              state: ServerState.Connected,
              replicaSetConfig: new ReplicaSetConfig(new[] { __endPoint1 }, "rs", __endPoint1, null));

            var serverMock = new Mock<IClusterableServer>();
            serverMock.Setup(s => s.EndPoint).Returns(__endPoint1);
            serverMock.Setup(s => s.IsInitialized).Returns(true);
            serverMock.Setup(s => s.Description).Returns(serverDescriptionDisconnected);
            serverMock.Setup(s => s.RequestHeartbeat()).Callback(BlockHeartbeatRequested);

            var serverFactoryMock = new Mock<IClusterableServerFactory>();
            serverFactoryMock
                .Setup(f => f.CreateServer(It.IsAny<ClusterId>(), It.IsAny<IClusterClock>(), It.IsAny<EndPoint>()))
                .Returns(serverMock.Object);

            using (var cluster = new MultiServerCluster(clusterSettings, serverFactoryMock.Object, new EventCapturer()))
            {
                cluster.__minHeartbeatIntervalSet(TimeSpan.FromMilliseconds(10));
                cluster.__minHeartbeatInterval().Should().Be(TimeSpan.FromMilliseconds(10));

                ForceClusterId(cluster, __clusterId);

                cluster.Initialize();

                // Trigger Cluster._rapidHeartbeatTimer
                var selectServerTask = cluster.SelectServerAsync(CreateWritableServerAndEndPointSelector(__endPoint1), CancellationToken.None);

                // Postpone change description, to allow timer to be actived
                await Task.Delay(100);

                // Change description
                var args = new ServerDescriptionChangedEventArgs(serverDescriptionDisconnected, serverDescriptionConnected);
                serverMock.Raise(s => s.DescriptionChanged += null, serverMock.Object, args);

                var selectedServer = await selectServerTask;

                // Wait for all hearbeats to complete
                await Task.WhenAny(allHeartbeatsRecieved.Task, Task.Delay(500));
            }

            allHeartbeatsRecieved.Task.Status.Should().Be(TaskStatus.RanToCompletion);
            calledConcurrently.Should().Be(false);
        }

        [Fact(Timeout = 10000)]
        public async Task Ensure_no_deadlock_after_primary_update()
        {
            // Force async execution, otherwise test timeout won't be respected
            await Task.Yield();

            // ensure that isMaster check response is finished only after network error
            var noLongerPrimaryStalled = new TaskCompletionSource<bool>();
            s_primaries.Add(__serverId1);

            EndPoint initialSelectedEndpoint = null;
            using (var cluster = CreateAndSetupCluster())
            {
                ForceClusterId(cluster, __clusterId);

                cluster.Initialize();
                foreach (var server in cluster._servers())
                {
                    server.DescriptionChanged += ProcessServerDescriptionChanged;
                }

                var selectedServer = cluster.SelectServer(CreateWritableServerAndEndPointSelector(__endPoint1), CancellationToken.None);
                initialSelectedEndpoint = selectedServer.EndPoint;
                initialSelectedEndpoint.Should().Be(__endPoint1);

                // Change primary
                s_primaries.Add(__serverId2);
                selectedServer = cluster.SelectServer(CreateWritableServerAndEndPointSelector(__endPoint2), CancellationToken.None);
                selectedServer.EndPoint.Should().Be(__endPoint2);

                // Ensure stalling happened
                await noLongerPrimaryStalled.Task;
            }

            void ProcessServerDescriptionChanged(object sender, ServerDescriptionChangedEventArgs e)
            {
                // Stall once for first primary
                if (e.NewServerDescription.ReasonChanged == "InvalidatedBecause:NoLongerPrimary" && s_primaries.Remove(__serverId1))
                {
                    var server = (IServer)sender;
                    server.EndPoint.Should().Be(__endPoint1);

                    // Postpone Server.Invalidate invoke in MultiServerCluster.ProcessReplicaSetChange
                    Thread.Sleep(1000);

                    noLongerPrimaryStalled.SetResult(true);
                }
            }
        }

        // private methods
        private IConnectionPoolFactory CreateAndSetupConnectionPoolFactory(params (ServerId ServerId, EndPoint Endpoint)[] serverInfoCollection)
        {
            var mockConnectionPoolFactory = new Mock<IConnectionPoolFactory>();

            foreach (var serverInfo in serverInfoCollection)
            {
                var mockConnectionPool = new Mock<IConnectionPool>();
                SetupConnectionPoolFactory(mockConnectionPoolFactory, mockConnectionPool.Object, serverInfo.ServerId, serverInfo.Endpoint);

                var mockServerConnection = new Mock<IConnectionHandle>();
                SetupConnection(mockServerConnection, serverInfo.ServerId);

                SetupConnectionPool(mockConnectionPool, mockServerConnection.Object);
            }

            return mockConnectionPoolFactory.Object;

            void SetupConnection(Mock<IConnectionHandle> mockConnectionHandle, ServerId serverId)
            {
                mockConnectionHandle.SetupGet(c => c.ConnectionId).Returns(new ConnectionId(serverId));
            }

            void SetupConnectionPool(Mock<IConnectionPool> mockConnectionPool, IConnectionHandle connection)
            {
                mockConnectionPool
                    .Setup(c => c.AcquireConnection(It.IsAny<CancellationToken>()))
                    .Returns(connection);
                mockConnectionPool
                    .Setup(c => c.AcquireConnectionAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(connection));
            }

            void SetupConnectionPoolFactory(Mock<IConnectionPoolFactory> mockFactory, IConnectionPool connectionPool, ServerId serverId, EndPoint endPoint)
            {
                mockFactory.Setup(c => c.CreateConnectionPool(serverId, endPoint)).Returns(connectionPool);
            }
        }

        private IConnectionFactory CreateAndSetupServerMonitorConnectionFactory(
            params (ServerId ServerId, EndPoint Endpoint)[] serverInfoCollection)
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();

            foreach (var serverInfo in serverInfoCollection)
            {
                var mockServerMonitorConnection = new Mock<IConnection>();
                SetupServerMonitorConnection(mockServerMonitorConnection, serverInfo.ServerId);
                mockConnectionFactory
                    .Setup(c => c.CreateConnection(serverInfo.ServerId, serverInfo.Endpoint))
                    .Returns(mockServerMonitorConnection.Object);
            }

            return mockConnectionFactory.Object;
        }

        private MultiServerCluster CreateAndSetupCluster()
        {
            (ServerId ServerId, EndPoint Endpoint)[] serverInfoCollection = new[]
            {
                (__serverId1, __endPoint1),
                (__serverId2, __endPoint2),
            };

            var clusterSettings = new ClusterSettings(
                connectionMode: __clusterConnectionMode,
                connectionModeSwitch: __connectionModeSwitch,
                serverSelectionTimeout: TimeSpan.FromSeconds(30),
                endPoints: serverInfoCollection.Select(c => c.Endpoint).ToArray());

            var serverMonitorSettings = new ServerMonitorSettings(
                connectTimeout: TimeSpan.FromMilliseconds(1),
                heartbeatInterval: __heartbeatInterval);
            var serverSettings = new ServerSettings(serverMonitorSettings.HeartbeatInterval);

            var eventCapturer = new EventCapturer();
            var connectionPoolFactory = CreateAndSetupConnectionPoolFactory(serverInfoCollection);
            var serverMonitorConnectionFactory = CreateAndSetupServerMonitorConnectionFactory(serverInfoCollection);
            var serverMonitorFactory = new ServerMonitorFactory(serverMonitorSettings, serverMonitorConnectionFactory, eventCapturer);

            var serverFactory = new ServerFactory(__clusterConnectionMode, __connectionModeSwitch, __directConnection, serverSettings, connectionPoolFactory, serverMonitorFactory, eventCapturer);

            return new MultiServerCluster(clusterSettings, serverFactory, eventCapturer);
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
            Reflector.SetFieldValue(cluster, "_description", ClusterDescription.CreateInitial(clusterId, __clusterConnectionMode, __connectionModeSwitch, __directConnection));
        }

        private void SetupServerMonitorConnection(
            Mock<IConnection> mockConnection,
            ServerId serverId)
        {
            var connectionId = new ConnectionId(serverId);
            var serverVersion = "2.6";
            var baseDoc = new BsonDocument
            {
                { "ok", 1 },
                { "minWireVersion", 6 },
                { "maxWireVersion", 7 },
                { "setName", "rs" },
                { "hosts", new BsonArray(new [] { "localhost:27017", "localhost:27018" })},
                { "version", serverVersion },
                { "topologyVersion", new TopologyVersion(ObjectId.Empty, 1).ToBsonDocument(), false }
            };

            var primaryDoc = (BsonDocument)baseDoc.DeepClone();
            primaryDoc.Add("ismaster", true);

            var secondaryDoc = (BsonDocument)baseDoc.DeepClone();
            secondaryDoc.Add("secondary", true);

            mockConnection.SetupGet(c => c.ConnectionId).Returns(connectionId);
            mockConnection.SetupGet(c => c.EndPoint).Returns(serverId.EndPoint);

            mockConnection
                .SetupGet(c => c.Description)
                .Returns(GetConnectionDescription);

            mockConnection.Setup(c => c.Open(It.IsAny<CancellationToken>())); // no action is required
            mockConnection.Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(true)); // no action is required
            mockConnection
                .Setup(c => c.ReceiveMessageAsync(It.IsAny<int>(), It.IsAny<IMessageEncoderSelector>(), It.IsAny<MessageEncoderSettings>(), It.IsAny<CancellationToken>()))
                .Returns(GetIsMasterResponse);

            Task<ResponseMessage> GetIsMasterResponse()
            {
                var doc = s_primaries.Contains(serverId) ? primaryDoc : secondaryDoc;

                ResponseMessage result = MessageHelper.BuildReply(new RawBsonDocument(doc.ToBson()));
                return Task.FromResult(result);
            }

            ConnectionDescription GetConnectionDescription()
            {
                var doc = s_primaries.Contains(serverId) ? primaryDoc : secondaryDoc;

                return new ConnectionDescription(
                    mockConnection.Object.ConnectionId,
                    new IsMasterResult(doc),
                    new BuildInfoResult(new BsonDocument("version", serverVersion)));
            }
        }
    }
}
;
