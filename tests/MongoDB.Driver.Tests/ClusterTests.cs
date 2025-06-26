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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.Logging;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.TestHelpers.XunitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace MongoDB.Driver.Tests
{
    [Trait("Category", "Integration")]
    public class ClusterTests : LoggableTestClass
    {
        private static readonly HashSet<string> __commandsToNotCapture = new HashSet<string>
        {
            "hello",
            OppressiveLanguageConstants.LegacyHelloCommandName,
            "getLastError",
            "authenticate",
            "saslStart",
            "saslContinue",
            "getnonce"
        };

        private const string _collectionName = "test";
        private const string _databaseName = "test";

        public ClusterTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Test that starting a new transaction on a pinned ClientSession unpins the
        /// session and normal server selection is performed for the next operation.
        /// </summary>
        [Theory]
        [ParameterAttributeData]
        public void SelectServer_loadbalancing_prose_test([Values(false, true)] bool async)
        {
            RequireServer.Check()
                .Supports(Feature.ShardedTransactions, Feature.FailPointsBlockConnection)
                .ClusterType(ClusterType.Sharded)
                .MultipleMongoses(true);

            // temporary disable the test on Auth envs due to operations timings irregularities
            RequireServer.Check().Authentication(false);

            var applicationName = FailPoint.DecorateApplicationName("loadBalancingTest", async);
            const int threadsCount = 10;
            const int commandsFailPointPerThreadCount = 10;
            const int commandsPerThreadCount = 100;
            const double maxCommandsOnSlowServerRatio = 0.3; // temporary set slow server load to 30% from 25% until find timings are investigated
            const double operationsCountTolerance = 0.10;

            var failCommand = BsonDocument.Parse($"{{ configureFailPoint: 'failCommand', mode : {{ times : 10000 }}, data : {{ failCommands : [\"find\"], blockConnection: true, blockTimeMS: 500, appName: '{applicationName}' }} }}");

            DropCollection();
            var eventCapturer = CreateEventCapturer();
            using (var client = CreateMongoClient(eventCapturer, applicationName))
            {
                var (slowServer, slowServerRtt) = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, WritableServerSelector.Instance);
                var (fastServer, _) = client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, new DelegateServerSelector((_, servers) => servers.Where(s => s.ServerId != slowServer.ServerId)));

                using var failPoint = FailPoint.Configure(slowServer, slowServerRtt, NoCoreSession.NewHandle(), failCommand, async);
                var database = client.GetDatabase(_databaseName);
                CreateCollection();
                var collection = database.GetCollection<BsonDocument>(_collectionName);

                // warm up connections
                var channels = new ConcurrentBag<IConnectionHandle>();
                ThreadingUtilities.ExecuteOnNewThreads(threadsCount, i =>
                {
                    channels.Add(slowServer.GetConnection(OperationContext.NoTimeout));
                    channels.Add(fastServer.GetConnection(OperationContext.NoTimeout));
                });

                foreach (var channel in channels)
                {
                    channel.Dispose();
                }

                var (allCount, eventsOnSlowServerCount) = ExecuteFindOperations(collection, slowServer.ServerId, commandsFailPointPerThreadCount);
                eventsOnSlowServerCount.Should().BeLessThan((int)(allCount * maxCommandsOnSlowServerRatio));

                failPoint.Dispose();

                (allCount, eventsOnSlowServerCount) = ExecuteFindOperations(collection, slowServer.ServerId, commandsPerThreadCount);

                var singleServerOperationsPortion = allCount / 2;
                var singleServerOperationsRange = (int)Math.Ceiling(allCount * operationsCountTolerance);

                eventsOnSlowServerCount.Should().BeInRange(singleServerOperationsPortion - singleServerOperationsRange, singleServerOperationsPortion + singleServerOperationsRange);
            }

            (int allCount, int slowServerCount) ExecuteFindOperations(IMongoCollection<BsonDocument> collection, ServerId serverId, int operationsCount)
            {
                eventCapturer.Clear();

                ThreadingUtilities.ExecuteOnNewThreads(threadsCount, __ =>
                {
                    for (int i = 0; i < operationsCount; i++)
                    {
                        if (async)
                        {
                            var cursor = collection.FindAsync(new BsonDocument()).GetAwaiter().GetResult();
                            _ = cursor.FirstOrDefaultAsync().GetAwaiter().GetResult();
                        }
                        else
                        {
                            _ = collection.Find(new BsonDocument()).FirstOrDefault();
                        }
                    }
                });

                var events = eventCapturer.Events
                    .Where(e => e is CommandStartedEvent)
                    .Cast<CommandStartedEvent>()
                    .ToArray();

                var eventsOnSlowServerCountActual = events.Where(e => e.ConnectionId.ServerId == serverId).Count();

                return (events.Length, eventsOnSlowServerCountActual);
            }
        }

        private EventCapturer CreateEventCapturer() =>
            new EventCapturer()
                .Capture<CommandStartedEvent>(e => !__commandsToNotCapture.Contains(e.CommandName));

        private void CreateCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);

            var collection = database.GetCollection<BsonDocument>(_collectionName);
            collection.InsertOne(new BsonDocument());
        }

        private IMongoClient CreateMongoClient(EventCapturer eventCapturer, string applicationName)
        {
            // Increase localThresholdMS and wait until all nodes are discovered to avoid false positives.
            var client = DriverTestConfiguration.CreateMongoClient((MongoClientSettings settings) =>
                {
                    settings.Servers = settings.Servers.Take(2).ToArray();
                    settings.ApplicationName = applicationName;
                    settings.ClusterConfigurator = c => c.Subscribe(eventCapturer);
                    settings.LocalThreshold = TimeSpan.FromMilliseconds(1000);
                    settings.LoggingSettings = LoggingSettings;
                },
                true);
            var timeOut = TimeSpan.FromSeconds(60);
            bool AllServersConnected() => client.Cluster.Description.Servers.All(s => s.State == ServerState.Connected);
            SpinWait.SpinUntil(AllServersConnected, timeOut).Should().BeTrue();
            return client;
        }

        private void DropCollection()
        {
            var client = DriverTestConfiguration.Client;
            var database = client.GetDatabase(_databaseName).WithWriteConcern(WriteConcern.WMajority);
            database.DropCollection(_collectionName);
        }
    }
}
