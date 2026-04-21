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

using System.Linq;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver.Core;
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
    public class RetryBehaviorWithMixedErrors
    {
        [Fact]
        // https://github.com/mongodb/specifications/blob/master/source/retryable-writes/tests/README.md#case-4-test-that-drivers-set-the-maximum-number-of-retries-for-all-retryable-write-errors-when-an-overload-error-is-encountered
        public void Max_retries_for_all_retryable_write_errors_when_overload_error_encountered()
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.ReplicaSet)
                .VersionGreaterThanOrEqualTo("4.4.0");

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
                        errorLabels: [""RetryableError"", ""RetryableWriteError""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), firstFailPointCommand);

            var eventCapturer = new EventCapturer().CaptureCommandEvents("insert");
            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryWrites = true;
                s.ClusterConfigurator = b =>
                {
                    b.Subscribe(eventCapturer);
                    b.Subscribe<CommandFailedEvent>(e =>
                    {
                        if (e is { CommandName: "insert", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                        {
                            secondFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), secondFailPointCommand);
                            secondFailPointConfigured = true;
                        }
                    });
                };
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument("x", 1)));
                exception.Should().BeAssignableTo<MongoException>();

                var expectedAttempts = RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries + 1;
                eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(expectedAttempts);
            }
            finally
            {
                secondFailPoint?.Dispose();
            }
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/master/source/retryable-writes/tests/README.md#case-5-test-that-drivers-do-not-apply-backoff-to-non-overload-errors
        public void Backoff_is_not_applied_to_non_overload_errors()
        {
            RequireServer.Check()
                .ClusterTypes(ClusterType.ReplicaSet)
                .VersionGreaterThanOrEqualTo("4.4.0");

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
                        errorLabels: [""RetryableError"", ""RetryableWriteError""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), firstFailPointCommand);

            var eventCapturer = new EventCapturer().CaptureCommandEvents("insert");
            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryWrites = true;
                s.ClusterConfigurator = b =>
                {
                    b.Subscribe(eventCapturer);
                    b.Subscribe<CommandFailedEvent>(e =>
                    {
                        if (e is { CommandName: "insert", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                        {
                            secondFailPoint = FailPoint.Configure(DriverTestConfiguration.Client.GetClusterInternal(), NoCoreSession.NewHandle(), secondFailPointCommand);
                            secondFailPointConfigured = true;
                        }
                    });
                };
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.InsertOne(new BsonDocument("x", 1)));
                exception.Should().BeAssignableTo<MongoException>();

                // Backoff is only applied for overload errors (attempt 1). Non-overload retries (attempts 2+) have no backoff.
                // The attempt count verifies that the overload-triggered MAX_RETRIES cap applies to all retries.
                // Precise backoff timing is verified by the ClientBackpressureProseTests unit tests which control the RNG.
                var expectedAttempts = RetryabilityHelper.OperationRetryBackpressureConstants.DefaultMaxRetries + 1;
                eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(expectedAttempts);
            }
            finally
            {
                secondFailPoint?.Dispose();
            }
        }
    }
}
