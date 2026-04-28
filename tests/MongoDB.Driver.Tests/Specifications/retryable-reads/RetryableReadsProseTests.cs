/* Copyright 2021-present MongoDB Inc.
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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Operations;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_reads
{
    [Trait("Category", "Integration")]
    public class RetryableReadsProseTests
    {
        [Theory]
        [ParameterAttributeData]
        public async Task PoolClearedError_read_retryablity_test([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.FailPointsBlockConnection)
                .VersionGreaterThanOrEqualTo("4.4.0"); // MongoDB 4.2 does not respect blockTimeMS in combination with errorCode.

            var heartbeatInterval = TimeSpan.FromMilliseconds(50);
            var eventsWaitTimeout = TimeSpan.FromMilliseconds(5000);

            var failPointCommand = BsonDocument.Parse(
                $@"{{
                    configureFailPoint : 'failCommand',
                    mode : {{ 'times' : 1 }},
                    data :
                    {{
                        failCommands : [ 'find' ],
                        errorCode : 91,
                        blockConnection: true,
                        blockTimeMS: 1000
                    }}
                }}");

            IServerSelector failPointSelector = new ReadPreferenceServerSelector(ReadPreference.Primary);
            var settings = DriverTestConfiguration.GetClientSettings();

            if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded)
            {
                var serverAddress = settings.Servers.First();
                settings.Servers = new[] { serverAddress };
                settings.DirectConnection = true;

                failPointSelector = new EndPointServerSelector(new DnsEndPoint(serverAddress.Host, serverAddress.Port));
            }

            settings.MaxConnectionPoolSize = 1;
            settings.RetryReads = true;

            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolClearedEvent>()
               .Capture<ConnectionPoolCheckedOutConnectionEvent>()
               .Capture<ConnectionPoolCheckingOutConnectionFailedEvent>()
               .CaptureCommandEvents("find");

            using var failPoint = FailPoint.Configure(failPointSelector, failPointCommand);

            using var client = CreateClient(settings, eventCapturer, heartbeatInterval);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            eventCapturer.Clear();

            if (async)
            {
                await ThreadingUtilities.ExecuteTasksOnNewThreads(2, async __ =>
                {
                    var cursor = await collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
                    _ = await cursor.ToListAsync();
                });
            }
            else
            {
                ThreadingUtilities.ExecuteOnNewThreads(2, __ =>
                {
                    _ = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                });
            }

            // wait for 2 CommandSucceededEvent events, meaning that all other events should be received
            eventCapturer.WaitForOrThrowIfTimeout(
                events => events.OfType<CommandSucceededEvent>().Count() == 2,
                eventsWaitTimeout);

            eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(3);
            eventCapturer.Events.OfType<CommandFailedEvent >().Count().Should().Be(1);
            eventCapturer.Events.OfType<CommandSucceededEvent>().Count().Should().Be(2);
            eventCapturer.Events.OfType<ConnectionPoolClearedEvent>().Count().Should().Be(1);
            eventCapturer.Events.OfType<ConnectionPoolCheckedOutConnectionEvent>().Count().Should().Be(3);
            eventCapturer.Events.OfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Count().Should().Be(1);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Sharded_cluster_retryable_reads_are_retried_on_different_mongos_if_available([Values(true, false)] bool async)
        {
            RequireServer.Check()
                .Supports(Feature.FailPointsFailCommandForSharded)
                .ClusterTypes(ClusterType.Sharded)
                .MultipleMongoses(true);

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: 'failCommand',
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: ['find'],
                        errorCode: 6
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryReads = true;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                },
                useMultipleShardRouters: true);

            var failPointServerSelector1 = new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint);
            var failPointServerSelector2 = new EndPointServerSelector(client.Cluster.Description.Servers[1].EndPoint);

            using var failPoint1 = FailPoint.Configure(failPointServerSelector1, failPointCommand, cluster: client.GetClusterInternal());
            using var failPoint2 = FailPoint.Configure(failPointServerSelector2, failPointCommand, cluster: client.GetClusterInternal());

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            var exception = async ?
                await Record.ExceptionAsync(() => collection.FindAsync(Builders<BsonDocument>.Filter.Empty)) :
                Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));

            exception.Should().BeOfType<MongoCommandException>();
            var failedEvents = eventCapturer.Events.OfType<CommandFailedEvent>().ToArray();
            failedEvents.Length.Should().Be(2);

            failedEvents[0].CommandName.Should().Be(failedEvents[1].CommandName).And.Be("find");
            failedEvents[0].ConnectionId.ServerId.Should().NotBe(failedEvents[1].ConnectionId.ServerId);
        }

        [Theory]
        [ParameterAttributeData]
        public async Task Sharded_cluster_retryable_reads_are_retried_on_same_mongos_if_no_other_is_available([Values(true, false)] bool async)
        {
            RequireServer.Check()
                .Supports(Feature.FailPointsFailCommandForSharded)
                .ClusterTypes(ClusterType.Sharded);

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: 'failCommand',
                    mode: { times: 1 },
                    data:
                    {
                        failCommands: ['find'],
                        errorCode: 6
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryReads = true;
                    s.DirectConnection = false;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                },
                useMultipleShardRouters: false);
            DriverTestConfiguration.WaitForAllServersToBeConnected(client.GetClusterInternal());

            var failPointServerSelector = new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint);

            using var failPoint = FailPoint.Configure(failPointServerSelector, failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _ = async ?
                await collection.FindAsync(Builders<BsonDocument>.Filter.Empty) :
                collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            var failedEvents = eventCapturer.Events.OfType<CommandFailedEvent>().ToArray();
            var succeededEvents = eventCapturer.Events.OfType<CommandSucceededEvent>().ToArray();

            failedEvents.Length.Should().Be(1);
            succeededEvents.Length.Should().Be(1);

            failedEvents[0].CommandName.Should().Be(succeededEvents[0].CommandName).And.Be("find");
            failedEvents[0].ConnectionId.ServerId.Should().Be(succeededEvents[0].ConnectionId.ServerId);
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/19ed647f6d6ca2de231e21de25bd1279f6d0bd14/source/retryable-reads/tests/README.md?plain=1#L130
        public async Task Retryable_reads_caused_by_overload_error_retried_on_different_replicaset_server([Values(true, false)] bool async)
        {
            RequireServer.Check()
                .VersionGreaterThanOrEqualTo("4.4")
                .ClusterTypes(ClusterType.ReplicaSet)
                .Cluster(c => DriverTestConfiguration.GetReplicaSetNumberOfDataBearingMembers(c) > 1, "Replicaset cluster must have more then 1 data-bearing nodes.");

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: 'failCommand',
                    mode: { times: 1 },
                    data: {
                        failCommands: ['find'],
                        errorLabels: ['RetryableError', 'SystemOverloadedError'],
                        errorCode: 6
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryReads = true;
                    s.EnableOverloadRetargeting = true;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                    s.ReadPreference = ReadPreference.PrimaryPreferred;
                },
                useMultipleShardRouters: false,
                waitForAllServersToBeConnected: true);

            var failPointServerSelector = new ReadPreferenceServerSelector(ReadPreference.Primary);
            using var failPoint = FailPoint.Configure(failPointServerSelector, failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _ = async ?
                await collection.FindAsync(Builders<BsonDocument>.Filter.Empty) :
                collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.EndPoint.Should().Be(failPoint.Server.ServerId.EndPoint);
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.EndPoint.Should().NotBe(failPoint.Server.ServerId.EndPoint);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        [Theory]
        [ParameterAttributeData]
        // https://github.com/mongodb/specifications/blob/19ed647f6d6ca2de231e21de25bd1279f6d0bd14/source/retryable-reads/tests/README.md?plain=1#L160C10-L160C97
        public async Task Retryable_reads_caused_by_non_overload_error_retried_on_same_replicaset_server([Values(true, false)] bool async)
        {
            RequireServer.Check()
                .VersionGreaterThanOrEqualTo("4.4")
                .ClusterTypes(ClusterType.ReplicaSet);

            var failPointCommand = BsonDocument.Parse(
                @"{
                    configureFailPoint: 'failCommand',
                    mode: { times: 1 },
                    data: {
                        failCommands: ['find'],
                        errorLabels: ['RetryableError'],
                        errorCode: 6
                    }
                }");

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");

            using var client = DriverTestConfiguration.CreateMongoClient(
                s =>
                {
                    s.RetryReads = true;
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                    s.ReadPreference = ReadPreference.PrimaryPreferred;
                },
                useMultipleShardRouters: false,
                waitForAllServersToBeConnected: true);

            var failPointServerSelector = new ReadPreferenceServerSelector(ReadPreference.Primary);
            using var failPoint = FailPoint.Configure(failPointServerSelector, failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _ = async ?
                await collection.FindAsync(Builders<BsonDocument>.Filter.Empty) :
                collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.EndPoint.Should().Be(failPoint.Server.ServerId.EndPoint);
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.EndPoint.Should().Be(failPoint.Server.ServerId.EndPoint);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
        }

        [Fact]
        // https://github.com/mongodb/specifications/blob/master/source/retryable-reads/tests/README.md#4-test-that-drivers-set-the-maximum-number-of-retries-for-all-retryable-read-errors-when-an-overload-error-is-encountered
        public void Max_retries_for_all_retryable_read_errors_when_overload_error_encountered()
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
                        failCommands: [""find""],
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
                        failCommands: [""find""],
                        errorCode: 91,
                        errorLabels: [""RetryableError""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(firstFailPointCommand);

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");
            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryReads = true;
                s.ClusterConfigurator = b =>
                {
                    b.Subscribe(eventCapturer);
                    b.Subscribe<CommandFailedEvent>(e =>
                    {
                        if (e is { CommandName: "find", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                        {
                            secondFailPoint = FailPoint.Configure(secondFailPointCommand);
                            secondFailPointConfigured = true;
                        }
                    });
                };
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));
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
        // https://github.com/mongodb/specifications/blob/master/source/retryable-reads/tests/README.md#5-test-that-drivers-do-not-apply-backoff-to-non-overload-errors
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
                        failCommands: [""find""],
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
                        failCommands: [""find""],
                        errorCode: 91,
                        errorLabels: [""RetryableError""]
                    }
                }");

            var secondFailPointConfigured = false;
            FailPoint secondFailPoint = null;

            using var firstFailPoint = FailPoint.Configure(firstFailPointCommand);

            var eventCapturer = new EventCapturer().CaptureCommandEvents("find");
            using var client = DriverTestConfiguration.CreateMongoClient(s =>
            {
                s.RetryReads = true;
                s.ClusterConfigurator = b =>
                {
                    b.Subscribe(eventCapturer);
                    b.Subscribe<CommandFailedEvent>(e =>
                    {
                        if (e is { CommandName: "find", Failure: MongoCommandException { Code: 91 } } && !secondFailPointConfigured)
                        {
                            secondFailPoint = FailPoint.Configure(secondFailPointCommand);
                            secondFailPointConfigured = true;
                        }
                    });
                };
            });

            try
            {
                var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
                var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

                var exception = Record.Exception(() => collection.FindSync(Builders<BsonDocument>.Filter.Empty));
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

        // private methods
        private IMongoClient CreateClient(MongoClientSettings mongoClientSettings, EventCapturer eventCapturer, TimeSpan heartbeatInterval, string applicationName = null)
        {
            var clonedClientSettings = mongoClientSettings ?? DriverTestConfiguration.Client.Settings.Clone();
            clonedClientSettings.ApplicationName = applicationName;
            clonedClientSettings.HeartbeatInterval = heartbeatInterval;
            clonedClientSettings.ClusterConfigurator = builder => builder.Subscribe(eventCapturer);

            return DriverTestConfiguration.CreateMongoClient(clonedClientSettings);
        }
    }
}
