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
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Moq;
using Xunit;
using MongoDB.Bson;
using MongoDB.Driver.Core.Tests.Core.Clusters;
using System.Threading.Tasks;

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
        public void constructor_should_initialize_instance()
        {
            var settings = new ClusterSettings(replicaSetName: "rs");
            var serverFactory = Mock.Of<IClusterableServerFactory>();
            var mockEventSubscriber = new Mock<IEventSubscriber>();
            var dnsMonitorFactory = Mock.Of<IDnsMonitorFactory>();

            var result = new MultiServerCluster(settings, serverFactory, mockEventSubscriber.Object, dnsMonitorFactory);

            result._dnsMonitorFactory().Should().BeSameAs(dnsMonitorFactory);
            result._eventSubscriber().Should().BeSameAs(mockEventSubscriber.Object);
            result._replicaSetName().Should().BeSameAs(settings.ReplicaSetName);
            result._state().Value.Should().Be(0); // State.Initial
            AssertTryGetEventHandlerWasCalled<ClusterClosingEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterClosedEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterOpeningEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterOpenedEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterAddingServerEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterAddedServerEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterRemovingServerEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<ClusterRemovedServerEvent>(mockEventSubscriber);
            AssertTryGetEventHandlerWasCalled<SdamInformationEvent>(mockEventSubscriber);
        }

        [Fact]
        public void Constructor_should_throw_if_no_endpoints_are_specified()
        {
            var settings = new ClusterSettings(endPoints: new EndPoint[0]);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentOutOfRangeException>();
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(ClusterConnectionMode.Direct)]
        [InlineData(ClusterConnectionMode.Standalone)]
        public void Constructor_should_throw_if_cluster_connection_mode_is_not_supported(ClusterConnectionMode mode)
        {
            var settings = new ClusterSettings(
                endPoints: new[] { new DnsEndPoint("localhost", 27017) },
                connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode,
#pragma warning restore CS0618 // Type or member is obsolete
                connectionMode: mode);
            Action act = () => new MultiServerCluster(settings, _serverFactory, _capturedEvents);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void constructor_should_use_default_DnsMonitorFactory_when_dnsMonitorFactory_is_null()
        {
            var settings = new ClusterSettings();
            var serverFactory = Mock.Of<IClusterableServerFactory>();
            var eventSubscriber = Mock.Of<IEventSubscriber>();

            var result = new MultiServerCluster(settings, serverFactory, eventSubscriber, dnsMonitorFactory: null);

            var dnsMonitorFactory = result._dnsMonitorFactory().Should().BeOfType<DnsMonitorFactory>().Subject;
            dnsMonitorFactory._eventSubscriber().Should().BeSameAs(eventSubscriber);
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
        public void Initialize_should_call_base_Initialize()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                // if the base class's _state changed from 0 to 1 that is evidence that base.Initialize was called
                var subjectBase = (Cluster)subject;
                subjectBase._state().Value.Should().Be(1); // State.Open
            }
        }

        [Fact]
        public void Initialize_should_change_state_to_Open()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                subject._state().Value.Should().Be(1); // State.Open
            }
        }

        [Fact]
        public void Initialize_should_raise_ClusterOpeningEvent()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var clusterOpeningEvent = ShouldHaveOneEventOfType<ClusterOpeningEvent>(_capturedEvents);
                clusterOpeningEvent.ClusterId.Should().Be(subject.Description.ClusterId);
                clusterOpeningEvent.ClusterSettings.Should().BeSameAs(subject.Settings);
            }
        }

        [Fact]
        public void Initialize_should_raise_ClusterAddingServerEvent()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var clusterAddingServerEvent = ShouldHaveOneEventOfType<ClusterAddingServerEvent>(_capturedEvents);
                clusterAddingServerEvent.ClusterId.Should().Be(subject.Description.ClusterId);
                clusterAddingServerEvent.EndPoint.Should().Be(subject.Settings.EndPoints[0]);
            }
        }

        [Fact]
        public void Initialize_should_raise_ClusterAddedServerEvent()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var clusterAddedServerEvent = ShouldHaveOneEventOfType<ClusterAddedServerEvent>(_capturedEvents);
                clusterAddedServerEvent.ClusterId.Should().Be(subject.Description.ClusterId);
                TimeSpanShouldBeShort(clusterAddedServerEvent.Duration);
            }
        }

        [Fact]
        public void Initialize_should_raise_ClusterDescriptionChangedEvent()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var clusterDescriptionChangedEvent = ShouldHaveOneEventOfType<ClusterDescriptionChangedEvent>(_capturedEvents);
                var oldClusterDescription = clusterDescriptionChangedEvent.OldDescription;
                var newClusterDescription = clusterDescriptionChangedEvent.NewDescription;
                oldClusterDescription.Servers.Should().HaveCount(0);
                newClusterDescription.Servers.Should().HaveCount(1);
                var newServer = newClusterDescription.Servers[0];
                newServer.EndPoint.Should().Be(subject.Settings.EndPoints[0]);
            }
        }

        [Fact]
        public void Initialize_should_raise_ClusterOpenedEvent()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var clusterOpenedEvent = ShouldHaveOneEventOfType<ClusterOpenedEvent>(_capturedEvents);
                clusterOpenedEvent.ClusterId.Should().Be(subject.Description.ClusterId);
                clusterOpenedEvent.ClusterSettings.Should().BeSameAs(subject.Settings);
                TimeSpanShouldBeShort(clusterOpenedEvent.Duration);
            }
        }

        [Fact]
        public void Initialize_should_raise_expected_events()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                var eventTypes = _capturedEvents.Events.Select(e => e.GetType()).ToList();
                var expectedEventTypes = new[]
                {
                    typeof(ClusterOpeningEvent),
                    typeof(ClusterAddingServerEvent),
                    typeof(ClusterAddedServerEvent),
                    typeof(ClusterDescriptionChangedEvent),
                    typeof(ClusterOpenedEvent),
                };
                eventTypes.Should().Equal(expectedEventTypes);
            }
        }

        [Fact]
        public void Initialize_should_initialize_all_servers()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                foreach (var server in subject._servers())
                {
                    var mockServer = Mock.Get<IClusterableServer>(server);
                    mockServer.Verify(m => m.Initialize(), Times.Once);
                }
            }
        }

        [Fact]
        public void Initialize_should_not_start_dns_monitor_thread_when_scheme_is_MongoDB()
        {
            using (var subject = CreateSubject())
            {
                subject.Initialize();

                subject._dnsMonitorThread().Should().BeNull();
            }
        }

        [Fact]
        public void Initialize_should_start_dns_monitor_thread_when_scheme_is_MongoDBPlusSrv()
        {
            var settings = new ClusterSettings(
                scheme: ConnectionStringScheme.MongoDBPlusSrv,
                endPoints: new[] { new DnsEndPoint("a.b.com", 53) });

            using (var subject = CreateSubject(settings))
            {
                subject.Initialize();

                subject._dnsMonitorThread().Should().NotBeNull();
            }
        }

        [Theory]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ReplicaSetArbiter, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ReplicaSetGhost, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ReplicaSetOther, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ReplicaSetPrimary, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ReplicaSetSecondary, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.ShardRouter, false)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.Standalone, true)]
        [InlineData(-1, -1, ClusterType.Standalone, ServerType.Unknown, false)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ReplicaSetArbiter, true)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ReplicaSetGhost, true)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ReplicaSetOther, true)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ReplicaSetPrimary, true)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ReplicaSetSecondary, true)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.ShardRouter, false)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.Standalone, false)]
        [InlineData(-1, -1, ClusterType.ReplicaSet, ServerType.Unknown, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ReplicaSetArbiter, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ReplicaSetGhost, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ReplicaSetOther, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ReplicaSetPrimary, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ReplicaSetSecondary, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.ShardRouter, true)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.Standalone, false)]
        [InlineData(-1, -1, ClusterType.Sharded, ServerType.Unknown, false)]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ReplicaSetArbiter, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ReplicaSetGhost, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ReplicaSetOther, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ReplicaSetPrimary, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ReplicaSetSecondary, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.ShardRouter, true)]
        [InlineData(ConnectionStringScheme.MongoDB, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.Standalone, false)]
        [InlineData(ConnectionStringScheme.MongoDBPlusSrv, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.Standalone, true)]
        [InlineData(-1, ClusterConnectionMode.Automatic, ClusterType.Unknown, ServerType.Unknown, false)]
        public void IsServerValidForCluster_should_return_expected_result(ConnectionStringScheme scheme, ClusterConnectionMode connectionMode, ClusterType clusterType, ServerType serverType, bool expectedResult)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            var settings = new ClusterSettings(scheme: scheme);
            using (var subject = CreateSubject(settings: settings))
            {
                var clusterSettings = new ClusterSettings(
#pragma warning disable CS0618 // Type or member is obsolete
                    connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode,
#pragma warning restore CS0618 // Type or member is obsolete
                    connectionMode: connectionMode);
                var result = subject.IsServerValidForCluster(clusterType, clusterSettings, serverType);

                result.Should().Be(expectedResult);
            }
        }

        [Theory]
#pragma warning disable CS0618 // Type or member is obsolete
        [InlineData(-1, ClusterConnectionMode.Automatic)]
        [InlineData(ClusterType.Unknown, -1)]
        public void IsServerValidForCluster_should_throw_when_any_argument_value_is_unexpected(ClusterType clusterType, ClusterConnectionMode connectionMode)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            using (var subject = CreateSubject())
            {
                var clusterSettings = new ClusterSettings(
#pragma warning disable CS0618 // Type or member is obsolete
                    connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode,
#pragma warning restore CS0618 // Type or member is obsolete
                    connectionMode: connectionMode);
                var exception = Record.Exception(() => subject.IsServerValidForCluster(clusterType, clusterSettings, ServerType.Unknown));

                exception.Should().BeOfType<MongoInternalException>();
            }
        }

        [Fact]
        public void ProcessStandaloneChange_should_not_remove_server_when_type_is_unknown()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                PublishDnsResults(subject, _firstEndPoint);
                PublishDescription(subject, _firstEndPoint, ServerType.Standalone);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint);

                PublishDisconnectedDescription(subject, _firstEndPoint);

                subject.Description.Type.Should().Be(ClusterType.Standalone);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void ProcessStandaloneChange_should_remove_all_other_servers_when_one_server_is_discovered_to_be_a_standalone(int standaloneIndex)
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var endPoints = new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint };
                var standAloneEndpoint = endPoints[standaloneIndex];
                PublishDnsResults(subject, endPoints);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(endPoints);

                PublishDescription(subject, standAloneEndpoint, ServerType.Standalone);

                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(standAloneEndpoint);
            }
        }

        [Fact]
        public void ProcessStandaloneChange_should_remove_only_server_when_it_is_no_longer_a_standalone()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                PublishDnsResults(subject, _firstEndPoint);
                PublishDescription(subject, _firstEndPoint, ServerType.Standalone);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint);

                PublishDescription(subject, _firstEndPoint, ServerType.ReplicaSetPrimary);

                subject.Description.Servers.Should().HaveCount(0);
            }
        }

        [Fact]
        public void ProcessDnsException_should_update_cluster_description()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var exception = new Exception("Dns exception");

                PublishDnsException(subject, exception);

                subject.Description.DnsMonitorException.Should().BeSameAs(exception);
            }
        }

        [Fact]
        public void ProcessDnsResults_should_ignore_empty_end_points_list()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var endPoints = new EndPoint[0];
                var originalDescription = subject.Description;

                PublishDnsResults(subject, endPoints);

                subject.Description.Should().BeSameAs(originalDescription);
            }
        }

        [Fact]
        public void ProcessDnsResults_should_add_missing_servers()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                PublishDnsResults(subject, _firstEndPoint);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint);

                PublishDnsResults(subject, _firstEndPoint, _secondEndPoint);

                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint, _secondEndPoint);
            }
        }

        [Fact]
        public void ProcessDnsResults_should_remove_extra_servers()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                PublishDnsResults(subject, _firstEndPoint, _secondEndPoint);
                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint, _secondEndPoint);

                PublishDnsResults(subject, _firstEndPoint);

                subject.Description.Servers.Select(s => s.EndPoint).Should().Equal(_firstEndPoint);
            }
        }

        [Fact]
        public void ProcessDnsResults_should_clear_dns_monitor_exception()
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var exception = new Exception("Dns exception");
                PublishDnsException(subject, exception);
                subject.Description.DnsMonitorException.Should().BeSameAs(exception);

                PublishDnsResults(subject, _firstEndPoint);

                subject.Description.DnsMonitorException.Should().BeNull();
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void ProcessDnsResults_should_call_Initialize_on_added_servers(int numberOfServers)
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                var endPoints = new[] { _firstEndPoint, _secondEndPoint, _thirdEndPoint }.Take(numberOfServers).ToArray();

                PublishDnsResults(subject, endPoints);

                foreach (var server in subject._servers())
                {
                    var mockServer = Mock.Get<IClusterableServer>(server);
                    mockServer.Verify(m => m.Initialize(), Times.Once);
                }
            }
        }

        [Theory]
        [InlineData(ServerType.Unknown, ClusterType.Unknown, false)]
        [InlineData(ServerType.Standalone, ClusterType.Standalone, true)]
        [InlineData(ServerType.ReplicaSetPrimary, ClusterType.ReplicaSet, true)]
        [InlineData(ServerType.ShardRouter, ClusterType.Sharded, false)]
        public void ShouldMonitorStop_should_return_expected_result(ServerType serverType, ClusterType clusterType, bool expectedResult)
        {
            var settings = new ClusterSettings(scheme: ConnectionStringScheme.MongoDBPlusSrv, endPoints: new[] { new DnsEndPoint("a.b.com", 53) });
            var mockDnsMonitorFactory = CreateMockDnsMonitorFactory();
            using (var subject = CreateSubject(settings: settings, dnsMonitorFactory: mockDnsMonitorFactory.Object))
            {
                subject.Initialize();
                PublishDnsResults(subject, _firstEndPoint);
                PublishDescription(subject, _firstEndPoint, serverType);
                subject.Description.Type.Should().Be(clusterType);

                var result = ((IDnsMonitoringCluster)subject).ShouldDnsMonitorStop();

                result.Should().Be(expectedResult);
            }
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
#pragma warning disable CS0618 // Type or member is obsolete
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
                connectionMode: connectionMode,
                connectionModeSwitch: ConnectionModeSwitch.UseConnectionMode);
#pragma warning restore CS0618 // Type or member is obsolete

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(subject, _secondEndPoint, wrongType);

            var description = subject.Description;
            description.Servers.Should().BeEquivalentToWithComparer(GetDescriptions(_firstEndPoint, _thirdEndPoint), _serverDescriptionComparer);

            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
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
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
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
            mockServer.Verify(s => s.Invalidate("NoLongerPrimary", null), Times.Once);

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
            mockServer.Verify(s => s.Invalidate("NoLongerPrimary", null), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Initializing (maxSetVersion, maxElectionId)");
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
            mockServer.Verify(s => s.Invalidate("NoLongerPrimary", null), Times.Once);

            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Initializing (maxSetVersion, maxElectionId)");
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Updating stale setVersion");
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
            mockServer.Verify(s => s.Invalidate("NoLongerPrimary", null), Times.Once);

            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Initializing (maxSetVersion, maxElectionId)");
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Updating stale electionId");
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
            mockServer.Verify(s => s.Invalidate("ReportedPrimaryIsStale", null), Times.Once);

            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Initializing (maxSetVersion, maxElectionId)");
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Invalidating server");
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
            mockServer.Verify(s => s.Invalidate("ReportedPrimaryIsStale", null), Times.Once);

            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Initializing (maxSetVersion, maxElectionId)");
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<SdamInformationEvent>()
                .Subject.Message.Should().Contain("Invalidating server");
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

        // private methods
        private void AssertTryGetEventHandlerWasCalled<TEvent>(Mock<IEventSubscriber> mockEventSubscriber)
        {
            Action<TEvent> handler;
            mockEventSubscriber.Verify(m => m.TryGetEventHandler<TEvent>(out handler), Times.Once);
        }

        private Mock<IDnsMonitorFactory> CreateMockDnsMonitorFactory()
        {
            var mockDnsMonitorFactory = new Mock<IDnsMonitorFactory>();
            mockDnsMonitorFactory
                .Setup(m => m.CreateDnsMonitor(It.IsAny<IDnsMonitoringCluster>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Mock.Of<IDnsMonitor>());
            return mockDnsMonitorFactory;
        }

        private MultiServerCluster CreateSubject(ClusterSettings settings = null, IDnsMonitorFactory dnsMonitorFactory = null)
        {
            settings = settings ?? _settings;
            return new MultiServerCluster(settings, _serverFactory, _capturedEvents, dnsMonitorFactory);
        }

        private void TimeSpanShouldBeShort(TimeSpan value)
        {
            value.Should().BeGreaterOrEqualTo(TimeSpan.Zero);
            value.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(5));
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

        private void PublishDnsException(IDnsMonitoringCluster cluster, Exception exception)
        {
            cluster.ProcessDnsException(exception);
        }

        private void PublishDnsResults(IDnsMonitoringCluster cluster, params EndPoint[] endPoints)
        {
            cluster.ProcessDnsResults(endPoints.Cast<DnsEndPoint>().ToList());
        }

        private TEvent ShouldHaveOneEventOfType<TEvent>(EventCapturer capturedEvents)
        {
            var matchingEvents = capturedEvents.Events.OfType<TEvent>().ToList();
            matchingEvents.Should().HaveCount(1);
            return matchingEvents[0];
        }
    }

    internal static class MultiServerClusterReflector
    {
        public static IDnsMonitorFactory _dnsMonitorFactory(this MultiServerCluster cluster) => (IDnsMonitorFactory)Reflector.GetFieldValue(cluster, nameof(_dnsMonitorFactory));
        public static Thread _dnsMonitorThread(this MultiServerCluster cluster) => (Thread)Reflector.GetFieldValue(cluster, nameof(_dnsMonitorThread));
        public static IEventSubscriber _eventSubscriber(this MultiServerCluster cluster) => (IEventSubscriber)Reflector.GetFieldValue(cluster, nameof(_eventSubscriber));
        public static Task _monitorServersTask(this MultiServerCluster cluster) => (Task)Reflector.GetFieldValue(cluster, nameof(_monitorServersTask));
        public static string _replicaSetName(this MultiServerCluster cluster) => (string)Reflector.GetFieldValue(cluster, nameof(_replicaSetName));
        public static List<IClusterableServer> _servers(this MultiServerCluster cluster) => (List<IClusterableServer>)Reflector.GetFieldValue(cluster, nameof(_servers));
        public static InterlockedInt32 _state(this MultiServerCluster cluster) => (InterlockedInt32)Reflector.GetFieldValue(cluster, nameof(_state));

        public static bool IsServerValidForCluster(this MultiServerCluster cluster, ClusterType clusterType, ClusterSettings clusterSettings, ServerType serverType)
            => (bool)Reflector.Invoke(cluster, nameof(IsServerValidForCluster), clusterType, clusterSettings, serverType);
        public static void ProcessServerDescriptionChanged(this MultiServerCluster cluster, ServerDescriptionChangedEventArgs args)
            => Reflector.Invoke(cluster, nameof(ProcessServerDescriptionChanged), args);
    }
}
