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
using MongoDB.Driver.Encryption;

namespace MongoDB.Driver.SmokeTests.Sdk
{
    internal static class InfrastructureUtilities
    {
        public static readonly string MongoUri = Environment.GetEnvironmentVariable("MONGODB_URI") ??
                                        Environment.GetEnvironmentVariable("MONGO_URI") ??
                                        "mongodb://localhost";

        public static void ValidateMongoDBPackageVersion()
        {
            var packageShaExpected = Environment.GetEnvironmentVariable("SmokeTestsPackageSha");

            if (!string.IsNullOrEmpty(packageShaExpected))
            {
                var driverFileVersionInfo = FileVersionInfo.GetVersionInfo(typeof(MongoClient).Assembly.Location);
                var libmongocryptFileVersionInfo = FileVersionInfo.GetVersionInfo(typeof(ClientEncryption).Assembly.Location);

                driverFileVersionInfo.ProductVersion?.Contains(packageShaExpected)
                    .Should().BeTrue("Expected package sha {0} in {1} for driver package version.", packageShaExpected, driverFileVersionInfo.ProductVersion);

                libmongocryptFileVersionInfo.ProductVersion?.Contains(packageShaExpected)
                    .Should().BeTrue("Expected package sha {0} in {1} for libmongocrypt package version.", packageShaExpected, libmongocryptFileVersionInfo.ProductVersion);
            }
        }

        public static void AssertLogs(LogEntry[] expectedLogs, LogEntry[] actualLogs)
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

        public static ILoggerFactory GetLoggerFactory(TraceListener traceListener, (string Category, string LogLevel)[] categoriesVerbosity = null)
        {
            var configurationKeyValuePairs = categoriesVerbosity?
                .Select(p => new KeyValuePair<string, string>(p.Category, p.LogLevel)) ?? new[] { new KeyValuePair<string, string>("LogLevel:Default", "Trace") };

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
    }
}
