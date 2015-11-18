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
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;
using MongoDB.Bson;

namespace MongoDB.Driver.Core.Clusters
{
    [TestFixture]
    public class MultiServerClusterTests
    {
        private EventCapturer _capturedEvents;
        private MockClusterableServerFactory _serverFactory;
        private ClusterSettings _settings;
        private EndPoint _firstEndPoint = new DnsEndPoint("localhost", 27017);
        private EndPoint _secondEndPoint = new DnsEndPoint("localhost", 27018);
        private EndPoint _thirdEndPoint = new DnsEndPoint("localhost", 27019);

        [SetUp]
        public void Setup()
        {
            _settings = new ClusterSettings();
            _serverFactory = new MockClusterableServerFactory();
            _capturedEvents = new EventCapturer();
        }

        [Test]
        public void Constructor_should_throw_if_no_endpoints_are_specified()
        {
            var settings = new ClusterSettings(endPoints: new EndPoint[0]);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Test]
        [TestCase(ClusterConnectionMode.Direct)]
        [TestCase(ClusterConnectionMode.Standalone)]
        public void Constructor_should_throw_if_cluster_connection_mode_is_not_supported(ClusterConnectionMode mode)
        {
            var settings = new ClusterSettings(
                endPoints: new[] { new DnsEndPoint("localhost", 27017) },
                connectionMode: mode);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentException>();
        }

        [Test]
        public void Description_should_be_correct_after_initialization()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(ClusterType.Unknown);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Initialize_should_throw_when_already_disposed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });
            var subject = CreateSubject();
            subject.Dispose();

            Action act = () => subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Should_discover_all_servers_in_the_cluster_reported_by_the_primary()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_discover_all_servers_in_the_cluster_when_notified_by_a_secondary()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetSecondary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_remove_a_server_that_is_no_longer_in_the_primary_host_list()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_remove_a_server_whose_canonical_end_point_does_not_match_its_provided_end_point()
        {
            var nonCanonicalEndPoint = new DnsEndPoint("wrong", 27017);
            _settings = _settings.With(endPoints: new[] { nonCanonicalEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(nonCanonicalEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint },
                canonicalEndPoint: _firstEndPoint);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_not_remove_a_server_that_is_no_longer_in_a_secondaries_host_list()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_not_remove_a_server_that_is_disconnected()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDisconnectedDescription(_secondEndPoint);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_not_add_a_new_server_from_a_secondary_when_a_primary_exists()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            PublishDescription(_secondEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        [TestCase(ClusterConnectionMode.ReplicaSet, ServerType.ShardRouter)]
        [TestCase(ClusterConnectionMode.ReplicaSet, ServerType.Standalone)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetArbiter)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetGhost)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetOther)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetPrimary)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.ReplicaSetSecondary)]
        [TestCase(ClusterConnectionMode.Sharded, ServerType.Standalone)]
        public void Should_hide_a_seedlist_server_of_the_wrong_type(ClusterConnectionMode connectionMode, ServerType wrongType)
        {
            _settings = _settings.With(
                endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                connectionMode: connectionMode);

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_secondEndPoint, wrongType);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [TestCase(ServerType.ShardRouter)]
        [TestCase(ServerType.Standalone)]
        public void Should_hide_a_discovered_server_of_the_wrong_type(ServerType wrongType)
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(_secondEndPoint, wrongType);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_ignore_changes_from_a_ReplicaSetGhost()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetGhost,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_remove_a_server_reporting_the_wrong_set_name()
        {
            _settings = _settings.With(
                endPoints: new[] { _firstEndPoint, _secondEndPoint },
                replicaSetName: "test");

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                setName: "funny");

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_secondEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_remove_server_from_the_seed_list_that_is_not_in_the_hosts_lists()
        {
            var alternateEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017);
            _settings = _settings.With(endPoints: new[] { alternateEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(alternateEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_and_current_election_id_is_null()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(_secondEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(
                new[] { GetDisconnectedDescription(_firstEndPoint) }
                .Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)));

            _serverFactory.GetServer(_firstEndPoint).Received().Invalidate();

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_with_an_election_id_and_current_id_is_null()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(_secondEndPoint, ServerType.ReplicaSetPrimary, electionId: new ElectionId(ObjectId.GenerateNewId()));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(
                new[] { GetDisconnectedDescription(_firstEndPoint) }
                .Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)));

            _serverFactory.GetServer(_firstEndPoint).Received().Invalidate();

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_with_a_higher_election_id()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary, electionId: new ElectionId(ObjectId.Empty));
            PublishDescription(_secondEndPoint, ServerType.ReplicaSetPrimary, electionId: new ElectionId(ObjectId.GenerateNewId()));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(
                new[] { GetDisconnectedDescription(_firstEndPoint) }
                .Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)));

            _serverFactory.GetServer(_firstEndPoint).Received().Invalidate();

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_invalidate_new_primary_when_it_shows_up_with_a_lesser_election_id()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary, electionId: new ElectionId(ObjectId.GenerateNewId()));
            PublishDescription(_secondEndPoint, ServerType.ReplicaSetPrimary, electionId: new ElectionId(ObjectId.Empty));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentTo(
                new[] { GetDisconnectedDescription(_secondEndPoint) }
                .Concat(GetDescriptions(_firstEndPoint, _thirdEndPoint)));

            _serverFactory.GetServer(_secondEndPoint).Received().Invalidate();

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_ignore_a_notification_from_a_server_which_has_been_removed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            PublishDescription(_thirdEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentTo(GetDescriptions(_firstEndPoint, _secondEndPoint));

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_call_initialize_on_all_servers()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            foreach (var endPoint in new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint })
            {
                var server = _serverFactory.GetServer(endPoint);
                server.Received().Initialize();
            }

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_call_dispose_on_servers_when_they_are_removed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var server = _serverFactory.GetServer(_thirdEndPoint);
            server.Received().Dispose();

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Should_call_dispose_on_servers_when_the_cluster_is_disposed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();
            subject.Dispose();

            foreach (var endPoint in new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint })
            {
                var server = _serverFactory.GetServer(endPoint);
                server.Received().Dispose();
            }

            _capturedEvents.Next().Should().BeOfType<ClusterClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        private MultiServerCluster CreateSubject()
        {
            return new MultiServerCluster(_settings, _serverFactory, _capturedEvents);
        }

        private IEnumerable<ServerDescription> GetDescriptions(params EndPoint[] endPoints)
        {
            return endPoints.Select(x => _serverFactory.GetServerDescription(x));
        }

        private ServerDescription GetDisconnectedDescription(EndPoint endPoint)
        {
            var desc = _serverFactory.GetServerDescription(endPoint);
            return new ServerDescription(desc.ServerId, endPoint);
        }

        private void PublishDisconnectedDescription(EndPoint endPoint)
        {
            var current = _serverFactory.GetServerDescription(endPoint);

            var description = new ServerDescription(current.ServerId, endPoint);
            _serverFactory.PublishDescription(description);
        }

        private void PublishDescription(EndPoint endPoint, ServerType serverType, IEnumerable<EndPoint> hosts = null, string setName = null, EndPoint primary = null, ElectionId electionId = null, EndPoint canonicalEndPoint = null)
        {
            var current = _serverFactory.GetServerDescription(endPoint);

            var config = new ReplicaSetConfig(
                hosts ?? new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                setName ?? "test",
                primary,
                null);

            var description = current.With(
                averageRoundTripTime: TimeSpan.FromMilliseconds(10),
                replicaSetConfig: serverType.IsReplicaSetMember() ? config : null,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                state: ServerState.Connected,
                tags: null,
                type: serverType,
                version: new SemanticVersion(2, 6, 3),
                wireVersionRange: new Range<int>(0, int.MaxValue));

            _serverFactory.PublishDescription(description);
        }
    }
}