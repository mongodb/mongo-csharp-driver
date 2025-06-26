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

namespace MongoDB.Driver.Tests.Specifications.retryable_writes.prose_tests
{
    [Trait("Category", "Integration")]
    public class PoolClearRetryability
    {
        [Theory]
        [ParameterAttributeData]
        public async Task PoolClearedError_write_retryablity_test([Values(false, true)] bool async)
        {
            RequireServer.Check()
                .Supports(Feature.FailPointsBlockConnection)
                .ClusterTypes(ClusterType.ReplicaSet, ClusterType.Sharded);

            var heartbeatInterval = TimeSpan.FromMilliseconds(50);
            var eventsWaitTimeout = TimeSpan.FromMilliseconds(5000);

            var failPointCommand = BsonDocument.Parse(
                $@"{{
                    configureFailPoint : 'failCommand',
                    mode : {{ 'times' : 1 }},
                    data :
                    {{
                        failCommands : [ 'insert' ],
                        errorCode : 91,
                        blockConnection: true,
                        blockTimeMS: 1000,
                        errorLabels: [""RetryableWriteError""]
                    }}
                }}");

            IServerSelector failPointSelector = WritableServerSelector.Instance;
            var settings = DriverTestConfiguration.GetClientSettings();

            if (CoreTestConfiguration.Cluster.Description.Type == ClusterType.Sharded)
            {
                var serverAddress = settings.Servers.First();
                settings.Servers = [serverAddress];
                settings.DirectConnection = true;

                failPointSelector = new EndPointServerSelector(new DnsEndPoint(serverAddress.Host, serverAddress.Port));
            }

            settings.MaxConnectionPoolSize = 1;
            settings.RetryWrites = true;

            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolClearedEvent>()
               .Capture<ConnectionPoolCheckedOutConnectionEvent>()
               .Capture<ConnectionPoolCheckingOutConnectionFailedEvent>()
               .CaptureCommandEvents("insert");

            var (failpointServer, roundTripTime) = DriverTestConfiguration.Client.GetClusterInternal().SelectServer(OperationContext.NoTimeout, failPointSelector);
            using var failPoint = FailPoint.Configure(failpointServer, roundTripTime, NoCoreSession.NewHandle(), failPointCommand);

            using var client = CreateClient(settings, eventCapturer, heartbeatInterval);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            eventCapturer.Clear();

            if (async)
            {
                await ThreadingUtilities.ExecuteTasksOnNewThreads(2, async _ =>
                {
                    await collection.InsertOneAsync(new BsonDocument("x", 1));
                });
            }
            else
            {
                ThreadingUtilities.ExecuteOnNewThreads(2, _ =>
                {
                        collection.InsertOne(new BsonDocument("x", 1));
                });
            }

            // wait for 2 CommandSucceededEvent events, meaning that all other events should be received
            eventCapturer.WaitForOrThrowIfTimeout(
                events => events.OfType<CommandSucceededEvent>().Count() == 2,
                eventsWaitTimeout);

            eventCapturer.Events.OfType<CommandStartedEvent>().Count().Should().Be(3);
            eventCapturer.Events.OfType<CommandFailedEvent>().Count().Should().Be(1);
            eventCapturer.Events.OfType<CommandSucceededEvent>().Count().Should().Be(2);
            eventCapturer.Events.OfType<ConnectionPoolClearedEvent>().Count().Should().Be(1);
            eventCapturer.Events.OfType<ConnectionPoolCheckedOutConnectionEvent>().Count().Should().Be(3);
            eventCapturer.Events.OfType<ConnectionPoolCheckingOutConnectionFailedEvent>().Count().Should().Be(1);
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
