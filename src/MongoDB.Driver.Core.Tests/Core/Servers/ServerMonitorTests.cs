/* Copyright 2016 MongoDB Inc.
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
    public class ServerMonitorTests
    {
        private EndPoint _endPoint;
        private MockConnection _connection;
        private IConnectionFactory _connectionFactory;
        private EventCapturer _capturedEvents;
        private ServerId _serverId;
        private ServerMonitor _subject;


        [SetUp]
        public void Setup()
        {
            _endPoint = new DnsEndPoint("localhost", 27017);
            _connection = new MockConnection();
            _connectionFactory = Substitute.For<IConnectionFactory>();
            _connectionFactory.CreateConnection(null, null)
                .ReturnsForAnyArgs(_connection);

            _capturedEvents = new EventCapturer();

            _serverId = new ServerId(new ClusterId(), _endPoint);
            _subject = new ServerMonitor(_serverId, _endPoint, _connectionFactory, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);
        }

        [Test]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ServerMonitor(null, _endPoint, _connectionFactory, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, null, _connectionFactory, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ServerMonitor(_serverId, _endPoint, _connectionFactory, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan, null);

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

            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
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

            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatStartedEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerHeartbeatSucceededEvent>();
            _capturedEvents.Any().Should().BeFalse();
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

            _subject.Description.Type.Should().Be(ServerType.Unknown);

            // the next requests down heartbeat connection will fail, so the state should
            // go back to disconnected
            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Disconnected, TimeSpan.FromSeconds(4));

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