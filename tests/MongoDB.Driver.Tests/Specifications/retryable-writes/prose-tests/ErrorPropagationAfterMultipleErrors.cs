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

using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_writes.prose_tests
{
    [Trait("Category", "Integration")]
    // https://github.com/mongodb/specifications/blob/master/source/retryable-writes/tests/README.md#6-test-error-propagation-after-encountering-multiple-errors
    public class ErrorPropagationAfterMultipleErrors
    {
        // Case 1: only errors without NoWritesPerformed — driver should return the last error (10107).
        [Fact]
        public void Only_errors_without_NoWritesPerformed_return_last_error()
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.ReplicaSet)
                .VersionGreaterThanOrEqualTo("6.0.0");

            var firstFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 91,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError""]
                    }
                }");

            var secondFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: ""alwaysOn"",
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 10107,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), firstFailPointCommand);

            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryWrites = true;
                s.ClusterConfigurator = b => b.Subscribe<CommandFailedEvent>(e =>
                {
                    if (e is { CommandName: "insert", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                    {
                        secondFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), secondFailPointCommand);
                        secondFailPointConfigured = true;
                    }
                });
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Assert.ThrowsAny<MongoCommandException>(() => collection.InsertOne(new BsonDocument("x", 1)));
                exception.Code.Should().Be(10107);
            }
            finally
            {
                secondFailPoint?.Dispose();
            }
        }

        // Case 2: only errors with NoWritesPerformed — driver should return the original error (91).
        [Fact]
        public void Only_errors_with_NoWritesPerformed_return_original_error()
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.ReplicaSet)
                .VersionGreaterThanOrEqualTo("6.0.0");

            var firstFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 91,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError"", ""NoWritesPerformed""]
                    }
                }");

            var secondFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: ""alwaysOn"",
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 10107,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError"", ""NoWritesPerformed""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), firstFailPointCommand);

            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryWrites = true;
                s.ClusterConfigurator = b => b.Subscribe<CommandFailedEvent>(e =>
                {
                    if (e is { CommandName: "insert", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                    {
                        secondFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), secondFailPointCommand);
                        secondFailPointConfigured = true;
                    }
                });
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Assert.ThrowsAny<MongoCommandException>(() => collection.InsertOne(new BsonDocument("x", 1)));
                exception.Code.Should().Be(91);
            }
            finally
            {
                secondFailPoint?.Dispose();
            }
        }

        // Case 3: mixed errors (first without NoWritesPerformed, retry with NoWritesPerformed) — driver should return the original error (91) without the NoWritesPerformed label.
        [Fact]
        public void Mixed_errors_return_original_error_without_NoWritesPerformed()
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.ReplicaSet)
                .VersionGreaterThanOrEqualTo("6.0.0");

            var firstFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 91,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError""]
                    }
                }");

            var secondFailPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: ""failCommand"",
                    mode: ""alwaysOn"",
                    data:
                    {
                        failCommands: [""insert""],
                        errorCode: 91,
                        errorLabels: [""RetryableError"", ""SystemOverloadedError"", ""NoWritesPerformed""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), firstFailPointCommand);

            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryWrites = true;
                s.ClusterConfigurator = b => b.Subscribe<CommandFailedEvent>(e =>
                {
                    if (e is { CommandName: "insert", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                    {
                        secondFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), secondFailPointCommand);
                        secondFailPointConfigured = true;
                    }
                });
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Assert.ThrowsAny<MongoCommandException>(() => collection.InsertOne(new BsonDocument("x", 1)));
                exception.Code.Should().Be(91);
                exception.HasErrorLabel(RetryabilityHelper.NoWritesPerformedErrorLabel).Should().BeFalse();
            }
            finally
            {
                secondFailPoint?.Dispose();
            }
        }
    }
}
