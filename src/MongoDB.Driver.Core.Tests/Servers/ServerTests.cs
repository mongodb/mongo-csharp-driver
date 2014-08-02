/* Copyright 2013-2014 MongoDB Inc.
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Tests.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Tests.Servers
{
    [TestFixture]
    public class ServerTests
    {
        private ClusterId _clusterId;
        private IConnectionPoolFactory _connectionPoolFactory;
        private EndPoint _endPoint;
        private MockConnection _heartbeatConnection;
        private IConnectionFactory _heartbeatConnectionFactory;
        private IServerListener _listener;
        private ServerSettings _settings;
        private Server _subject;


        [SetUp]
        public void Setup()
        {
            _clusterId = new ClusterId();
            _connectionPoolFactory = Substitute.For<IConnectionPoolFactory>();

            _endPoint = new DnsEndPoint("localhost", 27017);
            _heartbeatConnection = new MockConnection();
            _heartbeatConnectionFactory = Substitute.For<IConnectionFactory>();
            _heartbeatConnectionFactory.CreateConnection(null, null)
                .ReturnsForAnyArgs(_heartbeatConnection);

            _listener = Substitute.For<IServerListener>();
            _settings = new ServerSettings()
                .WithHeartbeatInterval(Timeout.InfiniteTimeSpan);

            _subject = new Server(_settings, _clusterId, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _listener);
        }

        [Test]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new Server(null, _clusterId, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            Action act = () => new Server(_settings, null, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new Server(_settings, _clusterId, null, _connectionPoolFactory, _heartbeatConnectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new Server(_settings, _clusterId, _endPoint, null, _heartbeatConnectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_heartbeatConnectionFactory_is_null()
        {
            Action act = () => new Server(_settings, _clusterId, _endPoint, _connectionPoolFactory, null, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_not_throw_when_listener_is_null()
        {
            Action act = () => new Server(_settings, _clusterId, _endPoint, _connectionPoolFactory, _heartbeatConnectionFactory, null);

            act.ShouldNotThrow();
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
        }

        [Test]
        public void Description_should_be_connected_after_successful_heartbeat()
        {
            SetupHeartbeatConnection();
            _subject.Initialize();

            SpinWait.SpinUntil(() => _subject.Description.State == ServerState.Connected, TimeSpan.FromSeconds(4));

            _subject.Description.State.Should().Be(ServerState.Connected);
            _subject.Description.Type.Should().Be(ServerType.Standalone);
        }

        [Test]
        public void GetConnectionAsync_should_throw_when_not_initialized()
        {
            Action act = () => _subject.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void GetConnectionAsync_should_throw_when_disposed()
        {
            _subject.Dispose();

            Action act = () => _subject.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void GetConnectionAsync_should_get_a_connection()
        {
            _subject.Initialize();

            var connection = _subject.GetConnectionAsync(Timeout.InfiniteTimeSpan, CancellationToken.None).Result;

            connection.Should().NotBeNull();
        }

        private void SetupHeartbeatConnection()
        {
            var isMasterReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1 }"));
            var buildInfoReply = MessageHelper.BuildSuccessReply<RawBsonDocument>(
                RawBsonDocumentHelper.FromJson("{ ok: 1, version: \"2.6.3\" }"));

            _heartbeatConnection.EnqueueReplyMessage(isMasterReply);
            _heartbeatConnection.EnqueueReplyMessage(buildInfoReply);
        }
    }
}