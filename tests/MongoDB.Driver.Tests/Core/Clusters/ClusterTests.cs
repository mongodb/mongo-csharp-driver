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
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Clusters
{
    public class ClusterTests : LoggableTestClass
    {
        private readonly EventCapturer _capturedEvents;
        private readonly Mock<IClusterableServerFactory> _mockServerFactory;
        private ClusterSettings _settings;

        public ClusterTests(ITestOutputHelper output) : base(output)
        {
            _settings = new ClusterSettings(serverSelectionTimeout: TimeSpan.FromSeconds(2));
            _mockServerFactory = new Mock<IClusterableServerFactory>();
            _mockServerFactory.Setup(f => f.CreateServer(It.IsAny<ClusterType>(), It.IsAny<ClusterId>(), It.IsAny<IClusterClock>(), It.IsAny<EndPoint>()))
                .Returns((ClusterType _, ClusterId clusterId, IClusterClock _, EndPoint endPoint) =>
                {
                    var mockServer = new Mock<IClusterableServer>();
                    mockServer.SetupGet(s => s.EndPoint).Returns(endPoint);
                    mockServer.SetupGet(s => s.Description).Returns(new ServerDescription(new ServerId(clusterId, endPoint), endPoint));
                    return mockServer.Object;
                });
            _capturedEvents = new EventCapturer();
        }

        [Fact]
        public void SupportedWireVersionRange_should_return_expected_result()
        {
            var result = Cluster.SupportedWireVersionRange;

            result.Should().Be(new Range<int>(7, 27));
        }

        [Fact]
        public void Constructor_should_throw_if_settings_is_null()
        {
            Action act = () => new StubCluster(null, _mockServerFactory.Object, _capturedEvents, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_serverFactory_is_null()
        {
            Action act = () => new StubCluster(_settings, null, _capturedEvents, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_if_eventSubscriber_is_null()
        {
            Action act = () => new StubCluster(_settings, _mockServerFactory.Object, null, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Description_should_return_correct_description_when_not_initialized()
        {
            var subject = CreateSubject();
            var description = subject.Description;

            description.Servers.Should().BeEmpty();
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(ClusterType.Unknown);
        }

        [Fact]
        public void AcquireServerSession_should_call_serverSessionPool_AcquireSession()
        {
            var subject = CreateSubject();
            var mockServerSessionPool = new Mock<ICoreServerSessionPool>();
            var serverSessionPoolInfo = typeof(Cluster).GetField("_serverSessionPool", BindingFlags.NonPublic | BindingFlags.Instance);
            serverSessionPoolInfo.SetValue(subject, mockServerSessionPool.Object);
            var expectedResult = new Mock<ICoreServerSession>().Object;
            mockServerSessionPool.Setup(m => m.AcquireSession()).Returns(expectedResult);

            var result = subject.AcquireServerSession();

            result.Should().BeSameAs(expectedResult);
            mockServerSessionPool.Verify(m => m.AcquireSession(), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_if_not_initialized(
            [Values(false, true)]
            bool async)
        {
            var selector = new Mock<IServerSelector>().Object;
            var subject = CreateSubject();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

            exception.Should().BeOfType<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var selector = new Mock<IServerSelector>().Object;
            var subject = CreateSubject();
            subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

            exception.Should().BeOfType<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_if_serverSelector_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, null)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, null));

            exception.Should().BeOfType<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_return_a_server_if_one_matches(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            var result = async ?
                await subject.SelectServerAsync(OperationContext.NoTimeout, selector) :
                subject.SelectServer(OperationContext.NoTimeout, selector);

            result.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_return_second_server_if_first_cannot_be_found(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var endPoint1 = new DnsEndPoint("localhost", 27017);
            var endPoint2 = new DnsEndPoint("localhost", 27018);
            var connected1 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, endPoint1);
            var connected2 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, endPoint2);
            subject.SetServerDescriptions(connected1, connected2);
            subject.RemoveServer(endPoint1);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            var (server, _) = async ?
                await subject.SelectServerAsync(OperationContext.NoTimeout, selector) :
                subject.SelectServer(OperationContext.NoTimeout, selector);


            server.Should().NotBeNull();
            server.EndPoint.Should().Be(endPoint2);

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_if_no_servers_match(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject(serverSelectionTimeout: TimeSpan.FromMilliseconds(10));
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => Enumerable.Empty<ServerDescription>());

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

            exception.Should().BeOfType<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterEnteredSelectionQueueEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_throw_if_the_matched_server_cannot_be_found_and_no_others_matched(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject(serverSelectionTimeout: TimeSpan.FromMilliseconds(10));
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            subject.RemoveServer(connected.EndPoint);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

            exception.Should().BeOfType<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterEnteredSelectionQueueEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(0, 0, true)]
        [InlineData(28, 29, false)]
        [InlineData(28, 29, true)]
        public async Task SelectServer_should_throw_if_any_servers_are_incompatible(int min, int max, bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId, wireVersionRange: new Range<int>(min, max));
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            var exception = async ?
                await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

            exception.Should().BeOfType<MongoIncompatibleDriverException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_keep_trying_to_match_by_waiting_on_cluster_description_changes(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connecting = ServerDescriptionHelper.Disconnected(subject.Description.ClusterId);
            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);

            subject.SetServerDescriptions(connecting);
            _capturedEvents.Clear();

            _ = Task.Run(() =>
            {
                _capturedEvents.WaitForEventOrThrowIfTimeout<ClusterEnteredSelectionQueueEvent>(TimeSpan.FromSeconds(1));

                var descriptions = new Queue<ServerDescription>(new[] { connecting, connecting, connecting, connected });
                while (descriptions.Count > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    var next = descriptions.Dequeue();
                    subject.SetServerDescriptions(next);
                }
            });

            var selector = new DelegateServerSelector((c, s) => s);

            var result = async ?
                await subject.SelectServerAsync(OperationContext.NoTimeout, selector) :
                subject.SelectServer(OperationContext.NoTimeout, selector);

            result.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterEnteredSelectionQueueEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_ignore_deprioritized_servers_if_cluster_is_sharded(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject(clusterType: ClusterType.Sharded);
            subject.Initialize();

            var connected1 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27017));
            var connected2 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27018));
            var connected3 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27019));

            subject.SetServerDescriptions(connected1, connected2, connected3);

            var deprioritizedServers = new List<ServerDescription> { connected1 };

            var selector = new PriorityServerSelector(deprioritizedServers);

            for (int i = 0; i < 15; i++)
            {
                _capturedEvents.Clear();

                var (server, _) = async ?
                    await subject.SelectServerAsync(OperationContext.NoTimeout, selector) :
                    subject.SelectServer(OperationContext.NoTimeout, selector);

                server.Should().NotBeNull();

                deprioritizedServers.Should().NotContain(d => d.EndPoint == server.Description.EndPoint);

                _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
                _capturedEvents.Any().Should().BeFalse();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_return_deprioritized_servers_if_no_other_servers_exist_or_cluster_not_sharded(
            [Values(false, true)] bool async,
            [Values(false, true)] bool isSharded)
        {
            StubCluster subject = isSharded ? CreateSubject(null, ClusterType.Sharded) : CreateSubject();

            subject.Initialize();

            var connected1 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27017));
            var connected2 = ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27018));

            subject.SetServerDescriptions(connected1, connected2);

            var deprioritizedServers = new List<ServerDescription> { connected1, connected2 };

            var selector = new PriorityServerSelector(deprioritizedServers);

            _capturedEvents.Clear();
            var (server, _) = async ?
                await subject.SelectServerAsync(OperationContext.NoTimeout, selector) :
                subject.SelectServer(OperationContext.NoTimeout, selector);

            server.Should().NotBeNull();

            deprioritizedServers.Should().Contain(d => d.EndPoint == server.Description.EndPoint);

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void StartSession_should_return_expected_result()
        {
            var subject = CreateSubject();
            var options = new CoreSessionOptions();

            var result = subject.StartSession(options);

            result.Options.Should().BeSameAs(options);
            result.ServerSession.Should().NotBeNull();
        }

        [Fact]
        public void DescriptionChanged_should_be_raised_when_the_description_changes()
        {
            int count = 0;
            var subject = CreateSubject();
            subject.Initialize();

            // clear the ClusterDescriptionChanged event from initializing the StubCluster
            _capturedEvents.Clear();

            subject.DescriptionChanged += (o, e) => count++;

            subject.SetServerDescriptions(ServerDescriptionHelper.Connected(subject.Description.ClusterId));
            subject.SetServerDescriptions(ServerDescriptionHelper.Connected(subject.Description.ClusterId, averageRoundTripTime: TimeSpan.FromMilliseconds(10)));
            subject.SetServerDescriptions(ServerDescriptionHelper.Connected(subject.Description.ClusterId, averageRoundTripTime: TimeSpan.FromMilliseconds(13)));

            count.Should().Be(3);
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_apply_both_pre_and_post_server_selectors(
            [Values(false, true)]
            bool async)
        {
            _mockServerFactory.Setup(f => f.CreateServer(It.IsAny<ClusterType>(), It.IsAny<ClusterId>(), It.IsAny<IClusterClock>(), It.IsAny<EndPoint>()))
                .Returns((ClusterType _, ClusterId clusterId, IClusterClock clusterClock, EndPoint endPoint) =>
                {
                    var mockServer = new Mock<IClusterableServer>();
                    mockServer.SetupGet(s => s.EndPoint).Returns(endPoint);
                    mockServer.SetupGet(s => s.Description).Returns(new ServerDescription(new ServerId(clusterId, endPoint), endPoint));
                    return mockServer.Object;
                });

            var preSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27017));
            var middleSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27018));
            var postSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27019));

            var settings = new ClusterSettings(
                preServerSelector: preSelector,
                postServerSelector: postSelector);

            var subject = new StubCluster(settings, _mockServerFactory.Object, _capturedEvents, LoggerFactory);
            subject.Initialize();

            subject.SetServerDescriptions(
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27017)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27018)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27019)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27020)));
            _capturedEvents.Clear();

            var (server, _) = async ?
                await subject.SelectServerAsync(OperationContext.NoTimeout, middleSelector) :
                subject.SelectServer(OperationContext.NoTimeout, middleSelector);

            ((DnsEndPoint)server.EndPoint).Port.Should().Be(27020);
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task SelectServer_should_call_custom_selector(
            [Values(true, false)] bool withEligibleServers,
            [Values(true, false)] bool async)
        {
            int numberOfCustomServerSelectorCalls = 0;
            var customServerSelector = new DelegateServerSelector((c, s) =>
            {
                numberOfCustomServerSelectorCalls++;
                return s.Skip(1);
            });

            var settings = _settings.With(postServerSelector: customServerSelector);
            var subject = new StubCluster(settings, _mockServerFactory.Object, _capturedEvents, LoggerFactory);

            subject.Initialize();
            subject.SetServerDescriptions(
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27019)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27020)));
            _capturedEvents.Clear();

            if (withEligibleServers)
            {
                var selector = new DelegateServerSelector((c, s) => s);
                var (selectedServer, _) = async ?
                    await subject.SelectServerAsync(OperationContext.NoTimeout, selector):
                    subject.SelectServer(OperationContext.NoTimeout, selector);

                var selectedServerPort = ((DnsEndPoint)selectedServer.EndPoint).Port;
                selectedServerPort.Should().Be(27020);
                _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            }
            else
            {
                var selector = new DelegateServerSelector((c, s) => new ServerDescription[0]);
                var exception = async ?
                    await Record.ExceptionAsync(() => subject.SelectServerAsync(OperationContext.NoTimeout, selector)) :
                    Record.Exception(() => subject.SelectServer(OperationContext.NoTimeout, selector));

                exception.Should().BeOfType<TimeoutException>();
                _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterEnteredSelectionQueueEvent>();
                _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            }

            numberOfCustomServerSelectorCalls.Should().Be(1);
            _capturedEvents.Any().Should().BeFalse();
        }

        // private methods
        private StubCluster CreateSubject(TimeSpan? serverSelectionTimeout = null, ClusterType? clusterType = null)
        {
            if (serverSelectionTimeout != null)
            {
                _settings = _settings.With(serverSelectionTimeout: serverSelectionTimeout.Value);
            }

            return new StubCluster(_settings, _mockServerFactory.Object, _capturedEvents, LoggerFactory, clusterType);
        }

        // nested types
        private class StubCluster : Cluster
        {
            private Dictionary<EndPoint, IClusterableServer> _servers = new Dictionary<EndPoint, IClusterableServer>();
            private ClusterType? _clusterType;

            public StubCluster(ClusterSettings settings,
                IClusterableServerFactory serverFactory,
                IEventSubscriber eventSubscriber,
                ILoggerFactory loggerFactory,
                ClusterType? clusterType = null)
                : base(settings, serverFactory, eventSubscriber, loggerFactory)
            {
                _clusterType = clusterType;
            }

            public override void Initialize()
            {
                base.Initialize();

                UpdateClusterDescription(Description.WithType(_clusterType ?? Settings.GetInitialClusterType()));
            }

            public void RemoveServer(EndPoint endPoint)
            {
                _servers.Remove(endPoint);
            }

            public void SetServerDescriptions(params ServerDescription[] serverDescriptions)
            {
                var description = serverDescriptions.Aggregate(Description, (d, s) => d.WithServerDescription(s));
                UpdateClusterDescription(description);
                AddOrRemoveServers(description);
            }

            protected override void RequestHeartbeat()
            {
            }

            protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
            {
                return _servers.TryGetValue(endPoint, out server);
            }

            private void AddOrRemoveServers(ClusterDescription clusterDescription)
            {
                var endPoints = clusterDescription.Servers.Select(s => s.EndPoint).ToList();
                var endPointsToAdd = endPoints.Where(e => !_servers.ContainsKey(e));
                var endPointsToRemove = _servers.Keys.Where(e => !endPoints.Contains(e));
                foreach (var endPoint in endPointsToAdd)
                {
                    _servers.Add(endPoint, CreateServer(endPoint));
                }
                foreach (var endPoint in endPointsToRemove)
                {
                    _servers.Remove(endPoint);
                }
            }
        }
    }

    internal static class ClusterReflector
    {
        public static InterlockedInt32 _state(this Cluster cluster) => (InterlockedInt32)Reflector.GetFieldValue(cluster, nameof(_state));

        public static TimeSpan _minHeartbeatInterval(this Cluster cluster) => (TimeSpan)Reflector.GetFieldValue(cluster, nameof(_minHeartbeatInterval));
        public static void _minHeartbeatInterval(this Cluster cluster, TimeSpan timeSpan) => Reflector.SetFieldValue(cluster, nameof(_minHeartbeatInterval), timeSpan);
    }
}
