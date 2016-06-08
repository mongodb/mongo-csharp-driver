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
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Clusters
{
    public class SingleServerClusterTests
    {
        private EventCapturer _capturedEvents;
        private MockClusterableServerFactory _mockServerFactory;
        private ClusterSettings _settings;

        private EndPoint _endPoint = new DnsEndPoint("localhost", 27017);

        public SingleServerClusterTests()
        {
            _settings = new ClusterSettings();
            _mockServerFactory = new MockClusterableServerFactory();
            _capturedEvents = new EventCapturer();
        }

        [Fact]
        public void Constructor_should_throw_if_more_than_one_endpoint_is_specified()
        {
            _settings = _settings.With(endPoints: new[] { _endPoint, new DnsEndPoint("localhost", 27018) });
            Action act = () => new SingleServerCluster(_settings, _mockServerFactory, _capturedEvents);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Initialize_should_throw_if_disposed()
        {
            var subject = CreateSubject();
            subject.Dispose();

            Action act = () => subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Initialize_should_create_and_initialize_the_server()
        {
            var subject = CreateSubject();
            subject.Initialize();

            var mockServer = Mock.Get(_mockServerFactory.GetServer(_endPoint));
            mockServer.Verify(s => s.Initialize(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterAddedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(ClusterConnectionMode.ReplicaSet, ServerType.ShardRouter)]
        [InlineData(ClusterConnectionMode.ReplicaSet, ServerType.Standalone)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ReplicaSetArbiter)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ReplicaSetGhost)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ReplicaSetOther)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ReplicaSetPrimary)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ReplicaSetSecondary)]
        [InlineData(ClusterConnectionMode.Standalone, ServerType.ShardRouter)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetArbiter)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetGhost)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetOther)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetPrimary)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.ReplicaSetSecondary)]
        [InlineData(ClusterConnectionMode.Sharded, ServerType.Standalone)]
        public void Description_should_not_contain_any_servers_if_the_provided_server_is_not_of_the_required_type(ClusterConnectionMode connectionMode, ServerType serverType)
        {
            _settings = _settings.With(connectionMode: connectionMode);

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_endPoint, serverType);

            subject.Description.Servers.Should().BeEmpty();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Description_should_regain_a_server_if_the_provided_server_is_rebooted_to_its_expected_type()
        {
            _settings = _settings.With(connectionMode: ClusterConnectionMode.Standalone);

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_endPoint, ServerType.ReplicaSetGhost);
            PublishDescription(_endPoint, ServerType.Standalone);

            subject.Description.Servers.Count.Should().Be(1);

            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Dispose_should_dispose_of_the_server()
        {
            var subject = CreateSubject();
            subject.Initialize();

            _capturedEvents.Clear();

            subject.Dispose();

            var mockServer = Mock.Get(_mockServerFactory.GetServer(_endPoint));
            mockServer.Verify(s => s.Dispose(), Times.Once);

            _capturedEvents.Next().Should().BeOfType<ClusterClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovingServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterRemovedServerEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ClusterClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        private SingleServerCluster CreateSubject()
        {
            return new SingleServerCluster(_settings, _mockServerFactory, _capturedEvents);
        }

        private void PublishDescription(EndPoint endPoint, ServerType serverType)
        {
            var current = _mockServerFactory.GetServerDescription(endPoint);

            var description = current.With(
                state: ServerState.Connected,
                type: serverType);

            _mockServerFactory.PublishDescription(description);
        }
    }
}