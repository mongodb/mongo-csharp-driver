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
    public class ExclusiveConnectionPoolMaintenanceTests
    {
        private static readonly DnsEndPoint __endPoint = new DnsEndPoint("localhost", 27017);
        private static readonly ServerId __serverId = new ServerId(new ClusterId(), __endPoint);

        [Theory]
        [ParameterAttributeData]
        public void RequestCancel_should_trigger_immidiate_maintenace_call(
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
                subject.RequestStoppingMaintenance(closeInProgressConnection: closeInProgressConnection);

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
        public void RequestCancel_should_trigger_additional_request_when_there_is_in_progress_request()
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
                TimeSpan.FromMinutes(10), // First initial Maintenance attempt and then waiting
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
                var maitenanceInPlayTimeout = TimeSpan.FromMilliseconds(5000);
                eventCapturer.WaitForOrThrowIfTimeout(new[] { typeof(ConnectionPoolAddedConnectionEvent), typeof(ConnectionPoolAddedConnectionEvent) }, maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();  // minPoolSize has been enrolled
                eventCapturer.Next().Should().BeOfType<ConnectionPoolAddedConnectionEvent>();  // minPoolSize has been enrolled
                eventCapturer.Any().Should().BeFalse();

                // refresh _attemptsAfterCancellingRequested
                Reflector.SetFieldValue(Reflector.GetFieldValue(subject, "_maintenanceExecutingManager"), "_attemptsAfterCancellingRequested", 0);

                // emulate pool.clear (for connection 1 based on test setup)
                subject.RequestStoppingMaintenance(closeInProgressConnection: false); // only first connection is expired yet

                // 2. connection1 is being removed, but waiting removeConnectionTaskCompletionSource for completion
                var requestInPlayTimeout = TimeSpan.FromMilliseconds(1000);
                eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovingConnectionEvent>(maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>()
                    .Which.ConnectionId.LocalValue.Should().Be(1);  // connection 1 in the pool is being removed, but to finish it needs removedConnectionTaskCompletionSource
                Thread.Sleep(100); // ensure no more events
                eventCapturer.Any().Should().BeFalse(); // waiting until removeConnectionTaskCompletionSource

                connection2IsExpiredTaskCompletionSource.SetResult(true);  // connection 2 is expired

                // 3. emulate pool.clear (for connection 2 based on test setup)
                subject.RequestStoppingMaintenance(closeInProgressConnection: false);

                removedConnection1TaskCompletionSource.SetResult(true); // removing connection 1 is done

                eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovedConnectionEvent>(maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(1);  // connection 1 in the pool has been removed

                // new attempt to remove connection 2
                eventCapturer.WaitForEventOrThrowIfTimeout<ConnectionPoolRemovedConnectionEvent>(maitenanceInPlayTimeout);
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovingConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(2); 
                eventCapturer.Next().Should().BeOfType<ConnectionPoolRemovedConnectionEvent>().Which.ConnectionId.LocalValue.Should().Be(2);  // connection 2 in the pool has been removed
                eventCapturer.Any().Should().BeFalse();
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
        private void IncrementGeneration(ExclusiveConnectionPool pool)
        {
            var generation = pool._generation();
            pool._generation(++generation);
        }

        private ExclusiveConnectionPool CreatePool(
            EventCapturer eventCapturer,
            TimeSpan maintenanceInterval,
            int minPoolSize,
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
                new ConnectionPoolSettings(maintenanceInterval: maintenanceInterval, minConnections: minPoolSize),
                mockConnectionFactory.Object,
                eventCapturer,
                Mock.Of<IConnectionExceptionHandler>());

            exclusiveConnectionPool.Initialize();
            exclusiveConnectionPool.SetReady(); // MaintenanceHelper is started

            return exclusiveConnectionPool;
        }
    }
}
