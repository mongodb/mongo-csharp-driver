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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.ConnectionPools;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Moq;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.connection_monitoring_and_pooling
{
    [Trait("Category", "Pool")]
    [Trait("Category", "Integration")]
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
            nameof(ConnectionFailedEvent),
            nameof(ConnectionOpeningFailedEvent),
            nameof(ConnectionClosingEvent),
            nameof(ConnectionSendingMessagesEvent),
            nameof(ConnectionSendingMessagesFailedEvent),
            nameof(ConnectionSentMessagesEvent),
            nameof(ConnectionReceivingMessageEvent),
            nameof(ConnectionReceivingMessageFailedEvent),
            nameof(ConnectionReceivedMessageEvent),
        };

        private static class Schema
        {
            public readonly static string _path = nameof(_path);
            public readonly static string version = nameof(version);
            public readonly static string style = nameof(style);
            public readonly static string description = nameof(description);
            public readonly static string poolOptions = nameof(poolOptions);
            public readonly static string operations = nameof(operations);
            public readonly static string error = nameof(error);
            public readonly static string events = nameof(events);
            public readonly static string ignore = nameof(ignore);
            public readonly static string async = nameof(async);

            public static class Intergration
            {
                public readonly static string runOn = nameof(runOn);
                public readonly static string failPoint = nameof(failPoint);
            }

            public static class Styles
            {
                public readonly static string unit = nameof(unit);
                public readonly static string integration = nameof(integration);
            }

            public readonly static string[] AllFields = new[]
            {
                _path,
                version,
                style,
                description,
                poolOptions,
                operations,
                error,
                events,
                ignore,
                async,
                Intergration.runOn,
                Intergration.failPoint,
            };
        }

        #endregion

        [Theory]
        [ClassData(typeof(TestCaseFactory))]
        public void RunTestDefinition(JsonDrivenTestCase testCase)
        {
            var test = testCase.Test;
            CheckServerRequirements(test);

            var connectionMap = new ConcurrentDictionary<string, IConnection>();
            var eventCapturer = new EventCapturer();
            var tasks = new ConcurrentDictionary<string, Task>();

            JsonDrivenHelper.EnsureAllFieldsAreValid(test, Schema.AllFields);
            var isUnit = EnsureStyle(test) == Schema.Styles.unit;

            var (connectionPool, failPoint, cluster, eventsFilter) = SetupConnectionData(test, eventCapturer, isUnit);
            using var disposableBundle = new DisposableBundle(failPoint, connectionPool, cluster);

            var operations = testCase.Test.GetValue(Schema.operations).AsBsonArray;
            var async = testCase.Test.GetValue(Schema.async).ToBoolean();
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
            AssertEvents(test, eventCapturer, eventsFilter);
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
                    actualEvent.ConnectionId().LongLocalValue.Should().Be(expectedConnectionId, because: "expected connectionId and actual must match");
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
                    connectionPoolSettings.MaxConnections.Should().Be(expectedMaxPoolSize, because: "expected maxConnections and actual must match");
                    var expectedMinPoolSize = expectedOption["minPoolSize"].ToInt32();
                    connectionPoolSettings.MinConnections.Should().Be(expectedMinPoolSize, because: "expected minConnections and actual must match");
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

            if (expectedEvent.TryGetValue("duration", out var duration))
            {
                var expectedDuration = duration.ToInt32();
                TimeSpan actualDuration = actualEvent switch
                {
                    ConnectionPoolCheckedOutConnectionEvent connectionPoolCheckedOutEvent => connectionPoolCheckedOutEvent.Duration,
                    ConnectionPoolCheckingOutConnectionFailedEvent connectionPoolCheckingOutFailedEvent => connectionPoolCheckingOutFailedEvent.Duration,
                    ConnectionOpenedEvent connectionOpenedEvent => connectionOpenedEvent.Duration,
                    ConnectionClosedEvent connectionClosedEvent => connectionClosedEvent.Duration,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (expectedDuration == 42)
                {
                    actualDuration.Should().NotBe(default);
                }
                else
                {
                    // not expected code path
                    throw new NotImplementedException();
                }
            }
        }

        private void AssertEvents(BsonDocument test, EventCapturer eventCapturer, Func<object, bool> eventsFilter)
        {
            var actualEvents = GetFilteredEvents(eventCapturer, test, eventsFilter);
            var expectedEvents = GetExpectedEvents(test);
            try
            {
                var minCount = Math.Min(actualEvents.Count, expectedEvents.Count);
                for (var i = 0; i < minCount; i++)
                {
                    var expectedEvent = expectedEvents[i];
                    JsonDrivenHelper.EnsureAllFieldsAreValid(expectedEvent, "type", "address", "connectionId", "duration", "options", "reason");
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
            catch (Exception ex)
            {
                throw new Exception($"Events asserting failed: {ex.Message}. Triggered events: {eventCapturer}.", ex);
            }
        }

        private void CheckServerRequirements(BsonDocument document)
        {
            if (document.TryGetValue(Schema.Intergration.runOn, out var runOn))
            {
                RequireServer.Check().RunOn(runOn.AsBsonArray);
            }
        }

        private Task CreateTask(Action action)
        {
            // This scheduler is used because the JSON tests description contains a statement
            // that a Start test step should start a new thread, so we want to guarantee that each Task executes in its own thread.
            // We're not 100% sure it's needed but we want to maximize isolation between Tasks just in case
            return Task.Factory.StartNew(
                action,
                CancellationToken.None,
                TaskCreationOptions.None,
                new ThreadPerTaskScheduler());
        }

        private string EnsureStyle(BsonDocument test)
        {
            var style = test.GetValue(Schema.style).ToString();

            if (style == Schema.Styles.unit)
            {
                foreach (var integratoinField in new[] { Schema.Intergration.failPoint, Schema.Intergration.runOn })
                {
                    if (test.Contains(integratoinField))
                    {
                        throw new FormatException($"Invalid field: {integratoinField} for {style} style.");
                    }
                }
            }
            else if (style == Schema.Styles.integration)
            {
                // not further validations
            }
            else
            {
                throw new ArgumentException($"Unknown style {style}.");
            }

            return style;
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
                var conn = async ?
                    cp.AcquireConnectionAsync(OperationContext.NoTimeout).GetAwaiter().GetResult() :
                    cp.AcquireConnection(OperationContext.NoTimeout);

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
                case "ready":
                    connectionPool.SetReady();
                    break;
                case "checkIn":
                    ExecuteCheckIn(operation, connectionMap, out exception);
                    break;
                case "checkOut":
                    ExecuteCheckOut(connectionPool, operation, connectionMap, tasks, async, out exception);
                    break;
                case "clear":
                    JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "closeInUseConnections");
                    var closeInUseConnections = operation.GetValue("closeInUseConnections", defaultValue: false).ToBoolean();
                    connectionPool.Clear(closeInUseConnections: closeInUseConnections);
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
                    JsonDrivenHelper.EnsureAllFieldsAreValid(operation, "name", "event", "count", "timeout");
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

        private List<object> GetFilteredEvents(EventCapturer eventCapturer, BsonDocument test, Func<object, bool> eventsFilter)
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

            return eventCapturer.Events
                .Where(c =>
                {
                    var name = c.GetType().Name;
                    return name.StartsWith("Connection") &&
                        !ignoredEvents.Contains(name) &&
                        eventsFilter(c);
                })
                .ToList();
        }

        private string MapErrorTypeToExpected(Exception exception, out string expectedErrorMessage)
        {
            switch (exception)
            {
                case ObjectDisposedException objectDisposedException
                    when objectDisposedException.Message == $"Cannot access a disposed object.{Environment.NewLine}Object name: 'ExclusiveConnectionPool'.":
                    expectedErrorMessage = "Attempted to check out a connection from closed connection pool";
                    return "PoolClosedError";
                case TimeoutException timeoutException
                    when timeoutException.Message.Contains("Timed out waiting for a connection after") || timeoutException.Message.Contains("Timed out waiting in connecting queue after"):
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
                case "ConnectionPoolReady":
                    return nameof(ConnectionPoolReadyEvent);
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
            connectionPoolSettings = new ConnectionPoolSettings(maintenanceInterval: TimeSpan.FromMilliseconds(200));
            connectionSettings = new ConnectionSettings();

            if (test.TryGetValue(Schema.poolOptions, out var poolOptionsBson))
            {
                var poolOptionsDocument = poolOptionsBson.AsBsonDocument;
                foreach (var poolOption in poolOptionsDocument.Elements)
                {
                    switch (poolOption.Name)
                    {
                        case "backgroundThreadIntervalMS":
                            connectionPoolSettings = connectionPoolSettings.With(maintenanceInterval: TimeSpan.FromMilliseconds(poolOption.Value.ToInt32()));
                            break;
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
                        case "appName":
                            connectionSettings = connectionSettings.With(applicationName: poolOption.Value.AsString);
                            break;
                        case "maxConnecting":
                            connectionPoolSettings = connectionPoolSettings.With(maxConnecting: poolOption.Value.ToInt32());
                            break;
                        default:
                            throw new ArgumentException($"Unknown pool option {poolOption.Name}.");
                    }
                }
            }
        }

        private (IConnectionPool, FailPoint, IClusterInternal, Func<object, bool>) SetupConnectionData(BsonDocument test, EventCapturer eventCapturer, bool isUnit)
        {
            ParseSettings(test, out var connectionPoolSettings, out var connectionSettings);

            IConnectionPool connectionPool;
            IClusterInternal cluster = null;
            FailPoint failPoint = null;
            Func<object, bool> eventsFilter = _ => true;

            var connectionLocalValue = 0L;
            var connectionIdProvider = () => Interlocked.Increment(ref connectionLocalValue);

            if (isUnit)
            {
                var endPoint = new DnsEndPoint("localhost", 27017);
                var serverId = new ServerId(new ClusterId(), endPoint);

                var connectionFactory = new Mock<IConnectionFactory>();
                var connectionExceptionHandler = new Mock<IConnectionExceptionHandler>();
                connectionFactory.Setup(f => f.ConnectionSettings).Returns(() => new ConnectionSettings());
                connectionFactory
                    .Setup(c => c.CreateConnection(serverId, endPoint))
                    .Returns(() =>
                    {
                        var connectionId = new ConnectionId(serverId, connectionIdProvider());
                        var connection = new MockConnection(connectionId, connectionSettings, eventCapturer);
                        return connection;
                    });

                connectionPool = new ExclusiveConnectionPool(
                    serverId,
                    endPoint,
                    connectionPoolSettings,
                    connectionFactory.Object,
                    connectionExceptionHandler.Object,
                    eventCapturer.ToEventLogger<LogCategories.Connection>());

                connectionPool.Initialize();
            }
            else
            {
                var async = test.GetValue(Schema.async).ToBoolean();
                cluster = CoreTestConfiguration.CreateCluster(b => b
                    .ConfigureServer(s => s.With(
                        heartbeatInterval: TimeSpan.FromMinutes(10), serverMonitoringMode: ServerMonitoringMode.Poll))
                    .ConfigureConnectionPool(c => c.With(
                        maxConnecting: connectionPoolSettings.MaxConnecting,
                        maxConnections: connectionPoolSettings.MaxConnections,
                        minConnections: connectionPoolSettings.MinConnections,
                        maintenanceInterval: connectionPoolSettings.MaintenanceInterval,
                        waitQueueTimeout: connectionPoolSettings.WaitQueueTimeout))
                    .ConfigureConnection(s => s.WithInternal(
                        applicationName: $"{connectionSettings.ApplicationName}_async_{async}",
                        connectionIdLocalValueProvider: connectionIdProvider))
                    .Subscribe(eventCapturer));

                var (server, _) = cluster.SelectServer(OperationContext.NoTimeout, WritableServerSelector.Instance);
                connectionPool = server._connectionPool();

                if (test.TryGetValue(Schema.Intergration.failPoint, out var failPointDocument))
                {
                    if (failPointDocument.AsBsonDocument.Contains("data"))
                    {
                        var data = failPointDocument["data"].AsBsonDocument;
                        if (data.TryGetValue("appName", out var appNameValue))
                        {
                            data["appName"] = FailPoint.DecorateApplicationName(appNameValue.AsString, async);
                        }
                    }

                    var resetPool = connectionPoolSettings.MinConnections > 0;

                    if (resetPool)
                    {
                        eventCapturer.WaitForOrThrowIfTimeout(events => events.Any(e => e is ConnectionCreatedEvent), TimeSpan.FromMilliseconds(500));

                        var connectionIdsToIgnore = new HashSet<long>(eventCapturer.Events
                            .OfType<ConnectionCreatedEvent>()
                            .Select(c => c.ConnectionId.LongLocalValue)
                            .ToList());

                        eventsFilter = o =>
                        {
                            if (o is ConnectionOpenedEvent
                                or ConnectionClosedEvent
                                or ConnectionCreatedEvent
                                or ConnectionFailedEvent)
                            {
                                var connectionId = o.ConnectionId();
                                return !connectionIdsToIgnore.Contains(connectionId.LongLocalValue) &&
                                    EndPointHelper.Equals(connectionId.ServerId.EndPoint, server.EndPoint);
                            }

                            if (o is ConnectionPoolReadyEvent
                                or ConnectionPoolClearedEvent)
                            {
                                var serverId = o.ServerId();
                                return EndPointHelper.Equals(serverId.EndPoint, server.EndPoint);
                            }

                            if (o is ServerHeartbeatStartedEvent ||
                                o is ServerHeartbeatSucceededEvent ||
                                o is ServerHeartbeatFailedEvent)
                            {
                                return false;
                            }

                            return true;
                        };

                        connectionPool.Clear(closeInUseConnections: false);
                        eventCapturer.WaitForOrThrowIfTimeout(events => events.Any(e => e is ConnectionPoolClearedEvent), TimeSpan.FromMilliseconds(500));
                    }

                    var (failPointServer, failPointServerRoundTripTime) = CoreTestConfiguration.Cluster.SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(server.EndPoint));
                    failPoint = FailPoint.Configure(failPointServer, failPointServerRoundTripTime, NoCoreSession.NewHandle(), failPointDocument.AsBsonDocument, withAsync: async);

                    if (resetPool)
                    {
                        eventCapturer.Clear();
                    }
                }
            }

            // Reset connection id after initial setup
            connectionLocalValue = 0;

            return (connectionPool, failPoint, cluster, eventsFilter);
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

            var timeout = TimeSpan.FromMinutes(1);
            if (operation.TryGetValue("timeout", out var timeoutValue))
            {
                timeout = TimeSpan.FromMilliseconds(timeoutValue.AsInt32);
            }

            var index = Task.WaitAny(new[] { notifyTask }, timeout);
            if (index != 0)
            {
                throw new Exception($"{nameof(WaitForEvent)} for {eventType}({expectedCount}) executing exceeded timeout: {timeout}. \n\nTriggered events:\n{eventCapturer}");
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
            protected override string PathPrefix => "MongoDB.Driver.Tests.Specifications.connection_monitoring_and_pooling.tests.cmap_format.";

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

    internal static class IServerReflector
    {
        public static IConnectionPool _connectionPool(this IServer server) => (IConnectionPool)Reflector.GetFieldValue(server, nameof(_connectionPool));
    }
}
