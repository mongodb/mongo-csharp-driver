/* Copyright 2013-2016 MongoDB Inc.
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
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Moq;
using Xunit;
using MongoDB.Bson;
using System.Reflection;
using MongoDB.Driver.Core.Async;

namespace MongoDB.Driver.Core.Clusters
{
    public class MultiServerClusterTests
    {
        private EventCapturer _capturedEvents;
        private MockClusterableServerFactory _serverFactory;
        private ClusterSettings _settings;
        private EndPoint _firstEndPoint = new DnsEndPoint("localhost", 27017);
        private EndPoint _secondEndPoint = new DnsEndPoint("localhost", 27018);
        private EndPoint _thirdEndPoint = new DnsEndPoint("localhost", 27019);
        private IEqualityComparer<ServerDescription> _serverDescriptionComparer = ServerDescriptionWithSimilarLastUpdateTimestampEqualityComparer.Instance;

        public MultiServerClusterTests()
        {
            _settings = new ClusterSettings();
            _serverFactory = new MockClusterableServerFactory();
            _capturedEvents = new EventCapturer();
        }

        [Fact]
        public void Constructor_should_throw_if_no_endpoints_are_specified()
        {
            var settings = new ClusterSettings(endPoints: new EndPoint[0]);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(ClusterConnectionMode.Direct)]
        [InlineData(ClusterConnectionMode.Standalone)]
        public void Constructor_should_throw_if_cluster_connection_mode_is_not_supported(ClusterConnectionMode mode)
        {
            var settings = new ClusterSettings(
                endPoints: new[] { new DnsEndPoint("localhost", 27017) },
                connectionMode: mode);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Description_should_be_correct_after_initialization()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(ClusterType.Unknown);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Initialize_should_throw_when_already_disposed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });
            var subject = CreateSubject();
            subject.Dispose();

            Action act = () => subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Should_discover_all_servers_in_the_cluster_reported_by_the_primary()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_discover_all_servers_in_the_cluster_when_notified_by_a_secondary()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetSecondary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_remove_a_server_that_is_no_longer_in_the_primary_host_list()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_remove_a_server_whose_canonical_end_point_does_not_match_its_provided_end_point()
        {
            var nonCanonicalEndPoint = new DnsEndPoint("wrong", 27017);
            _settings = _settings.With(endPoints: new[] { nonCanonicalEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, nonCanonicalEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint },
                canonicalEndPoint: _firstEndPoint);
            SpinWait.SpinUntil(() => !subject.Description.Servers.Any(d => ((DnsEndPoint)d.EndPoint).Host == "wrong"), TimeSpan.FromSeconds(5)).Should().BeTrue();

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Disconnected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_not_remove_a_server_that_is_no_longer_in_a_secondaries_host_list()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_not_remove_a_server_that_is_disconnected()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDisconnectedDescription(subject, _secondEndPoint);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_not_add_a_new_server_from_a_secondary_when_a_primary_exists()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(ClusterConnectionMode.ReplicaSet, ServerType.ShardRouter)]
        [InlineData(ClusterConnectionMode.ReplicaSet, ServerType.Standalone)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetArbiter)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetGhost)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetOther)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetPrimary)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetSecondary)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.Standalone)]
        public void Should_hide_a_seedlist_server_of_the_wrong_type(ClusterConnectionMode connectionMode, ServerType wrongType)
        {
            _settings = _settings.With(
                endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                connectionMode: connectionMode);

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _secondEndPoint, wrongType);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(ServerType.ShardRouter)]
        [InlineData(ServerType.Standalone)]
        public void Should_hide_a_discovered_server_of_the_wrong_type(ServerType wrongType)
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(subject, _secondEndPoint, wrongType);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_ignore_changes_from_a_ReplicaSetGhost()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetGhost,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_remove_a_server_reporting_the_wrong_set_name()
        {
            _settings = _settings.With(
                endPoints: new[] { _firstEndPoint, _secondEndPoint },
                replicaSetName: "test");

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetSecondary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                setName: "funny");

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_secondEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_remove_server_from_the_seed_list_that_is_not_in_the_hosts_lists()
        {
            var alternateEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27017);
            _settings = _settings.With(endPoints: new[] { alternateEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, alternateEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint, _thirdEndPoint), _serverDescriptionComparer);

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

        [Fact]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_and_current_set_version_and_election_id_are_null()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_firstEndPoint) }.Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_firstEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_with_an_election_id_and_current_set_version_and_election_id_are_null()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.GenerateNewId()));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_firstEndPoint) }.Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_firstEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_with_a_higher_set_version()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.Empty));
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary, setVersion: 2, electionId: new ElectionId(ObjectId.Empty));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_firstEndPoint) }.Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_firstEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_invalidate_existing_primary_when_a_new_primary_shows_up_with_the_same_set_version_and_a_higher_election_id()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.Empty));
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.GenerateNewId()));

            var description = subject.Description;

            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_firstEndPoint) }.Concat(GetDescriptions(_secondEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_firstEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_invalidate_new_primary_when_it_shows_up_with_a_lesser_set_version()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary, setVersion: 2, electionId: new ElectionId(ObjectId.Empty));
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.GenerateNewId()));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_secondEndPoint) }.Concat(GetDescriptions(_firstEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_secondEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_invalidate_new_primary_when_it_shows_up_with_the_same_set_version_and_a_lesser_election_id()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.GenerateNewId()));
            PublishDescription(subject, _secondEndPoint, ServerType.ReplicaSetPrimary, setVersion: 1, electionId: new ElectionId(ObjectId.Empty));

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(
                new[] { GetDisconnectedDescription(_secondEndPoint) }.Concat(GetDescriptions(_firstEndPoint, _thirdEndPoint)),
                _serverDescriptionComparer);

            var mockServer = Mock.Get(_serverFactory.GetServer(_secondEndPoint));
            mockServer.Verify(s => s.Invalidate(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_ignore_a_notification_from_a_server_which_has_been_removed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            PublishDescription(subject, _thirdEndPoint, ServerType.ReplicaSetPrimary);

            var description = subject.Description;
            description.State.Should().Be(ClusterState.Connected);
            description.Type.Should().Be(ClusterType.ReplicaSet);
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _secondEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_call_initialize_on_all_servers()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint });

            var subject = CreateSubject();
            subject.Initialize();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });
            SpinWait.SpinUntil(() => subject.Description.Servers.Count == 3, TimeSpan.FromSeconds(5)).Should().BeTrue();

            foreach (var endPoint in new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint })
            {
                var mockServer = Mock.Get(_serverFactory.GetServer(endPoint));
                mockServer.Verify(s => s.Initialize(), Times.Once);
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

        [Fact]
        public void Should_call_dispose_on_servers_when_they_are_removed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary,
                hosts: new[] { _firstEndPoint, _secondEndPoint });

            var mockServer = Mock.Get(_serverFactory.GetServer(_thirdEndPoint));
            mockServer.Verify(s => s.Dispose(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Should_call_dispose_on_servers_when_the_cluster_is_disposed()
        {
            _settings = _settings.With(endPoints: new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint });

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();
            subject.Dispose();

            foreach (var endPoint in new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint })
            {
                var mockServer = Mock.Get(_serverFactory.GetServer(endPoint));
                mockServer.Verify(s => s.Dispose(), Times.Once);
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

        private void PublishDisconnectedDescription(ICluster cluster, EndPoint endPoint)
        {
            var current = _serverFactory.GetServerDescription(endPoint);

            var serverDescription = new ServerDescription(current.ServerId, endPoint);
            var currentClusterDescription = cluster.Description;
            _serverFactory.PublishDescription(serverDescription);
            SpinWait.SpinUntil(() => !object.ReferenceEquals(cluster.Description, currentClusterDescription), 100); // sometimes returns false and that's OK
        }

        private void PublishDescription(ICluster cluster, EndPoint endPoint, ServerType serverType, IEnumerable<EndPoint> hosts = null, string setName = null, EndPoint primary = null, ElectionId electionId = null, EndPoint canonicalEndPoint = null, int? setVersion = null)
        {
            var current = _serverFactory.GetServerDescription(endPoint);

            var config = new ReplicaSetConfig(
                hosts ?? new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint },
                setName ?? "test",
                primary,
                setVersion);

            var serverDescription = current.With(
                averageRoundTripTime: TimeSpan.FromMilliseconds(10),
                replicaSetConfig: serverType.IsReplicaSetMember() ? config : null,
                canonicalEndPoint: canonicalEndPoint,
                electionId: electionId,
                state: ServerState.Connected,
                tags: null,
                type: serverType,
                version: new SemanticVersion(2, 6, 3),
                wireVersionRange: new Range<int>(0, int.MaxValue));

            var currentClusterDescription = cluster.Description;
            _serverFactory.PublishDescription(serverDescription);
            SpinWait.SpinUntil(() => !object.ReferenceEquals(cluster.Description, currentClusterDescription), 100); // sometimes returns false and that's OK
        }
    }
}