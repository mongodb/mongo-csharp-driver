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
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Events;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.Servers
{
    [TestFixture]
    public class ServerTests
    {
        private ClusterId _clusterId;
        private ClusterConnectionMode _clusterConnectionMode;
        private IConnectionPool _connectionPool;
        private IConnectionPoolFactory _connectionPoolFactory;
        private EndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private IServerMonitor _serverMonitor;
        private IServerMonitorFactory _serverMonitorFactory;
        private ServerSettings _settings;
        private Server _subject;

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

            _serverMonitor = Substitute.For<IServerMonitor>();
            _serverMonitorFactory = Substitute.For<IServerMonitorFactory>();
            _serverMonitorFactory.Create(null, null).ReturnsForAnyArgs(_serverMonitor);

            _capturedEvents = new EventCapturer();
            _settings = new ServerSettings(heartbeatInterval: Timeout.InfiniteTimeSpan);

            _subject = new Server(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _serverMonitorFactory, _capturedEvents);
        }

        [Test]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterConnectionMode, null, _endPoint, _connectionPoolFactory, _serverMonitorFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_clusterId_is_null()
        {
            Action act = () => new Server(null, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _serverMonitorFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterConnectionMode, _settings, null, _connectionPoolFactory, _serverMonitorFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_connectionPoolFactory_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterConnectionMode, _settings, _endPoint, null, _serverMonitorFactory, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_serverMonitorFactory_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, null, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new Server(_clusterId, _clusterConnectionMode, _settings, _endPoint, _connectionPoolFactory, _serverMonitorFactory, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Dispose_should_dispose_the_server()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Dispose();
            _connectionPool.Received().Dispose();
            _serverMonitor.Received().Dispose();

            _capturedEvents.Next().Should().BeOfType<ServerClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerClosedEvent>();
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
        public void Initialize_should_initialize_the_server()
        {
            _subject.Initialize();
            _connectionPool.Received().Initialize();
            _serverMonitor.Received().Initialize();

            _capturedEvents.Next().Should().BeOfType<ServerOpeningEvent>();
            _capturedEvents.Next().Should().BeOfType<ServerOpenedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void Invalidate_should_tell_the_monitor_to_invalidate_and_clear_the_connection_pool()
        {
            _subject.Initialize();
            _capturedEvents.Clear();

            _subject.Invalidate();
            _connectionPool.Received().Clear();
            _serverMonitor.Received().Invalidate();
        }

        [Test]
        public void RequestHeartbeat_should_tell_the_monitor_to_request_a_heartbeat()
        {
            _subject.Initialize();
            _capturedEvents.Clear();
            _subject.RequestHeartbeat();
            _serverMonitor.Received().RequestHeartbeat();

            _capturedEvents.Any().Should().BeFalse();
        }

        [Test]
        public void A_description_changed_event_with_a_heartbeat_exception_should_clear_the_connection_pool()
        {
            _subject.Initialize();
            var description = new ServerDescription(_subject.ServerId, _subject.EndPoint)
                .With(heartbeatException: new Exception("ughhh"));
            _serverMonitor.DescriptionChanged += Raise.EventWith(new ServerDescriptionChangedEventArgs(description, description));

            _connectionPool.Received().Clear();
        }
    }
}