/* Copyright 2020-present MongoDB Inc.
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
using System.Linq;
using System.Threading;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Clusters.ServerSelectors;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Misc;
using MongoDB.Driver.Core.Servers;
using MongoDB.Driver.Core.TestHelpers;
using MongoDB.Driver.Core.TestHelpers.XunitExtensions;
using MongoDB.Driver.TestHelpers;
using Xunit;

namespace MongoDB.Driver.Tests.Specifications.server_discovery_and_monitoring
{
    public class ServerDiscoveryAndMonitoringProseTests
    {
        [SkippableFact]
        public void Heartbeat_should_work_as_expected()
        {
            var heartbeatSuceededTimestamps = new ConcurrentQueue<DateTime>();
            var eventCapturer = new EventCapturer()
                .Capture<ServerHeartbeatSucceededEvent>(
                    (@event) =>
                    {
                        heartbeatSuceededTimestamps.Enqueue(DateTime.UtcNow);
                        return true;
                    }
                );

            var heartbeatInterval = TimeSpan.FromMilliseconds(500);
            eventCapturer.Clear();
            using (var client = CreateClient(eventCapturer, heartbeatInterval))
            {
                eventCapturer.WaitForOrThrowIfTimeout(
                    events => events.Count() > 3, // wait for at least 3 events
                    TimeSpan.FromSeconds(10),
                    (timeout) =>
                    {
                        return $"Waiting for the expected events exceeded the timeout {timeout}. The number of triggered events is {eventCapturer.Events.ToList().Count}.";
                    });
            }

            var heartbeatSuceededTimestampsList = heartbeatSuceededTimestamps.ToList();
            // we have at least 3 items here
            // Skip the first event because we have nothing to compare it to
            for (int i = 1; i < heartbeatSuceededTimestampsList.Count; i++)
            {
                var attemptDuration = heartbeatSuceededTimestampsList[i] - heartbeatSuceededTimestampsList[i - 1];
                attemptDuration
                    .Should()
                    .BeLessThan(TimeSpan.FromSeconds(2));
                // Assert the client processes isMaster replies more frequently than 10 secs (approximately every 500ms)
            }
        }

        [SkippableFact]
        public void RoundTimeTrip_test()
        {
            RequireServer.Check().Supports(Feature.StreamingIsMaster);

            var eventCapturer = new EventCapturer().Capture<ServerDescriptionChangedEvent>();

            var heartbeatInterval = TimeSpan.FromMilliseconds(500);
            using (var client = CreateClient(eventCapturer, heartbeatInterval, applicationName: "streamingRttTest"))
            {
                // Run a find command to wait for the server to be discovered.
                _ = client
                    .GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName)
                    .GetCollection<BsonDocument>(DriverTestConfiguration.CollectionNamespace.CollectionName)
                    .Find(FilterDefinition<BsonDocument>.Empty)
                    .ToList();

                // Sleep for 2 seconds. This must be long enough for multiple heartbeats to succeed.
                Thread.Sleep(TimeSpan.FromSeconds(2));

                foreach (ServerDescriptionChangedEvent @event in eventCapturer.Events.ToList())
                {
                    @event.NewDescription.HeartbeatException.Should().BeNull();
                    @event.NewDescription.AverageRoundTripTime.Should().NotBe(default);
                }

                var failPointCommand = BsonDocument.Parse(
                    @"{
                        configureFailPoint : 'failCommand',
                        mode : { times : 1000 },
                        data :
                        {
                            failCommands : [ 'isMaster' ],
                            blockConnection : true,
                            blockTimeMS : 500,
                            appName : 'streamingRttTest'
                        }
                    }");

                using (FailPoint.Configure(client.Cluster, NoCoreSession.NewHandle(), failPointCommand))
                {
                    // Note that the Server Description Equality rule means that ServerDescriptionChangedEvents will not be published.
                    // So we use reflection to obtain the latest RTT instead.
                    var server = client.Cluster.SelectServer(WritableServerSelector.Instance, CancellationToken.None);
                    var roundTripTimeMonitor = server._monitor()._roundTripTimeMonitor();
                    var expectedRoundTripTime = TimeSpan.FromMilliseconds(250);
                    var timeout = TimeSpan.FromSeconds(30); // should not be reached without a driver bug
                    SpinWait.SpinUntil(() => roundTripTimeMonitor.Average >= expectedRoundTripTime, timeout).Should().BeTrue();
                }
            }
        }

        // private methods
        private DisposableMongoClient CreateClient(EventCapturer eventCapturer, TimeSpan heartbeatInterval, string applicationName = null)
        {
            var clonedClient = DriverTestConfiguration.Client.Settings.Clone();
            return DriverTestConfiguration.CreateDisposableClient(
                (clientSettings) =>
                {
                    clientSettings.ApplicationName = applicationName;
                    clientSettings.HeartbeatInterval = heartbeatInterval;
                    clientSettings.ClusterConfigurator = builder => builder.Subscribe(eventCapturer);
                });
        }
    }

    internal static class ServerReflector
    {
        public static IServerMonitor _monitor(this IServer server)
        {
            return (IServerMonitor)Reflector.GetFieldValue(server, nameof(_monitor));
        }
    }

    internal static class ServerMonitorRelfector
    { 
        public static IRoundTripTimeMonitor _roundTripTimeMonitor(this IServerMonitor serverMonitor)
        {
            return (IRoundTripTimeMonitor)Reflector.GetFieldValue(serverMonitor, nameof(_roundTripTimeMonitor));
        }
    }
}
