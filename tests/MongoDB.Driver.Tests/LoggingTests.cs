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
using MongoDB.Bson;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Logging;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Linq;
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
            using (var client = DriverTestConfiguration.CreateMongoClient(LoggingSettings))
            {
                client.ListDatabases(new ListDatabasesOptions());
            }

            var logs = Logs;

            AssertLogs(new[]
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
                Client("MongoClient disposing"),
                SDAM("Stopping topology monitoring"),
                SDAM("Removing server"),
                SDAM("Stopping server monitoring"),
                Connection("Connection closing"),
                Connection("Connection closed"),
                Connection("Connection pool closed"),
                SDAM("Stopped server monitoring"),
                SDAM("Removed server"),
                SDAM("Topology description changed"),
                SDAM("Stopped topology monitoring"),
                Client("MongoClient disposed")
            },
            logs);

            (LogLevel, string, string) Client(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.Client>(), message);
            (LogLevel, string, string) Connection(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.Connection>(), message);
            (LogLevel, string, string) SDAM(string message) => (LogLevel.Debug, LogCategoryHelper.GetCategoryName<LogCategories.SDAM>(), message);
        }

        [Fact]
        public void MongoClient_should_not_throw_when_factory_is_null()
        {
            using (var client = DriverTestConfiguration.CreateMongoClient(loggingSettings: null))
            {
                client.ListDatabases(new ListDatabasesOptions());
            }

            Logs.Any().Should().BeFalse();
        }

        [Theory]
        [InlineData(null)]
        [InlineData(100)]
        public void Prose_tests_truncation_limit_1(int? maxDocumentSize)
        {
            var expectedMaxSize = (maxDocumentSize ?? 1000) + 3; // 3 to account for '...'
            const string collectionName = "proseLogTests";

            var documents = Enumerable.Range(0, 100).Select(_ => new BsonDocument() { { "x", "y" } }).ToArray();

            var loggingSettings = maxDocumentSize == null
                ? new LoggingSettings(LoggerFactory)
                : new LoggingSettings(LoggerFactory, maxDocumentSize.Value);
            using (var client = DriverTestConfiguration.CreateMongoClient(loggingSettings))
            {
                
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);

                try
                {
                    var collection = db.GetCollection<BsonDocument>(collectionName);
                    collection.InsertMany(documents);
                    _ = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                }
                finally
                {
                    db.DropCollection(collectionName);
                }
            }

            var commandCategory = LogCategoryHelper.GetCategoryName<LogCategories.Command>();
            var commands = Logs.Where(l => l.Category == commandCategory).ToArray();

            GetCommandParameter(commands, "insert", "Command started", StructuredLogTemplateProviders.Command)
                .Length.Should().Be(expectedMaxSize);
            GetCommandParameter(commands, "insert", "Command succeeded", StructuredLogTemplateProviders.Reply)
                .Length.Should().BeLessOrEqualTo(expectedMaxSize);
            GetCommandParameter(commands, "find", "Command succeeded", StructuredLogTemplateProviders.Reply)
                .Length.Should().Be(expectedMaxSize);
        }

        [Fact]
        public void Prose_tests_truncation_limit_2()
        {
            const int truncationSize = 5;
            const int maxDocumentSize = truncationSize + 3; // 3 to account for '...'
            var loggingSettings = new LoggingSettings(LoggerFactory, truncationSize);
            using (var client = DriverTestConfiguration.CreateMongoClient(loggingSettings))
            {
                var db = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);

                try
                {
                    db.RunCommand<BsonDocument>(new BsonDocument() { { "hello", "true" } });
                    db.RunCommand<BsonDocument>(new BsonDocument() { { "notARealCommand", "true" } });
                }
                catch (MongoCommandException) { }
            }

            var commandCategory = LogCategoryHelper.GetCategoryName<LogCategories.Command>();
            var commands = Logs.Where(l => l.Category == commandCategory).ToArray();

            GetCommandParameter(commands, "hello", "Command started", StructuredLogTemplateProviders.Command)
                .Length.Should().Be(maxDocumentSize);
            GetCommandParameter(commands, "hello", "Command succeeded", StructuredLogTemplateProviders.Reply)
                .Length.Should().Be(maxDocumentSize);
            GetCommandParameter(commands, "notARealCommand", "Command failed", StructuredLogTemplateProviders.Failure)
                .Length.Should().Be(maxDocumentSize);
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

        private string GetCommandParameter(LogEntry[] commandLogs, string commandName, string message, string parameter)
        {
            var command = commandLogs.Single(c =>
                c.GetParameter<string>(StructuredLogTemplateProviders.CommandName) == commandName &&
                c.GetParameter<string>(StructuredLogTemplateProviders.Message) == message);

            return command.GetParameter<string>(parameter);
        }
    }
}
