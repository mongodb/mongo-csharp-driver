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
using MongoDB.Driver.Core.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    public sealed class LoggingTests
    {
        private readonly ITestOutputHelper _output;

        public LoggingTests(ITestOutputHelper output)
        {
            InfrastructureUtilities.ValidateMongoDBPackageVersion();
            _output = output;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("MongoDB.SDAM")]
        [InlineData("MongoDB.Connection")]
        [InlineData("MongoDB.Internal.IServerMonitor")]
        public void MongoClient_should_log_only_configured_categories(string categoryName)
        {
            var expectedLogs = GetExpectedLogs();
            (string, string)[] categories = null;

            if (categoryName != null)
            {
                categories = new[] { ("LogLevel:Default", "Error"),  ($"LogLevel:{categoryName}", "Trace")};
                expectedLogs = expectedLogs.Where(l => l.Category == categoryName).ToArray();
            }

            using var logsTracer = new LogsTraceListener();
            using (var loggerFactory = InfrastructureUtilities.GetLoggerFactory(logsTracer, categories))
            {
                var settings = MongoClientSettings.FromConnectionString(InfrastructureUtilities.MongoUri);
                settings.LoggingSettings = new LoggingSettings(loggerFactory);
                var mongoClient = new MongoClient(settings);

                try
                {
                    mongoClient.ListDatabases(new ListDatabasesOptions());
                }
                finally
                {
                    ClusterRegistry.Instance.UnregisterAndDisposeCluster(mongoClient.Cluster);
                }
            }

            var actualLogs = logsTracer.GetLogs();

            try
            {
                InfrastructureUtilities.AssertLogs(expectedLogs, actualLogs);

                if (categoryName != null)
                {
                    Array.ForEach(actualLogs, l => l.Category.Should().Be(categoryName));
                }
            }
            catch
            {
                _output.WriteLine("Logs observed:");
                foreach (var log in actualLogs)
                {
                    _output.WriteLine(log.ToString());
                }

                throw;
            }
        }

        private static LogEntry[] GetExpectedLogs()
        {
            return new[]
            {
                SDAM("Topology description changed"),
                SDAM("Starting server monitoring"),
                Connection("Connection pool opening"),
                Connection("Connection pool created"),
                SDAM("Started server monitoring"),
                SDAM("Started topology monitoring"),
                Connection("Connection checkout started"),
                Connection("Connection created"),
                Connection("Connection ready"),
                Connection("Connection added"),
                Connection("Connection checked out"),
                SDAM("Stopping topology monitoring"),
                SDAM("Removing server"),
                SDAM("Stopping server monitoring"),
                Connection("Connection closing"),
                Connection("Connection closed"),
                Connection("Connection pool closed"),
                SDAM("Stopped server monitoring"),
                SDAM("Removed server"),
                SDAM("Cluster disposing"),
                SDAM("Topology description changed"),
                SDAM("Cluster disposed"),
                SDAM("Stopped topology monitoring")
            };

            LogEntry Connection(string message) => new LogEntry(LogLevel.Debug, "MongoDB.Connection", message);
            LogEntry SDAM(string message) => new LogEntry(LogLevel.Debug, "MongoDB.SDAM", message);
        }
    }
}
