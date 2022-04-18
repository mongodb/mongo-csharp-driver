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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.ConnectionPools
{
    public class MaintenanceHelperTests
    {
        private static readonly TimeSpan __dummyInterval = TimeSpan.FromMinutes(1);
        private static readonly DnsEndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), __endPoint);

        [Fact]
        public void ctor_should_throw_if_connectionPool_is_null()
        {
            Record.Exception(() => new MaintenanceHelper(connectionPool: null, __dummyInterval)).Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Background_threads_should_not_have_leaks_after_stopping()
        {
            const int attempts = 100;
            var usedThreads = new List<Thread>();
            var saveThreadlock = new object();
            var random = new Random();

            using (var pool = CreatePool())
            using (var subject = CreateSubject(pool: pool))
            {
                subject.Start(); // run initial maintenance thread

                for (int i = 0; i < attempts; i++)
                {
                    StepByState(subject, random.Next(minValue: 0, maxValue: 2)); // 0 - SetReady, 1 - Clear
                };
                Thread.Sleep(100);
                StepByState(subject, state: 0); // 0 - SetReady, should be stopepd by dispose
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            foreach (var usedThread in usedThreads)
            {
                usedThread.ThreadState.Should().Be(ThreadState.Stopped);
            }

            void StepByState(MaintenanceHelper helper, int state)
            {
                lock (saveThreadlock)
                {
                    if (state == 0)
                    {
                        helper.Start();
                        usedThreads.Add(helper._maintenanceThread()); // might be with duplicates
                    }
                    else
                    {
                        var dummyCloseInUseConnections = false;
                        helper.RequestStoppingMaintenance(closeInUseConnections: dummyCloseInUseConnections);
                    }
                }
            }
        }

        [Fact]
        public void IsRunning_should_return_expected_result()
        {
            MaintenanceHelper subject;
            using (var pool = CreatePool())
            using (subject = CreateSubject(pool))
            {
                subject.IsRunning.Should().BeFalse();
                subject.Start();
                subject.IsRunning.Should().BeTrue();
                subject.RequestStoppingMaintenance(closeInUseConnections: false);
                subject.IsRunning.Should().BeFalse();
            }
            subject.IsRunning.Should().BeFalse();
        }

        [Theory]
        [ParameterAttributeData]
        public void RequestStoppingMaintenance_should_trigger_immidiate_maintenace_call(
            [Values(false, true)] bool checkOutConnection,
            [Values(false, true)] bool closeInProgressConnection)
        {
            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolAddedConnectionEvent>()
                .Capture<ConnectionPoolRemovedConnectionEvent>();

            using (var pool = CreatePool(
                eventCapturer,
                TimeSpan.FromMinutes(1), // First initial Maintenance attempt and then waiting
                minPoolSize: 1)) // use to ensure that Maintenance attempt has been called
            {
                var subject = pool._maintenanceHelper();

                var maitenanceInPlayTimeout = TimeSpan.FromMilliseconds(50);
                eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolAddedConnectionEvent>(maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();  // minPoolSize has been enrolled
                eventCapturer.Any().Should().BeFalse();

                if (checkOutConnection)
                {
                    _ = pool.AcquireConnection(CancellationToken.None);
                }

                IncrementGeneration(pool);
                subject.RequestStoppingMaintenance(closeInUseConnections: closeInProgressConnection);

                var requestInPlayTimeout = TimeSpan.FromMilliseconds(100);
                if (!closeInProgressConnection && checkOutConnection)
                {
                    // connection in progress should be not touched 
                    Thread.Sleep(requestInPlayTimeout);
                }
                else
                {
                    eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovedConnectionEvent>(requestInPlayTimeout);
                    eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
                }
                eventCapturer.Any().Should().BeFalse();
            }
        }

        [Fact]
        public void RequestStoppingMaintenance_should_trigger_additional_prune_when_there_is_in_progress_request()
        {
            var removedConnection1TaskCompletionSource = new TaskCompletionSource<bool>();
            var connection2IsExpiredTaskCompletionSource = new TaskCompletionSource<bool>();
            var formatterWithWaiting = new EventFormatterWithWaiting(removedConnection1TaskCompletionSource);

            var eventCapturer = new EventCapturer(formatterWithWaiting)
                .Capture<ConnectionPoolAddedConnectionEvent>()
                .Capture<ConnectionPoolRemovingConnectionEvent>()
                .Capture<ConnectionPoolRemovedConnectionEvent>();

            using (var pool = CreatePool(
                eventCapturer,
                TimeSpan.FromMilliseconds(1), // Run first attempt immidiatelly, set 1 minute period for the second attempt in below steps
                minPoolSize: 2,
                connectionFactoryConfigurator: (connectionFactoryMock) =>
                {
                    connectionFactoryMock
                        .SetupSequence(c => c.CreateConnection(__serverId, __endPoint))
                        .Returns(() => new MockConnection(new ConnectionId(__serverId, 1), new ConnectionSettings(), eventCapturer) { IsExpired = true })
                        .Returns(() => new MockConnection(new ConnectionId(__serverId, 2), new ConnectionSettings(), eventCapturer, connection2IsExpiredTaskCompletionSource));
                }))
            {
                var subject = pool._maintenanceHelper();

                // 1. connections 1 (expired) and 2 (not expired yet) are created
                var maitenanceInPlayTimeout = TimeSpan.FromMilliseconds(100);
                eventCapturer.WaitForOrThrowIfTimeout(
                    new[]
                    {
                        typeof(ConnectionPoolAddedConnectionEvent),
                        typeof(ConnectionPoolAddedConnectionEvent),
                        typeof(ConnectionPoolRemovingConnectionEvent)
                    },
                    maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();  // minPoolSize has been enrolled
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();  // minPoolSize has been enrolled
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(1);  // start removing connection 1. 
                eventCapturer.Any().Should().BeFalse();

                // 2. connection1 is being removed, but waiting removeConnectionTaskCompletionSource for completion
                var requestInPlayTimeout = TimeSpan.FromMilliseconds(100);
                Thread.Sleep(100); // ensure no more events
                eventCapturer.Any().Should().BeFalse(); // waiting until removeConnectionTaskCompletionSource

                // 3. Disable next regular maintenance calls
                Reflector.SetFieldValue(subject._maintenanceExecutingContext(), "_interval", TimeSpan.FromMinutes(1));

                connection2IsExpiredTaskCompletionSource.SetResult(true);  // connection 2 is expired

                // 4. emulate pool.clear (for connection 2 based on test setup)
                subject.RequestStoppingMaintenance(closeInUseConnections: false);
                removedConnection1TaskCompletionSource.SetResult(true); // removing connection 1 is done

                eventCapturer.WaitForOrThrowIfTimeout(capturer => capturer.OfType<ConnectionPoolRemovedConnectionEvent>().Count() >= 2, maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(1);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(2);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(2);  // connection 2 in the pool has been removed
                eventCapturer.Any().Should().BeFalse();
            }
        }

        [Fact]
        public void Start_should_not_create_thread_if_already_running()
        {
            using (var pool = CreatePool())
            using (var subject = CreateSubject(pool))
            {
                subject.Start();
                var createdThread = subject._maintenanceThread();
                subject.Start();
                createdThread.ManagedThreadId.Should().Be(createdThread.ManagedThreadId); // same thread
            }
        }

        private class EventFormatterWithWaiting : IEventFormatter
        {
            private readonly TaskCompletionSource<bool> _taskCompletionSource;

            public EventFormatterWithWaiting(TaskCompletionSource<bool> taskCompletionSource)
            {
                _taskCompletionSource = taskCompletionSource;
            }

            public object Format(object @event)
            {
                if (@event is ConnectionPoolRemovedConnectionEvent)
                {
                    var index = Task.WaitAny(_taskCompletionSource.Task, Task.Delay(TimeSpan.FromSeconds(5)));
                    if (index != 0)
                    {
                        throw new Exception("Waiting for ConnectionPoolRemovedConnectionEvent is too long.");
                    }
                }
                return @event;
            }
        }

        // private methods
        private MaintenanceHelper CreateSubject(ExclusiveConnectionPool pool)
        {
            return new MaintenanceHelper(pool, __dummyInterval);
        }

        private ExclusiveConnectionPool CreatePool(
            EventCapturer eventCapturer = null,
            TimeSpan? maintenanceInterval = null,
            int minPoolSize = 0,
            Action<Mock<IConnectionFactory>> connectionFactoryConfigurator = null)
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            if (connectionFactoryConfigurator == null)
            {
                mockConnectionFactory
                    .Setup(f => f.CreateConnection(__serverId, __endPoint))
                    .Returns(() => new MockConnection(__serverId, new ConnectionSettings(), eventCapturer));
            }
            else
            {
                connectionFactoryConfigurator(mockConnectionFactory);
            }

            var exclusiveConnectionPool = new ExclusiveConnectionPool(
                __serverId,
                __endPoint,
                new ConnectionPoolSettings(maintenanceInterval: maintenanceInterval.GetValueOrDefault(defaultValue: __dummyInterval), minConnections: minPoolSize),
                mockConnectionFactory.Object,
                eventCapturer ?? Mock.Of<IEventSubscriber>(),
                Mock.Of<IConnectionExceptionHandler>());

            exclusiveConnectionPool.Initialize();
            exclusiveConnectionPool.SetReady(); // MaintenanceHelper is started

            return exclusiveConnectionPool;
        }

        private void IncrementGeneration(ExclusiveConnectionPool pool)
        {
            var generation = pool._generation();
            pool._generation(++generation);
        }
    }

    public static class MaintenanceHelperReflector
    {
        internal static MaintenanceExecutingContext _maintenanceExecutingContext(this MaintenanceHelper maintenanceHelper)
        {
            return (MaintenanceExecutingContext)Reflector.GetFieldValue(maintenanceHelper, nameof(_maintenanceExecutingContext));
        }

        internal static Thread _maintenanceThread(this MaintenanceHelper maintenanceHelper)
        {
            return (Thread)Reflector.GetFieldValue(maintenanceHelper, nameof(_maintenanceThread));
        }
    }
}
