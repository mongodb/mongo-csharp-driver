/* Copyright 2013-2015 MongoDB Inc.
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
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Clusters
{
    [TestFixture]
    public class ClusterTests
    {
        private EventCapturer _capturedEvents;
        private IClusterableServerFactory _serverFactory;
        private ClusterSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new ClusterSettings(serverSelectionTimeout: TimeSpan.FromSeconds(2),
                postServerSelector: new LatencyLimitingServerSelector(TimeSpan.FromMinutes(2)));
            _serverFactory = Substitute.For<IClusterableServerFactory>();
            _capturedEvents = new EventCapturer();
        }

        [Test]
        public void Constructor_should_throw_if_settings_is_null()
        {
            Action act = () => new StubCluster(null, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_serverFactory_is_null()
        {
            Action act = () => new StubCluster(_settings, null, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_if_eventSubscriber_is_null()
        {
            Action act = () => new StubCluster(_settings, _serverFactory, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        [TestCase(ClusterConnectionMode.Automatic, ClusterType.Unknown)]
        [TestCase(ClusterConnectionMode.Direct, ClusterType.Unknown)]
        [TestCase(ClusterConnectionMode.ReplicaSet, ClusterType.ReplicaSet)]
        [TestCase(ClusterConnectionMode.Sharded, ClusterType.Sharded)]
        [TestCase(ClusterConnectionMode.Standalone, ClusterType.Standalone)]
        public void Description_should_return_correct_description_when_not_initialized(ClusterConnectionMode connectionMode, ClusterType clusterType)
        {
            var subject = CreateSubject(connectionMode);
            var description = subject.Description;

            description.Servers.Should().BeEmpty();
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(clusterType);
        }

        [Test]
        public void SelectServer_should_throw_if_not_initialized(
            [Values(false, true)]
            bool async)
        {
            var selector = Substitute.For<IServerSelector>();
            var subject = CreateSubject();

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(selector, CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void SelectServer_should_throw_if_disposed(
            [Values(false, true)]
            bool async)
        {
            var selector = Substitute.For<IServerSelector>();
            var subject = CreateSubject();
            subject.Dispose();

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(selector, CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void SelectServer_should_throw_if_serverSelector_is_null(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(null, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(null, CancellationToken.None);
            }

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void SelectServer_should_return_a_server_if_one_matches(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            IServer result;
            if (async)
            {
                result = subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.SelectServer(selector, CancellationToken.None);
            }

            result.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void SelectServer_should_return_second_server_if_first_cannot_be_found(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs((IClusterableServer)null, Substitute.For<IClusterableServer>());

            var connected1 = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            var connected2 = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected1, connected2);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            IServer result;
            if (async)
            {
                result = subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.SelectServer(selector, CancellationToken.None);
            }

            result.Should().NotBeNull();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void SelectServer_should_throw_if_no_servers_match(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => Enumerable.Empty<ServerDescription>());

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(selector, CancellationToken.None);
            }

            act.ShouldThrow<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void SelectServer_should_throw_if_the_matched_server_cannot_be_found_and_no_others_matched(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs((IClusterableServer)null);

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(selector, CancellationToken.None);
            }

            act.ShouldThrow<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void SelectServer_should_throw_if_any_servers_are_incompatible(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId, wireVersionRange: new Range<int>(10, 12));
            subject.SetServerDescriptions(connected);
            _capturedEvents.Clear();

            var selector = new DelegateServerSelector((c, s) => s);

            Action act;
            if (async)
            {
                act = () => subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => subject.SelectServer(selector, CancellationToken.None);
            }

            act.ShouldThrow<MongoException>();

            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void SelectServer_should_keep_trying_to_match_by_waiting_on_cluster_description_changes(
            [Values(false, true)]
            bool async)
        {
            var subject = CreateSubject();
            subject.Initialize();

            var connecting = ServerDescriptionHelper.Disconnected(subject.Description.ClusterId);
            var connected = ServerDescriptionHelper.Connected(subject.Description.ClusterId);

            subject.SetServerDescriptions(connecting);
            _capturedEvents.Clear();

            Task.Run(() =>
            {
                var descriptions = new Queue<ServerDescription>(new[] { connecting, connecting, connecting, connected });
                while (descriptions.Count > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(20));
                    var next = descriptions.Dequeue();
                    subject.SetServerDescriptions(next);
                }
            });

            var selector = new DelegateServerSelector((c, s) => s);

            IServer result;
            if (async)
            {
                result = subject.SelectServerAsync(selector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.SelectServer(selector, CancellationToken.None);
            }

            result.Should().NotBeNull();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void DescriptionChanged_should_be_raised_when_the_description_changes()
        {
            int count = 0;
            var subject = CreateSubject();
            subject.Initialize();
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

        [Test]
        public void SelectServer_should_apply_both_pre_and_post_server_selectors(
            [Values(false, true)]
            bool async)
        {
            _serverFactory.CreateServer(null, null).ReturnsForAnyArgs(ci =>
            {
                var endPoint = ci.Arg<EndPoint>();
                var server = Substitute.For<IClusterableServer>();
                server.EndPoint.Returns(endPoint);
                return server;
            });

            var preSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27017));
            var middleSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27018));
            var postSelector = new DelegateServerSelector((cd, sds) => sds.Where(x => ((DnsEndPoint)x.EndPoint).Port != 27019));

            var settings = new ClusterSettings(
                preServerSelector: preSelector,
                postServerSelector: postSelector);

            var subject = new StubCluster(settings, _serverFactory, _capturedEvents);
            subject.Initialize();

            subject.SetServerDescriptions(
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27017)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27018)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27019)),
                ServerDescriptionHelper.Connected(subject.Description.ClusterId, new DnsEndPoint("localhost", 27020)));
            _capturedEvents.Clear();

            IServer result;
            if (async)
            {
                result = subject.SelectServerAsync(middleSelector, CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                result = subject.SelectServer(middleSelector, CancellationToken.None);
            }

            ((DnsEndPoint)result.EndPoint).Port.Should().Be(27020);
            _capturedEvents.Next().Should().BeOfType<ClusterSelectingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterSelectedServerEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        private StubCluster CreateSubject(ClusterConnectionMode connectionMode = ClusterConnectionMode.Automatic)
        {
            _settings = _settings.With(connectionMode: connectionMode);

            return new StubCluster(_settings, _serverFactory, _capturedEvents);
        }

        private class StubCluster : Cluster
        {
            public StubCluster(ClusterSettings settings, IClusterableServerFactory serverFactory, IEventSubscriber eventSubscriber)
                : base(settings, serverFactory, eventSubscriber)
            {


            }

            public override void Initialize()
            {
                base.Initialize();
            }

            public void SetServerDescriptions(params ServerDescription[] serverDescriptions)
            {
                var description = serverDescriptions.Aggregate(Description, (d, s) => d.WithServerDescription(s));
                UpdateClusterDescription(description);
            }

            protected override void RequestHeartbeat()
            {

            }

            protected override bool TryGetServer(EndPoint endPoint, out IClusterableServer server)
            {
                server = CreateServer(endPoint);
                return server != null;
            }
        }
    }
}