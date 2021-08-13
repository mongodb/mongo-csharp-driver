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
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Bson.TestHelpers.XunitExtensions;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.retryable_reads
{
    public class RetryableReadsProseTests
    {
        [SkippableTheory]
        [ParameterAttributeData]
        public void PoolClearedError_read_retryablity_test([Values(true, false)] bool async)
        {
            RequireServer.Check().Supports(Feature.FailPointsBlockConnection);

            var heartbeatInterval = TimeSpan.FromMilliseconds(500);
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

            if (CoreTestConfiguration.Cluster.Description.Type == Core.Clusters.ClusterType.Sharded)
            {
                var serverAddress = settings.Servers.First();
                settings.Servers = new[] { serverAddress };

                // set settings.DirectConnection = true after removing obsolete ConnectionMode
#pragma warning disable CS0618 // Type or member is obsolete
                settings.ConnectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete

                failPointSelector = new EndPointServerSelector(new DnsEndPoint(serverAddress.Host, serverAddress.Port));
            }

            settings.MaxConnectionPoolSize = 1;
            settings.RetryReads = true;

            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolClearedEvent>()
               .Capture<ConnectionPoolCheckedOutConnectionEvent>()
               .Capture<ConnectionPoolCheckingOutConnectionFailedEvent>()
               .CaptureCommandEvents("find");

            var failpointServer = DriverTestConfiguration.Client.Cluster.SelectServer(failPointSelector, default);
            using var failPoint = FailPoint.Configure(failpointServer, NoCoreSession.NewHandle(), failPointCommand);

            using var client = CreateClient(settings, eventCapturer, heartbeatInterval);
            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName);

            eventCapturer.Clear();
            ThreadingUtilities.ExecuteOnNewThreads(2, i =>
            {
                if (async)
                {
                    _ = collection.FindAsync(FilterDefinition<BsonDocument>.Empty)
                        .GetAwaiter()
                        .GetResult()
                        .ToListAsync()
                        .GetAwaiter()
                        .GetResult();
                }
                else
                {
                    _ = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
                }
            });

            eventCapturer.WaitForOrThrowIfTimeout(new[]
                {
                    // first command fails
                    typeof(ConnectionPoolCheckedOutConnectionEvent),
                    typeof(CommandStartedEvent),
                    typeof(CommandFailedEvent),
                    typeof(ConnectionPoolClearedEvent),

                    // second checkout fails
                    typeof(ConnectionPoolCheckingOutConnectionFailedEvent),

                    // retry or second command succeeds
                    typeof(ConnectionPoolCheckedOutConnectionEvent),
                    typeof(CommandStartedEvent),
                    typeof(CommandSucceededEvent),

                    // retry or second command succeeds
                    typeof(ConnectionPoolCheckedOutConnectionEvent),
                    typeof(CommandStartedEvent),
                    typeof(CommandSucceededEvent)
                },
                eventsWaitTimeout);
        }

        // private methods
        private DisposableMongoClient CreateClient(MongoClientSettings mongoClientSettings, EventCapturer eventCapturer, TimeSpan heartbeatInterval, string applicationName = null)
        {
            var clonedClientSettings = mongoClientSettings ?? DriverTestConfiguration.Client.Settings.Clone();
            clonedClientSettings.ApplicationName = applicationName;
            clonedClientSettings.HeartbeatInterval = heartbeatInterval;
            clonedClientSettings.ClusterConfigurator = builder => builder.Subscribe(eventCapturer);

            return DriverTestConfiguration.CreateDisposableClient(clonedClientSettings);
        }
    }
}
