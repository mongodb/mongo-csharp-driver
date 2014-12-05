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
using System.Net;
using System.Threading;
using FluentAssertions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.Helpers;
using NSubstitute;
using NUnit.Framework;

namespace MongoDB.Driver.Core.ConnectionPools
{
    [TestFixture]
    public class ExclusiveConnectionPoolTests
    {
        private IConnectionFactory _connectionFactory;
        private DnsEndPoint _endPoint;
        private IConnectionPoolListener _listener;
        private ServerId _serverId;
        private ConnectionPoolSettings _settings;
        private ExclusiveConnectionPool _subject;

        [SetUp]
        public void Setup()
        {
            _connectionFactory = Substitute.For<IConnectionFactory>();
            _endPoint = new DnsEndPoint("localhost", 27017);
            _listener = Substitute.For<IConnectionPoolListener>();
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
                _connectionFactory,
                _listener);
        }

        [Test]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(null, _endPoint, _settings, _connectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, null, _settings, _connectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, null, _connectionFactory, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void Constructor_should_throw_when_settings_is_connectionFactory()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, null, _listener);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Test]
        public void AcquireConnectionAsync_should_throw_an_InvalidOperationException_if_not_initialized()
        {
            Action act = () => _subject.AcquireConnectionAsync(CancellationToken.None).Wait();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void AcquireConnectionAsync_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.AcquireConnectionAsync(CancellationToken.None).Wait();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void AcquireConnectionAsync_should_return_a_connection()
        {
            InitializeAndWait();

            var connection = _subject.AcquireConnectionAsync(CancellationToken.None).Result;

            connection.Should().NotBeNull();
            _subject.AvailableCount.Should().Be(_settings.MaxConnections - 1);
            _subject.CreatedCount.Should().Be(_settings.MinConnections);
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);
            _subject.UsedCount.Should().Be(1);
        }

        [Test]
        public void AcquireConnectionAsync_should_increase_count_up_to_the_max_number_of_connections()
        {
            InitializeAndWait();

            var connections = new List<IConnection>();

            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                connections.Add(_subject.AcquireConnectionAsync(CancellationToken.None).Result);
            }

            _subject.AvailableCount.Should().Be(0);
            _subject.CreatedCount.Should().Be(_settings.MaxConnections);
            _subject.DormantCount.Should().Be(0);
            _subject.UsedCount.Should().Be(_settings.MaxConnections);
        }

        [Test]
        public void AcquiredConnections_should_return_connections_to_the_pool_when_disposed()
        {
            InitializeAndWait();

            var connection = _subject.AcquireConnectionAsync(CancellationToken.None).Result;
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);
            connection.Dispose();
            _subject.DormantCount.Should().Be(_settings.MinConnections);
        }

        [Test]
        public void AcquiredConnections_should_not_return_connections_to_the_pool_when_disposed_and_expired()
        {
            var createdConnections = new List<MockConnection>();
            _connectionFactory.CreateConnection(null, null).ReturnsForAnyArgs(c =>
            {
                var conn = new MockConnection(c.Arg<ServerId>());
                createdConnections.Add(conn);
                return conn;
            });

            InitializeAndWait();

            var connection = _subject.AcquireConnectionAsync(CancellationToken.None).Result;
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);

            createdConnections.ForEach(c => c.IsExpired = true);

            connection.Dispose();
            _subject.DormantCount.Should().Be(_settings.MinConnections - 1);
        }

        [Test]
        public void AcquireConnectionAsync_should_throw_a_TimeoutException_when_all_connections_are_checked_out()
        {
            InitializeAndWait();

            var connections = new List<IConnection>();
            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                connections.Add(_subject.AcquireConnectionAsync(CancellationToken.None).Result);
            }

            Action act = () => _subject.AcquireConnectionAsync(CancellationToken.None).Wait();

            act.ShouldThrow<TimeoutException>();
        }

        [Test]
        public void AcquiredConnections_should_not_throw_exceptions_when_disposed_after_the_pool_was_disposed()
        {
            InitializeAndWait();

            var connection1 = _subject.AcquireConnectionAsync(CancellationToken.None).Result;
            var connection2 = _subject.AcquireConnectionAsync(CancellationToken.None).Result;

            connection1.Dispose();
            _subject.Dispose();

            Action act = () => connection2.Dispose();
            act.ShouldNotThrow();
        }

        [Test]
        public void Clear_should_throw_an_InvalidOperationException_if_not_initialized()
        {
            Action act = () => _subject.Clear();

            act.ShouldThrow<InvalidOperationException>();
        }

        [Test]
        public void Clear_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.Clear();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Clear_should_cause_existing_connections_to_be_expired()
        {
            _subject.Initialize();

            var connection = _subject.AcquireConnectionAsync(CancellationToken.None).Result;

            connection.IsExpired.Should().BeFalse();
            _subject.Clear();
            connection.IsExpired.Should().BeTrue();
        }

        [Test]
        public void Initialize_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.Initialize();

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Test]
        public void Initialize_should_scale_up_the_number_of_connections_to_min_size()
        {
            _subject.CreatedCount.Should().Be(0);
            _subject.DormantCount.Should().Be(0);
            InitializeAndWait();
        }

        private void InitializeAndWait()
        {
            _subject.Initialize();

            SpinWait.SpinUntil(
                () => _subject.CreatedCount == _settings.MinConnections && 
                    _subject.AvailableCount == _settings.MaxConnections &&
                    _subject.DormantCount == _settings.MinConnections &&
                    _subject.UsedCount == 0,
                TimeSpan.FromSeconds(4));

            _subject.AvailableCount.Should().Be(_settings.MaxConnections);
            _subject.CreatedCount.Should().Be(_settings.MinConnections);
            _subject.DormantCount.Should().Be(_settings.MinConnections);
            _subject.UsedCount.Should().Be(0);
        }
    }
}