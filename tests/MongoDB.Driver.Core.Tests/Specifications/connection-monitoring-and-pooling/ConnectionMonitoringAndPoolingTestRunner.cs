/* Copyright 2019-present MongoDB Inc.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.JsonDrivenTests;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Helpers;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace MongoDB.Driver.Specifications.connection_monitoring_and_pooling
{
    public class ConnectionMonitoringAndPoolingTestRunner
    {
        #region static
        private static readonly string[] __alwaysIgnoredEvents =
        {
            nameof(ConnectionPoolOpeningEvent),
            nameof(ConnectionPoolAddingConnectionEvent),
            nameof(ConnectionPoolAddedConnectionEvent),
            nameof(ConnectionPoolCheckingInConnectionEvent),
            nameof(ConnectionPoolRemovingConnectionEvent),
            nameof(ConnectionPoolRemovedConnectionEvent),
            nameof(ConnectionPoolClosingEvent),
            nameof(ConnectionPoolClearingEvent),
            nameof(ConnectionOpeningEvent),
            nameof(ConnectionClosingEvent)
        };
        #endregion

        [SkippableTheory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var connectionMap = new ConcurrentDictionary<string, IConnection>();
            var eventCapturer = new EventCapturer();
            var tasks = new ConcurrentDictionary<string, Task>();

            var test = testCase.Test;
            JsonDrivenHelper.EnsureAllFieldsAreValid(test, "_path", "version", "style", "description", "poolOptions", "operations", "error", "events", "ignore", "async");
            EnsureAvailableStyle(test);
            ResetConnectionId();

            var connectionPool = SetupConnectionPool(test, eventCapturer);
            connectionPool.Initialize();

            var operations = testCase.Test.GetValue("operations").AsBsonArray;
            var async = testCase.Test.GetValue("async").ToBoolean();
            Exception exception = null;
            foreach (var operation in operations.Cast<BsonDocument>())
            {
                ExecuteOperation(
                    operation,
                    eventCapturer,
                    connectionMap,
                    tasks,
                    connectionPool,
                    async,
                    out exception);

                if (exception != null)
                {
                    break;
                }
            }

            AssertError(test, exception);
            AssertEvents(test, eventCapturer);
        }

        // private methods
        private void AssertError(BsonDocument test, Exception ex)
        {
            var containsErrorNode = test.Contains("error");
            if (!containsErrorNode && ex != null)
            {
                throw new Exception("Unexpected exception has been thrown.", ex);
            }
            else if (containsErrorNode && ex == null)
            {
                throw new Exception($"The test was expected to throw an exception {test["error"]}, but no exception was thrown.");
            }
            else if (containsErrorNode)
            {
                var error = test["error"].AsBsonDocument;
                JsonDrivenHelper.EnsureAllFieldsAreValid(error, "type", "message");
                var exType = MapErrorTypeToExpected(ex, out var exMessage);

                var expectedExceptionType = error["type"].ToString();
                var expectedErrorMessage = error["message"].ToString();

                exType.Should().Be(expectedExceptionType);
                exMessage.Should().Be(expectedErrorMessage);
            }
        }

        private void AssertEvent(object actualEvent, BsonDocument expectedEvent)
        {
            var actualType = actualEvent.GetType().Name;
            var expectedType = expectedEvent.GetValue("type").ToString();
            actualType.Should().Be(expectedType);
            if (expectedEvent.TryGetValue("connectionId", out var connectionId))
            {
                var expectedConnectionId = connectionId.ToInt32();
                if (expectedConnectionId == 42) // 42 - placeholder
                {
                    actualEvent.ConnectionId().Should().NotBeNull();
                }
                else
                {
                    actualEvent.ConnectionId().LocalValue.Should().Be(expectedConnectionId);
                }
            }

            if (expectedEvent.TryGetValue("options", out var expectedOption))
            {
                var connectionPoolSettings = actualEvent.ConnectionPoolSettings();
                if (expectedOption.IsInt32 && expectedOption == 42) // 42 - placeholder
                {
                    connectionPoolSettings.Should().NotBeNull();
                }
                else
                {
                    var expectedMaxPoolSize = expectedOption["maxPoolSize"].ToInt32();
                    connectionPoolSettings.MaxConnections.Should().Be(expectedMaxPoolSize);
                    var expectedMinPoolSize = expectedOption["minPoolSize"].ToInt32();
                    connectionPoolSettings.MinConnections.Should().Be(expectedMinPoolSize);
                }
            }

            if (expectedEvent.TryGetValue("address", out var address))
            {
                var expectedAddress = address.ToInt32();
                if (expectedAddress == 42)
                {
                    actualEvent.ServerId().EndPoint.Should().NotBeNull();
                }
                else
                {
                    // not expected code path
                    throw new NotImplementedException();
                }
            }

            if (expectedEvent.TryGetValue("reason", out var reason))
            {
                if (actualEvent is ConnectionPoolCheckingOutConnectionFailedEvent checkingOutFailedEvent)
                {
                    var actualReason = checkingOutFailedEvent.Reason;
                    actualReason.Should().NotBeNull();
                    actualReason.ToString().ToLower().Should().Be(reason.ToString().ToLower());
                }
                else if (actualEvent is ConnectionClosedEvent connectionClosedEvent)
                {
                    // we don't support Reason for ConnectionClosedEvent
                }
                else
                {
                    throw new Exception($"Don't know how to assert against reason for class : {actualEvent.GetType().FullName}.");
                }
            }
        }

        private void AssertEvents(BsonDocument test, EventCapturer eventCapturer)
        {
            var actualEvents = GetFilteredEvents(eventCapturer, test);
            var expectedEvents = GetExpectedEvents(test);
            var minCount = Math.Min(actualEvents.Count, expectedEvents.Count);
            for (var i = 0; i < minCount; i++)
            {
                var expectedEvent = expectedEvents[i];
                JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "type", "address", "connectionId", "options", "reason");
                AssertEvent(actualEvents[i], expectedEvent);
            }

            if (actualEvents.Count < expectedEvents.Count)
            {
                throw new Exception($"Missing event: {expectedEvents[actualEvents.Count]}.");
            }

            if (actualEvents.Count > expectedEvents.Count)
            {
                throw new Exception($"Unexpected event of type: {actualEvents[expectedEvents.Count].GetType().Name}.");
            }
        }

        private Task CreateTask(Action action)
        {
            return Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());
        }

        private void EnsureAvailableStyle(BsonDocument test)
        {
            var style = test.GetValue("style").ToString();
            switch (style)
            {
                case "unit":
                    return;
                default:
                    throw new ArgumentException($"Unknown style {style}.");
            }
        }

        private void ExecuteCheckIn(BsonDocument operation, ConcurrentDictionary<string, IConnection> map, out Exception exception)
        {
            exception = null;
            var connectionName = operation.GetValue("connection").ToString();
            if (map.TryGetValue(connectionName, out var connection))
            {
                exception = Record.Exception(() => connection.Dispose());
            }
            else
            {
                throw new Exception("Connection must have a label.");
            }
        }

        private void ExecuteCheckOut(
            IConnectionPool connectionPool,
            BsonDocument operation,
            ConcurrentDictionary<string, IConnection> map,
            ConcurrentDictionary<string, Task> tasks,
            bool async,
            out Exception exception)
        {
            exception = null;

            if (operation.TryGetValue("thread", out var thread))
            {
                var target = thread.ToString();
                if (!tasks.ContainsKey(target))
                {
                    throw new ArgumentException($"Task {target} must be started before usage.");
                }
                else
                {
                    if (tasks[target] != null)
                    {
                        throw new Exception($"Task {target} must not be processed.");
                    }
                    else
                    {
                        tasks[target] = CreateTask(() => CheckOut(operation, connectionPool, map));
                    }
                }
            }
            else
            {
                exception = Record.Exception(() => CheckOut(operation, connectionPool, map));
            }

            void CheckOut(BsonDocument op, IConnectionPool cp, ConcurrentDictionary<string, IConnection> cm)
            {
                IConnection conn;
                if (async)
                {
                    conn = cp
                        .AcquireConnectionAsync(CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    conn = cp.AcquireConnection(CancellationToken.None);
                }

                if (op.TryGetValue("label", out var label))
                {
                    cm.GetOrAdd(label.ToString(), conn);
                }
                else
                {
                    // do nothing
                }
            }
        }

        private void ExecuteOperation(
            BsonDocument operation,
            EventCapturer eventCapturer,
            ConcurrentDictionary<string, IConnection> connectionMap,
            ConcurrentDictionary<string, Task> tasks,
            IConnectionPool connectionPool,
            bool async,
            out Exception exception)
        {
            exception = null;
            var name = operation.GetValue("name").ToString();

            switch (name)
            {
                case "checkIn":
                    ExecuteCheckIn(operation, connectionMap, out exception);
                    break;
                case "checkOut":
                    ExecuteCheckOut(connectionPool, operation, connectionMap, tasks, async, out exception);
                    break;
                case "clear":
                    connectionPool.Clear();
                    break;
                case "close":
                    connectionPool.Dispose();
                    break;
                case "start":
                    JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "target");
                    Start(operation, tasks);
                    break;
                case "wait":
                    var ms = operation.GetValue("ms").ToInt32();
                    Thread.Sleep(TimeSpan.FromMilliseconds(ms));
                    break;
                case "waitForEvent":
                    JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "event", "count");
                    WaitForEvent(eventCapturer, operation);
                    break;
                case "waitForThread":
                    JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "target");
                    WaitForThread(operation, tasks, out exception);
                    break;
                default:
                    throw new ArgumentException($"Unknown operation {name}.");
            }
        }

        private List<BsonDocument> GetExpectedEvents(BsonDocument test)
        {
            var expectedEvents = test
                .GetValue("events")
                .AsBsonArray
                .Select(e =>
                {
                    var expectedType = e["type"].ToString();
                    var mappedType = MapExpectedEventNameToDriverTypeName(expectedType);
                    if (mappedType != null)
                    {
                        e["type"] = mappedType;
                    }
                    return e;
                })
                .Cast<BsonDocument>()
                .ToList();

            return expectedEvents;
        }

        private List<object> GetFilteredEvents(EventCapturer eventCapturer, BsonDocument test)
        {
            var ignoredEvents = new List<string>();
            ignoredEvents.AddRange(__alwaysIgnoredEvents);
            if (test.TryGetValue("ignore", out var ignore))
            {
                var testCaseIgnoredEvent = ignore
                    .AsBsonArray
                    .Select(c => MapExpectedEventNameToDriverTypeName(c.ToString()))
                    .Where(c => c != null)
                    .ToList();
                ignoredEvents.AddRange(testCaseIgnoredEvent);
            }

            return eventCapturer.Events.Where(c => ignoredEvents.Contains(c.GetType().Name.ToString()) != true).ToList();
        }

        private string MapErrorTypeToExpected(Exception exception, out string expectedErrorMessage)
        {
            switch (exception)
            {
                case ObjectDisposedException objectDisposedException
                    when objectDisposedException.Message == "Cannot access a disposed object.\r\nObject name: 'ExclusiveConnectionPool'.":
                    expectedErrorMessage = "Attempted to check out a connection from closed connection pool";
                    return "PoolClosedError";
                case TimeoutException timeoutException when timeoutException.Message.Contains("Timed out waiting for a connection after"):
                    expectedErrorMessage = "Timed out while checking out a connection from connection pool";
                    return "WaitQueueTimeoutError";
                default:
                    expectedErrorMessage = exception.Message;
                    return exception.GetType().Name;
            }
        }

        private string MapExpectedEventNameToDriverTypeName(string expectedEventName)
        {
            switch (expectedEventName)
            {
                case "ConnectionPoolCreated":
                    return nameof(ConnectionPoolOpenedEvent);
                case "ConnectionPoolClosed":
                    return nameof(ConnectionPoolClosedEvent);
                case "ConnectionPoolCleared":
                    return nameof(ConnectionPoolClearedEvent);
                case "ConnectionReady":
                    return nameof(ConnectionOpenedEvent);
                case "ConnectionCheckedIn":
                    return nameof(ConnectionPoolCheckedInConnectionEvent);
                case "ConnectionCheckedOut":
                    return nameof(ConnectionPoolCheckedOutConnectionEvent);
                case "ConnectionCheckOutFailed":
                    return nameof(ConnectionPoolCheckingOutConnectionFailedEvent);
                case "ConnectionCheckOutStarted":
                    return nameof(ConnectionPoolCheckingOutConnectionEvent);
                case "ConnectionClosed":
                    return nameof(ConnectionClosedEvent);
                case "ConnectionCreated":
                    return nameof(ConnectionCreatedEvent);

                default:
                    throw new ArgumentException($"Unexpected event name {expectedEventName}.");
            }
        }

        private void ParseSettings(
            BsonDocument test,
            out ConnectionPoolSettings connectionPoolSettings,
            out ConnectionSettings connectionSettings)
        {
            connectionPoolSettings = new ConnectionPoolSettings();
            connectionSettings = new ConnectionSettings();

            if (test.Contains("poolOptions"))
            {
                var poolOptionsDocument = test["poolOptions"].AsBsonDocument;
                foreach (var poolOption in poolOptionsDocument.Elements)
                {
                    switch (poolOption.Name)
                    {
                        case "maxPoolSize":
                            connectionPoolSettings = connectionPoolSettings.With(maxConnections: poolOption.Value.ToInt32());
                            break;
                        case "minPoolSize":
                            connectionPoolSettings = connectionPoolSettings.With(minConnections: poolOption.Value.ToInt32());
                            break;
                        case "waitQueueTimeoutMS":
                            connectionPoolSettings = connectionPoolSettings.With(waitQueueTimeout: TimeSpan.FromMilliseconds(poolOption.Value.ToInt32()));
                            break;
                        case "maxIdleTimeMS":
                            connectionSettings = connectionSettings.With(maxIdleTime: TimeSpan.FromMilliseconds(poolOption.Value.ToInt32()));
                            break;
                        default:
                            throw new ArgumentException($"Unknown pool option {poolOption.Name}.");
                    }
                }
            }
        }

        public void ResetConnectionId()
        {
            IdGeneratorReflector.__lastId(0);
        }

        private IConnectionPool SetupConnectionPool(BsonDocument test, IEventSubscriber eventSubscriber)
        {
            var endPoint = new DnsEndPoint("localhost", 27017);
            var serverId = new ServerId(new ClusterId(), endPoint);
            ParseSettings(test, out var connectionPoolSettings, out var connectionSettings);

            var connectionFactory = new Mock<IConnectionFactory>();
            connectionFactory
                .Setup(c => c.CreateConnection(serverId, endPoint))
                .Returns(() =>
                {
                    var connection = new MockConnection(serverId, connectionSettings, eventSubscriber);
                    connection.Open(CancellationToken.None);
                    return connection;
                });
            var connectionPool = new ExclusiveConnectionPool(
                serverId,
                endPoint,
                connectionPoolSettings,
                connectionFactory.Object,
                eventSubscriber);

            return connectionPool;
        }

        private void Start(BsonDocument operation, ConcurrentDictionary<string, Task> tasks)
        {
            var startTarget = operation.GetValue("target").ToString();
            tasks.GetOrAdd(startTarget, (Task)null);
        }

        private void WaitForEvent(EventCapturer eventCapturer, BsonDocument operation)
        {
            var eventType = MapExpectedEventNameToDriverTypeName(operation.GetValue("event").ToString());
            if (eventType == null)
            {
                return;
            }

            var expectedCount = operation.GetValue("count").ToInt32();
            var notifyTask = eventCapturer.NotifyWhen(coll => coll.Count(c => c.GetType().Name == eventType) >= expectedCount);

            var testFailedTimeout = Task.Delay(TimeSpan.FromMinutes(1), CancellationToken.None);
            var index = Task.WaitAny(notifyTask, testFailedTimeout);
            if (index != 0)
            {
                throw new Exception($"{nameof(WaitForEvent)} executing is too long.");
            }
        }

        private void WaitForThread(BsonDocument operation, ConcurrentDictionary<string, Task> tasks, out Exception exception)
        {
            exception = null;
            var waitThread = operation.GetValue("target").ToString();
            if (tasks.TryGetValue(waitThread, out var task) && task != null)
            {
                exception = Record.Exception(() => task.GetAwaiter().GetResult());
            }
            else
            {
                throw new Exception($"The task {waitThread} must be configured before waiting.");
            }
        }

        // nested types
        private class TestCaseFactory : JsonDrivenTestCaseFactory
        {
            protected override string PathPrefix => "MongoDB.Driver.Core.Tests.Specifications.connection_monitoring_and_pooling.tests.";

            protected override IEnumerable<JsonDrivenTestCase> CreateTestCases(BsonDocument document)
            {
                var index = 0;
                foreach (var async in new[] { false, true })
                {
                    var name = GetTestCaseName(document, document, index);
                    name = $"{name}:async={async}";
                    var test = document.DeepClone().AsBsonDocument.Add("async", async);
                    yield return new JsonDrivenTestCase(name, test, test);
                }
            }
        }

        // This scheduler is used because the JSON tests description contains a statement
        // that a Start test step should start a new thread, so we want to guarantee that each Task executes in its own thread.
        // We're not 100% sure it's needed but we want to maximize isolation between Tasks just in case
        // https://code.msdn.microsoft.com/Samples-for-Parallel-b4b76364/sourcecode?fileId=44488&pathId=2098696067
        private class ThreadPerTaskScheduler : TaskScheduler
        {
            /// <summary>Gets the tasks currently scheduled to this scheduler.</summary> 
            /// <remarks>This will always return an empty enumerable, as tasks are launched as soon as they're queued.</remarks> 
            protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); }

            /// <summary>Starts a new thread to process the provided task.</summary> 
            /// <param name="task">The task to be executed.</param> 
            protected override void QueueTask(Task task)
            {
                new Thread(() => TryExecuteTask(task)) { IsBackground = true }.Start();
            }

            /// <summary>Runs the provided task on the current thread.</summary> 
            /// <param name="task">The task to be executed.</param> 
            /// <param name="taskWasPreviouslyQueued">Ignored.</param> 
            /// <returns>Whether the task could be executed on the current thread.</returns> 
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }
    }

    internal static class CmapEventsReflector
    {
        public static ConnectionId ConnectionId(this object @event)
        {
            return (ConnectionId)Reflector.GetPropertyValue(@event, nameof(ConnectionId), BindingFlags.Public | BindingFlags.Instance);
        }

        public static ConnectionPoolSettings ConnectionPoolSettings(this object @event)
        {
            return (ConnectionPoolSettings)Reflector.GetPropertyValue(@event, nameof(ConnectionPoolSettings), BindingFlags.Public | BindingFlags.Instance);
        }

        public static ServerId ServerId(this object @event)
        {
            return (ServerId)Reflector.GetPropertyValue(@event, nameof(ServerId), BindingFlags.Public | BindingFlags.Instance);
        }
    }

    internal static class IdGeneratorReflector
    {
        public static void __lastId(int value) => Reflector.SetStaticFieldValue(typeof(IdGenerator<ConnectionId>), nameof(__lastId), value);
    }
}
