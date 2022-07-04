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
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    public class LoggingTests : LoggableTestClass
    {
        public LoggingTests(ITestOutputHelper output) : base(output, includeAllCategories: true)
        {
        }

        [Fact]
        public void MongoClient_should_log()
        {
            using (var client = DriverTestConfiguration.CreateDisposableClient(LoggerFactory))
            {
                client.ListDatabases(new ListDatabasesOptions());
            }

            var logs = Logs;

            AssertLogs(new[]
            {
                ClusterDebug("Initialized"),
                Cluster("Opening"),
                Cluster("Description changed"),
                SDAM("Opening"),
                Connection("Opening"),
                Connection("Opened"),
                InternalDebug<IServerMonitor>("Initializing"),
                InternalDebug<RoundTripTimeMonitor>("Monitoring started"),
                InternalDebug<IServerMonitor>("Initialized"),
                SDAM("Opened"),
                Cluster("Opened"),
                Connection("Checking out connection"),
                Connection("Connection created"),
                Connection("Opening"),
                Connection("Sending"),
                Connection("Sent"),
                Connection("Receiving"),
                Connection("Received"),
                Connection("Connection added"),
                Connection("Checked out connection"),
                Connection("Checking connection in"),
                TestsDebug<DisposableMongoClient>("Disposing"),
                Cluster("Closing"),
                Cluster("Removing server"),
                SDAM("Closing"),
                InternalDebug<IServerMonitor>("Disposing"),
                InternalDebug<RoundTripTimeMonitor>("Disposing"),
                InternalDebug<RoundTripTimeMonitor>("Disposed"),
                InternalDebug<IServerMonitor>("Disposed"),
                Connection("Closing"),
                Connection("Removing"),
                Connection("Closing"),
                Connection("Closed"),
                Connection("Removed"),
                Connection("Closed"),
                SDAM("Closed"),
                Cluster("Removed server"),
                ClusterDebug("Disposing"),
                Cluster("Description changed"),
                ClusterDebug("Disposed"),
                Cluster("Closed"),
                TestsDebug<DisposableMongoClient>("Cluster unregistered and disposed"),
                TestsDebug<DisposableMongoClient>("Disposed")
            },
            logs);

            (LogLevel, string, string) Cluster(string message) => (LogLevel.Information, "MongoDB.Cluster", message);
            (LogLevel, string, string) ClusterDebug(string message) => (LogLevel.Debug, "MongoDB.Cluster", message);
            (LogLevel, string, string) Connection(string message) => (LogLevel.Information, "MongoDB.Connection", message);
            (LogLevel, string, string) SDAM(string message) => (LogLevel.Information, "MongoDB.SDAM", message);
            (LogLevel, string, string) InternalDebug<T>(string message) => (LogLevel.Debug, $"MongoDB.Internal.{typeof(T).Name}", message);
            (LogLevel, string, string) TestsDebug<T>(string message) => (LogLevel.Debug, $"MongoDB.Tests.{typeof(T).Name}", message);
        }

        [Fact]
        public void MongoClient_should_not_throw_when_factory_is_null()
        {
            using (var client = DriverTestConfiguration.CreateDisposableClient(loggerFactory: null))
            {
                client.ListDatabases(new ListDatabasesOptions());
            }

            Logs.Any().Should().BeFalse();
        }

        private void AssertLogs((LogLevel logLevel, string categorySubString, string messageSubString)[] expectedLogs, LogEntry[] actualLogs)
        {
            var actualLogIndex = 0;
            foreach (var (logLevel, categorySubString, messageSubString) in expectedLogs)
            {
                actualLogIndex = Array.FindIndex(actualLogs, actualLogIndex, Match);

                if (actualLogIndex < 0)
                {
                    throw new Exception($"Log entry '{logLevel}_{categorySubString}_{messageSubString}' not found");
                }

                bool Match(LogEntry logEntry) =>
                    logEntry.LogLevel == logLevel &&
                    logEntry.Category?.Contains(categorySubString) == true &&
                    logEntry.FormattedMessage?.Contains(messageSubString) == true;
            }
        }
    }
}
