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
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            using (var loggerFactory = GetLoggerFactory(logsTracer, categories))
            {
                var settings = GetMongoClientSettings();
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
                AssertLogs(expectedLogs, actualLogs);

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
                SDAM("Description changed"),
                SDAM("Server opening"),
                Connection("Connection pool opening"),
                Connection("Connection pool created"),
                SDAM("Server opened"),
                SDAM("Cluster opened"),
                Connection("Connection checkout started"),
                Connection("Connection created"),
                Connection("Connection ready"),
                Connection("Connection added"),
                Connection("Connection checked out"),
                SDAM("Cluster closing"),
                SDAM("Removing server"),
                SDAM("Server closing"),
                Connection("Connection closing"),
                Connection("Connection closed"),
                Connection("Connection pool closed"),
                SDAM("Server closed"),
                SDAM("Removed server"),
                SDAM("Disposing"),
                SDAM("Description changed"),
                SDAM("Disposed"),
                SDAM("Cluster closed")
            };

            LogEntry Connection(string message) => new LogEntry(LogLevel.Debug, "MongoDB.Connection", message);
            LogEntry SDAM(string message) => new LogEntry(LogLevel.Debug, "MongoDB.SDAM", message);
        }

        private static void AssertLogs(LogEntry[] expectedLogs, LogEntry[] actualLogs)
        {
            var actualLogIndex = 0;
            foreach (var logEntryExpected in expectedLogs)
            {
                var newIndex = Array.FindIndex(actualLogs, actualLogIndex, Match);

                if (newIndex < 0)
                {
                    throw new Exception($"Log entry '{logEntryExpected}' not found. Previous matched log entry {actualLogs[actualLogIndex]}");
                }

                actualLogIndex = newIndex;

                bool Match(LogEntry logEntryActual) =>
                    logEntryActual.LogLevel == logEntryExpected.LogLevel &&
                    logEntryActual.Category.Contains(logEntryExpected.Category) &&
                    logEntryActual.Message.Contains(logEntryExpected.Message);
            }
        }

        private static ILoggerFactory GetLoggerFactory(TraceListener traceListener, (string Category, string LogLevel)[] categoriesVerbosity = null)
        {
            var configurationKeyValuePairs = categoriesVerbosity?.Select(p =>
                new KeyValuePair<string, string>(p.Category, p.LogLevel)) ??
                new[] { new KeyValuePair<string, string>("LogLevel:Default", "Trace") };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationKeyValuePairs)
                .Build();

            var testSwitch = new SourceSwitch("TestSwitch");
            testSwitch.Level = SourceLevels.All;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder
                .AddConfiguration(config)
                .AddTraceSource(testSwitch, traceListener));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            return loggerFactory;
        }

        private static MongoClientSettings GetMongoClientSettings()
        {
            var uri = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                Environment.GetEnvironmentVariable("MONGO_URI") ??
                "mongodb://localhost";

            return MongoClientSettings.FromConnectionString(uri);
        }
    }
}
