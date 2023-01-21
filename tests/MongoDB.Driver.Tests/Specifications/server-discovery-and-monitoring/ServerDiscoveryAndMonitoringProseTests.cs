﻿/* Copyright 2020-present MongoDB Inc.
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
using System.Diagnostics;
using System.Linq;
using System.Net;
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
        [Fact]
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
            using (var client = CreateClient(null, eventCapturer, heartbeatInterval))
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
                // Assert the client processes heartbeat replies more frequently than 10 secs (approximately every 500ms)
            }
        }

        [Fact]
        public void Monitor_sleep_at_least_minHeartbeatFreqencyMS_between_checks()
        {
            var minVersion = new SemanticVersion(4, 9, 0, "");
            RequireServer.Check().VersionGreaterThanOrEqualTo(minVersion);

            const string appName = "SDAMMinHeartbeatFrequencyTest";

            var failPointCommand = BsonDocument.Parse(
                $@"{{
                    configureFailPoint : 'failCommand',
                    mode : {{ 'times' : 5 }},
                    data :
                    {{
                        failCommands : [ '{OppressiveLanguageConstants.LegacyHelloCommandName}', 'hello' ],
                        errorCode : 1234,
                        appName : '{appName}'
                    }}
                }}");

            var settings = DriverTestConfiguration.GetClientSettings();
            var serverAddress = settings.Servers.First();
            settings.Servers = new[] { serverAddress };

            // set settings.DirectConnection = true after removing obsolete ConnectionMode
#pragma warning disable CS0618 // Type or member is obsolete
            settings.ConnectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete

            settings.ApplicationName = appName;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            var server = DriverTestConfiguration.Client.Cluster.SelectServer(new EndPointServerSelector(new DnsEndPoint(serverAddress.Host, serverAddress.Port)), default);
            using var failPoint = FailPoint.Configure(server, NoCoreSession.NewHandle(), failPointCommand);
            using var client = DriverTestConfiguration.CreateDisposableClient(settings);

            var database = client.GetDatabase(DriverTestConfiguration.DatabaseNamespace.DatabaseName);
            var sw = Stopwatch.StartNew();
            _ = database.RunCommand<BsonDocument>("{ ping : 1 }");
            sw.Stop();

            sw.ElapsedMilliseconds.Should().BeInRange(2000, 3500);
        }

        [Fact]
        public void RoundTimeTrip_test()
        {
            RequireServer.Check().Supports(Feature.StreamingHello);

            var eventCapturer = new EventCapturer().Capture<ServerDescriptionChangedEvent>();

            var heartbeatInterval = TimeSpan.FromMilliseconds(500);
            using (var client = CreateClient(null, eventCapturer, heartbeatInterval, applicationName: "streamingRttTest"))
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
                    $@"{{
                        configureFailPoint : 'failCommand',
                        mode : {{ times : 1000 }},
                        data :
                        {{
                            failCommands : [ '{OppressiveLanguageConstants.LegacyHelloCommandName}', 'hello' ],
                            blockConnection : true,
                            blockTimeMS : 500,
                            appName : 'streamingRttTest'
                        }}
                    }}");

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

        [Fact]
        public void ConnectionPool_cleared_on_failed_hello()
        {
            var minVersion = new SemanticVersion(4, 9, 0, "");
            RequireServer.Check().VersionGreaterThanOrEqualTo(minVersion);

            const string appName = "SDAMPoolManagementTest";
            // Using a 100ms heartbeatInterval can result in sporadic failures of this test if the RTT thread
            // consumes both of the configured failpoints before the monitoring thread can run.
            // Increasing the heartbeatInterval to 200ms avoids this race condition.
            var heartbeatInterval = TimeSpan.FromMilliseconds(200);
            var eventsWaitTimeout = TimeSpan.FromMilliseconds(5000);

            var failPointCommand = BsonDocument.Parse(
                $@"{{
                    configureFailPoint : 'failCommand',
                    mode : {{ 'times' : 2 }},
                    data :
                    {{
                        failCommands : [ 'isMaster', 'hello' ],
                        errorCode : 1234,
                        appName : '{appName}'
                    }}
                }}");

            var settings = DriverTestConfiguration.GetClientSettings();
            var serverAddress = settings.Servers.First();
            settings.Servers = new[] { serverAddress };

            // set settings.DirectConnection = true after removing obsolete ConnectionMode
#pragma warning disable CS0618 // Type or member is obsolete
            settings.ConnectionMode = ConnectionMode.Direct;
#pragma warning restore CS0618 // Type or member is obsolete

            settings.ApplicationName = appName;

            var eventCapturer = new EventCapturer()
               .Capture<ConnectionPoolReadyEvent>()
               .Capture<ConnectionPoolClearedEvent>()
               .Capture<ServerHeartbeatSucceededEvent>()
               .Capture<ServerHeartbeatFailedEvent>();

            using var client = CreateClient(settings, eventCapturer, heartbeatInterval, appName);

            eventCapturer.WaitForOrThrowIfTimeout(new[]
                {
                    typeof(ConnectionPoolReadyEvent),
                    typeof(ServerHeartbeatSucceededEvent),
                },
                eventsWaitTimeout);
            eventCapturer.Clear();

            var failpointServer = DriverTestConfiguration.Client.Cluster.SelectServer(new EndPointServerSelector(new DnsEndPoint(serverAddress.Host, serverAddress.Port)), default);
            using var failPoint = FailPoint.Configure(failpointServer, NoCoreSession.NewHandle(), failPointCommand);

            eventCapturer.WaitForOrThrowIfTimeout(new[]
                {
                    typeof(ServerHeartbeatFailedEvent),
                    typeof(ConnectionPoolClearedEvent),
                    typeof(ConnectionPoolReadyEvent),
                    typeof(ServerHeartbeatSucceededEvent),
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

    internal static class ServerMonitorReflector
    {
        public static IRoundTripTimeMonitor _roundTripTimeMonitor(this IServerMonitor serverMonitor)
        {
            return (IRoundTripTimeMonitor)Reflector.GetFieldValue(serverMonitor, nameof(_roundTripTimeMonitor));
        }
    }
}
