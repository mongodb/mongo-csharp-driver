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
        public void HandleClusterOpeningEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterOpeningEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterOpeningEvent(new ClusterId(), new ClusterSettings());
            var logMessage = $"{TraceSourceEventHelper.Label(@event.ClusterId)}: opening.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterOpenedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterOpenedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterOpenedEvent(new ClusterId(), new ClusterSettings(), new TimeSpan(1));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: opened in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterClosingEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterClosing_EventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterClosingEvent(new ClusterId());
            var logMessage = $"{TraceSourceEventHelper.Label(@event.ClusterId)}: closing.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterClosedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterClosedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterClosedEvent(new ClusterId(), new TimeSpan(1));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: closed in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterAddingServerEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterAddingServerEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterAddingServerEvent(
                new ClusterId(), 
                new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ClusterId)}: adding server at endpoint "+ 
                $"{TraceSourceEventHelper.Format(@event.EndPoint)}.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterAddedServerEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterAddedServerEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterAddedServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)), 
                new TimeSpan(1));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: added server " + 
                $"{TraceSourceEventHelper.Format(@event.ServerId)} in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterRemovingServerEventShould_Log_To_File()
        {
            const string traceSourceName = "HandleClusterRemovingServerEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterRemovingServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)), 
                "The cake is a lie.");
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: removing server " + 
                $"{TraceSourceEventHelper.Format(@event.ServerId)}. Reason: {@event.Reason}";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterRemovedServerEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleClusterRemovedServerEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ClusterRemovedServerEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)), 
                "The cake is a lie.",
                new TimeSpan(42));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId.ClusterId)}: removed server " + 
                $"{TraceSourceEventHelper.Format(@event.ServerId)} in {@event.Duration.TotalMilliseconds}ms. " +
                $"Reason: {@event.Reason}";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleClusterDescriptionChangedEventShould_Log_To_File()
        {
            const string traceSourceName = "HandleClusterDescriptionChangedEventShould_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var ipAddress = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42);

            var @event = new ClusterDescriptionChangedEvent(
                oldDescription: new ClusterDescription(
                    new ClusterId(),
                    ClusterConnectionMode.Automatic,
                    ClusterType.Unknown,
                    new ServerDescription[] { }),
                newDescription: new ClusterDescription(
                    new ClusterId(),
                    ClusterConnectionMode.Direct,
                    ClusterType.Standalone,
                    new ServerDescription[] { new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress)}));

            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.OldDescription.ClusterId)}: {@event.NewDescription}";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
          
        [Fact]
        public void HandleSdamInformationEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleSdamInformationEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            const string logMessage = "This was a triumph.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", new SdamInformationEvent(() => logMessage));
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleServerOpeningEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerOpeningEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerOpeningEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)), 
                new ServerSettings());
            var logMessage = $"{TraceSourceEventHelper.Label(@event.ServerId)}: opening.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        public void HandleServerOpenedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerOpenedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerOpenedEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)), 
                new ServerSettings(),
                new TimeSpan(42));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId)}: opened in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        public void HandleServerClosingEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerClosingEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = 
                new ServerClosingEvent(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)));
            var logMessage = $"{TraceSourceEventHelper.Label(@event.ServerId)}: closing.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        public void HandleServerClosedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerClosedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerClosedEvent(
                new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42)),
                new TimeSpan(42));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ServerId)}: closed in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        public void HandleServerHeartbeatStartedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerHeartbeatStartedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatStartedEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))));
            var logMessage = $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: sending heartbeat.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleServerHeartbeatSucceededEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerHeartbeatSucceededEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatSucceededEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))), 
                new TimeSpan(42));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: sent heartbeat in {@event.Duration.TotalMilliseconds}ms.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }
        
        [Fact]
        public void HandleServerHeartbeatFailedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerHeartbeatFailedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var @event = new ServerHeartbeatFailedEvent(
                new ConnectionId(new ServerId(new ClusterId(), new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42))),
                new Exception("The cake is a lie."));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.ConnectionId)}: error sending heartbeat.";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        public void HandleServerDescriptionChangedEvent_Should_Log_To_File()
        {
            const string traceSourceName = "HandleServerDescriptionChangedEvent_Should_Log_To_File";
            const string logFileName = traceSourceName + "-log";
            var ipAddress = new IPEndPoint(IPAddress.Parse("1.2.3.4"), 42);
            var @event = new ServerDescriptionChangedEvent(
                oldDescription: new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress),
                newDescription: new ServerDescription(new ServerId(new ClusterId(), ipAddress), ipAddress));
            var logMessage =
                $"{TraceSourceEventHelper.Label(@event.OldDescription.ServerId)}: {@event.NewDescription}";
            File.Delete(logFileName);
            var traceSource = CreateTraceSource(logFileName, logFileName);
            var subject = new TraceSourceSdamEventSubscriber(traceSource);
            
            Reflector.Invoke(subject, "Handle", @event);
            traceSource.Close();
            var log = File.ReadAllText(logFileName);

            log.Should().Contain(logMessage);
        }

        [Fact]
        private void TryGetEventHandler_Should_Return_ExpectedHandlers()
        {
            const string traceSourceName = "TryGetEventHandler_Should_Return_ExpectedHandlers";
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
            var traceSource = new TraceSource(name, SourceLevels.All);
            traceSource.Listeners.Clear();
            var logFileStream = new FileStream(logFileName, FileMode.Append);
            traceSource.Listeners.Add(
                new TextWriterTraceListener(logFileStream) { TraceOutputOptions = TraceOptions.DateTime });
            return traceSource;
        }

    }
}
