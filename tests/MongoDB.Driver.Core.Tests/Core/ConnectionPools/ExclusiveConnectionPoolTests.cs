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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.ConnectionPools
{
    public class ExclusiveConnectionPoolTests
    {
        private Mock<IConnectionFactory> _mockConnectionFactory;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private ServerId _serverId;
        private ConnectionPoolSettings _settings;
        private ExclusiveConnectionPool _subject;

        public ExclusiveConnectionPoolTests()
        {
            _mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            _endPoint = new DnsEndPoint("localhost", 27017);
            _capturedEvents = new EventCapturer();
            _serverId = new ServerId(new ClusterId(), _endPoint);
            _settings = new ConnectionPoolSettings(
                maintenanceInterval: Timeout.InfiniteTimeSpan,
                maxConnections: 4,
                minConnections: 2,
                waitQueueSize: 1,
                waitQueueTimeout: TimeSpan.FromSeconds(2));

            _subject = new ExclusiveConnectionPool(
                _serverId,
                _endPoint,
                _settings,
                _mockConnectionFactory.Object,
                _capturedEvents);
        }

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(null, _endPoint, _settings, _mockConnectionFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, null, _settings, _mockConnectionFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, null, _mockConnectionFactory.Object, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, null, _capturedEvents);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventSubscriber_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, _mockConnectionFactory.Object, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_throw_an_InvalidOperationException_if_not_initialized(
            [Values(false, true)]
            bool async)
        {
            Action act;
            if (async)
            {
                act = () => _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.AcquireConnection(CancellationToken.None);
            }

            act.ShouldThrow<InvalidOperationException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_throw_an_ObjectDisposedException_after_disposing(
            [Values(false, true)]
            bool async)
        {
            _subject.Dispose();

            Action act;
            if (async)
            {
                act = () => _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.AcquireConnection(CancellationToken.None);
            }

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_return_a_connection(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            _capturedEvents.Clear();

            IConnectionHandle connection;
            if (async)
            {
                connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                connection = _subject.AcquireConnection(CancellationToken.None);
            }

            connection.Should().NotBeNull();
            _subject.AvailableCount.Should().Be(_settings.MaxConnections - 1);
            _subject.CreatedCount.Should().Be(_settings.MinConnections);
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);
            _subject.UsedCount.Should().Be(1);

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_increase_count_up_to_the_max_number_of_connections(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            _capturedEvents.Clear();

            var connections = new List<IConnection>();

            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                IConnection connection;
                if (async)
                {
                    connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    connection = _subject.AcquireConnection(CancellationToken.None);
                }
                connections.Add(connection);
            }

            _subject.AvailableCount.Should().Be(0);
            _subject.CreatedCount.Should().Be(_settings.MaxConnections);
            _subject.DormantCount.Should().Be(0);
            _subject.UsedCount.Should().Be(_settings.MaxConnections);

            for (int i = 0; i < _settings.MinConnections; i++)
            {
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
            }
            for (int i = _settings.MinConnections; i < _settings.MaxConnections; i++)
            {
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddingConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
            }
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquiredConnection_should_return_connections_to_the_pool_when_disposed(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();

            IConnectionHandle connection;
            if (async)
            {
                connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                connection = _subject.AcquireConnection(CancellationToken.None);
            }

            _capturedEvents.Clear();

            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);
            connection.Dispose();
            _subject.DormantCount.Should().Be(_settings.MinConnections);

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquiredConnection_should_not_return_connections_to_the_pool_when_disposed_and_expired(
            [Values(false, true)]
            bool async)
        {
            var createdConnections = new List<MockConnection>();
            _mockConnectionFactory.Setup(f => f.CreateConnection(_serverId, _endPoint))
                .Returns(() =>
                {
                    var conn = new MockConnection(_serverId);
                    createdConnections.Add(conn);
                    return conn;
                });

            InitializeAndWait();

            IConnectionHandle connection;
            if (async)
            {
                connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                connection = _subject.AcquireConnection(CancellationToken.None);
            }

            _capturedEvents.Clear();

            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);

            createdConnections.ForEach(c => c.IsExpired = true);

            connection.Dispose();
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_throw_a_TimeoutException_when_all_connections_are_checked_out(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            var connections = new List<IConnection>();
            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                IConnection connection;
                if (async)
                {
                    connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    connection = _subject.AcquireConnection(CancellationToken.None);
                }
                connections.Add(connection);
            }
            _capturedEvents.Clear();

            Action act;
            if (async)
            {
                act = () => _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                act = () => _subject.AcquireConnection(CancellationToken.None);
            }

            act.ShouldThrow<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquiredConnection_should_not_throw_exceptions_when_disposed_after_the_pool_was_disposed(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            IConnectionHandle connection1;
            IConnectionHandle connection2;
            if (async)
            {
               connection1 = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
               connection2 = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                connection1 = _subject.AcquireConnection(CancellationToken.None);
                connection2 = _subject.AcquireConnection(CancellationToken.None);
            }
            _capturedEvents.Clear();

            connection1.Dispose();
            _subject.Dispose();

            Action act = () => connection2.Dispose();
            act.ShouldNotThrow();

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolClosedEvent>();
            // no connection pool events exist for the disposed connection2
            _capturedEvents.Any().Should().BeFalse();
        }

        [Fact]
        public void Clear_should_throw_an_InvalidOperationException_if_not_initialized()
        {
            Action act = () => _subject.Clear();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void Clear_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.Clear();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public void Clear_should_cause_existing_connections_to_be_expired(
            [Values(false, true)]
            bool async)
        {
            _subject.Initialize();

            IConnectionHandle connection;
            if (async)
            {
                connection = _subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
            else
            {
                connection = _subject.AcquireConnection(CancellationToken.None);
            }

            connection.IsExpired.Should().BeFalse();
            _subject.Clear();
            connection.IsExpired.Should().BeTrue();
        }

        [Fact]
        public void Initialize_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Fact]
        public void Initialize_should_scale_up_the_number_of_connections_to_min_size()
        {
            _subject.CreatedCount.Should().Be(0);
            _subject.DormantCount.Should().Be(0);
            InitializeAndWait();

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolOpeningEvent>();
            for (int i = 0; i < _settings.MinConnections; i++)
            {
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddingConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();
            }
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolOpenedEvent>();
        }

        private void InitializeAndWait()
        {
            _subject.Initialize();

            SpinWait.SpinUntil(
                () => _subject.CreatedCount == _settings.MinConnections &&
                    _subject.AvailableCount == _settings.MaxConnections &&
                    _subject.DormantCount == _settings.MinConnections &&
                    _subject.UsedCount == 0,
                TimeSpan.FromSeconds(5))
                .Should().BeTrue();

            _subject.AvailableCount.Should().Be(_settings.MaxConnections);
            _subject.CreatedCount.Should().Be(_settings.MinConnections);
            _subject.DormantCount.Should().Be(_settings.MinConnections);
            _subject.UsedCount.Should().Be(0);
        }
    }
}