/* Copyright 2016 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing,.Setup software
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
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Servers
{
    public class ServerMonitorTests
    {
        private EndPoint _endPoint;
        private MockConnection _connection;
        private Mock<IConnectionFactory> _mockConnectionFactory;
        private EventCapturer _capturedEvents;
        private ServerId _serverId;
        private ServerMonitor _subject;


        public ServerMonitorTests()
        {
            _endPoint = new DnsEndPoint("localhost", 27017);
            _serverId = new ServerId(new ClusterId(), _endPoint);
            _connection = new MockConnection();
            _mockConnectionFactory = new Mock<IConnectionFactory>();
            _mockConnectionFactory
                .Setup(f => f.CreateConnection(_serverId, _endPoint))
                .Returns(_connection);
            _capturedEvents = new EventCapturer();

            _subject = new ServerMonitor(_serverId, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);
        }

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ServerMonitor(null, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, null, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, _mockConnectionFactory.Object, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Description_should_return_default_when_uninitialized()
        {
            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void Description_should_return_default_when_disposed()
        {
            _subject.Dispose();

            var description = _subject.Description;

            description.EndPoint.Should().Be(_endPoint);
            description.Type.Should().Be(ServerType.Unknown);
            description.State.Should().Be(ServerState.Disconnected);
        }

        [Fact]
        public void DescriptionChanged_should_be_raised_when_moving_from_disconnected_to_connected()
        {
            var changes = new List<ServerDescriptionChangedEventArgs>();
            _subject.DescriptionChanged += (o, e) => changes.Add(e);

            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            changes.Count.Should().Be(1);
            changes[0].OldServerDescription.State.Should().Be(ServerState.Disconnected);
            changes[0].NewServerDescription.State.Should().Be(ServerState.Connected);

            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Description_should_be_connected_after_successful_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            _subject.Description.State.Should().Be(ServerState.Connected);
            _subject.Description.Type.Should().Be(ServerType.Standalone);

            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void RequestHeartbeat_should_force_another_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();
            _capturedEvents.Clear();

            _subject.RequestHeartbeat();

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(5)).Should().BeTrue();

            // when heart fails, we immediately attempt a second, hence the multiple events...
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Invalidate_should_do_everything_invalidate_is_supposed_to_do()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(5)).Should().BeTrue();
            _capturedEvents.Clear();

            _subject.Invalidate();

            _subject.Description.Type.Should().Be(ServerType.Unknown);

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(5)).Should().BeTrue();
            SpinWait.SpinUntil(() => _capturedEvents.Count >= 4, TimeSpan.FromSeconds(5)).Should().BeTrue();

            // when heart fails, we immediately attempt a second, hence the multiple events...
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        private void SetupHeartbeatConnection()
        {
            var isMasterReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));

            _connection.EnqueueReplyMessage(isMasterReply);
            _connection.EnqueueReplyMessage(buildInfoReply);
        }
    }
}