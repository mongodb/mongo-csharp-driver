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
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
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

            var failpointServer = DriverTestConfiguration.Client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, failPointSelector);
            using var failPoint = FailPoint.Configure(failpointServer, NoCoreSession.NewHandle(), failPointCommand);

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

            var failPointServer1 = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint));
            var failPointServer2 = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[1].EndPoint));

            using var failPoint1 = FailPoint.Configure(failPointServer1, NoCoreSession.NewHandle(), failPointCommand);
            using var failPoint2 = FailPoint.Configure(failPointServer2, NoCoreSession.NewHandle(), failPointCommand);

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

            var failPointServer = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new EndPointServerSelector(client.Cluster.Description.Servers[0].EndPoint));

            using var failPoint = FailPoint.Configure(failPointServer, NoCoreSession.NewHandle(), failPointCommand);

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
                    s.ClusterConfigurator = b => b.Subscribe(eventCapturer);
                    s.ReadPreference = ReadPreference.PrimaryPreferred;
                },
                useMultipleShardRouters: false);

            var failPointServer = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new ReadPreferenceServerSelector(ReadPreference.Primary));
            using var failPoint = FailPoint.Configure(failPointServer, NoCoreSession.NewHandle(), failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _ = async ?
                await collection.FindAsync(Builders<BsonDocument>.Filter.Empty) :
                collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.Should().Be(failPointServer.ServerId);
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.Should().NotBe(failPointServer.ServerId);
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
                useMultipleShardRouters: false);

            var failPointServer = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new ReadPreferenceServerSelector(ReadPreference.Primary));
            using var failPoint = FailPoint.Configure(failPointServer, NoCoreSession.NewHandle(), failPointCommand);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            _ = async ?
                await collection.FindAsync(Builders<BsonDocument>.Filter.Empty) :
                collection.FindSync(Builders<BsonDocument>.Filter.Empty);

            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.Should().Be(failPointServer.ServerId);
            eventCapturer.Next().Should().BeOfType<CommandFailedEvent>();
            eventCapturer.Next().Should().BeOfType<CommandStartedEvent>().Subject.ConnectionId.ServerId.Should().Be(failPointServer.ServerId);
            eventCapturer.Next().Should().BeOfType<CommandSucceededEvent>();
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
