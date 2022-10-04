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
using MongoDB.Driver.Core.Logging;
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
                Cluster("Description changed"),
                SDAM("Server opening"),
                Connection("Connection pool opening"),
                Connection("Connection pool created"),
                SDAM("Server opened"),
                Cluster("Cluster opened"),
                Connection("Connection checkout started"),
                Connection("Connection created"),
                Connection("Connection added"),
                Connection("Connection ready"),
                Connection("Connection checked out"),
                TestsDebug<DisposableMongoClient>("Disposing"),
                Cluster("Cluster closing"),
                Cluster("Removing server"),
                SDAM("Server closing"),
                Connection("Connection closing"),
                Connection("Connection closed"),
                Connection("Connection pool closed"),
                SDAM("Server closed"),
                Cluster("Removed server"),
                Cluster("Disposing"),
                Cluster("Description changed"),
                Cluster("Disposed"),
                Cluster("Cluster closed"),
                TestsDebug<DisposableMongoClient>("Cluster unregistered and disposed"),
                TestsDebug<DisposableMongoClient>("Disposed")
            },
            logs);

            (LogLevel, string, string) Cluster(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.Cluster>(), message);
            (LogLevel, string, string) Connection(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.Connection>(), message);
            (LogLevel, string, string) SDAM(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.SDAM>(), message);
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
