/* Copyright 2018–present MongoDB Inc.
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

using FluentAssertions;
using MongoDB.Bson.TestHelpers;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Events.Diagnostics;
using MongoDB.Driver.Core.Servers;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Xunit;

namespace MongoDB.Driver.Core.Events
{
    public class TraceSourceSdamEventSubscriberTests
    {
        [Fact]
        public void Handle_with_ClusterOpeningEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ClusterOpeningEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterOpeningEvent(new ClusterId(), new ClusterSettings());
            var expectedLogMessage = $"{TraceSourceEventHelper.Label(@event.ClusterId)}: opening.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterOpenedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ClusterOpenedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterOpenedEvent(new ClusterId(), new ClusterSettings(), new TimeSpan(1));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: opened in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterClosingEvent_should_trace_event()
        {
            const string traceSourceName = "HandleClusterClosing_EventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterClosingEvent(new ClusterId());
            var expectedLogMessage = $"{TraceSourceEventHelper.Label(@event.ClusterId)}: closing.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterClosedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ClusterClosedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterClosedEvent(new ClusterId(), new TimeSpan(1));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: closed in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterAddingServerEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ClusterAddingServerEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterAddingServerEvent(
                new ClusterId(),
                new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: adding server at endpoint " +
                $"{TraceSourceEventHelper.Format(@event.EndPoint)}.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterAddedServerEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ClusterAddedServerEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterAddedServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                new TimeSpan(1));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: added server " +
                $"{TraceSourceEventHelper.Format(@event.ServerId)} in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void HandleClusterRemovingServerEventShould_Log_To_File()
        {
            const string traceSourceName = "HandleClusterRemovingServerEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterRemovingServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                "The cake is a lie.");
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: removing server " +
                $"{TraceSourceEventHelper.Format(@event.ServerId)}. Reason: {@event.Reason}";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ClusterRemovedServerEvent_should_trace_event()
        {
            const string traceSourceName = "HandleClusterRemovedServerEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterRemovedServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                "The cake is a lie.",
                new TimeSpan(42));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: removed server " +
                $"{TraceSourceEventHelper.Format(@event.ServerId)} in {@event.Duration.TotalMilliseconds}ms. " +
                $"Reason: {@event.Reason}";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void HandleClusterDescriptionChangedEventShould_Log_To_File()
        {
            const string traceSourceName = "HandleClusterDescriptionChangedEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var ipAddress = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42);
            var @event = new ClusterDescriptionChangedEvent(
#pragma warning disable CS0618 // Type or member is obsolete
                oldDescription: new ClusterDescription(
                    new ClusterId(),
                    ClusterConnectionMode.Automatic,
                    ClusterType.Unknown,
                    new ServerDescription[] { }),
                newDescription: new ClusterDescription(
                    new ClusterId(),
                    ClusterConnectionMode.Direct,
                    ClusterType.Standalone,
                    new ServerDescription[] { new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress) }));
#pragma warning restore CS0618 // Type or member is obsolete
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.OldDescription.ClusterId)}: {@event.NewDescription}";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_SdamInformationEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_SdamInformationEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            const string expectedLogMessage = "This was a triumph.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(new SdamInformationEvent(() => expectedLogMessage));
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerOpeningEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerOpeningEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerOpeningEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                new ServerSettings());
            var expectedLogMessage = $"{TraceSourceEventHelper.Label(@event.ServerId)}: opening.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerOpenedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerOpenedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerOpenedEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                new ServerSettings(),
                new TimeSpan(42));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId)}: opened in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerClosingEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerClosingEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event =
                new ServerClosingEvent(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)));
            var expectedLogMessage = $"{TraceSourceEventHelper.Label(@event.ServerId)}: closing.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerClosedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerClosedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerClosedEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                new TimeSpan(42));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId)}: closed in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerHeartbeatStartedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerHeartbeatStartedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatStartedEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))),
                awaited:  true);
            var expectedLogMessage = $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: sending heartbeat.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerHeartbeatSucceededEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerHeartbeatSucceededEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatSucceededEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))),
                new TimeSpan(42),
                awaited: true);
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: sent heartbeat in {@event.Duration.TotalMilliseconds}ms.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerHeartbeatFailedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerHeartbeatFailedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatFailedEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))),
                new Exception("The cake is a lie."),
                awaited: true);
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: error sending heartbeat.";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        public void Handle_with_ServerDescriptionChangedEvent_should_trace_event()
        {
            const string traceSourceName = "Handle_with_ServerDescriptionChangedEvent_should_trace_event";
            const string logFileName = traceSourceName + "-log";
            var ipAddress = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42);
            var @event = new ServerDescriptionChangedEvent(
                oldDescription: new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress),
                newDescription: new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress));
            var expectedLogMessage =
                $"{TraceSourceEventHelper.Label(@event.OldDescription.ServerId)}: {@event.NewDescription}";
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);

            subject.Handle(@event);
            var log = ReadLog(traceSource, logFileName);

            log.Should().Contain(expectedLogMessage);
        }

        [Fact]
        private void TryGetEventHandler_should_return_expected_handlers()
        {
            const string traceSourceName = "TryGetEventHandler_should_return_expected_handlers";
            const string logFileName = traceSourceName + "-log";

            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            Action<ClusterOpeningEvent> clusterOpeningEventHandler;
            Action<ClusterOpenedEvent> clusterOpenedEventHandler;
            Action<ClusterClosingEvent> clusterClosingEventHandler;
            Action<ClusterClosedEvent> clusterClosedEventHandler;
            Action<ClusterAddingServerEvent> clusterAddingServerEventHandler;
            Action<ClusterAddedServerEvent> clusterAddedServerEventHandler;
            Action<ClusterRemovingServerEvent> clusterRemovingServerEventHandler;
            Action<ClusterRemovedServerEvent> clusterRemovedServerEventHandler;
            Action<ClusterDescriptionChangedEvent> clusterDescriptionChangedEventHandler;
            Action<SdamInformationEvent> sdamInformationEventHandler;
            Action<ServerOpeningEvent> serverOpeningEventHandler;
            Action<ServerOpenedEvent> serverOpenedEventHandler;
            Action<ServerClosingEvent> serverClosingEventHandler;
            Action<ServerClosedEvent> serverClosedEventHandler;
            Action<ServerHeartbeatStartedEvent> serverHeartbeatStartedEventHandler;
            Action<ServerHeartbeatSucceededEvent> serverHeartbeatSucceededEventHandler;
            Action<ServerHeartbeatFailedEvent> serverHeartbeatFailedEventHandler;
            Action<ServerDescriptionChangedEvent> serverDescriptionChangedEventHandler;


            subject.TryGetEventHandler(out clusterOpeningEventHandler);
            subject.TryGetEventHandler(out clusterOpenedEventHandler);
            subject.TryGetEventHandler(out clusterClosingEventHandler);
            subject.TryGetEventHandler(out clusterClosedEventHandler);
            subject.TryGetEventHandler(out clusterAddingServerEventHandler);
            subject.TryGetEventHandler(out clusterAddedServerEventHandler);
            subject.TryGetEventHandler(out clusterRemovingServerEventHandler);
            subject.TryGetEventHandler(out clusterRemovedServerEventHandler);
            subject.TryGetEventHandler(out clusterDescriptionChangedEventHandler);
            subject.TryGetEventHandler(out sdamInformationEventHandler);
            subject.TryGetEventHandler(out serverOpeningEventHandler);
            subject.TryGetEventHandler(out serverOpenedEventHandler);
            subject.TryGetEventHandler(out serverClosingEventHandler);
            subject.TryGetEventHandler(out serverClosedEventHandler);
            subject.TryGetEventHandler(out serverHeartbeatStartedEventHandler);
            subject.TryGetEventHandler(out serverHeartbeatSucceededEventHandler);
            subject.TryGetEventHandler(out serverHeartbeatFailedEventHandler);
            subject.TryGetEventHandler(out serverDescriptionChangedEventHandler);

            clusterOpeningEventHandler.Should().NotBeNull();
            clusterOpenedEventHandler.Should().NotBeNull();
            clusterClosingEventHandler.Should().NotBeNull();
            clusterClosedEventHandler.Should().NotBeNull();
            clusterAddingServerEventHandler.Should().NotBeNull();
            clusterAddedServerEventHandler.Should().NotBeNull();
            clusterRemovingServerEventHandler.Should().NotBeNull();
            clusterRemovedServerEventHandler.Should().NotBeNull();
            clusterDescriptionChangedEventHandler.Should().NotBeNull();
            sdamInformationEventHandler.Should().NotBeNull();
            serverOpeningEventHandler.Should().NotBeNull();
            serverOpenedEventHandler.Should().NotBeNull();
            serverClosingEventHandler.Should().NotBeNull();
            serverClosedEventHandler.Should().NotBeNull();
            serverHeartbeatStartedEventHandler.Should().NotBeNull();
            serverHeartbeatSucceededEventHandler.Should().NotBeNull();
            serverHeartbeatFailedEventHandler.Should().NotBeNull();
            serverDescriptionChangedEventHandler.Should().NotBeNull();
        }

        private TraceSource CreateTraceSource(string name, string logFileName)
        {
            File.Delete(logFileName);
            var traceSource = new TraceSource(name, SourceLevels.All);
            traceSource.Listeners.Clear();
            var logFileStream = new FileStream(logFileName, FileMode.Append);
            traceSource.Listeners.Add(
                new TextWriterTraceListener(logFileStream) { TraceOutputOptions = TraceOptions.DateTime });
            return traceSource;
        }

        private string ReadLog(TraceSource traceSource, string logFileName)
        {
            traceSource.Close();
            return File.ReadAllText(logFileName);
        }
    }

    internal static class TraceSourceSdamEventSubscriberReflector
    {
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterOpeningEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterOpenedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterClosingEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterClosedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterAddingServerEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterAddedServerEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterRemovingServerEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterRemovedServerEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ClusterDescriptionChangedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, SdamInformationEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerOpeningEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerOpenedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerClosingEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerClosedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerHeartbeatStartedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerHeartbeatSucceededEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerHeartbeatFailedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
        public static void Handle(this TraceSourceSdamEventSubscriber subject, ServerDescriptionChangedEvent @event)
            => Reflector.Invoke(subject, "Handle", @event);
    }
}
