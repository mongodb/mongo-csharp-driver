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
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using static MongoDB.Driver.Core.ConnectionPools.ExclusiveConnectionPool;

namespace MongoDB.Driver.Core.ConnectionPools
{
    [Trait("Category", "Pool")]
    public class ExclusiveConnectionPoolTests : LoggableTestClass
    {
        private Mock<IConnectionFactory> _mockConnectionFactory;
        private Mock<IConnectionExceptionHandler> _mockConnectionExceptionHandler;
        private DnsEndPoint _endPoint;
        private EventCapturer _capturedEvents;
        private EventLogger<LogCategories.Connection> _eventLogger;
        private ServerId _serverId;
        private ConnectionPoolSettings _settings;
        private ExclusiveConnectionPool _subject;

        public ExclusiveConnectionPoolTests(ITestOutputHelper output) : base(output)
        {
            _mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            _mockConnectionExceptionHandler = new Mock<IConnectionExceptionHandler>();
            _endPoint = new DnsEndPoint("localhost", 27017);
            _capturedEvents = new EventCapturer();
            _eventLogger = _capturedEvents.ToEventLogger<LogCategories.Connection>();
            _serverId = new ServerId(new ClusterId(), _endPoint);

            _mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            _mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(() =>
                {
                    return new MockConnection();
                });
            _settings = new ConnectionPoolSettings(
                maintenanceInterval: TimeSpan.FromDays(1),
                maxConnections: 4,
                minConnections: 2,
                waitQueueSize: 1,
                waitQueueTimeout: TimeSpan.FromSeconds(2));

            _subject = CreateSubject();
        }

        [Fact]
        public void Constructor_should_throw_when_serverId_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(null, _endPoint, _settings, _mockConnectionFactory.Object, _mockConnectionExceptionHandler.Object, _eventLogger);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_endPoint_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, null, _settings, _mockConnectionFactory.Object, _mockConnectionExceptionHandler.Object, _eventLogger);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_settings_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, null, _mockConnectionFactory.Object, _mockConnectionExceptionHandler.Object, _eventLogger);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_connectionFactory_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, null, _mockConnectionExceptionHandler.Object, _eventLogger);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_should_throw_when_eventLogger_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, _mockConnectionFactory.Object, _mockConnectionExceptionHandler.Object, null);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void AcquireConnection_should_iterate_over_all_dormant_connections()
        {
            const int connectionsCount = 10;

            var settings = _settings.With(
                minConnections: 0,
                maxConnections: connectionsCount,
                waitQueueTimeout: TimeSpan.FromMilliseconds(20),
                maintenanceInterval: TimeSpan.FromMilliseconds(10000));

            var connectionsCreated = new HashSet<ConnectionId>();
            var connectionsExpired = new HashSet<ConnectionId>();
            var connectionsDisposed = new HashSet<ConnectionId>();

            var syncRoot = new object();

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
            subject.SetReady();

            // acquire all connections and return them
            var allConnections = Enumerable.Range(0, connectionsCount)
                .Select(i => subject.AcquireConnection(OperationContext.NoTimeout))
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

        [Fact]
        public void Constructor_should_throw_when_exceptionHandler_is_null()
        {
            Action act = () => new ExclusiveConnectionPool(_serverId, _endPoint, _settings, _mockConnectionFactory.Object, null, _eventLogger);

            act.ShouldThrow<ArgumentNullException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task AcquireConnection_should_throw_an_InvalidOperationException_if_not_initialized(
            [Values(false, true)]
            bool async)
        {
            _capturedEvents.Clear();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.AcquireConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.AcquireConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<InvalidOperationException>();
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            var connectionPoolCheckingOutConnectionFailedEvent = _capturedEvents.Next();
            var e = connectionPoolCheckingOutConnectionFailedEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
            e.Reason.Should().Be(ConnectionCheckOutFailedReason.ConnectionError);
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task AcquireConnection_should_throw_an_ObjectDisposedException_after_disposing(
            [Values(false, true)]
            bool async)
        {
            _capturedEvents.Clear();
            _subject.Dispose();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.AcquireConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.AcquireConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<ObjectDisposedException>();

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
        public async Task AcquireConnection_should_return_a_connection(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            _capturedEvents.Clear();

            var connection = async ?
                await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                _subject.AcquireConnection(OperationContext.NoTimeout);

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
        public async Task AcquireConnection_should_invoke_error_handling_before_releasing_maxConnectionsQueue(
            [Values(false, true)] bool async)
        {
            const int maxConnections = 1;
            ExclusiveConnectionPool subject = null;

            var connectionMock = new Mock<IConnection>();
            var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 1234)));
            var exception = new MongoConnectionException(connectionId, "Connection error");

            var mockConnectionExceptionHandler = new Mock<IConnectionExceptionHandler>();
            mockConnectionExceptionHandler
                .Setup(handler => handler.HandleExceptionOnOpen(It.IsAny<Exception>()))
                .Callback(() => subject.AvailableCount.Should().Be(maxConnections - 1));

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(() =>
                {
                    connectionMock
                         .Setup(c => c.ConnectionId)
                         .Returns(connectionId);
                    connectionMock
                         .Setup(c => c.Settings)
                         .Returns(new ConnectionSettings());
                    connectionMock
                        .Setup(c => c.Open(It.IsAny<OperationContext>()))
                        .Throws(exception);
                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Throws(exception);

                    return connectionMock.Object;
                });

            var settings = new ConnectionPoolSettings(maxConnections: maxConnections, maintenanceInterval: Timeout.InfiniteTimeSpan);
            subject = CreateSubject(
                connectionPoolSettings: settings,
                connectionFactory: mockConnectionFactory.Object,
                connectionExceptionHandler: mockConnectionExceptionHandler.Object);
            subject.Initialize();
            subject.SetReady();

            var resultException = async ?
                await Record.ExceptionAsync(() => subject.AcquireConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => subject.AcquireConnection(OperationContext.NoTimeout));

            resultException.Should().BeOfType<MongoConnectionException>();
            subject.AvailableCount.Should().Be(maxConnections);
            mockConnectionExceptionHandler.Verify(handler => handler.HandleExceptionOnOpen(exception), Times.Once);
        }

        [Theory]
        [ParameterAttributeData]
        internal async Task AcquireConnection_should_track_checked_out_reasons(
            [Values(CheckOutReason.Cursor, CheckOutReason.Transaction)] CheckOutReason reason,
            [Values(1, 3, 5)] int attempts,
            [Values(false, true)] bool async)
        {
            var subjectSettings = new ConnectionPoolSettings(minConnections: 0, maintenanceInterval: TimeSpan.FromDays(1));

            var connectionMock = new Mock<IConnection>();
            var connectionId = new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 1234)));
            connectionMock.SetupGet(c => c.ConnectionId).Returns(connectionId);

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(() => connectionMock.Object);

            var subject = CreateSubject(subjectSettings, connectionFactory: mockConnectionFactory.Object);

            InitializeAndWait(subject, subjectSettings);
            _capturedEvents.Clear();

            List<IConnectionHandle> connections = new();
            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                var connection = async ?
                    await subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                    subject.AcquireConnection(OperationContext.NoTimeout);
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
        public async Task AcquireConnection_should_increase_count_up_to_the_max_number_of_connections(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            _capturedEvents.Clear();

            var connections = new List<IConnection>();

            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                var connection = async ?
                    await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                    _subject.AcquireConnection(OperationContext.NoTimeout);
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
        public async Task AcquiredConnection_should_return_connections_to_the_pool_when_disposed(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();

            var connection = async ?
                await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                _subject.AcquireConnection(OperationContext.NoTimeout);

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
        public async Task AcquiredConnection_should_not_return_connections_to_the_pool_when_disposed_and_expired(
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

            var connection = async ?
                await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                _subject.AcquireConnection(OperationContext.NoTimeout);

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
        public async Task AcquireConnection_should_throw_a_TimeoutException_when_all_connections_are_checked_out(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            var connections = new List<IConnection>();
            for (int i = 0; i < _settings.MaxConnections; i++)
            {
                var connection = async ?
                    await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                    _subject.AcquireConnection(OperationContext.NoTimeout);
                connections.Add(connection);
            }
            _capturedEvents.Clear();

            var exception = async ?
                await Record.ExceptionAsync(() => _subject.AcquireConnectionAsync(OperationContext.NoTimeout)) :
                Record.Exception(() => _subject.AcquireConnection(OperationContext.NoTimeout));

            exception.Should().BeOfType<TimeoutException>();

            _capturedEvents.Next().Should().BeOfType<ConnectionPoolCheckingOutConnectionEvent>();
            var connectionPoolCheckingOutConnectionFailedEvent = _capturedEvents.Next();
            var e = connectionPoolCheckingOutConnectionFailedEvent.Should().BeOfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Subject;
            e.Reason.Should().Be(ConnectionCheckOutFailedReason.Timeout);
            _capturedEvents.Any().Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void AcquireConnection_should_timeout_when_non_sufficient_reused_connections(
            [Values(true, false)] bool async,
            [Values(1, 10, null)] int? maxConnectingOptional)
        {
            int maxConnecting = maxConnectingOptional ?? MongoInternalDefaults.ConnectionPool.MaxConnecting;
            int initalAcquiredCount = maxConnecting;
            int maxAcquiringCount = maxConnecting * 2;
            const int queueTimeoutMS = 50;

            var settings = _settings
                .With(waitQueueSize: maxAcquiringCount + initalAcquiredCount + maxConnecting,
                    maxConnections: maxAcquiringCount + initalAcquiredCount + maxConnecting,
                    waitQueueTimeout: TimeSpan.FromMilliseconds(queueTimeoutMS),
                    minConnections: 0,
                    maxConnecting: maxConnecting);

            var allAcquiringCountEvent = new CountdownEvent(maxAcquiringCount + initalAcquiredCount);
            var blockEstablishmentEvent = new ManualResetEventSlim(true);
            var establishingCount = new CountdownEvent(maxConnecting + initalAcquiredCount);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                        .Setup(c => c.Open(It.IsAny<OperationContext>()))
                        .Callback(() =>
                        {
                            if (establishingCount.CurrentCount > 0)
                            {
                                establishingCount.Signal();
                            }

                            blockEstablishmentEvent.Wait();
                        });
                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
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
            subject.SetReady();

            subject.PendingCount.Should().Be(0);
            var connectionsAcquired = Enumerable.Range(0, initalAcquiredCount)
                .Select(i => AcquireConnection(subject, async))
                .ToArray();

            // block further establishments
            blockEstablishmentEvent.Reset();

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

            expectedTimeouts.Should().Be(actualTimeouts);
            subject.PendingCount.Should().Be(0);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task AcquiredConnection_should_not_throw_exceptions_when_disposed_after_the_pool_was_disposed(
            [Values(false, true)]
            bool async)
        {
            InitializeAndWait();
            IConnectionHandle connection1;
            IConnectionHandle connection2;
            if (async)
            {
                connection1 = await _subject.AcquireConnectionAsync(OperationContext.NoTimeout);
                connection2 = await _subject.AcquireConnectionAsync(OperationContext.NoTimeout);
            }
            else
            {
                connection1 = _subject.AcquireConnection(OperationContext.NoTimeout);
                connection2 = _subject.AcquireConnection(OperationContext.NoTimeout);
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
        public void Acquire_and_release_connection_stress_test(
            [RandomSeed(new[] { 0 })] int seed,
            [Values(2, 10, 30)] int threadsCount,
            [Values(true, false, null)] bool? asyncOrRandom,
            [Values(0, 1, null)] int? minPoolSizeOrRandom)
        {
            var random = new Random(seed);

            const int iterations = 200;
            const int minEstablishingTime = 0;
            const int maxEstablishingTime = 50;

            var minPoolSize = minPoolSizeOrRandom ?? random.Next(2, 50);
            var maxPoolSize = Math.Max(minPoolSize, threadsCount + random.Next(0, 10));
            var waitQueueSize = random.Next(threadsCount, threadsCount * 5);
            var maintenanceInterval = TimeSpan.FromMilliseconds(random.Next(10, 40));
            var settings = _settings
                .With(minConnections: minPoolSize,
                    maxConnections: maxPoolSize,
                    waitQueueSize: waitQueueSize,
                    maintenanceInterval: maintenanceInterval,
                    maxConnecting: threadsCount);

            // random probabilities for open/clear/setready operations in [0..100] range
            var openOpMaxIndex = random.Next(10, 70);
            var clearOpMaxIndex = random.Next(openOpMaxIndex, 90);

            _capturedEvents.Clear();
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                        .Setup(c => c.Open(It.IsAny<OperationContext>()))
                        .Callback(() =>
                        {
                            var sleepMS = random.Next(minEstablishingTime, maxEstablishingTime);
                            Thread.Sleep(sleepMS);
                        });
                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Returns(async () =>
                        {
                            var sleepMS = random.Next(minEstablishingTime, maxEstablishingTime);
                            await Task.Delay(sleepMS);
                        });

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            var clearedCount = 0;
            var readyCount = 0;
            var checkingOutCount = 0;
            var checkoutFailedCount = 0;
            var checkoutSuccesfullCount = 0;

            ThreadingUtilities.ExecuteOnNewThreads(threadsCount, threadIndex =>
            {
                for (int i = 0; i < iterations; i++)
                {
                    var operationIndex = random.Next(0, 100);

                    if (operationIndex < openOpMaxIndex)
                    {
                        try
                        {
                            Interlocked.Increment(ref checkingOutCount);

                            var async = asyncOrRandom ?? random.NextDouble() < 0.5;
                            using var connection = AcquireConnection(subject, async);

                            subject.AvailableCount.Should().BeLessOrEqualTo(maxPoolSize);
                            subject.UsedCount.Should().BeGreaterOrEqualTo(1);

                            Interlocked.Increment(ref checkoutSuccesfullCount);
                        }
                        catch (MongoConnectionPoolPausedException)
                        {
                            Interlocked.Increment(ref checkoutFailedCount);
                        }
                    }
                    else if (operationIndex < clearOpMaxIndex)
                    {
                        subject.Clear(closeInUseConnections: random.NextDouble() < 0.5);
                        Interlocked.Increment(ref clearedCount);
                    }
                    else
                    {
                        subject.SetReady();
                        Interlocked.Increment(ref readyCount);
                    }
                }
            });

            var actualCleared = subject.Generation;
            actualCleared.Should().BeInRange(0, clearedCount);

            CountEvents<ConnectionPoolCheckingOutConnectionEvent>().Should().Be(checkingOutCount);
            CountEvents<ConnectionPoolCheckingOutConnectionFailedEvent>().Should().Be(checkoutFailedCount);
            CountEvents<ConnectionPoolCheckedOutConnectionEvent>().Should().Be(checkoutSuccesfullCount);
            CountEvents<ConnectionPoolCheckedInConnectionEvent>().Should().Be(checkoutSuccesfullCount);
            CountEvents<ConnectionPoolClearedEvent>().Should().Be(actualCleared);
            CountEvents<ConnectionPoolReadyEvent>().Should().BeInRange(actualCleared, actualCleared + 1);

            int CountEvents<T>() => _capturedEvents.Events.OfType<T>().Count();
        }

        [Fact]
        public void Clear_should_throw_an_InvalidOperationException_if_not_initialized()
        {
            Action act = () => _subject.Clear(closeInUseConnections: false);

            act.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void Clear_should_throw_an_ObjectDisposedException_after_disposing()
        {
            _subject.Dispose();

            Action act = () => _subject.Clear(closeInUseConnections: false);

            act.ShouldThrow<ObjectDisposedException>();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Clear_should_cause_existing_connections_to_be_expired(
            [Values(false, true)]
            bool async)
        {
            _subject.Initialize();
            _subject.SetReady();

            var connection = async ?
                await _subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                _subject.AcquireConnection(OperationContext.NoTimeout);

            connection.IsExpired.Should().BeFalse();
            _subject.Clear(closeInUseConnections: false);
            connection.IsExpired.Should().BeTrue();
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Clear_with_serviceId_should_cause_only_expected_connections_to_be_expired(
            [Values(false, true)] bool async)
        {
            var serviceId = ObjectId.GenerateNewId();
            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            var connectionId = new ConnectionId(_serverId);
            var connectionMock = new Mock<IConnection>();
            connectionMock
                .SetupGet(c => c.Description)
                .Returns(
                    new ConnectionDescription(
                        new ConnectionId(_serverId),
                        new HelloResult(new BsonDocument("serviceId", serviceId).Add("maxWireVersion", WireVersion.Server50))));
            connectionMock
                .SetupGet(c => c.ConnectionId)
                .Returns(connectionId);
            connectionMock
                .SetupGet(c => c.Settings)
                .Returns(new ConnectionSettings());

            mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns(connectionMock.Object);

            var connectionPoolSettings = new ConnectionPoolSettings(minConnections: 0).WithInternal(isPausable: false);
            var subject = CreateSubject(connectionPoolSettings: connectionPoolSettings, connectionFactory: mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            var connection = async ?
                await subject.AcquireConnectionAsync(OperationContext.NoTimeout) :
                subject.AcquireConnection(OperationContext.NoTimeout);

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
            _capturedEvents.Next().Should().BeOfType<ConnectionPoolReadyEvent>();

            for (int i = 0; i < _settings.MinConnections; i++)
            {
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddingConnectionEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionCreatedEvent>();
                _capturedEvents.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void In_use_marker_should_work_as_expected(
            [Values(0, 1)] int minPoolSize,
            [Values(false, true)] bool openConnectionFailed,
            [Values(true, false)] bool async)
        {
            var timeout = TimeSpan.FromSeconds(5);
            var acquiredCompletionSource = new TaskCompletionSource<bool>();

            var openException = new MongoConnectionException(new ConnectionId(_serverId), "OpenConnection failed");

            var settings = _settings.With(minConnections: minPoolSize, waitQueueTimeout: TimeSpan.FromMinutes(1) /* no op */);
            _capturedEvents = new EventCapturer();
            int connectionIndex = 0;
            _mockConnectionFactory
                .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
                .Returns<ServerId, EndPoint>((serverId, endpoint) =>
                {
                    var ci = ++connectionIndex;
                    var mockConnection = new Mock<IConnection>();
                    mockConnection.SetupGet(c => c.ConnectionId).Returns(new ConnectionId(serverId, ci));
                    mockConnection
                        .Setup(c => c.Open(It.IsAny<OperationContext>()))
                        .Callback(() =>
                        {
                            if (minPoolSize == 0 || ci == 2) // ignore connection 1 created in minPoolSize logic
                            {
                                acquiredCompletionSource.Task.WaitOrThrow(timeout);
                                if (openConnectionFailed)
                                {
                                    throw openException;
                                }
                            }
                        });

                    mockConnection
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Returns(async () =>
                        {
                            if (minPoolSize == 0 || ci == 2) // ignore connection 1 created in minPoolSize logic
                            {
                                await acquiredCompletionSource.Task.WithTimeout(timeout);
                                if (openConnectionFailed)
                                {
                                    throw openException;
                                }
                            }
                        });

                    return mockConnection.Object;
                });

            _subject = CreateSubject(connectionPoolSettings: settings);
            InitializeAndWait(poolSettings: settings);

            // initial state
            // MinPoolSize creates collections and immediately returns them to the pool, so no "in use" connections should be after first maintenance iteration
            ValidateConnectionsCount(inUse: false, expectedCount: settings.MinConnections);
            ValidateConnectionsCount(inUse: true, expectedCount: 0);

            IConnection connection = null;
            IConnection prepopulatedConnection = null;
            Exception acquireException = null;
            _ = Task.Run(() =>
            {
                if (minPoolSize > 0)
                {
                    prepopulatedConnection = AcquireConnection(_subject, async);
                }

                try
                {
                    connection = AcquireConnection(_subject, async);
                }
                catch (Exception ex)
                {
                    acquireException = ex;
                }
            });

            SpinWait.SpinUntil(() => _subject.ConnectionHolder._connectionsInUse().Count >= settings.MinConnections + 1, timeout).Should().BeTrue();

            // During First acquire
            ValidateConnectionsCount(inUse: false, expectedCount: 0);
            ValidateConnectionsCount(inUse: true, expectedCount: settings.MinConnections + 1);

            acquiredCompletionSource.SetResult(true); // connection is acquired
            SpinWait.SpinUntil(() => connection != null || acquireException != null, timeout).Should().BeTrue();

            ValidateConnectionsCount(inUse: false, expectedCount: 0);
            ValidateConnectionsCount(inUse: true, expectedCount: settings.MinConnections + (openConnectionFailed ? 0 : 1));

            if (openConnectionFailed)
            {
                if (minPoolSize > 0)
                {
                    prepopulatedConnection.Dispose();
                    ValidateConnectionsCount(inUse: false, expectedCount: settings.MinConnections);
                    ValidateConnectionsCount(inUse: true, expectedCount: 0);
                }

                connection.Should().BeNull();
                acquireException.Should().Be(openException);
            }
            else
            {
                // return connections
                if (minPoolSize > 0)
                {
                    prepopulatedConnection.Dispose();
                    ValidateConnectionsCount(inUse: false, expectedCount: settings.MinConnections);
                    ValidateConnectionsCount(inUse: true, expectedCount: 1);
                }

                connection.Dispose();
                ValidateConnectionsCount(inUse: false, expectedCount: settings.MinConnections + 1);
                ValidateConnectionsCount(inUse: true, expectedCount: 0);
                acquireException.Should().BeNull();
            }
        }

        [Fact]
        public void Maintenance_should_call_connection_dispose_when_connection_authentication_fail()
        {
            var connectionId = new ConnectionId(_serverId);
            var authenticationException = new MongoAuthenticationException(connectionId, "test message");
            var authenticationFailedConnection = new Mock<IConnection>();
            authenticationFailedConnection
                .Setup(c => c.Open(It.IsAny<OperationContext>())) // an authentication exception is thrown from _connectionInitializer.InitializeConnection
                                                                   // that in turn is called from OpenAsync
                .Throws(authenticationException);
            authenticationFailedConnection.SetupGet(c => c.ConnectionId).Returns(connectionId);

            _mockConnectionExceptionHandler
                .Setup(handler => handler.HandleExceptionOnOpen(authenticationException))
                .Callback(() => _subject.Clear(closeInUseConnections: false));

            using (var subject = CreateSubject())
            {
                _mockConnectionFactory
                    .Setup(f => f.CreateConnection(_serverId, _endPoint))
                    .Returns(() => authenticationFailedConnection.Object);

                subject.Initialize();
                subject.SetReady();

                var maintenanceHelper = subject._maintenanceHelper();
                SpinWait.SpinUntil(() => !maintenanceHelper.IsRunning, 1000);

                authenticationFailedConnection.Verify(conn => conn.Dispose(), Times.Once);
                _mockConnectionExceptionHandler.Verify(c => c.HandleExceptionOnOpen(authenticationException), Times.Once);
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Maintenance_should_run_with_finite_maintenanceInterval(
            [Values(0, 1, 1000)] int intervalMilliseconds)
        {
            var settings = _settings.With(maintenanceInterval: TimeSpan.FromMilliseconds(intervalMilliseconds));

            using var subject = CreateSubject(settings);

            subject.Initialize();
            subject.SetReady();

            subject._maintenanceHelper().IsRunning.Should().BeTrue();
        }

        [Fact]
        public void Maintenance_should_not_run_with_infinite_maintenanceInterval()
        {
            var settings = _settings.With(maintenanceInterval: Timeout.InfiniteTimeSpan);

            using var subject = CreateSubject(settings);

            subject.Initialize();
            subject.SetReady();

            subject._maintenanceHelper().IsRunning.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void MaxConnecting_queue_should_be_cleared_on_pool_clear(
           [Values(true, false)] bool isAsync,
           [Values(1, 2, 5)] int blockedInMaxConnecting)
        {
            const int maxConnecting = 2;
            int threadsCount = maxConnecting + blockedInMaxConnecting;
            var settings = _settings
                .With(minConnections: 0,
                    maxConnections: threadsCount,
                    waitQueueSize: threadsCount,
                    waitQueueTimeout: TimeSpan.FromMinutes(10),
                    maxConnecting: maxConnecting);

            var allEstablishing = new CountdownEvent(maxConnecting);
            var allInQueueFailed = new CountdownEvent(blockedInMaxConnecting);
            var blockEstablishmentEvent = new ManualResetEventSlim(false);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                       .Setup(c => c.Open(It.IsAny<OperationContext>()))
                       .Callback(() =>
                       {
                           allEstablishing.Signal();
                           blockEstablishmentEvent.Wait();
                       });

                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Returns(() =>
                        {
                            allEstablishing.Signal();
                            blockEstablishmentEvent.Wait();
                            return Task.FromResult(1);
                        });

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            var exceptions = ThreadingUtilities.ExecuteOnNewThreadsCollectExceptions(threadsCount + 1, threadIndex =>
            {
                if (threadIndex < threadsCount)
                {
                    try
                    {
                        using var connection = AcquireConnection(subject, isAsync);
                    }
                    catch (MongoConnectionPoolPausedException)
                    {
                        allInQueueFailed.Signal();
                        throw;
                    }
                }
                else
                {
                    // wait until maxConnecting connection are being established
                    allEstablishing.Wait();

                    // clear, all in maxConnecting queue should fail
                    subject.Clear(closeInUseConnections: false);

                    // unblock after all in maxConnecting queue failed
                    allInQueueFailed.Wait();
                    blockEstablishmentEvent.Set();
                };
            });

            exceptions.Length.ShouldBeEquivalentTo(blockedInMaxConnecting);
            foreach (var e in exceptions)
            {
                e.Should().BeOfType<MongoConnectionPoolPausedException>();
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Prune_should_respect_generation_when_closing_inUse_connections(
            [RandomSeed(new[] { 0 })] int seed,
            [Values(
                new[] { 1, 10 },
                new[] { 9, 10 },
                new[] { 50, 100 },
                new[] { 90, 100 })] int[] generationInfo)
        {
            int maxExpiredGeneration = generationInfo[0];
            int maxGeneration = generationInfo[1];

            var random = new Random(seed);
            int connectionIndex = 0;
            var connectionsCount = maxGeneration + 1; // generations are 0-based
            var connectionsToBeRemovedCount = maxExpiredGeneration + 1; // generations are 0-based

            var capturedEvents = new EventCapturer().Capture<ConnectionPoolRemovedConnectionEvent>();
            var poolSettings = _settings.With(
                maintenanceInterval: TimeSpan.FromMinutes(1),
                minConnections: 0,
                maxConnections: connectionsCount,
                waitQueueTimeout: TimeSpan.FromMinutes(1));

            var mockConnectionFactory = new Mock<IConnectionFactory> { DefaultValue = DefaultValue.Mock };
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
            mockConnectionFactory
              .Setup(c => c.CreateConnection(It.IsAny<ServerId>(), It.IsAny<EndPoint>()))
              .Returns<ServerId, EndPoint>(CreateConnection);

            var subject = CreateSubject(poolSettings, mockConnectionFactory.Object, eventCapturer: capturedEvents);
            InitializeAndWait(subject, poolSettings);

            // Acquire connections with incrementing generations
            // allConnections[i].Generation == i;
            var allConnections = new List<IConnection>();
            for (int i = 0; i < connectionsCount; i++)
            {
                var connection = AcquireConnection(subject, random.NextDouble() > 0.5);
                connection.Generation.Should().Be(i);

                allConnections.Add(connection);
                subject._generation(subject.Generation + 1);
            }

            // All connections in-use are expired
            // connections with generation <= maxExpiredGeneration should be removed by next Prune
            // connections with generation > maxExpiredGeneration should be removed only upon return
            subject._generation(maxExpiredGeneration);
            subject.Clear(true);

            capturedEvents.WaitForOrThrowIfTimeout(events => events.Count() >= connectionsToBeRemovedCount, TimeSpan.FromSeconds(5));
            var removedConnections = new HashSet<long>(
                capturedEvents
                    .Events
                    .OfType<ConnectionPoolRemovedConnectionEvent>()
                    .Select(c => c.ConnectionId.LongLocalValue));

            foreach (var connection in allConnections)
            {
                connection.IsExpired.Should().BeTrue();

                removedConnections
                    .Contains(connection.ConnectionId.LongLocalValue)
                    .Should().Be(connection.Generation <= maxExpiredGeneration);
            }

            IConnection CreateConnection(ServerId serverId, EndPoint _)
            {
                var connection = new Mock<IConnection>();
                connection.SetupGet(c => c.ConnectionId).Returns(new ConnectionId(_serverId, ++connectionIndex));
                connection.SetupGet(c => c.IsExpired).Returns(true);

                return connection.Object;
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
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
            subject.SetReady();

            SpinWait.SpinUntil(() => subject.DormantCount == connectionsCount, TimeSpan.FromSeconds(1));

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
                .Select(e => e.ConnectionId.LongLocalValue)
                .ShouldBeEquivalentTo(connectionsExpired.Select(c => c.LongLocalValue));

            _capturedEvents.Events
                .Take(connectionsCount * 2)
                .OfType<ConnectionPoolRemovedConnectionEvent>()
                .Select(e => e.ConnectionId.LongLocalValue)
                .ShouldAllBeEquivalentTo(connectionsExpired.Select(c => c.LongLocalValue));
        }

        [Theory]
        [ParameterAttributeData]
        public void WaitQueue_should_throw_when_full(
            [Values(true, false)] bool isAsync,
            [Values(1, 10)] int waitQueueSize)
        {
            var maxConnections = waitQueueSize + 1;
            var settings = _settings
                .With(minConnections: 0,
                    maxConnections: maxConnections,
                    waitQueueSize: waitQueueSize,
                    waitQueueTimeout: TimeSpan.FromSeconds(10),
                    maxConnecting: maxConnections);

            var blockEstablishmentEvent = new ManualResetEventSlim(false);
            var allAcquiringCountdownEvent = new CountdownEvent(waitQueueSize);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                       .Setup(c => c.Open(It.IsAny<OperationContext>()))
                       .Callback(() =>
                       {
                           allAcquiringCountdownEvent.Signal();
                           blockEstablishmentEvent.Wait();
                       });

                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Returns(() =>
                        {
                            allAcquiringCountdownEvent.Signal();
                            blockEstablishmentEvent.Wait();
                            return Task.FromResult(1);
                        });

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            subject._waitQueueFreeSlots().Should().Be(waitQueueSize);

            MongoWaitQueueFullException exception = null;

            ThreadingUtilities.ExecuteOnNewThreads(maxConnections, threadIndex =>
                {
                    if (threadIndex < waitQueueSize)
                    {
                        using var connection = AcquireConnection(subject, isAsync);
                    }
                    else
                    {
                        allAcquiringCountdownEvent.Wait();

                        try
                        {
                            using var connection = AcquireConnection(subject, isAsync);
                        }
                        catch (MongoWaitQueueFullException ex)
                        {
                            exception = ex;
                        }
                        finally
                        {
                            blockEstablishmentEvent.Set();
                        }
                    }
                });

            exception.Should().NotBeNull();
            subject._waitQueueFreeSlots().Should().Be(waitQueueSize);
        }

        [Theory]
        [ParameterAttributeData]
        public void WaitQueue_should_be_cleared_on_pool_clear(
            [Values(true, false)] bool isAsync,
            [Values(1, 2, 5)] int blockedInQueueCount)
        {
            const int maxConnecting = 2;
            var threadsCount = maxConnecting + blockedInQueueCount;
            var waitQueueSize = threadsCount;
            var settings = _settings
                .With(minConnections: 0,
                    maxConnections: maxConnecting,
                    waitQueueSize: waitQueueSize,
                    waitQueueTimeout: TimeSpan.FromMinutes(10),
                    maxConnecting: maxConnecting);

            var allEstablishing = new CountdownEvent(maxConnecting);
            var blockEstablishmentEvent = new ManualResetEventSlim(false);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                       .Setup(c => c.Open(It.IsAny<OperationContext>()))
                       .Callback(() =>
                       {
                           allEstablishing.Signal();
                           blockEstablishmentEvent.Wait();
                       });

                    connectionMock
                        .Setup(c => c.OpenAsync(It.IsAny<OperationContext>()))
                        .Returns(() =>
                        {
                            allEstablishing.Signal();
                            blockEstablishmentEvent.Wait();
                            return Task.FromResult(1);
                        });

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            var exceptions = ThreadingUtilities.ExecuteOnNewThreadsCollectExceptions(threadsCount + 1, threadIndex =>
            {
                if (threadIndex < threadsCount)
                {
                    using var connection = AcquireConnection(subject, isAsync);
                }
                else
                {
                    // wait until maxConnecting connection are being established
                    allEstablishing.Wait();

                    // wait until all are waiting to establish
                    SpinWait.SpinUntil(() => subject._waitQueueFreeSlots() == 0);

                    // pause the pool, blockedInQueueCount threads waiting to establish should observe MongoPoolPausedException exception
                    subject.Clear(closeInUseConnections: false);

                    SpinWait.SpinUntil(() => subject._waitQueueFreeSlots() >= blockedInQueueCount);
                    blockEstablishmentEvent.Set();
                };
            });

            exceptions.Length.ShouldBeEquivalentTo(blockedInQueueCount);
            foreach (var e in exceptions)
            {
                e.Should().BeOfType<MongoConnectionPoolPausedException>();
            }

            subject._waitQueueFreeSlots().Should().Be(waitQueueSize);
        }

        [Theory]
        [ParameterAttributeData]
        public void WaitQueue_should_release_slot_after_connection_checkout(
            [Values(true, false)] bool isAsync,
            [Values(1, 10)] int waitQueueSize)
        {
            var settings = _settings.With(
                waitQueueSize: waitQueueSize,
                maxConnections: waitQueueSize,
                minConnections: 0);

            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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

                    return connectionMock.Object;
                });

            using var subject = CreateSubject(settings, mockConnectionFactory.Object);
            subject.Initialize();
            subject.SetReady();

            subject._waitQueueFreeSlots().Should().Be(waitQueueSize);

            ThreadingUtilities.ExecuteOnNewThreads(waitQueueSize, threadIndex =>
            {
                using var connection = AcquireConnection(subject, isAsync);
            });

            subject._waitQueueFreeSlots().Should().Be(waitQueueSize);
        }

        // private methods
        private IConnection AcquireConnection(ExclusiveConnectionPool subject, bool async)
        {
            if (async)
            {
                return subject
                    .AcquireConnectionAsync(OperationContext.NoTimeout)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return subject.AcquireConnection(OperationContext.NoTimeout);
            }
        }

        private ExclusiveConnectionPool CreateSubject(
            ConnectionPoolSettings connectionPoolSettings = null,
            IConnectionFactory connectionFactory = null,
            IConnectionExceptionHandler connectionExceptionHandler = null,
            EventCapturer eventCapturer = null)
        {
            return new ExclusiveConnectionPool(
                _serverId,
                _endPoint,
                connectionPoolSettings ?? _settings,
                connectionFactory ?? _mockConnectionFactory.Object,
                connectionExceptionHandler ?? _mockConnectionExceptionHandler.Object,
                (eventCapturer ?? _capturedEvents).ToEventLogger<LogCategories.Connection>());
        }

        private void InitializeAndWait(ExclusiveConnectionPool pool = null, ConnectionPoolSettings poolSettings = null)
        {
            var connectionPool = pool ?? _subject;
            var connectionPoolSettings = poolSettings ?? _settings;
            connectionPool.Initialize();
            connectionPool.SetReady();

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

        private void ValidateConnectionsCount(bool inUse, int expectedCount, ExclusiveConnectionPool pool = null, params ConnectionId[] expectedConnectionIds)
        {
            if (expectedConnectionIds.Length > 0)
            {
                Ensure.That(expectedCount == expectedConnectionIds.Length, "ExpectedCount must be the same as expectedConnectionIds.Count");
            }

            var connectionHolder = (pool ?? _subject).ConnectionHolder;
            var connections = inUse ? connectionHolder._connectionsInUse() : connectionHolder._connections();
            connections.Should().HaveCount(expectedCount);
            for (int i = 0; i < connections.Count; i++)
            {
                PooledConnection connection = connections[i];
                if (expectedConnectionIds.Length > 0)
                {
                    connection.ConnectionId.Should().Be(expectedConnectionIds[i]);
                }
            }
        }
    }

    internal static class ExclusiveConnectionPoolReflector
    {
        public static int _generation(this ExclusiveConnectionPool obj) => (int)Reflector.GetFieldValue(obj, nameof(_generation));

        public static void _generation(this ExclusiveConnectionPool obj, int generation) => Reflector.SetFieldValue(obj, nameof(_generation), generation);

        public static int _waitQueueFreeSlots(this ExclusiveConnectionPool obj)
        {
            return (int)Reflector.GetFieldValue(obj, nameof(_waitQueueFreeSlots));
        }

        public static CheckOutReasonCounter _checkOutReasonCounter(this ExclusiveConnectionPool obj) => (CheckOutReasonCounter)Reflector.GetFieldValue(obj, nameof(_checkOutReasonCounter));

        public static MaintenanceHelper _maintenanceHelper(this ExclusiveConnectionPool obj)
        {
            return (MaintenanceHelper)Reflector.GetFieldValue(obj, nameof(_maintenanceHelper));
        }

        public static ServiceStates _serviceStates(this ExclusiveConnectionPool obj)
        {
            return (ServiceStates)Reflector.GetFieldValue(obj, nameof(_serviceStates));
        }

        public static void _connectionExceptionHandler(this ExclusiveConnectionPool server, IConnectionExceptionHandler connectionExceptionHandler)
        {
            Reflector.SetFieldValue(server, nameof(_connectionExceptionHandler), connectionExceptionHandler);
        }
    }

    internal static class ListConnectionHolderReflector
    {
        public static List<PooledConnection> _connections(this ListConnectionHolder obj) => (List<PooledConnection>)Reflector.GetFieldValue(obj, nameof(_connections));
        public static List<PooledConnection> _connectionsInUse(this ListConnectionHolder obj) => (List<PooledConnection>)Reflector.GetFieldValue(obj, nameof(_connectionsInUse));
    }
}
