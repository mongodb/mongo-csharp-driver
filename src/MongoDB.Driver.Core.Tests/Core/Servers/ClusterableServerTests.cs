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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Servers
{
    [TestFixture]
    public class ClusterableServerTests
    {
        private ClusterId _clusterId;
        private ClusterConnectionMode _clusterConnectionMode;
        private IConnectionPool _connectionPool;
        private IConnectionPoolFactory _connectionPoolFactory;
        private EndPoint _endPoint;
        private MockConnection _heartbeatConnection;
        private IConnectionFactory _heartbeatConnectionFactory;
        private EventCapturer _capturedEvents;
        private ServerSettings _settings;
        private ClusterableServer _subject;


        [SetUp]
        public void Setup()
        {
            _clusterId = new ClusterId();
            _clusterConnectionMode = ClusterConnectionMode.Standalone;
            _connectionPool = Substitute.For<IConnectionPool>();
            _connectionPoolFactory = Substitute.For<IConnectionPoolFactory>();
            _connectionPoolFactory.CreateConnectionPool(null, null)
                .ReturnsForAnyArgs(_connectionPool);

            _endPoint = new DnsEndPoint("localhost", 27017);
            _heartbeatConnection = new MockConnection();
            _heartbeatConnectionFactory = Substitute.For<IConnectionFactory>();
            _heartbeatConnectionFactory.CreateConnection(null, null)
                .ReturnsForAnyArgs(_heartbeatConnection);

            _capturedEvents = new EventCapturer();
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new ClusterableServer(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _capturedEvents);
        }

        [Test]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ClusterableServer(_clusterId, _clusterConnectionMode, null, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            Action act = () => new ClusterableServer(null, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ClusterableServer(_clusterId, _clusterConnectionMode, _settings, null, _connectionPoolFactory, _heartbeatConnectionFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new ClusterableServer(_clusterId, _clusterConnectionMode, _settings, _endPoint, null, _heartbeatConnectionFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_heartbeatConnectionFactory_is_null()
        {
            Action act = () => new ClusterableServer(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, null, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ClusterableServer(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Description_should_return_default_when_uninitialized()
        {
            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Test]
        public void Description_should_return_default_when_disposed()
        {
            _subject.Dispose();

            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);

            _capturedEvents.Next().Should().BeOfType<ServerClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerClosedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void DescriptionChanged_should_be_raised_when_moving_from_disconnected_to_connected()
        {
            var changes = new List<ServerDescriptionChangedEventArgs>();
            _subject.DescriptionChanged += (o, e) => changes.Add(e);

            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));

            changes.Count.Should().Be(1);
            changes[0].OldServerDescription.State.Should().Be(ServerState.Disconnected);
            changes[0].NewServerDescription.State.Should().Be(ServerState.Connected);

            _capturedEvents.Next().Should().BeOfType<ServerOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Description_should_be_connected_after_successful_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));

            _subject.Description.State.Should().Be(ServerState.Connected);
            _subject.Description.Type.Should().Be(ServerType.Standalone);

            _capturedEvents.Next().Should().BeOfType<ServerOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void GetChannel_should_throw_when_not_initialized(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                act = () => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void GetChannel_should_throw_when_disposed(
            [Values(false, true)]
            bool async)
        {
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.GetChannel(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetChannel_should_get_a_connection(
            [Values(false, true)]
            bool async)
        {
            _subject.Initialize();

            IChannelHandle channel;
            if (async)
            {
                channel = _subject.GetChannelAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                channel = _subject.GetChannel(CancellationToken.None);
            }

            channel.Should().NotBeNull();
        }

        [Test]
        public void RequestHeartbeat_should_force_another_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));
            _capturedEvents.Clear();

            _subject.RequestHeartbeat();

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(4));

            // when heart fails, we immediately attempt a second, hence the multiple events...
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Invalidate_should_do_everything_invalidate_is_supposed_to_do()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));
            _capturedEvents.Clear();

            _subject.Invalidate();

            _connectionPool.Received().Clear();
            _subject.Description.Type.Should().Be(ServerType.Unknown);

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(4));

            // when heart fails, we immediately attempt a second, hence the multiple events...
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void A_failed_heartbeat_should_clear_the_connection_pool()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));
            _capturedEvents.Clear();

            _subject.RequestHeartbeat();

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(4));

            _connectionPool.ReceivedWithAnyArgs().Clear();

            // when heart fails, we immediately attempt a second, hence the multiple events...
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerDescriptionChangedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        private void SetupHeartbeatConnection()
        {
            var isMasterReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));

            _heartbeatConnection.EnqueueReplyMessage(isMasterReply);
            _heartbeatConnection.EnqueueReplyMessage(buildInfoReply);
        }
    }
}