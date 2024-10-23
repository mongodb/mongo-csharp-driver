/* Copyright 2010-present MongoDB Inc.
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
using System.Linq;
using System.Net;
using FluentAssertions;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.TestHelpers.XunitExtensions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Core.Clusters
{
    public class SingleServerClusterTests : LoggableTestClass
    {
        private readonly EventCapturer _capturedEvents;
        private readonly EndPoint _endPoint = new DnsEndPoint("localhost", 27017);
        private readonly MockClusterableServerFactory _mockServerFactory;
        private ClusterSettings _settings;

        public SingleServerClusterTests(ITestOutputHelper output) : base(output)
        {
            _settings = new ClusterSettings(directConnection: true);
            _mockServerFactory = new MockClusterableServerFactory(LoggerFactory);
            _capturedEvents = new EventCapturer();
        }

        [Fact]
        public void Constructor_should_throw_if_more_than_one_endpoint_is_specified()
        {
            _settings = _settings.With(endPoints: new[] { _endPoint, new DnsEndPoint("localhost", 27018) });
            Action act = () => new SingleServerCluster(_settings, _mockServerFactory, _capturedEvents, loggerFactory: null);

            act.ShouldThrow<ArgumentException>();
        }

        [Fact]
        public void Constructor_should_throw_if_srvMaxHosts_is_greater_than_zero()
        {
            _settings = _settings.With(srvMaxHosts: 2);

            var exception = Record.Exception(() => new SingleServerCluster(_settings, _mockServerFactory, _capturedEvents, loggerFactory: null));

            exception.Should().BeOfType<ArgumentException>();
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
        [InlineData(ServerType.Standalone)]
        [InlineData(ServerType.ShardRouter)]
        [InlineData(ServerType.ReplicaSetArbiter)]
        [InlineData(ServerType.ReplicaSetGhost)]
        [InlineData(ServerType.ReplicaSetOther)]
        [InlineData(ServerType.ReplicaSetPrimary)]
        [InlineData(ServerType.ReplicaSetSecondary)]
        [InlineData(ServerType.ShardRouter)]
        public void Description_should_contain_any_new_server(ServerType serverType)
        {
            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            PublishDescription(_endPoint, serverType);

            subject.Description.Servers.Count.Should().Be(1);
            subject.Description.Servers.Single().Type.Should().Be(serverType);
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Description_should_regain_a_server_if_the_provided_server_is_rebooted_to_its_expected_type()
        {
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

        [Theory]
        [ParameterAttributeData]
        public void ServerDescription_type_should_be_replaced_with_Unknown_when_setName_is_different(
            [Values(null, "wrong")] string newSetName)
        {
            _settings = _settings.With(replicaSetName: "rs");

            var subject = CreateSubject();
            subject.Initialize();
            _capturedEvents.Clear();

            var replicaSetConfig = new ReplicaSetConfig([_endPoint], name: newSetName, _endPoint, 1);
            PublishDescription(_endPoint, ServerType.Standalone, replicaSetConfig);

            subject.Description.Type.Should().Be(ClusterType.Standalone);
            var resultServers = subject.Description.Servers;
            resultServers.Count.Should().Be(1);
            resultServers.First().Type.Should().Be(ServerType.Unknown);
            _capturedEvents.Next().Should().BeOfType<ClusterDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        // private methods
        private SingleServerCluster CreateSubject()
        {
            return new SingleServerCluster(_settings, _mockServerFactory, _capturedEvents, LoggerFactory);
        }

        private void PublishDescription(EndPoint endPoint, ServerType serverType, ReplicaSetConfig replicaSetConfig = null)
        {
            var current = _mockServerFactory.GetServerDescription(endPoint);

            var description = current.With(
                state: ServerState.Connected,
                type: serverType,
                replicaSetConfig: replicaSetConfig);

            _mockServerFactory.PublishDescription(description);
        }
    }
}
