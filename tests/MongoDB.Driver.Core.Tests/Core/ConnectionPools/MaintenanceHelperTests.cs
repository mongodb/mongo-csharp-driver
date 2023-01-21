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
using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Core.Tests.Core.ConnectionPools
{
    [Trait("Category", "Pool")]
    public class MaintenanceHelperTests
    {
        private static readonly TimeSpan __dummyInterval = TimeSpan.FromMinutes(1);
        private static readonly int __dummyPoolGeneration = 1;
        private static readonly DnsEndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), __endPoint);

        [Fact]
        public void Constructor_should_throw_if_connectionPool_is_null()
        {
            Record.Exception(() => new MaintenanceHelper(connectionPool: null, __dummyInterval)).Should().BeOfType<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_should_stop_maintenace_thread()
        {
            var pool = CreatePool();
            var subject = pool._maintenanceHelper();

            var createdThread = subject._maintenanceThread();

            subject.Dispose();
            Thread.Sleep(50);

            createdThread.ThreadState.Should().Be(ThreadState.Stopped);
        }

        [Theory]
        [ParameterAttributeData]
        public void MaintenanceHelper_should_not_leak_threads_after_stopping([RandomSeed(new[] { 0 })] int seed)
        {
            const int attempts = 100;
            var usedThreads = new HashSet<Thread>();
            var random = new Random(seed);

            using (var pool = CreatePool())
            using (var subject = CreateSubject(pool: pool))
            {
                subject.Start(); // run initial maintenance thread

                for (int i = 0; i < attempts; i++)
                {
                    StepByState(subject, random.Next(maxValue: 2)); // 0 - SetReady, 1 - Clear
                };
                Thread.Sleep(random.Next(50));
                StepByState(subject, state: 0); // 0 - SetReady, should be stopped by dispose
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));

            foreach (var usedThread in usedThreads)
            {
                usedThread.ThreadState.Should().Be(ThreadState.Stopped);
            }

            usedThreads.Count.Should().BeLessOrEqualTo(attempts);
            void StepByState(MaintenanceHelper helper, int state)
            {
                if (state == 0)
                {
                    helper.Start();
                    usedThreads.Add(helper._maintenanceThread()); // might be with duplicates
                }
                else
                {
                    helper.Stop(maxGenerationToReap: null);
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
                subject.Stop(__dummyPoolGeneration);
                subject.IsRunning.Should().BeFalse();
            }
            subject.IsRunning.Should().BeFalse();
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
                createdThread.ManagedThreadId.Should().Be(subject._maintenanceThread().ManagedThreadId); // same thread
            }
        }

        [Theory]
        [ParameterAttributeData]
        public void Stop_should_trigger_immidiate_maintenace_call(
            [Values(false, true)] bool checkOutConnection,
            [Values(false, true)] bool closeInUseConnection)
        {
            var eventCapturer = new EventCapturer()
                .Capture<ConnectionPoolAddedConnectionEvent>()
                .Capture<ConnectionPoolRemovedConnectionEvent>();

            int connectionId = 0;
            using (var pool = CreatePool(
                eventCapturer,
                minPoolSize: 1,
                connectionFactoryConfigurator: (factory) =>
                {
                    factory
                        .Setup(f => f.CreateConnection(__serverId, __endPoint))
                        .Returns(() => new MockConnection(new ConnectionId(__serverId, ++connectionId), new ConnectionSettings(), eventCapturer));
                })) // use to ensure that Maintenance attempt has been called
            {
                var subject = pool._maintenanceHelper();

                var maitenanceInPlayTimeout = TimeSpan.FromMilliseconds(50);
                eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolAddedConnectionEvent>(maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>().Which.ConnectionId.LongLocalValue.Should().Be(1);  // minPoolSize has been enrolled
                eventCapturer.Any().Should().BeFalse();

                SpinWait.SpinUntil(() => pool.ConnectionHolder._connections().Count > 0, TimeSpan.FromSeconds(1)).Should().BeTrue(); // wait until connection 1 has been returned to the pool after minPoolSize logic

                IConnection acquiredConnection = null;
                if (checkOutConnection)
                {
                    acquiredConnection = pool.AcquireConnection(CancellationToken.None);
                    acquiredConnection.ConnectionId.LongLocalValue.Should().Be(1);
                }

                IncrementGeneration(pool);
                subject.Stop(maxGenerationToReap: closeInUseConnection ? pool.Generation : null);

                var requestInPlayTimeout = TimeSpan.FromMilliseconds(100);
                if (!closeInUseConnection && checkOutConnection)
                {
                    // connection in progress should be not touched 
                    Thread.Sleep(requestInPlayTimeout);
                }
                else
                {
                    eventCapturer.WaitForOrThrowIfTimeout((events) => events.OfType<ConnectionPoolRemovedConnectionEvent>().Count() >= 1, requestInPlayTimeout);
                    eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>();
                }
                eventCapturer.Any().Should().BeFalse();

                pool.AvailableCount.Should().Be(checkOutConnection ? pool.Settings.MaxConnections - 1 : pool.Settings.MaxConnections);
                pool.CreatedCount.Should().Be(checkOutConnection ? 1 : 0);
                pool.DormantCount.Should().Be(0);
                pool.PendingCount.Should().Be(0);
                pool.UsedCount.Should().Be(checkOutConnection ? 1 : 0);
            }
        }

        // private methods
        private MaintenanceHelper CreateSubject(ExclusiveConnectionPool pool)
        {
            return new MaintenanceHelper(pool, __dummyInterval);
        }

        private ExclusiveConnectionPool CreatePool(
            IEventSubscriber eventCapturer = null,
            TimeSpan? maintenanceInterval = null,
            int minPoolSize = 0,
            Action<Mock<IConnectionFactory>> connectionFactoryConfigurator = null)
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            mockConnectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
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
                Mock.Of<IConnectionExceptionHandler>(),
                eventCapturer.ToEventLogger<LogCategories.Connection>());

            exclusiveConnectionPool.Initialize();
            exclusiveConnectionPool.SetReady(); // MaintenanceHelper is started

            return exclusiveConnectionPool;
        }

        private void IncrementGeneration(ExclusiveConnectionPool pool) => pool._generation(pool._generation() + 1);
    }

    internal static class MaintenanceHelperReflector
    {
        public static MaintenanceExecutingContext _maintenanceExecutingContext(this MaintenanceHelper maintenanceHelper) => (MaintenanceExecutingContext)Reflector.GetFieldValue(maintenanceHelper, nameof(_maintenanceExecutingContext));

        public static Thread _maintenanceThread(this MaintenanceHelper maintenanceHelper) => (Thread)Reflector.GetFieldValue(maintenanceHelper, nameof(_maintenanceThread));
    }
}
