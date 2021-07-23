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
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
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
            _mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(() =>
                {
                    var connectionMock = new Mock<IConnection>();
                    connectionMock
                        .Setup(c => c.Settings)
                        .Returns(new ConnectionSettings());
                    return connectionMock.Object;
                });
            _settings = new ConnectionPoolSettings(
                maintenanceInterval: Timeout.InfiniteTimeSpan,
                maxConnections: 4,
                minConnections: 2,
                waitQueueSize: 1,
                waitQueueTimeout: TimeSpan.FromSeconds(2));

            _subject = CreateSubject();
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

        [Fact]
        public void AcquireConnection_should_iterate_over_all_dormant_connections()
        {
            const int connectionsCount = 10;

            var settings = _settings.With(
                minConnections: 0,
                maxConnections: connectionsCount,
                waitQueueTimeout: TimeSpan.FromMilliseconds(1),
                maintenanceInterval: TimeSpan.FromMilliseconds(10000));

            var connectionsCreated = new HashSet<ConnectionId>();
            var connectionsExpired = new HashSet<ConnectionId>();
            var connectionsDisposed = new HashSet<ConnectionId>();

            var syncRoot = new object();

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory
                .Setup(f => f.CreateConnection(_serverId, _endPoint))
                .Returns(() =>
                {
                    var connectionMock = new Mock<IConnection>();

                    connectionMock
                        .Setup(c => c.ConnectionId)
                        .Returns(new ConnectionId(_serverId));

                    connectionMock
                        .Setup(c => c.Settings)
                        .Returns(new ConnectionSettings());

                    connectionMock
                        .Setup(c => c.IsExpired)
                        .Returns(() =>
                        {
                            lock (syncRoot)
                            {
                                return connectionsExpired.Contains(connectionMock.Object.ConnectionId);
                            }
                        });

                    connectionMock
                        .Setup(c => c.Dispose())
                        .Callback(() => connectionsDisposed.Add(connectionMock.Object.ConnectionId));

                    connectionsCreated.Add(connectionMock.Object.ConnectionId);

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();

            // acquire all connections and return them
            var allConnections = Enumerable.Range(0, connectionsCount)
                .Select(i => subject.AcquireConnection(default))
                .ToArray();

            var connectionNotToExpire = allConnections[allConnections.Length / 2].ConnectionId;

            foreach (var connection in allConnections)
            {
                connection.Dispose();
            }

            subject.DormantCount.Should().Be(connectionsCount);

            // expire all of the connections except one
            _capturedEvents.Clear();
            lock (syncRoot)
            {
                foreach (var connectionId in connectionsCreated)
                {
                    connectionsExpired.Add(connectionId);
                }

                connectionsExpired.Remove(connectionNotToExpire);
            }

            // acquire connection again, no new connections should be created, some expired connections should be removed
            AcquireConnection(subject, true).Should().NotBeNull();

            // ensure no new connections where created
            subject.DormantCount.Should().Be(connectionsCount - connectionsDisposed.Count - 1);
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_throw_an_InvalidOperationException_if_not_initialized(
            [Values(false, true)]
            bool async)
        {
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

            act.ShouldThrow<InvalidOperationException>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            var connectionPoolCheckingOutConnectionFailedEvent = _capturedEvents.Next();
            var e = connectionPoolCheckingOutConnectionFailedEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
            e.Reason.Should().Be(ConnectionCheckOutFailedReason.ConnectionError);
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_throw_an_ObjectDisposedException_after_disposing(
            [Values(false, true)]
            bool async)
        {
            _capturedEvents.Clear();
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
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolClosingEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolClosedEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            var connectionPoolCheckingOutConnectionFailedEvent = _capturedEvents.Next();
            var e = connectionPoolCheckingOutConnectionFailedEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
            e.Reason.Should().Be(ConnectionCheckOutFailedReason.PoolClosed);
            _capturedEvents.Any().Should().BeFalse();
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
        internal void AcquireConnection_should_track_checked_out_reasons(
            [Values(CheckOutReason.Cursor, CheckOutReason.Transaction)] CheckOutReason reason,
            [Values(1, 3, 5)] int attempts,
            [Values(false, true)] bool async)
        {
            var subjectSettings = new ConnectionPoolSettings(minConnections: 0);

            var mockConnectionFactory = Mock.Of<IConnectionFactory>(c => c.CreateConnection(_serverId, _endPoint) == Mock.Of<IConnection>());

            var subject = CreateSubject(subjectSettings, connectionFactory: mockConnectionFactory);

            InitializeAndWait(subject, subjectSettings);
            _capturedEvents.Clear();

            List<IConnectionHandle> connections = new();
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                IConnectionHandle connection;
                if (async)
                {
                    connection = subject.AcquireConnectionAsync(CancellationToken.None).GetAwaiter().GetResult();
                }
                else
                {
                    connection = subject.AcquireConnection(CancellationToken.None);
                }
                ((ICheckOutReasonTracker)connection).SetCheckOutReasonIfNotAlreadySet(reason);
                connections.Add(connection);

                connections.Should().HaveCount(attempt);
                subject._checkOutReasonCounter().GetCheckOutsCount(reason).Should().Be(attempt);
                foreach (var restItem in GetEnumItemsExcept(reason))
                {
                    subject._checkOutReasonCounter().GetCheckOutsCount(restItem).Should().Be(0);
                }

                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddingConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionCreatedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedOutConnectionEvent>();
            }

            _capturedEvents.Any().Should().BeFalse();

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                connections[attempt - 1].Dispose(); // return connection to the pool

                subject._checkOutReasonCounter().GetCheckOutsCount(reason).Should().Be(attempts - attempt);
                foreach (var restItem in GetEnumItemsExcept(reason))
                {
                    subject._checkOutReasonCounter().GetCheckOutsCount(restItem).Should().Be(0);
                }

                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            }
            _capturedEvents.Any().Should().BeFalse();

            IEnumerable<CheckOutReason> GetEnumItemsExcept(CheckOutReason reason)
            {
                foreach (var reasonItem in Enum.GetValues(typeof(CheckOutReason)).Cast<CheckOutReason>())
                {
                    if (reasonItem == reason)
                    {
                        continue;
                    }
                    yield return reasonItem;
                }
            }
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
                _capturedEvents.Next().Should().BeOfType<ConnectionCreatedEvent>();
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
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
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
            var connectionPoolCheckingOutConnectionFailedEvent = _capturedEvents.Next();
            var e = connectionPoolCheckingOutConnectionFailedEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
            e.Reason.Should().Be(ConnectionCheckOutFailedReason.Timeout);
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
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckedInConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AquireConnection_should_timeout_when_no_sufficient_reused_connections(
            [Values(true, false)]
            bool async)
        {
            int maxConnecting = MongoInternalDefaults.ConnectionPool.MaxConnecting;
            const int initalAcquiredCount = 2;
            const int maxAcquiringCount = 4;
            const int queueTimeoutMS = 50;

            var settings = _settings.With(
                waitQueueSize: maxAcquiringCount + initalAcquiredCount + maxConnecting,
                maxConnections: maxAcquiringCount + initalAcquiredCount + maxConnecting,
                waitQueueTimeout: TimeSpan.FromMilliseconds(queueTimeoutMS),
                minConnections: 0);

            var allAcquiringCountEvent = new CountdownEvent(maxAcquiringCount + initalAcquiredCount);
            var blockEstablishmentEvent = new ManualResetEventSlim(true);
            var establishingCount = new CountdownEvent(maxConnecting + initalAcquiredCount);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(() =>
                {
                    var connectionMock = new Mock<IConnection>();

                    connectionMock
                        .Setup(c => c.ConnectionId)
                        .Returns(new ConnectionId(_serverId));
                    connectionMock
                        .Setup(c => c.Settings)
                        .Returns(new ConnectionSettings());
                    connectionMock
                        .Setup(c => c.Open(It.IsAny<CancellationToken>()))
                        .Callback(() =>
                        {
                            if (establishingCount.CurrentCount > 0)
                            {
                                establishingCount.Signal();
                            }

                            blockEstablishmentEvent.Wait();
                        });
                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>()))
                        .Returns(() =>
                        {
                            if (establishingCount.CurrentCount > 0)
                            {
                                establishingCount.Signal();
                            }

                            blockEstablishmentEvent.Wait();
                            return Task.FromResult(0);
                        });

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();

            subject.PendingCount.Should().Be(0);
            var connectionsAcquired = Enumerable.Range(0, initalAcquiredCount)
                .Select(i => AcquireConnection(subject, async))
                .ToArray();

            // block further establishments
            blockEstablishmentEvent.Reset();

            var allConnections = new List<IConnection>();
            var actualTimeouts = 0;
            var expectedTimeouts = maxAcquiringCount - maxConnecting;

            ThreadingUtilities.ExecuteOnNewThreads(maxAcquiringCount + maxConnecting + 1, threadIndex =>
            {
                if (threadIndex < maxConnecting)
                {
                    // maximize maxConnecting
                    allAcquiringCountEvent.Signal();
                    AcquireConnection(subject, async);
                }
                else if (threadIndex < maxConnecting + maxAcquiringCount)
                {
                    // wait until all maxConnecting maximized
                    establishingCount.Wait();
                    subject.PendingCount.Should().Be(maxConnecting);

                    allAcquiringCountEvent.Signal();

                    try
                    {
                        AcquireConnection(subject, async);
                    }
                    catch (TimeoutException)
                    {
                        Interlocked.Increment(ref actualTimeouts);
                    }

                    // speedup the test
                    if (expectedTimeouts == actualTimeouts)
                    {
                        blockEstablishmentEvent.Set();
                    }
                }
                else
                {
                    // wait until all trying to acquire
                    allAcquiringCountEvent.Wait();

                    // return connections
                    foreach (var connection in connectionsAcquired)
                    {
                        connection.Dispose();
                    }
                }
            });

            expectedTimeouts.Should().Be(expectedTimeouts);
            subject.PendingCount.Should().Be(0);
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

        [Theory]
        [ParameterAttributeData]
        public void Clear_with_serviceId_should_cause_only_expected_connections_to_be_expired(
            [Values(false, true)] bool async)
        {
            var serviceId = ObjectId.GenerateNewId();
            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            var connectionMock = new Mock<IConnection>();
            connectionMock
                .SetupGet(c => c.Description)
                .Returns(
                    new ConnectionDescription(
                        new ConnectionId(_serverId),
                        new IsMasterResult(new BsonDocument("serviceId", serviceId)),
                        new BuildInfoResult(new BsonDocument("version", "5.0.0").Add("ok", 1))));
            connectionMock
                .SetupGet(c => c.Settings)
                .Returns(new ConnectionSettings());

            mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(connectionMock.Object);

            var subject = CreateSubject(connectionPoolSettings: new ConnectionPoolSettings(minConnections: 0), connectionFactory: mockConnectionFactory.Object);
            subject.Initialize();

            var connection = AcquireConnection(subject, async);

            connection.IsExpired.Should().BeFalse();
            var randomServiceId = ObjectId.GenerateNewId();
            subject.Clear(randomServiceId);
            connection.IsExpired.Should().BeFalse();
            subject._serviceStates().TryGetGeneration(connectionMock.Object.Description?.ServiceId, out _).Should().BeTrue();
            subject.Clear(serviceId);
            connection.IsExpired.Should().BeTrue();
            subject._serviceStates().TryGetGeneration(connectionMock.Object.Description?.ServiceId, out _).Should().BeTrue();
            connection.Dispose();
            subject._serviceStates().TryGetGeneration(connectionMock.Object.Description?.ServiceId, out _).Should().BeFalse();
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
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolOpenedEvent>();

            for (int i = 0; i < _settings.MinConnections; i++)
            {
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddingConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionCreatedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();
            }
        }

        [Fact]
        public void MaintainSizeAsync_should_call_connection_dispose_when_connection_authentication_fail()
        {
            var authenticationFailedConnection = new Mock<IConnection>();
            authenticationFailedConnection
                .Setup(c => c.OpenAsync(It.IsAny<CancellationToken>())) // an authentication exception is thrown from _connectionInitializer.InitializeConnection
                                                                        // that in turn is called from OpenAsync
                .Throws(new MongoAuthenticationException(new ConnectionId(_serverId), "test message"));

            using (var subject = CreateSubject())
            {
                _mockConnectionFactory
                    .Setup(f => f.CreateConnection(_serverId, _endPoint))
                    .Returns(() =>
                    {
                        subject._maintenanceCancellationTokenSource().Cancel(); // Task.Delay will be canceled 
                        return authenticationFailedConnection.Object;
                    });

                var _ = Record.Exception(() => subject.MaintainSizeAsync().GetAwaiter().GetResult());
                authenticationFailedConnection.Verify(conn => conn.Dispose(), Times.Once);
            }
        }

        [Fact]
        public void MaintainSizeAsync_should_not_try_new_attempt_after_failing_without_delay()
        {
            var settings = _settings.With(maintenanceInterval: TimeSpan.FromSeconds(10));

            using (var subject = CreateSubject(settings))
            {
                _mockConnectionFactory
                    .SetupSequence(f => f.CreateConnection(_serverId, _endPoint))
                    .Throws<Exception>()    // failed attempt
                    .Returns(() =>          // successful attempt which should be delayed
                    {
                        // break the loop. With this line the MaintainSizeAsync will contain only 2 iterations
                        subject._maintenanceCancellationTokenSource().Cancel();
                        return new MockConnection(_serverId);
                    });

                var testResult = Task.WaitAny(
                    subject.MaintainSizeAsync(),            // if this task is completed first, it will mean that there was no delay (10 sec) 
                    Task.Delay(TimeSpan.FromSeconds(1)));   // time to be sure that delay is happening,
                                                            // if the method is running more than 1 second, then delay is happening
                testResult.Should().Be(1);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void PrunePoolAsync_should_remove_all_expired_connections([RandomSeed] int seed)
        {
            const int connectionsCount = 10;

            var settings = _settings.With(
                minConnections: connectionsCount,
                maxConnections: connectionsCount,
                maintenanceInterval: TimeSpan.FromMilliseconds(10));

            var connectionsCreated = new HashSet<ConnectionId>();
            var connectionsExpired = new HashSet<ConnectionId>();
            var connectionsDisposed = new HashSet<ConnectionId>();

            var syncRoot = new object();

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory
                .Setup(f => f.CreateConnection(_serverId, _endPoint))
                .Returns(() =>
                {
                    var connectionMock = new Mock<IConnection>();

                    connectionMock
                        .Setup(c => c.ConnectionId)
                        .Returns(new ConnectionId(_serverId));

                    connectionMock
                        .Setup(c => c.Settings)
                        .Returns(new ConnectionSettings());

                    connectionMock
                        .Setup(c => c.IsExpired)
                        .Returns(() =>
                        {
                            lock (syncRoot)
                            {
                                return connectionsExpired.Contains(connectionMock.Object.ConnectionId);
                            }
                        });

                    connectionMock
                        .Setup(c => c.Dispose())
                        .Callback(() => connectionsDisposed.Add(connectionMock.Object.ConnectionId));

                    connectionsCreated.Add(connectionMock.Object.ConnectionId);

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();

            _capturedEvents.WaitForOrThrowIfTimeout(events => events.Where(e => e is ConnectionCreatedEvent).Count() >= connectionsCount, TimeSpan.FromSeconds(10));
            subject.DormantCount.Should().Be(connectionsCount);

            // expire some of the connections
            _capturedEvents.Clear();
            var random = new Random(seed);
            lock (syncRoot)
            {
                foreach (var connectionId in connectionsCreated)
                {
                    if (random.NextDouble() > 0.5)
                    {
                        connectionsExpired.Add(connectionId);
                    }
                }
            }

            // ensure removed events are received in subsequent order, meaning all expired connections where removed in same pass
            _capturedEvents.WaitForOrThrowIfTimeout(events => events.Count() >= connectionsExpired.Count * 2, TimeSpan.FromSeconds(10));

            _capturedEvents.Events
                .Take(connectionsCount * 2)
                .OfType<ConnectionPoolRemovingConnectionEvent>()
                .Select(e => e.ConnectionId.LocalValue)
                .ShouldBeEquivalentTo(connectionsExpired.Select(c => c.LocalValue));

            _capturedEvents.Events
                .Take(connectionsCount * 2)
                .OfType<ConnectionPoolRemovedConnectionEvent>()
                .Select(e => e.ConnectionId.LocalValue)
                .ShouldAllBeEquivalentTo(connectionsExpired.Select(c => c.LocalValue));
        }

        // private methods
        private IConnection AcquireConnection(ExclusiveConnectionPool subject, bool async)
        {
            if (async)
            {
                return subject
                    .AcquireConnectionAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return subject.AcquireConnection(CancellationToken.None);
            }
        }

        private ExclusiveConnectionPool CreateSubject(ConnectionPoolSettings connectionPoolSettings = null, IConnectionFactory connectionFactory = null)
        {
            return new ExclusiveConnectionPool(
                _serverId,
                _endPoint,
                connectionPoolSettings ?? _settings,
                connectionFactory ?? _mockConnectionFactory.Object,
                _capturedEvents);
        }

        private void InitializeAndWait(ExclusiveConnectionPool pool = null, ConnectionPoolSettings poolSettings = null)
        {
            var connectionPool = pool ?? _subject;
            var connectionPoolSettings = poolSettings ?? _settings;

            connectionPool.Initialize();

            SpinWait.SpinUntil(
                () =>
                    connectionPool.CreatedCount == connectionPoolSettings.MinConnections &&
                    connectionPool.AvailableCount == connectionPoolSettings.MaxConnections &&
                    connectionPool.DormantCount == connectionPoolSettings.MinConnections &&
                    connectionPool.UsedCount == 0,
                TimeSpan.FromSeconds(5))
                .Should()
                .BeTrue();

            connectionPool.AvailableCount.Should().Be(connectionPoolSettings.MaxConnections);
            connectionPool.CreatedCount.Should().Be(connectionPoolSettings.MinConnections);
            connectionPool.DormantCount.Should().Be(connectionPoolSettings.MinConnections);
            connectionPool.UsedCount.Should().Be(0);
        }
    }

    internal static class ExclusiveConnectionPoolReflector
    {
        public static CancellationTokenSource _maintenanceCancellationTokenSource(this ExclusiveConnectionPool obj)
        {
            return (CancellationTokenSource)Reflector.GetFieldValue(obj, nameof(_maintenanceCancellationTokenSource));
        }

        public static Task MaintainSizeAsync(this ExclusiveConnectionPool obj)
        {
            return (Task)Reflector.Invoke(obj, nameof(MaintainSizeAsync));
        }

        public static CheckOutReasonCounter _checkOutReasonCounter(this ExclusiveConnectionPool obj) => (CheckOutReasonCounter)Reflector.GetFieldValue(obj, nameof(_checkOutReasonCounter));

        public static ServiceStates _serviceStates(this ExclusiveConnectionPool obj)
        {
            return (ServiceStates)Reflector.GetFieldValue(obj, nameof(_serviceStates));
        }
    }
}
